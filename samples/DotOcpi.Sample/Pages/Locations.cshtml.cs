using System.Text.Json;
using DotOcpi.Registry;
using DotOcpi.Sample.Services;
using DotOcpi.Serialization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotOcpi.Sample.Pages;

public class LocationsModel(ICpoRegistry registry, InMemoryDataStore store) : PageModel
{
    public IReadOnlyList<(string CpoKey, JsonElement Data)> Locations { get; private set; } = [];
    public IReadOnlyList<string> CpoKeys { get; private set; } = [];
    public string? FilterCpo { get; private set; }

    public void OnGet(string? cpo)
    {
        FilterCpo = cpo;
        CpoKeys = registry.GetAll().Select(c => c.ConnectionKey).ToList();

        Locations = store
            .Locations.Where(kv => cpo is null || kv.Key.StartsWith(cpo + ":"))
            .Select(kv =>
            {
                var cpoKey = kv.Key.Split(':')[0] + ":" + kv.Key.Split(':')[1];
                var version = registry.FindByConnectionKey(cpoKey)?.Version ?? OcpiVersion.V2_2_1;
                var opts = OcpiJsonOptions.GetOptions(version);
                var json = JsonSerializer.Serialize(kv.Value, opts.GetTypeInfo(kv.Value.GetType()));
                return (cpoKey, JsonDocument.Parse(json).RootElement.Clone());
            })
            .ToList();
    }
}
