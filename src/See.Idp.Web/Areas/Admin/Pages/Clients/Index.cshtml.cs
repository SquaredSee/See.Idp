using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Services;

namespace See.Idp.Web.Areas.Admin.Pages.Clients;

public sealed class IndexModel(IClientApplicationService clientApplicationService) : PageModel
{
    public List<ClientRow> Clients { get; } = [];

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string clientId)
    {
        await clientApplicationService.DeleteClientAsync(clientId);

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var clients = await clientApplicationService.ListClientsAsync();

        foreach (var client in clients)
        {
            Clients.Add(new ClientRow(client.ClientId, client.DisplayName));
        }
    }

    public sealed record ClientRow(string ClientId, string? DisplayName);
}
