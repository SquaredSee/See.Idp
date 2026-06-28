using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Dtos.Users;
using See.Idp.Core.Services.Users;

namespace See.Idp.Web.Areas.Admin.Pages.Users;

public sealed class EditModel(
    IUserQueryService userQueryService,
    IUserCommandService userCommandService
) : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? StatusKind { get; set; }

    public string? UserId { get; private set; }
    public string? Email { get; private set; }

    public string? GeneratedConfirmationUrl { get; private set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string userId)
    {
        if (!await TryLoadProfileAsync(userId)) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            await TryLoadProfileAsync(Input.UserId);
            return Page();
        }

        var result = await userCommandService.UpdatePhoneNumberAsync(
            new UpdatePhoneNumberCommand(Input.UserId, NullIfEmpty(Input.PhoneNumber)));

        if (result.Succeeded)
        {
            StatusKind = "success";
            StatusMessage = "Phone number updated.";
        }
        else
        {
            StatusKind = "error";
            StatusMessage = result.Error ?? "Unable to update phone number.";
        }

        return RedirectToPage(new { userId = Input.UserId });
    }

    public async Task<IActionResult> OnPostGenerateConfirmationLinkAsync(string userId)
    {
        var token = await userQueryService.GenerateEmailConfirmationTokenAsync(
            new GenerateEmailConfirmationTokenQuery(userId));

        if (token is null)
        {
            StatusKind = "error";
            StatusMessage = "User not found.";
            return RedirectToPage(new { userId });
        }

        if (!await TryLoadProfileAsync(userId)) return NotFound();

        GeneratedConfirmationUrl = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new { area = "Identity", userId, code = token },
            protocol: Request.Scheme);

        return Page();
    }

    private async Task<bool> TryLoadProfileAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;

        var profile = await userQueryService.GetUserProfileAsync(new GetUserProfileQuery(userId));
        if (profile is null) return false;

        UserId = userId;
        Email = profile.Email;
        Input = new InputModel { UserId = userId, PhoneNumber = profile.PhoneNumber };
        return true;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public sealed class InputModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }
    }
}
