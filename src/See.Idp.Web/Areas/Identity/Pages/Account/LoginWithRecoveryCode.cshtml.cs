using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Core.Services.Auth;

namespace See.Idp.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class LoginWithRecoveryCodeModel(IAuthenticationCommandService authService)
    : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public string? ReturnUrl { get; set; }

    public sealed class InputModel
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Recovery code")]
        public string RecoveryCode { get; set; } = string.Empty;
    }

    public IActionResult OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

        if (!ModelState.IsValid)
            return Page();

        var result = await authService.RecoveryCodeSignInAsync(
            new RecoveryCodeSignInCommand(Input.RecoveryCode.Replace(" ", string.Empty))
        );

        if (result.Succeeded)
            return LocalRedirect(ReturnUrl);

        if (result.IsLockedOut)
            return RedirectToPage("./Lockout");

        ModelState.AddModelError(string.Empty, result.Error ?? "Invalid recovery code.");
        return Page();
    }
}
