using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotOcpi.Registry;

/// <summary>
/// Background service that periodically checks the health of connected CPOs
/// by verifying their versions endpoint is reachable. Marks connections as
/// Offline after repeated failures.
/// </summary>
public sealed partial class CpoHealthMonitor : BackgroundService
{
    private readonly ICpoRegistry _registry;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CpoHealthMonitor> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _interval;
    private readonly int _maxFailures;
    private readonly TimeSpan _timeout;
    private readonly ConcurrentDictionary<string, int> _failureCounts = new();

    /// <summary>
    /// Creates a new CpoHealthMonitor.
    /// </summary>
    public CpoHealthMonitor(
        ICpoRegistry registry,
        HttpClient httpClient,
        ILogger<CpoHealthMonitor> logger,
        TimeProvider? timeProvider = null,
        TimeSpan? interval = null,
        int maxConsecutiveFailures = 3,
        TimeSpan? healthCheckTimeout = null
    )
    {
        _registry = registry;
        _httpClient = httpClient;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _interval = interval ?? TimeSpan.FromMinutes(5);
        _maxFailures = maxConsecutiveFailures;
        _timeout = healthCheckTimeout ?? TimeSpan.FromSeconds(10);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await CheckAllConnectionsAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogHealthCheckCycleFailed(_logger, ex);
            }
        }
    }

    private async Task CheckAllConnectionsAsync(CancellationToken cancellationToken)
    {
        var connections = _registry.GetAll();

        foreach (var connection in connections)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (connection.Status is ConnectionStatus.Unregistered or ConnectionStatus.Pending)
                continue;

            var healthy = await CheckCpoHealthAsync(connection, cancellationToken).ConfigureAwait(false);

            if (healthy)
            {
                _failureCounts.TryRemove(connection.ConnectionKey, out _);

                if (connection.Status == ConnectionStatus.Offline)
                {
                    var restored = connection with
                    {
                        Status = ConnectionStatus.Connected,
                        LastHealthCheckAt = _timeProvider.GetUtcNow(),
                        UpdatedAt = _timeProvider.GetUtcNow(),
                    };
                    _registry.AddOrUpdate(restored);
                    LogHealthRestored(_logger, connection.ConnectionKey);
                }
                else
                {
                    var updated = connection with { LastHealthCheckAt = _timeProvider.GetUtcNow() };
                    _registry.AddOrUpdate(updated);
                }

                LogHealthCheck(_logger, connection.ConnectionKey, connection.Version);
            }
            else
            {
                var failures = _failureCounts.AddOrUpdate(connection.ConnectionKey, 1, (_, c) => c + 1);
                LogHealthCheckFailed(_logger, connection.ConnectionKey, failures, _maxFailures);

                if (failures >= _maxFailures && connection.Status == ConnectionStatus.Connected)
                {
                    var offline = connection with
                    {
                        Status = ConnectionStatus.Offline,
                        UpdatedAt = _timeProvider.GetUtcNow(),
                    };
                    _registry.AddOrUpdate(offline);
                    LogCpoMarkedOffline(_logger, connection.ConnectionKey, failures);
                }
            }
        }
    }

    private async Task<bool> CheckCpoHealthAsync(CpoConnection connection, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(connection.CpoVersionsUrl))
            return true; // No URL to check — assume healthy

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_timeout);

            using var response = await _httpClient.GetAsync(connection.CpoVersionsUrl, cts.Token).ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false; // Timeout
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "CPO health check cycle failed")]
    private static partial void LogHealthCheckCycleFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Health check passed for {ConnectionKey} ({Version})")]
    private static partial void LogHealthCheck(ILogger logger, string connectionKey, OcpiVersion version);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Health check failed for {ConnectionKey} ({Failures}/{MaxFailures})"
    )]
    private static partial void LogHealthCheckFailed(
        ILogger logger,
        string connectionKey,
        int failures,
        int maxFailures
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "CPO {ConnectionKey} marked Offline after {Failures} consecutive failures"
    )]
    private static partial void LogCpoMarkedOffline(ILogger logger, string connectionKey, int failures);

    [LoggerMessage(Level = LogLevel.Information, Message = "CPO {ConnectionKey} restored to Connected")]
    private static partial void LogHealthRestored(ILogger logger, string connectionKey);
}
