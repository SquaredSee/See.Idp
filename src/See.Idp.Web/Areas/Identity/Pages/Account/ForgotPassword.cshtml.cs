using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using See.Idp.Infrastructure;
using See.Idp.Web.Services;

namespace See.Idp.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class ForgotPasswordModel(
    UserManager<ApplicationUser> userManager,
    IEmailSender<ApplicationUser> emailSender
) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public string? ResetPasswordUrl { get; set; }

    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await userManager.FindByEmailAsync(Input.Email);

        // Always redirect to confirmation page to prevent user enumeration.
        if (user is null || !await userManager.IsEmailConfirmedAsync(user))
        {
            if (emailSender is not NoOpEmailSender)
                return RedirectToPage("./ForgotPasswordConfirmation");

            // In dev with no-op sender, fall through so the form re-renders with the dev link
            // missing (no confirmed user) — still safe.
            return RedirectToPage("./ForgotPasswordConfirmation");
        }

        var code = await userManager.GeneratePasswordResetTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var resetUrl = Url.Page(
            "/Account/ResetPassword",
            pageHandler: null,
            values: new { area = "Identity", code },
            protocol: Request.Scheme
        )!;

        // When using the no-op sender in development, display the link on-page instead.
        if (emailSender is NoOpEmailSender)
        {
            ResetPasswordUrl = resetUrl;
            return Page();
        }

        await emailSender.SendPasswordResetLinkAsync(
            user,
            Input.Email,
            HtmlEncoder.Default.Encode(resetUrl)
        );

        return RedirectToPage("./ForgotPasswordConfirmation");
    }
}
