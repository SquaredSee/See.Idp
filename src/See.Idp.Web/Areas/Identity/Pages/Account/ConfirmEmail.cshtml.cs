using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Dtos.Users;
using See.Idp.Core.Services.Users;

namespace See.Idp.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class ConfirmEmailModel(IUserRegistrationCommandService registrationService)
    : PageModel
{
    public string StatusMessage { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public async Task<IActionResult> OnGetAsync(string? userId, string? code)
    {
        if (userId is null || code is null)
            return RedirectToPage("/Index", new { area = "" });

        var result = await registrationService.ConfirmEmailAsync(
            new ConfirmEmailCommand(userId, code)
        );

        if (result.Succeeded)
        {
            Succeeded = true;
            StatusMessage = "Thank you for confirming your email. You can now sign in.";
        }
        else
        {
            StatusMessage =
                result.Error
                ?? "Error confirming your email. The link may have expired or already been used.";
        }

        return Page();
    }
}
