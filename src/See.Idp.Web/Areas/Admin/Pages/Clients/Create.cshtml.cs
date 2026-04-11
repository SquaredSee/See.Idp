using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Dtos;
using See.Idp.Core.Services;

namespace See.Idp.Web.Areas.Admin.Pages.Clients;

public sealed class CreateModel(IClientApplicationService clientApplicationService) : PageModel
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

        var result = await clientApplicationService.CreateClientAsync(
            new CreateClientRequest(Input.ClientId, Input.DisplayName)
        );

        if (!result.Succeeded)
        {
            ModelState.AddModelError(
                nameof(Input.ClientId),
                result.Error ?? "Client creation failed."
            );
            return Page();
        }

        return RedirectToPage("./Index");
    }

    public sealed class InputModel
    {
        [Required]
        public string ClientId { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
    }
}
