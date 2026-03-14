using DotOcpi.Modules;
using DotOcpi.Registry;
using DotOcpi.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotOcpi.AspNetCore;

/// <summary>
/// Validates required DotOcpi services are registered at startup rather than
/// failing on the first request. Runs once and stops.
/// </summary>
internal sealed partial class DotOcpiStartupValidator : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DotOcpiStartupValidator> _logger;

    public DotOcpiStartupValidator(IServiceProvider services, ILogger<DotOcpiStartupValidator> logger)
    {
        _services = services;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var missing = new List<string>();

        CheckRequired<ICpoRegistry>(missing);
        CheckRequired<ITokenStore>(missing);

        // Consumer interfaces — warn if not registered, as endpoints won't work
        CheckOptional<ILocationsReceiver>(missing);
        CheckOptional<ISessionsReceiver>(missing);
        CheckOptional<ICdrsReceiver>(missing);
        CheckOptional<ITariffsReceiver>(missing);

        if (missing.Count > 0)
        {
            var message = string.Join(", ", missing);
            LogMissingServices(message);
            throw new InvalidOperationException(
                $"DotOcpi startup validation failed. Missing required services: {message}. "
                    + "Register them via AddInMemoryTokenStore(), AddInMemoryCpoRegistry(), "
                    + "or your own implementations."
            );
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void CheckRequired<T>(List<string> missing)
    {
        if (_services.GetService<T>() is null)
        {
            missing.Add(typeof(T).Name);
        }
    }

    private void CheckOptional<T>(List<string> missing)
    {
        if (_services.GetService<T>() is null)
        {
            LogMissingOptionalService(typeof(T).Name);
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
}
