namespace RemoteX.Core.Models;

/// <summary>
/// Representa la configuración de conexión SSH
/// </summary>
public class ConnectionConfig
{
    public required string Host { get; set; }
    public int Port { get; set; } = 22;
    public required string Username { get; set; }
    public string? Password { get; set; }
    public string? PrivateKey { get; set; }
    public int Timeout { get; set; } = 20000;
    public string TerminalType { get; set; } = "xterm-256color";
    public int Columns { get; set; } = 80;
    public int Rows { get; set; } = 24;
}