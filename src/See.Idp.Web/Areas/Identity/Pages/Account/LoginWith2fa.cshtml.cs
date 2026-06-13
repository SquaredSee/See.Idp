using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Core.Services.Auth;

namespace See.Idp.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class LoginWith2faModel(IUserAuthenticationCommandService authService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }

    public sealed class InputModel
    {
        [Required]
        [StringLength(7, MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Authenticator code")]
        public string TwoFactorCode { get; set; } = string.Empty;

        [Display(Name = "Remember this machine")]
        public bool RememberMachine { get; set; }
    }

    public IActionResult OnGet(bool rememberMe, string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
        RememberMe = rememberMe;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(bool rememberMe, string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
        RememberMe = rememberMe;

        if (!ModelState.IsValid)
            return Page();

        var result = await authService.TwoFactorSignInAsync(
            new TwoFactorSignInCommand(
                Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty),
                rememberMe,
                Input.RememberMachine
            )
        );

        if (result.Succeeded)
            return LocalRedirect(ReturnUrl);

        if (result.IsLockedOut)
            return RedirectToPage("./Lockout");

        ModelState.AddModelError(string.Empty, result.Error ?? "Invalid authenticator code.");
        return Page();
    }
}
