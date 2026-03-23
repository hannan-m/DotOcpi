using DotOcpi.Registry;
using DotOcpi.Sample.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotOcpi.Sample.Pages;

public class CommandsModel(ICpoRegistry registry, InMemoryDataStore store) : PageModel
{
    public IReadOnlyList<CpoConnection> Connections { get; private set; } = [];

    public void OnGet()
    {
        Connections = registry.GetAll();
    }
}
