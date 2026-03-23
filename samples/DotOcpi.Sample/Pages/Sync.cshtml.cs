using DotOcpi.Registry;
using DotOcpi.Sample.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotOcpi.Sample.Pages;

public class SyncModel(ICpoRegistry registry, InMemoryDataStore store) : PageModel
{
    public IReadOnlyList<CpoSyncInfo> CpoSyncs { get; private set; } = [];

    public void OnGet()
    {
        CpoSyncs = registry.GetAll().Select(c => new CpoSyncInfo
        {
            ConnectionKey = c.ConnectionKey,
            Version = c.Version.ToVersionString(),
            Modules = c.ModuleEndpoints.Keys
                .Where(m => m is "locations" or "tariffs" or "sessions" or "cdrs")
                .Select(m => new ModuleSyncInfo
                {
                    Name = m,
                    ItemCount = m switch
                    {
                        "locations" => store.Locations.Count(kv => kv.Key.StartsWith(c.ConnectionKey + ":")),
                        "tariffs" => store.Tariffs.Count(kv => kv.Key.StartsWith(c.ConnectionKey + ":")),
                        "sessions" => store.Sessions.Count(kv => kv.Key.StartsWith(c.ConnectionKey + ":")),
                        "cdrs" => store.Cdrs.Count(kv => kv.Key.StartsWith(c.ConnectionKey + ":")),
                        _ => 0,
                    },
                })
                .ToList(),
        }).ToList();
    }

    public record CpoSyncInfo
    {
        public required string ConnectionKey { get; init; }
        public required string Version { get; init; }
        public required List<ModuleSyncInfo> Modules { get; init; }
    }

    public record ModuleSyncInfo
    {
        public required string Name { get; init; }
        public int ItemCount { get; init; }
    }
}
