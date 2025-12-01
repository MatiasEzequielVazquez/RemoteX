using RemoteX.Core.Interfaces;

namespace RemoteX.API;

public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _inactivityThreshold = TimeSpan.FromMinutes(30);

    public SessionCleanupService(
        IServiceProvider serviceProvider,
        ILogger<SessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session cleanup service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var sessionManager = scope.ServiceProvider.GetRequiredService<ISshSessionManager>();

                _logger.LogDebug("Running session cleanup");
                await sessionManager.CleanupInactiveSessionsAsync(_inactivityThreshold);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup");
            }
        }

        _logger.LogInformation("Session cleanup service stopped");
    }
}