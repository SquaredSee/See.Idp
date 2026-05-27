using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Infrastructure;

namespace See.Idp.Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
public sealed class IndexModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager
) : PageModel
{
    public string Username { get; set; } = string.Empty;

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public sealed class InputModel
    {
        [Phone]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }
    }

    private async Task LoadAsync(ApplicationUser user)
    {
        Username = await userManager.GetUserNameAsync(user) ?? string.Empty;
        Input = new InputModel { PhoneNumber = await userManager.GetPhoneNumberAsync(user) };
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        var currentPhone = await userManager.GetPhoneNumberAsync(user);
        if (Input.PhoneNumber != currentPhone)
        {
            var result = await userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
            if (!result.Succeeded)
            {
                StatusMessage = "Error: Unable to set phone number.";
                return RedirectToPage();
            }
        }

        await signInManager.RefreshSignInAsync(user);
        StatusMessage = "Your profile has been updated.";
        return RedirectToPage();
    }
}
