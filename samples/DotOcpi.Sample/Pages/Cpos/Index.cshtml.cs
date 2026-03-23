using DotOcpi.Registry;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotOcpi.Sample.Pages.Cpos;

public class IndexModel(ICpoRegistry registry) : PageModel
{
    public IReadOnlyList<CpoConnection> Connections { get; private set; } = [];

    public void OnGet()
    {
        Connections = registry.GetAll();
    }
}
