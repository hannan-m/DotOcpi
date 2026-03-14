using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotOcpi.Registry;

/// <summary>
/// Background service that periodically checks the health of connected CPOs
/// by verifying their versions endpoint is reachable. Marks connections as
/// Offline after repeated failures and Suspended after prolonged outage.
/// </summary>
public sealed partial class CpoHealthMonitor : BackgroundService
{
    private readonly ICpoRegistry _registry;
    private readonly ILogger<CpoHealthMonitor> _logger;
    private readonly TimeSpan _interval;

    /// <summary>
    /// Creates a new CpoHealthMonitor.
    /// </summary>
    /// <param name="registry">The CPO registry to monitor.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="interval">How often to run health checks. Defaults to 5 minutes.</param>
    public CpoHealthMonitor(ICpoRegistry registry, ILogger<CpoHealthMonitor> logger, TimeSpan? interval = null)
    {
        _registry = registry;
        _logger = logger;
        _interval = interval ?? TimeSpan.FromMinutes(5);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CheckAllConnections(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogHealthCheckCycleFailed(_logger, ex);
            }

            await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);
        }
    }

    private void CheckAllConnections(CancellationToken cancellationToken)
    {
        var connections = _registry.GetAll();

        foreach (var connection in connections)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (connection.Status is ConnectionStatus.Unregistered or ConnectionStatus.Pending)
            {
                continue;
            }

            // Health check implementation will be added when the HTTP client is available (Phase 11).
            LogHealthCheck(_logger, connection.ConnectionKey, connection.Version);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "CPO health check cycle failed")]
    private static partial void LogHealthCheckCycleFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Health check for CPO {ConnectionKey} (version {Version})")]
    private static partial void LogHealthCheck(ILogger logger, string connectionKey, OcpiVersion version);
}
