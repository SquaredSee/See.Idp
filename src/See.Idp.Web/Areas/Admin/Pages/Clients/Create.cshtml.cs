using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;

namespace See.Idp.Web.Areas.Admin.Pages.Clients;

public sealed class CreateModel(IOpenIddictApplicationManager applicationManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (await applicationManager.FindByClientIdAsync(Input.ClientId) is not null)
        {
            ModelState.AddModelError(nameof(Input.ClientId), "Client ID already exists.");
            return Page();
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = Input.ClientId,
            DisplayName = Input.DisplayName,
        };

        await applicationManager.CreateAsync(descriptor);
        return RedirectToPage("./Index");
    }

    public sealed class InputModel
    {
        [Required]
        public string ClientId { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
    }
}
