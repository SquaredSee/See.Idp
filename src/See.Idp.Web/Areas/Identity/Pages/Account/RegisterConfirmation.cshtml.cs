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
public sealed class RegisterConfirmationModel(
    UserManager<ApplicationUser> userManager,
    IEmailSender<ApplicationUser> emailSender
) : PageModel
{
    public string Email { get; set; } = string.Empty;

    public bool DisplayConfirmAccountLink { get; set; }

    public string? EmailConfirmationUrl { get; set; }

    public async Task<IActionResult> OnGetAsync(string? email, string? returnUrl = null)
    {
        if (email is null)
            return RedirectToPage("/Index", new { area = "" });

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return NotFound($"Unable to load user with email '{email}'.");

        Email = email;

        // Show the confirmation link directly when using the no-op sender (development).
        DisplayConfirmAccountLink = emailSender is NoOpEmailSender;

        if (DisplayConfirmAccountLink)
        {
            var userId = await userManager.GetUserIdAsync(user);
            var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            EmailConfirmationUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new
                {
                    area = "Identity",
                    userId,
                    code,
                    returnUrl,
                },
                protocol: Request.Scheme
            );
        }

        return Page();
    }
}
