using System.Collections.Concurrent;

namespace DotOcpi.Sample.Services;

/// <summary>
/// Tracks operational metrics per CPO: request counts, errors, syncs.
/// </summary>
public sealed class MetricsCollector
{
    private readonly ConcurrentDictionary<string, CpoMetrics> _metrics = new();
    private int _totalSyncs;
    private int _totalErrors;

    public int TotalSyncs => _totalSyncs;
    public int TotalErrors => _totalErrors;

    public void RecordSync(string cpoId)
    {
        Interlocked.Increment(ref _totalSyncs);
        GetOrCreate(cpoId).IncrementSyncs();
    }

    public void RecordRequest(string cpoId)
    {
        GetOrCreate(cpoId).IncrementRequests();
    }

    public void RecordError(string cpoId)
    {
        Interlocked.Increment(ref _totalErrors);
        GetOrCreate(cpoId).IncrementErrors();
    }

    public int GetRequestCount(string cpoId) => GetOrCreate(cpoId).Requests;
    public int GetErrorCount(string cpoId) => GetOrCreate(cpoId).Errors;
    public int GetSyncCount(string cpoId) => GetOrCreate(cpoId).Syncs;

    private CpoMetrics GetOrCreate(string cpoId) =>
        _metrics.GetOrAdd(cpoId, _ => new CpoMetrics());

    private sealed class CpoMetrics
    {
        private int _requests;
        private int _errors;
        private int _syncs;

        public int Requests => _requests;
        public int Errors => _errors;
        public int Syncs => _syncs;

        public void IncrementRequests() => Interlocked.Increment(ref _requests);
        public void IncrementErrors() => Interlocked.Increment(ref _errors);
        public void IncrementSyncs() => Interlocked.Increment(ref _syncs);
    }
}
