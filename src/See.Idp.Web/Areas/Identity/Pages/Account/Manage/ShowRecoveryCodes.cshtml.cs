using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace See.Idp.Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
public sealed class ShowRecoveryCodesModel : PageModel
{
    [TempData]
    public string[] RecoveryCodes { get; set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public IActionResult OnGet()
    {
        if (!RecoveryCodes.Any())
            return RedirectToPage("./TwoFactorAuthentication");

        return Page();
    }
}
