using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using See.Idp.Infrastructure;

namespace See.Idp.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class ConfirmEmailModel(UserManager<ApplicationUser> userManager) : PageModel
{
    public string StatusMessage { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public async Task<IActionResult> OnGetAsync(string? userId, string? code)
    {
        if (userId is null || code is null)
            return RedirectToPage("/Index", new { area = "" });

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound($"Unable to load user with ID '{userId}'.");

        var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await userManager.ConfirmEmailAsync(user, decodedCode);

        if (result.Succeeded)
        {
            Succeeded = true;
            StatusMessage = "Thank you for confirming your email. You can now sign in.";
        }
        else
        {
            StatusMessage =
                "Error confirming your email. The link may have expired or already been used.";
        }

        return Page();
    }
}
