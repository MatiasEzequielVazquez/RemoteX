using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RemoteX.Core.Interfaces;
using RemoteX.Core.Models;

namespace RemoteX.Core.Services;

/// <summary>
/// Gestiona múltiples sesiones SSH concurrentes de manera thread-safe
/// </summary>
public class SshSessionManager : ISshSessionManager
{
    private readonly ConcurrentDictionary<string, (SshSession Session, ISshClient Client)> _sessions = new();
    private readonly ILogger<SshSessionManager> _logger;
    private readonly Func<ISshClient> _sshClientFactory;

    public SshSessionManager(
        ILogger<SshSessionManager> logger,
        Func<ISshClient> sshClientFactory)
    {
        _logger = logger;
        _sshClientFactory = sshClientFactory;
    }

    public async Task<SshResult<SshSession>> CreateSessionAsync(
        string connectionId,
        ConnectionConfig config,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating SSH session for connection {ConnectionId} to {Host}:{Port}",
                connectionId, config.Host, config.Port);

            var session = new SshSession
            {
                ConnectionId = connectionId,
                Config = config,
                Status = SessionStatus.Connecting
            };

            var sshClient = _sshClientFactory();

            sshClient.DataReceived += (sender, data) =>
            {
                session.LastActivity = DateTime.UtcNow;
            };

            sshClient.ErrorReceived += (sender, error) =>
            {
                _logger.LogError("SSH Error on session {SessionId}: {Error}", session.SessionId, error);
            };

            sshClient.Disconnected += (sender, args) =>
            {
                _logger.LogInformation("SSH Disconnected for session {SessionId}", session.SessionId);
                session.Status = SessionStatus.Disconnected;
                session.IsConnected = false;
            };

            await sshClient.ConnectAsync(config, cancellationToken);

            session.Status = SessionStatus.Connected;
            session.IsConnected = true;
            session.ConnectedAt = DateTime.UtcNow;
            session.LastActivity = DateTime.UtcNow;

            _sessions[connectionId] = (session, sshClient);

            _logger.LogInformation("SSH session {SessionId} connected successfully", session.SessionId);

            return SshResult<SshSession>.SuccessResult(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create SSH session for {Host}:{Port}",
                config.Host, config.Port);

            return SshResult<SshSession>.FailureResult($"Connection failed: {ex.Message}");
        }
    }

    public SshSession? GetSession(string connectionId)
    {
        return _sessions.TryGetValue(connectionId, out var tuple) ? tuple.Session : null;
    }

    public ISshClient? GetClient(string connectionId)
    {
        return _sessions.TryGetValue(connectionId, out var tuple) ? tuple.Client : null;
    }

    public async Task<SshResult<bool>> SendInputAsync(
        string connectionId,
        string input,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_sessions.TryGetValue(connectionId, out var tuple))
            {
                return SshResult<bool>.FailureResult("Session not found");
            }

            var (session, client) = tuple;

            if (!client.IsConnected)
            {
                return SshResult<bool>.FailureResult("Session is not connected");
            }

            await client.SendDataAsync(input);
            session.LastActivity = DateTime.UtcNow;

            return SshResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send input to session {ConnectionId}", connectionId);
            return SshResult<bool>.FailureResult(ex.Message);
        }
    }

    public async Task<SshResult<bool>> ResizeTerminalAsync(
        string connectionId,
        int columns,
        int rows,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_sessions.TryGetValue(connectionId, out var tuple))
            {
                return SshResult<bool>.FailureResult("Session not found");
            }

            var (session, client) = tuple;
            await client.ResizeTerminalAsync(columns, rows);
            session.LastActivity = DateTime.UtcNow;

            return SshResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resize terminal for {ConnectionId}", connectionId);
            return SshResult<bool>.FailureResult(ex.Message);
        }
    }

    public async Task DisconnectSessionAsync(string connectionId)
    {
        try
        {
            if (_sessions.TryRemove(connectionId, out var tuple))
            {
                var (session, client) = tuple;

                _logger.LogInformation("Disconnecting session {SessionId}", session.SessionId);

                session.Status = SessionStatus.Disconnecting;
                await client.DisconnectAsync();
                client.Dispose();

                session.Status = SessionStatus.Disconnected;
                session.IsConnected = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting session {ConnectionId}", connectionId);
        }
    }

    public IEnumerable<SshSession> GetActiveSessions()
    {
        return _sessions.Values.Select(t => t.Session);
    }

    public async Task CleanupInactiveSessionsAsync(TimeSpan inactivityThreshold)
    {
        var now = DateTime.UtcNow;
        var inactiveSessions = _sessions
            .Where(kvp => now - kvp.Value.Session.LastActivity > inactivityThreshold)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var connectionId in inactiveSessions)
        {
            _logger.LogInformation("Cleaning up inactive session {ConnectionId}", connectionId);
            await DisconnectSessionAsync(connectionId);
        }
    }
}