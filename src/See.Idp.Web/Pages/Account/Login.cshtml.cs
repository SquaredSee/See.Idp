using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using See.Idp.Core.Logging;
using See.Idp.Core.Services;

namespace See.Idp.Web.Pages.Account;

public partial class LoginModel(IUserAccountService userAccountService, ILogger<LoginModel> logger)
    : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return Page();

        LogLoginAttempt(Input.Email);

        var result = await userAccountService.PasswordSignInAsync(
            Input.Email,
            Input.Password,
            Input.RememberMe
        );

        if (result.Succeeded)
        {
            LogLoginSuccess(Input.Email);
            return LocalRedirect(returnUrl ?? "/");
        }

        if (result.IsLockedOut)
        {
            LogLoginLockedOut(Input.Email);
            ModelState.AddModelError(
                string.Empty,
                "Your account is locked out. Please try again later."
            );
            return Page();
        }

        LogLoginFailed(Input.Email);
        ModelState.AddModelError(string.Empty, result.Error ?? "Login failed.");
        return Page();
    }

    [LoggerMessage(
        EventId = EventIds.LoginAttempt,
        Level = LogLevel.Information,
        Message = "Login attempt for {Email}"
    )]
    private partial void LogLoginAttempt(string email);

    [LoggerMessage(
        EventId = EventIds.LoginSuccess,
        Level = LogLevel.Information,
        Message = "Login succeeded for {Email}"
    )]
    private partial void LogLoginSuccess(string email);

    [LoggerMessage(
        EventId = EventIds.LoginFailed,
        Level = LogLevel.Warning,
        Message = "Login failed for {Email}"
    )]
    private partial void LogLoginFailed(string email);

    [LoggerMessage(
        EventId = EventIds.LoginLockedOut,
        Level = LogLevel.Warning,
        Message = "User {Email} is locked out"
    )]
    private partial void LogLoginLockedOut(string email);
}
