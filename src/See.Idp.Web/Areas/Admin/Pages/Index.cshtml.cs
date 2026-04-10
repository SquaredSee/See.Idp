using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace See.Idp.Web.Areas.Admin.Pages;

public class IndexModel(ILogger<IndexModel> logger) : PageModel
{
    public void OnGet() { }
}
