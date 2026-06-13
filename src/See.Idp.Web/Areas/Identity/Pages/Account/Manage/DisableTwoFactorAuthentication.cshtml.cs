using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Core.Services.Auth;

namespace See.Idp.Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
public sealed class DisableTwoFactorAuthenticationModel(
    ITwoFactorQueryService twoFactorQuery,
    ITwoFactorCommandService twoFactorCommand
) : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var info = await twoFactorQuery.GetTwoFactorInfoAsync(new GetTwoFactorInfoQuery(userId));
        if (info is null || !info.IsTwoFactorEnabled)
            return RedirectToPage("./TwoFactorAuthentication");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await twoFactorCommand.DisableTwoFactorAsync(
            new DisableTwoFactorCommand(userId)
        );

        if (!result.Succeeded)
        {
            StatusMessage = $"Error: {result.Error}";
            return RedirectToPage();
        }

        StatusMessage = "Two-factor authentication has been disabled.";
        return RedirectToPage("./TwoFactorAuthentication");
    }
}
