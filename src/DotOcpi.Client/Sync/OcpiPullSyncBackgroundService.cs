using DotOcpi.Registry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotOcpi.Client.Sync;

/// <summary>
/// Background service that periodically pulls data from all active CPOs
/// for configured modules. Uses <see cref="IOcpiSyncService"/> for the
/// actual sync work and <see cref="PullSyncOptions"/> for scheduling.
/// </summary>
internal sealed partial class OcpiPullSyncBackgroundService : BackgroundService
{
    private readonly IOcpiSyncService _syncService;
    private readonly ICpoRegistry _registry;
    private readonly IOptions<PullSyncOptions> _options;
    private readonly ISyncStateStore _syncStateStore;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OcpiPullSyncBackgroundService> _logger;

    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(30);

    public OcpiPullSyncBackgroundService(
        IOcpiSyncService syncService,
        ICpoRegistry registry,
        IOptions<PullSyncOptions> options,
        ISyncStateStore syncStateStore,
        TimeProvider timeProvider,
        ILogger<OcpiPullSyncBackgroundService> logger
    )
    {
        _syncService = syncService;
        _registry = registry;
        _options = options;
        _syncStateStore = syncStateStore;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TickInterval, _timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await RunSyncCycleAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogSyncCycleFailed(ex);
            }
        }
    }

    internal async Task RunSyncCycleAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var now = _timeProvider.GetUtcNow();
        var connections = _registry.GetAll();

        foreach (var connection in connections)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (connection.Status is not ConnectionStatus.Connected)
                continue;

            var cpoId = connection.ConnectionKey;
            var enabledModules = PullSyncOptionsResolver.ResolveEnabledModules(options, cpoId);

            foreach (var moduleId in enabledModules)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (!connection.ModuleEndpoints.ContainsKey(moduleId))
                    continue;

                var interval = PullSyncOptionsResolver.ResolveInterval(options, cpoId, moduleId);
                var lastSync = await _syncStateStore
                    .GetLastSyncAsync(cpoId, moduleId, cancellationToken)
                    .ConfigureAwait(false);

                if (lastSync.HasValue)
                {
                    var jitter = ComputeJitter(options.MaxJitter, cpoId, moduleId);
                    var nextDue = lastSync.Value + interval + jitter;
                    if (now < nextDue)
                        continue;
                }

                LogSyncScheduled(cpoId, moduleId, interval);

                try
                {
                    await _syncService
                        .SyncModuleFromCpoAsync(cpoId, moduleId, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogModuleSyncFailed(cpoId, moduleId, ex);
                }
            }
        }
    }

    /// <summary>
    /// Deterministic jitter for a CPO+module pair. Uses a hash to produce a stable
    /// fraction of <paramref name="maxJitter"/> so the same pair always gets the
    /// same offset, preventing timer drift across cycles.
    /// </summary>
    internal static TimeSpan ComputeJitter(TimeSpan maxJitter, string cpoId, string moduleId)
    {
        if (maxJitter <= TimeSpan.Zero)
            return TimeSpan.Zero;

        var hash = HashCode.Combine(cpoId, moduleId);
        var fraction = (double)(hash & 0x7FFFFFFF) / int.MaxValue;
        return TimeSpan.FromTicks((long)(maxJitter.Ticks * fraction));
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Pull sync cycle failed")]
    private partial void LogSyncCycleFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scheduling sync: {CpoId}/{ModuleId} (interval {Interval})")]
    private partial void LogSyncScheduled(string cpoId, string moduleId, TimeSpan interval);

    [LoggerMessage(Level = LogLevel.Error, Message = "Sync failed: {CpoId}/{ModuleId}")]
    private partial void LogModuleSyncFailed(string cpoId, string moduleId, Exception exception);
}
