using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Core.Services.Auth;
using See.Idp.Infrastructure;

namespace See.Idp.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class ForgotPasswordModel(
    IPasswordCommandService passwordService,
    IEmailSender<ApplicationUser> emailSender,
    IWebHostEnvironment env,
    ILogger<ForgotPasswordModel> logger
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

        var result = await passwordService.GeneratePasswordResetTokenAsync(
            new GeneratePasswordResetTokenCommand(Input.Email)
        );

        // Always redirect to confirmation page to prevent user enumeration.
        if (!result.Succeeded)
            return RedirectToPage("./ForgotPasswordConfirmation");

        var resetUrl = Url.Page(
            "/Account/ResetPassword",
            pageHandler: null,
            values: new { area = "Identity", code = result.Token },
            protocol: Request.Scheme
        )!;

        // In development, display the link on-page instead of sending an email.
        if (env.IsDevelopment())
        {
            ResetPasswordUrl = resetUrl;
            return RedirectToPage();
        }

        try
        {
            await emailSender.SendPasswordResetLinkAsync(
                new ApplicationUser { Email = Input.Email, UserName = Input.Email },
                Input.Email,
                HtmlEncoder.Default.Encode(resetUrl)
            );
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to send password reset email to {Email}", Input.Email);
        }

        return RedirectToPage("./ForgotPasswordConfirmation");
    }
}
