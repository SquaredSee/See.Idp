using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace See.Idp.Web.Pages;

public class IndexModel(ILogger<IndexModel> logger) : PageModel
{
    public void OnGet() { }
}
