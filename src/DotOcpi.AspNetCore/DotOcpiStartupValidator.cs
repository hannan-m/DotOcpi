using DotOcpi.Modules;
using DotOcpi.Observability;
using DotOcpi.Registry;
using DotOcpi.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotOcpi.AspNetCore;

/// <summary>
/// Validates required DotOcpi services are registered at startup rather than
/// failing on the first request. Runs once and stops.
/// </summary>
internal sealed partial class DotOcpiStartupValidator : IHostedLifecycleService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DotOcpiStartupValidator> _logger;

    public DotOcpiStartupValidator(IServiceProvider services, ILogger<DotOcpiStartupValidator> logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// Runs before the application starts accepting requests, ensuring all
    /// required services are registered.
    /// </summary>
    public Task StartingAsync(CancellationToken cancellationToken)
    {
        var missing = new List<string>();

        // Core infrastructure — library cannot function without these
        CheckRequired<ICpoRegistry>(missing);
        CheckRequired<ITokenStore>(missing);
        CheckRequired<OcpiTokenValidator>(missing);
        CheckRequired<OcpiMetrics>(missing);

        // Consumer interfaces — warn if not registered, as endpoints won't work
        CheckOptional<ILocationsReceiver>();
        CheckOptional<ISessionsReceiver>();
        CheckOptional<ICdrsReceiver>();
        CheckOptional<ITariffsReceiver>();

        // If the client package is used (IOutboundTokenProvider is a consumer-provided
        // interface required by the OCPI client), warn early rather than failing
        // on first outbound request. Uses Type.GetType to avoid a hard assembly reference.
        var outboundTokenType = Type.GetType("DotOcpi.Client.IOutboundTokenProvider, DotOcpi.Client");
        if (outboundTokenType is not null)
        {
            // Client assembly is loaded — check if the consumer registered the interface
            if (_services.GetService(outboundTokenType) is null)
            {
                LogMissingOptionalService("IOutboundTokenProvider");
            }
        }

        if (missing.Count > 0)
        {
            var message = string.Join(", ", missing);
            LogMissingServices(message);
            throw new InvalidOperationException(
                $"DotOcpi startup validation failed. Missing required services: {message}. "
                    + "Register them via AddInMemoryTokenStore(), AddInMemoryCpoRegistry(), "
                    + "AddAspNetCoreServer(), or your own implementations."
            );
        }

        // Security rule 16: Token A must never appear in file-based configuration.
        // Scan known key patterns to catch accidental leakage.
        CheckTokenAInConfiguration();

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void CheckRequired<T>(List<string> missing)
    {
        if (_services.GetService<T>() is null)
        {
            missing.Add(typeof(T).Name);
        }
    }

    private void CheckOptional<T>()
    {
        if (_services.GetService<T>() is null)
        {
            LogMissingOptionalService(typeof(T).Name);
        }
    }

    /// <summary>
    /// Scans IConfiguration for keys that look like Token A values stored in
    /// file-based config (appsettings.json, environment variables, etc.).
    /// Token A is a pre-shared secret that must only come from secure stores
    /// (user-secrets, Key Vault) — never from committed configuration files.
    /// </summary>
    private void CheckTokenAInConfiguration()
    {
        var config = _services.GetService<IConfiguration>();
        if (config is null)
            return;

        // Check well-known configuration key patterns where Token A might leak
        string[] suspectKeys =
        [
            "DotOcpi:TokenA",
            "DotOcpi:Token_A",
            "DotOcpi:Registration:TokenA",
            "Ocpi:TokenA",
            "OcpiTokenA",
        ];

        foreach (var key in suspectKeys)
        {
            var value = config[key];
            if (!string.IsNullOrEmpty(value))
            {
                LogTokenADetected(key);
                throw new InvalidOperationException(
                    $"DotOcpi security violation: Token A detected in configuration key '{key}'. "
                        + "Token A must NEVER be stored in appsettings.json or source-controlled configuration. "
                        + "Use dotnet user-secrets for development or a secrets provider (Azure Key Vault, "
                        + "HashiCorp Vault, environment variables from a secure source) for production."
                );
            }
        }
    }

    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "DotOcpi startup validation failed. Missing services: {MissingServices}"
    )]
    private partial void LogMissingServices(string missingServices);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Consumer interface {ServiceName} is not registered. OCPI endpoints requiring it will fail at runtime."
    )]
    private partial void LogMissingOptionalService(string serviceName);

    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "SECURITY: Token A value detected in configuration key '{ConfigKey}'. Token A must not be stored in file-based configuration."
    )]
    private partial void LogTokenADetected(string configKey);
}
