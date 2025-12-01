using RemoteX.Core.Models;

namespace RemoteX.Core.Interfaces;

public interface ISshSessionManager
{
    Task<SshResult<SshSession>> CreateSessionAsync(
        string connectionId,
        ConnectionConfig config,
        CancellationToken cancellationToken = default);

    SshSession? GetSession(string connectionId);

    ISshClient? GetClient(string connectionId);

    Task<SshResult<bool>> SendInputAsync(
        string connectionId,
        string input,
        CancellationToken cancellationToken = default);

    Task<SshResult<bool>> ResizeTerminalAsync(
        string connectionId,
        int columns,
        int rows,
        CancellationToken cancellationToken = default);

    Task DisconnectSessionAsync(string connectionId);

    IEnumerable<SshSession> GetActiveSessions();

    Task CleanupInactiveSessionsAsync(TimeSpan inactivityThreshold);
}