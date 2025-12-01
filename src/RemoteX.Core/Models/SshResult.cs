namespace RemoteX.Core.Models;

public class SshResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }

    public static SshResult<T> SuccessResult(T data) => new()
    {
        Success = true,
        Data = data
    };

    public static SshResult<T> FailureResult(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}