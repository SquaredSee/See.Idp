using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;

namespace See.Idp.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class RegisterConfirmationModel(IWebHostEnvironment env) : PageModel
{
    public string Email { get; set; } = string.Empty;

    public bool DisplayConfirmAccountLink { get; set; }

    public string? EmailConfirmationUrl { get; set; }

    public IActionResult OnGet(string? email, string? returnUrl = null)
    {
        if (email is null)
            return RedirectToPage("/Index", new { area = "" });

        Email = email;

        DisplayConfirmAccountLink = env.IsDevelopment();
        if (DisplayConfirmAccountLink)
            EmailConfirmationUrl = TempData["EmailConfirmationUrl"] as string;

        return Page();
    }
}
