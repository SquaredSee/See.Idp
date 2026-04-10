using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;

namespace See.Idp.Web.Areas.Admin.Pages.Clients;

public sealed class IndexModel(IOpenIddictApplicationManager applicationManager) : PageModel
{
    public List<ClientRow> Clients { get; } = [];

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return RedirectToPage();
        }

        var app = await applicationManager.FindByClientIdAsync(clientId);
        if (app is not null)
        {
            await applicationManager.DeleteAsync(app);
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        await foreach (var app in applicationManager.ListAsync())
        {
            Clients.Add(
                new ClientRow(
                    await applicationManager.GetClientIdAsync(app) ?? string.Empty,
                    await applicationManager.GetDisplayNameAsync(app)
                )
            );
        }

        Clients.Sort((a, b) => string.CompareOrdinal(a.ClientId, b.ClientId));
    }

    public sealed record ClientRow(string ClientId, string? DisplayName);
}
