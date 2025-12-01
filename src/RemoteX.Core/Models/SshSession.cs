namespace RemoteX.Core.Models;

public class SshSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public required string ConnectionId { get; set; }
    public required ConnectionConfig Config { get; set; }
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public bool IsConnected { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Connecting;
}

public enum SessionStatus
{
    Connecting,
    Connected,
    Disconnecting,
    Disconnected,
    Error
}
