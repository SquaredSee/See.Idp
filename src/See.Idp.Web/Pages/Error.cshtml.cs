using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace See.Idp.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public void OnGet()
    {
        // Use the 32-char hex TraceId rather than the full W3C traceparent string
        // so it can be pasted directly into Tempo/Loki without parsing.
        RequestId = Activity.Current?.TraceId.ToHexString() ?? HttpContext.TraceIdentifier;
    }
}
