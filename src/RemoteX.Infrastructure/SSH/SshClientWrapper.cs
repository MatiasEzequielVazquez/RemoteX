using System.Text;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using RemoteX.Core.Models;
using SshNetClient = Renci.SshNet.SshClient;
using SshNetShellStream = Renci.SshNet.ShellStream;
using CoreInterfaces = RemoteX.Core.Interfaces;

namespace RemoteX.Infrastructure.SSH;

/// <summary>
/// Wrapper sobre SSH.NET para abstraer la implementación
/// </summary>
public class SshClientWrapper : CoreInterfaces.ISshClient
{
    private readonly ILogger<SshClientWrapper> _logger;
    private SshNetClient? _sshClient;
    private SshNetShellStream? _shellStream;
    private CancellationTokenSource? _readCts;
    private Task? _readTask;

    public event EventHandler<string>? DataReceived;
    public event EventHandler<string>? ErrorReceived;
    public event EventHandler? Disconnected;

    public bool IsConnected => _sshClient?.IsConnected ?? false;

    public SshClientWrapper(ILogger<SshClientWrapper> logger)
    {
        _logger = logger;
    }

    public async Task ConnectAsync(ConnectionConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            ConnectionInfo connectionInfo;

            if (!string.IsNullOrEmpty(config.PrivateKey))
            {
                var keyFile = new PrivateKeyFile(new MemoryStream(Encoding.UTF8.GetBytes(config.PrivateKey)));
                var keyFiles = new[] { keyFile };

                connectionInfo = new ConnectionInfo(
                    config.Host,
                    config.Port,
                    config.Username,
                    new PrivateKeyAuthenticationMethod(config.Username, keyFiles));
            }
            else
            {
                connectionInfo = new ConnectionInfo(
                    config.Host,
                    config.Port,
                    config.Username,
                    new PasswordAuthenticationMethod(config.Username, config.Password ?? string.Empty));
            }

            connectionInfo.Timeout = TimeSpan.FromMilliseconds(config.Timeout);

            _sshClient = new SshNetClient(connectionInfo);

            await Task.Run(() => _sshClient.Connect(), cancellationToken);

            if (!_sshClient.IsConnected)
            {
                throw new Exception("Failed to establish SSH connection");
            }

            _logger.LogInformation("SSH connected to {Host}:{Port}", config.Host, config.Port);

            var stream = await CreateShellStreamAsync(config);

            if (stream == null)
            {
                throw new Exception("Failed to create shell stream");
            }

            _shellStream = (SshNetShellStream)stream;

            StartReadingOutput();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSH connection failed");
            throw;
        }
    }

    public async Task<Stream?> CreateShellStreamAsync(ConnectionConfig config)
    {
        if (_sshClient == null || !_sshClient.IsConnected)
        {
            throw new InvalidOperationException("SSH client is not connected");
        }

        try
        {
            var stream = _sshClient.CreateShellStream(
                terminalName: config.TerminalType,
                columns: (uint)config.Columns,
                rows: (uint)config.Rows,
                width: (uint)(config.Columns * 8),
                height: (uint)(config.Rows * 16),
                bufferSize: 4096);

            _logger.LogInformation("Shell stream created");
            return await Task.FromResult<Stream>(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create shell stream");
            throw;
        }
    }

    public async Task SendDataAsync(string data)
    {
        if (_shellStream == null || !_shellStream.CanWrite)
        {
            throw new InvalidOperationException("Shell stream is not available");
        }

        try
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            await _shellStream.WriteAsync(bytes, 0, bytes.Length);
            await _shellStream.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send data");
            throw;
        }
    }

    public Task ResizeTerminalAsync(int columns, int rows)
    {
        if (_sshClient == null || !_sshClient.IsConnected)
        {
            throw new InvalidOperationException("SSH client is not connected");
        }

        try
        {
            // SSH.NET no soporta resize dinámico del shell stream
            // Solo se puede configurar al crear el stream
            // Para implementar resize real, necesitarías recrear el stream
            _logger.LogDebug("Terminal resize requested to {Columns}x{Rows} (not supported by current shell stream)", columns, rows);

            // TODO: Para soporte completo de resize, implementar:
            // 1. Cerrar stream actual
            // 2. Crear nuevo stream con nuevas dimensiones
            // 3. Reconectar lectura de output

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resize terminal");
            throw;
        }
    }

    private void StartReadingOutput()
    {
        if (_shellStream == null)
        {
            return;
        }

        _readCts = new CancellationTokenSource();
        var cancellationToken = _readCts.Token;

        _readTask = Task.Run(async () =>
        {
            var buffer = new byte[4096];

            try
            {
                while (!cancellationToken.IsCancellationRequested && _shellStream.CanRead)
                {
                    var bytesRead = await _shellStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead > 0)
                    {
                        var output = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        DataReceived?.Invoke(this, output);
                    }

                    await Task.Delay(10, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Shell stream reading cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from shell stream");
                ErrorReceived?.Invoke(this, ex.Message);
            }
            finally
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }, cancellationToken);
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _readCts?.Cancel();

            if (_readTask != null)
            {
                await _readTask;
            }

            _shellStream?.Dispose();
            _shellStream = null;

            if (_sshClient?.IsConnected == true)
            {
                _sshClient.Disconnect();
            }

            _logger.LogInformation("SSH disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnect");
        }
    }

    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
        _sshClient?.Dispose();
        _readCts?.Dispose();
    }
}