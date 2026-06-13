using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Services.Auth;

namespace See.Idp.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class LogoutModel(IUserAuthenticationCommandService authService) : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Account/Login", new { area = "Identity" });
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        await authService.SignOutAsync();
        return LocalRedirect(returnUrl ?? Url.Content("~/"));
    }
}
