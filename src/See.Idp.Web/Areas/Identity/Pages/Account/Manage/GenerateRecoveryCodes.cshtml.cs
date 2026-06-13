using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Core.Services.Auth;

namespace See.Idp.Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
public sealed class GenerateRecoveryCodesModel(
    ITwoFactorQueryService twoFactorQuery,
    ITwoFactorCommandService twoFactorCommand
) : PageModel
{
    [TempData]
    public System.Collections.Generic.IEnumerable<string> RecoveryCodes { get; set; } = [];

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
        var result = await twoFactorCommand.GenerateRecoveryCodesAsync(
            new GenerateRecoveryCodesCommand(userId)
        );

        if (!result.Succeeded)
        {
            StatusMessage = $"Error: {result.Error}";
            return RedirectToPage();
        }

        StatusMessage = "New recovery codes generated.";
        RecoveryCodes = result.Codes;
        return RedirectToPage("./ShowRecoveryCodes");
    }
}
