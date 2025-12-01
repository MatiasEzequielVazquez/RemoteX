using Org.BouncyCastle.Tsp;
using RemoteX.API;
using RemoteX.API.Hubs;
using RemoteX.Core.Interfaces;
using RemoteX.Core.Services;
using RemoteX.Infrastructure.SSH;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. CONFIGURAR SERILOG (Logging)
// ============================================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/remotex-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================================
// 2. CONFIGURAR SERVICIOS
// ============================================================

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:8080",
                "http://localhost:5173",
                "http://127.0.0.1:8080")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dependency Injection - NUESTROS SERVICIOS
builder.Services.AddSingleton<ISshSessionManager, SshSessionManager>();

builder.Services.AddTransient<ISshClient>(provider =>
{
    var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger<SshClientWrapper>();
    return new SshClientWrapper(logger);
});

builder.Services.AddSingleton<Func<ISshClient>>(provider =>
    () => provider.GetRequiredService<ISshClient>());

builder.Services.AddHostedService<SessionCleanupService>();

// ============================================================
// 3. BUILD & CONFIGURE
// ============================================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseCors();
app.UseRouting();

app.MapHub<SshHub>("/ssh");

app.MapGet("/api/info", () => new
{
    Name = "RemoteX Server",
    Version = "1.0.0",
    Status = "Running",
    Timestamp = DateTime.UtcNow
});

app.MapGet("/api/sessions", (ISshSessionManager sessionManager) =>
{
    var sessions = sessionManager.GetActiveSessions()
        .Select(s => new
        {
            s.SessionId,
            s.ConnectionId,
            Host = s.Config.Host,
            s.ConnectedAt,
            s.LastActivity,
            s.IsConnected,
            s.Status
        });

    return Results.Ok(sessions);
});

// ============================================================
// 4. STARTUP
// ============================================================
Log.Information(@"
╔══════════════════════════════════════════════╗
║          REMOTEX SERVER                      ║
║   ========================================   ║
║   SignalR Hub: http://localhost:5000/ssh     ║
║   Swagger:     http://localhost:5000/swagger ║
║   Frontend:    http://localhost:5000         ║
╚══════════════════════════════════════════════╝
");

app.UseDefaultFiles();
app.MapFallbackToFile("index.html");


try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}