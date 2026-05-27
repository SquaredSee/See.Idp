using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using See.Idp.Core.Services.Users;

namespace See.Idp.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class RegisterConfirmationModel(
    IUserQueryService userQueryService,
    IWebHostEnvironment env
) : PageModel
{
    public string Email { get; set; } = string.Empty;

    public bool DisplayConfirmAccountLink { get; set; }

    public string? EmailConfirmationUrl { get; set; }

    public async Task<IActionResult> OnGetAsync(string? email, string? returnUrl = null)
    {
        if (email is null)
            return RedirectToPage("/Index", new { area = "" });

        var userId = await userQueryService.FindUserIdByEmailAsync(email);
        if (userId is null)
            return NotFound($"Unable to load user with email '{email}'.");

        Email = email;

        // Show the confirmation link directly in development.
        DisplayConfirmAccountLink = env.IsDevelopment();

        if (DisplayConfirmAccountLink)
        {
            var code = await userQueryService.GenerateEmailConfirmationTokenAsync(userId);
            if (code is not null)
            {
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
        }

        return Page();
    }
}
