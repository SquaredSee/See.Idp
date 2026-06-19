using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Dtos.Users;
using See.Idp.Core.Services.Auth;
using See.Idp.Core.Services.Users;

namespace See.Idp.Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
public sealed class IndexModel(
    IUserQueryService userQueryService,
    IUserCommandService userCommandService,
    IUserAuthenticationCommandService authService
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

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await userQueryService.GetUserProfileAsync(new GetUserProfileQuery(userId));
        if (profile is null)
            return NotFound($"Unable to load user with ID '{userId}'.");

        Username = profile.Email ?? string.Empty;
        Input = new InputModel { PhoneNumber = profile.PhoneNumber };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await userQueryService.GetUserProfileAsync(new GetUserProfileQuery(userId));
        if (profile is null)
            return NotFound($"Unable to load user with ID '{userId}'.");

        if (!ModelState.IsValid)
        {
            Username = profile.Email ?? string.Empty;
            Input = new InputModel { PhoneNumber = profile.PhoneNumber };
            return Page();
        }

        if (Input.PhoneNumber != profile.PhoneNumber)
        {
            var result = await userCommandService.UpdatePhoneNumberAsync(
                new UpdatePhoneNumberCommand(userId, Input.PhoneNumber)
            );
            if (!result.Succeeded)
            {
                StatusMessage = "Error: Unable to set phone number.";
                return RedirectToPage();
            }

            await authService.RefreshSignInAsync();
        }
        StatusMessage = "Your profile has been updated.";
        return RedirectToPage();
    }
}
