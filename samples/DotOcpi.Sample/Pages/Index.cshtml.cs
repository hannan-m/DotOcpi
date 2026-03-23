using DotOcpi.Registry;
using DotOcpi.Sample.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotOcpi.Sample.Pages;

public class IndexModel(ICpoRegistry registry, InMemoryDataStore store) : PageModel
{
    public int CpoCount { get; private set; }
    public int LocationCount { get; private set; }
    public int SessionCount { get; private set; }
    public int TariffCount { get; private set; }
    public int CdrCount { get; private set; }
    public IReadOnlyList<CpoConnection> Connections { get; private set; } = [];

    public void OnGet()
    {
        Connections = registry.GetAll();
        CpoCount = Connections.Count;
        LocationCount = store.Locations.Count;
        SessionCount = store.Sessions.Count;
        TariffCount = store.Tariffs.Count;
        CdrCount = store.Cdrs.Count;
    }
}
