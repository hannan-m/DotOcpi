using System.Text.Json;
using DotOcpi.Registry;
using DotOcpi.Sample.Services;
using DotOcpi.Serialization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotOcpi.Sample.Pages;

public class TariffsModel(ICpoRegistry registry, InMemoryDataStore store) : PageModel
{
    public IReadOnlyList<(string CpoKey, JsonElement Data)> Tariffs { get; private set; } = [];

    public void OnGet()
    {
        Tariffs = store.Tariffs
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
