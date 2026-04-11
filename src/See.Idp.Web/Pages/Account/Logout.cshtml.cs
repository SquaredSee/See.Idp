using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using See.Idp.Core.Logging;
using See.Idp.Core.Services;

namespace See.Idp.Web.Pages.Account;

public partial class LogoutModel(
    IUserAccountService userAccountService,
    ILogger<LogoutModel> logger
) : PageModel
{
    [BindProperty]
    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var email =
            User.Identity?.Name
            ?? throw new InvalidOperationException("User is not authenticated.");

        await userAccountService.SignOutAsync();
        LogUserLoggedOut(email);

        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/Index");
    }

    [LoggerMessage(EventIds.Logout, LogLevel.Information, "User {email} logged out.")]
    partial void LogUserLoggedOut(string email);
}
