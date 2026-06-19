using System.Collections.Generic;
using System.Linq;
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
    private const int PageSize = 20;

    public List<ClientRow> Clients { get; } = [];

    public int CurrentPage { get; private set; } = 1;

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? StatusKind { get; set; }

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = NormalizePage(pageNumber);
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string clientId, int pageNumber = 1)
    {
        var targetPage = NormalizePage(pageNumber);
        var result = await clientCommandService.DeleteClientAsync(
            new DeleteClientCommand(clientId)
        );

        if (result.Succeeded)
            SetStatusSuccess(result.Message ?? "Client deleted.");
        else
            SetStatusError(result.Error ?? "Unable to delete client.");

        return RedirectToPage(new { pageNumber = targetPage });
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
        var clients = await clientQueryService.ListClientsAsync(
            new ListClientsQuery(Skip: (CurrentPage - 1) * PageSize, Take: PageSize + 1)
        );

        HasNextPage = clients.Count > PageSize;

        foreach (var client in clients.Take(PageSize))
        {
            Clients.Add(new ClientRow(client.ClientId, client.DisplayName));
        }
    }

    private static int NormalizePage(int page)
    {
        return page < 1 ? 1 : page;
    }

    public sealed record ClientRow(string ClientId, string? DisplayName);
}
