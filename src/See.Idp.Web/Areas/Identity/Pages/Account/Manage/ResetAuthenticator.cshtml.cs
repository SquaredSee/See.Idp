using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Core.Services.Auth;

namespace See.Idp.Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
public sealed class ResetAuthenticatorModel(ITwoFactorCommandService twoFactorCommand) : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await twoFactorCommand.ResetAuthenticatorKeyAsync(
            new ResetAuthenticatorKeyCommand(userId)
        );

        if (!result.Succeeded)
        {
            StatusMessage = $"Error: {result.Error}";
            return RedirectToPage();
        }

        StatusMessage =
            "Your authenticator app key has been reset. Configure your authenticator app with the new key.";
        return RedirectToPage("./EnableAuthenticator");
    }
}
