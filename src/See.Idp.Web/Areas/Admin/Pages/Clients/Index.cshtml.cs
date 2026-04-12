using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Dtos.Clients;
using See.Idp.Core.Services.Clients;

namespace See.Idp.Web.Areas.Admin.Pages.Clients;

public sealed class IndexModel(
    IClientQueryService clientQueryService,
    IClientCommandService clientCommandService
) : PageModel
{
    public List<ClientRow> Clients { get; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? StatusKind { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string clientId)
    {
        var result = await clientCommandService.DeleteClientAsync(
            new DeleteClientCommand(clientId)
        );

        if (result.Succeeded)
            SetStatusSuccess(result.Message ?? "Client deleted.");
        else
            SetStatusError(result.Error ?? "Unable to delete client.");

        return RedirectToPage();
    }

    private void SetStatusSuccess(string message)
    {
        StatusKind = "success";
        StatusMessage = message;
    }

    private void SetStatusError(string message)
    {
        StatusKind = "error";
        StatusMessage = message;
    }

    private async Task LoadAsync()
    {
        var clients = await clientQueryService.ListClientsAsync(new ListClientsQuery());

        foreach (var client in clients)
        {
            Clients.Add(new ClientRow(client.ClientId, client.DisplayName));
        }
    }

    public sealed record ClientRow(string ClientId, string? DisplayName);
}
