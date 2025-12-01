using RemoteX.Core.Models;

namespace RemoteX.Core.Interfaces;

public interface ISshClient : IDisposable
{
    bool IsConnected { get; }

    Task ConnectAsync(ConnectionConfig config, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    Task<Stream?> CreateShellStreamAsync(ConnectionConfig config);
    Task SendDataAsync(string data);
    Task ResizeTerminalAsync(int columns, int rows);

    event EventHandler<string>? DataReceived;
    event EventHandler<string>? ErrorReceived;
    event EventHandler? Disconnected;
}