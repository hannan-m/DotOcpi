using System.Text.Json;
using DotOcpi.Registry;
using DotOcpi.Sample.Services;
using DotOcpi.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotOcpi.Sample.Pages.Cpos;

public class DetailModel(ICpoRegistry registry, InMemoryDataStore store) : PageModel
{
    public CpoConnection? Connection { get; private set; }
    public string? ActiveTab { get; private set; }
    public IReadOnlyList<JsonElement> TabData { get; private set; } = [];

    public IActionResult OnGet(string key, string? tab)
    {
        Connection = registry.FindByConnectionKey(key);
        if (Connection is null)
            return RedirectToPage("/Cpos/Index");

        ActiveTab = tab ?? "locations";
        TabData = LoadTabData(key, ActiveTab);
        return Page();
    }

    private IReadOnlyList<JsonElement> LoadTabData(string cpoKey, string module)
    {
        var dict = module switch
        {
            "locations" => store.Locations,
            "tariffs" => store.Tariffs,
            "sessions" => store.Sessions,
            "cdrs" => store.Cdrs,
            _ => store.Locations,
        };

        var opts = Connection is not null
            ? OcpiJsonOptions.GetOptions(Connection.Version)
            : OcpiJsonOptions.GetOptions(OcpiVersion.V2_2_1);

        return dict.Where(kv => kv.Key.StartsWith(cpoKey + ":"))
            .Select(kv =>
            {
                var json = JsonSerializer.Serialize(kv.Value, opts.GetTypeInfo(kv.Value.GetType()));
                return JsonDocument.Parse(json).RootElement.Clone();
            })
            .ToList();
    }
}
