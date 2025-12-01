using Microsoft.AspNetCore.SignalR;
using RemoteX.Core.Interfaces;
using RemoteX.Core.Models;

namespace RemoteX.API.Hubs;

/// <summary>
/// SignalR Hub para comunicación en tiempo real con clientes SSH
/// </summary>
public class SshHub : Hub
{
    private readonly ISshSessionManager _sessionManager;
    private readonly ILogger<SshHub> _logger;

    public SshHub(
        ISshSessionManager sessionManager,
        ILogger<SshHub> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    /// <summary>
    /// Cliente solicita conectarse a un servidor SSH
    /// </summary>
    public async Task ConnectToServer(ConnectionConfig config)
    {
        var connectionId = Context.ConnectionId;

        try
        {
            _logger.LogInformation("Client {ConnectionId} requesting SSH connection to {Host}:{Port}",
                connectionId, config.Host, config.Port);

            var result = await _sessionManager.CreateSessionAsync(connectionId, config);

            if (result.Success && result.Data != null)
            {
                // Configurar eventos para reenviar output al cliente
                SetupSessionEvents(connectionId);

                await Clients.Caller.SendAsync("Connected", new
                {
                    SessionId = result.Data.SessionId,
                    Message = $"Connected to {config.Host}:{config.Port}",
                    ConnectedAt = result.Data.ConnectedAt
                });

                _logger.LogInformation("SSH session {SessionId} established for client {ConnectionId}",
                    result.Data.SessionId, connectionId);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", new
                {
                    Message = result.ErrorMessage ?? "Unknown error",
                    ErrorType = "ConnectionFailed"
                });

                _logger.LogWarning("Failed to establish SSH connection for client {ConnectionId}: {Error}",
                    connectionId, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while connecting to SSH for client {ConnectionId}", connectionId);

            await Clients.Caller.SendAsync("Error", new
            {
                Message = $"Connection error: {ex.Message}",
                ErrorType = "Exception"
            });
        }
    }

    public async Task SendInput(string input)
    {
        var connectionId = Context.ConnectionId;

        try
        {
            var result = await _sessionManager.SendInputAsync(connectionId, input);

            if (!result.Success)
            {
                await Clients.Caller.SendAsync("Error", new
                {
                    Message = result.ErrorMessage,
                    ErrorType = "SendInputFailed"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending input for client {ConnectionId}", connectionId);
        }
    }

    public async Task ResizeTerminal(int columns, int rows)
    {
        var connectionId = Context.ConnectionId;

        try
        {
            var result = await _sessionManager.ResizeTerminalAsync(connectionId, columns, rows);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to resize terminal for {ConnectionId}: {Error}",
                    connectionId, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while resizing terminal for {ConnectionId}", connectionId);
        }
    }

    public async Task Disconnect()
    {
        var connectionId = Context.ConnectionId;

        try
        {
            _logger.LogInformation("Client {ConnectionId} requesting disconnect", connectionId);
            await _sessionManager.DisconnectSessionAsync(connectionId);

            await Clients.Caller.SendAsync("Disconnected", new
            {
                Message = "Disconnected from SSH server"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while disconnecting client {ConnectionId}", connectionId);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;

        _logger.LogInformation("Client {ConnectionId} disconnected", connectionId);

        await _sessionManager.DisconnectSessionAsync(connectionId);

        await base.OnDisconnectedAsync(exception);
    }

    private void SetupSessionEvents(string connectionId)
    {
        var client = _sessionManager.GetClient(connectionId);
        if (client == null) return;

        client.DataReceived += async (sender, data) =>
        {
            await Clients.Client(connectionId).SendAsync("Output", data);
        };

        client.ErrorReceived += async (sender, error) =>
        {
            await Clients.Client(connectionId).SendAsync("Error", new
            {
                Message = error,
                ErrorType = "SSHError"
            });
        };
    }
}