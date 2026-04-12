using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Dtos.Clients;
using See.Idp.Core.Services.Clients;

namespace See.Idp.Web.Areas.Admin.Pages.Clients;

public sealed class EditModel(
    IClientQueryService clientQueryService,
    IClientCommandService clientCommandService
) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return NotFound();
        }

        var client = await clientQueryService.GetClientByIdAsync(new GetClientByIdQuery(clientId));
        if (client is null)
        {
            return NotFound();
        }

        Input = new InputModel { ClientId = client.ClientId, DisplayName = client.DisplayName };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await clientCommandService.UpdateClientAsync(
            new UpdateClientCommand(Input.ClientId, Input.DisplayName)
        );

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Unable to update client.");
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
