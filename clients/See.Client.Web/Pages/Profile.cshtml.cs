using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace See.Client.Web.Pages;

[Authorize]
public class ProfileModel : PageModel
{
    public string Sub { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public IEnumerable<string> Roles { get; private set; } = [];

    public void OnGet()
    {
        Sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "-";
        Email = User.FindFirstValue("email") ?? User.FindFirstValue(ClaimTypes.Email) ?? "-";
        Roles = User.FindAll("role").Select(c => c.Value);
    }
}
