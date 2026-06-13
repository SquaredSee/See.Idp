using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Core.Services.Auth;

namespace See.Idp.Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
public sealed class TwoFactorAuthenticationModel(ITwoFactorQueryService twoFactorQuery) : PageModel
{
    public bool HasAuthenticator { get; set; }
    public bool Is2faEnabled { get; set; }
    public bool IsMachineRemembered { get; set; }
    public int RecoveryCodesLeft { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var info = await twoFactorQuery.GetTwoFactorInfoAsync(new GetTwoFactorInfoQuery(userId));
        if (info is null)
            return NotFound();

        HasAuthenticator = info.HasAuthenticator;
        Is2faEnabled = info.IsTwoFactorEnabled;
        IsMachineRemembered = info.IsMachineRemembered;
        RecoveryCodesLeft = info.RecoveryCodesLeft;
        return Page();
    }
}
