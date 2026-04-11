using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenIddict.Abstractions;
using See.Idp.Core.Dtos;
using See.Idp.Core.Services;

namespace See.Idp.Infrastructure.Services;

public sealed class ClientApplicationService(IOpenIddictApplicationManager applicationManager)
    : IClientApplicationService
{
    public async Task<IReadOnlyList<ClientApplicationDto>> ListClientsAsync(
        CancellationToken ct = default
    )
    {
        var clients = new List<ClientApplicationDto>();

        await foreach (var app in applicationManager.ListAsync(cancellationToken: ct))
        {
            var clientId = await applicationManager.GetClientIdAsync(app, ct);
            var displayName = await applicationManager.GetDisplayNameAsync(app, ct);

            if (!string.IsNullOrWhiteSpace(clientId))
            {
                clients.Add(new ClientApplicationDto(clientId, displayName));
            }
        }

        clients.Sort((a, b) => string.CompareOrdinal(a.ClientId, b.ClientId));
        return clients;
    }

    public async Task<ClientApplicationDto?> GetClientAsync(
        string clientId,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return null;
        }

        var app = await applicationManager.FindByClientIdAsync(clientId, ct);
        if (app is null)
        {
            return null;
        }

        var resolvedClientId = await applicationManager.GetClientIdAsync(app, ct);
        if (string.IsNullOrWhiteSpace(resolvedClientId))
        {
            return null;
        }

        var displayName = await applicationManager.GetDisplayNameAsync(app, ct);
        return new ClientApplicationDto(resolvedClientId, displayName);
    }

    public async Task<ClientApplicationActionResult> CreateClientAsync(
        CreateClientRequest request,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            return ClientApplicationActionResult.Failure("Client ID is required.");
        }

        if (await applicationManager.FindByClientIdAsync(request.ClientId, ct) is not null)
        {
            return ClientApplicationActionResult.Failure("Client ID already exists.");
        }

        await applicationManager.CreateAsync(
            new OpenIddictApplicationDescriptor
            {
                ClientId = request.ClientId,
                DisplayName = request.DisplayName,
            },
            ct
        );

        return ClientApplicationActionResult.Success();
    }

    public async Task<ClientApplicationActionResult> UpdateClientAsync(
        UpdateClientRequest request,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            return ClientApplicationActionResult.Failure("Client ID is required.");
        }

        var app = await applicationManager.FindByClientIdAsync(request.ClientId, ct);
        if (app is null)
        {
            return ClientApplicationActionResult.Failure("Client not found.");
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, app, ct);

        descriptor.DisplayName = request.DisplayName;

        await applicationManager.UpdateAsync(app, descriptor, ct);
        return ClientApplicationActionResult.Success();
    }

    public async Task<ClientApplicationActionResult> DeleteClientAsync(
        string clientId,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return ClientApplicationActionResult.Failure("Client ID is required.");
        }

        var app = await applicationManager.FindByClientIdAsync(clientId, ct);
        if (app is null)
        {
            return ClientApplicationActionResult.Failure("Client not found.");
        }

        await applicationManager.DeleteAsync(app, ct);
        return ClientApplicationActionResult.Success();
    }
}
