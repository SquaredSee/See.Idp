using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Dtos.Users;
using See.Idp.Core.Services.Users;

namespace See.Idp.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class ConfirmEmailModel(IRegistrationCommandService registrationService) : PageModel
{
    [BindProperty]
    public string UserId { get; set; } = string.Empty;

    [BindProperty]
    public string Code { get; set; } = string.Empty;

    public bool Confirmed { get; private set; }

    public bool Succeeded { get; private set; }

    public string StatusMessage { get; private set; } = string.Empty;

    public IActionResult OnGet(string? userId, string? code)
    {
        if (userId is null || code is null)
            return RedirectToPage("/Index", new { area = "" });

        UserId = userId;
        Code = code;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(UserId) || string.IsNullOrEmpty(Code))
            return RedirectToPage("/Index", new { area = "" });

        var result = await registrationService.ConfirmEmailAsync(
            new ConfirmEmailCommand(UserId, Code)
        );

        Confirmed = true;

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
