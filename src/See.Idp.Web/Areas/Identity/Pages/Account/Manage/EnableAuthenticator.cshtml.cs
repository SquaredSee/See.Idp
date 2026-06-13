using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Core.Services.Auth;

namespace See.Idp.Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
public sealed class EnableAuthenticatorModel(
    ITwoFactorQueryService twoFactorQuery,
    ITwoFactorCommandService twoFactorCommand
) : PageModel
{
    public string SharedKey { get; set; } = string.Empty;
    public string AuthenticatorUri { get; set; } = string.Empty;

    [TempData]
    public IEnumerable<string> RecoveryCodes { get; set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public sealed class InputModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(7, MinimumLength = 6)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Verification code")]
        public string Code { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var setup = await twoFactorQuery.GetAuthenticatorSetupAsync(
            new GetAuthenticatorSetupQuery(userId)
        );
        if (setup is null)
            return NotFound();

        SharedKey = setup.SharedKey;
        AuthenticatorUri = setup.AuthenticatorUri;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var setup = await twoFactorQuery.GetAuthenticatorSetupAsync(
            new GetAuthenticatorSetupQuery(userId)
        );
        if (setup is null)
            return NotFound();

        SharedKey = setup.SharedKey;
        AuthenticatorUri = setup.AuthenticatorUri;

        if (!ModelState.IsValid)
            return Page();

        var result = await twoFactorCommand.EnableTwoFactorAsync(
            new EnableTwoFactorCommand(userId, Input.Code)
        );

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Verification failed.");
            return Page();
        }

        StatusMessage = "Your authenticator app has been verified.";
        RecoveryCodes = result.Codes;
        return RedirectToPage("./ShowRecoveryCodes");
    }
}
