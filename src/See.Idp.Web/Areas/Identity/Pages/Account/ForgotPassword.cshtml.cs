using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using See.Idp.Core.Services.Users;
using See.Idp.Infrastructure;

namespace See.Idp.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class ForgotPasswordModel(
    IUserQueryService userQueryService,
    IEmailSender<ApplicationUser> emailSender,
    IWebHostEnvironment env
) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = default!;

    [TempData]
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

        var encodedCode = await userQueryService.GeneratePasswordResetTokenAsync(Input.Email);

        // Always redirect to confirmation page to prevent user enumeration.
        if (encodedCode is null)
            return RedirectToPage("./ForgotPasswordConfirmation");

        var resetUrl = Url.Page(
            "/Account/ResetPassword",
            pageHandler: null,
            values: new { area = "Identity", code = encodedCode },
            protocol: Request.Scheme
        )!;

        // In development, display the link on-page instead of sending an email.
        if (env.IsDevelopment())
        {
            ResetPasswordUrl = resetUrl;
            return RedirectToPage();
        }

        await emailSender.SendPasswordResetLinkAsync(
            new ApplicationUser { Email = Input.Email, UserName = Input.Email },
            Input.Email,
            HtmlEncoder.Default.Encode(resetUrl)
        );

        return RedirectToPage("./ForgotPasswordConfirmation");
    }
}
