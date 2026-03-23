using DotOcpi.Registry;
using DotOcpi.Sample.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotOcpi.Sample.Pages;

public class MetricsModel(ICpoRegistry registry, InMemoryDataStore store, MetricsCollector metrics) : PageModel
{
    public int TotalCpos { get; private set; }
    public int TotalLocations { get; private set; }
    public int TotalSessions { get; private set; }
    public int TotalTariffs { get; private set; }
    public int TotalCdrs { get; private set; }
    public int TotalSyncs { get; private set; }
    public int TotalErrors { get; private set; }
    public IReadOnlyList<CpoMetric> CpoMetrics { get; private set; } = [];

    public void OnGet()
    {
        var connections = registry.GetAll();
        TotalCpos = connections.Count;
        TotalLocations = store.Locations.Count;
        TotalSessions = store.Sessions.Count;
        TotalTariffs = store.Tariffs.Count;
        TotalCdrs = store.Cdrs.Count;
        TotalSyncs = metrics.TotalSyncs;
        TotalErrors = metrics.TotalErrors;

        CpoMetrics = connections
            .Select(c => new CpoMetric
            {
                ConnectionKey = c.ConnectionKey,
                Requests = metrics.GetRequestCount(c.ConnectionKey),
                Errors = metrics.GetErrorCount(c.ConnectionKey),
                Syncs = metrics.GetSyncCount(c.ConnectionKey),
            })
            .ToList();
    }

    public record CpoMetric
    {
        public required string ConnectionKey { get; init; }
        public int Requests { get; init; }
        public int Errors { get; init; }
        public int Syncs { get; init; }
        public string ErrorRate => Requests > 0 ? $"{(double)Errors / Requests * 100:F1}%" : "—";
    }
}
