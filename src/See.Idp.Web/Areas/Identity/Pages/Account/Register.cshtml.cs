using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using See.Idp.Core.Dtos.Users;
using See.Idp.Core.Services.Auth;
using See.Idp.Core.Services.Users;

namespace See.Idp.Web.Areas.Identity.Pages.Account;

public sealed class RegisterModel(
    IRegistrationCommandService registrationService,
    IRegistrationEmailService registrationEmailService,
    IWebHostEnvironment env,
    ILogger<RegisterModel> logger
) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(
            100,
            ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.",
            MinimumLength = 6
        )]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
            return Page();

        var result = await registrationService.RegisterAsync(
            new RegisterUserCommand(Input.Email, Input.Password)
        );

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);
            return Page();
        }

        var confirmationLink = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new
            {
                area = "Identity",
                userId = result.UserId,
                code = result.EmailConfirmationToken,
                returnUrl,
            },
            protocol: Request.Scheme
        )!;

        if (env.IsDevelopment())
            TempData["EmailConfirmationUrl"] = confirmationLink;

        try
        {
            await registrationEmailService.SendConfirmationLinkAsync(
                Input.Email,
                HtmlEncoder.Default.Encode(confirmationLink),
                HttpContext.RequestAborted
            );
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to send confirmation email to {Email}", Input.Email);
            ModelState.AddModelError(
                string.Empty,
                "Your account was created but we could not send the confirmation email. "
                    + "Please contact support."
            );
            return Page();
        }

        return RedirectToPage("./RegisterConfirmation", new { email = Input.Email, returnUrl });
    }
}
