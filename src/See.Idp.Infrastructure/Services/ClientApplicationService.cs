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

    public async Task<OperationResult> CreateClientAsync(
        CreateClientRequest request,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            return OperationResult.Failure("Client ID is required.");
        }

        if (await applicationManager.FindByClientIdAsync(request.ClientId, ct) is not null)
        {
            return OperationResult.Failure("Client ID already exists.");
        }

        await applicationManager.CreateAsync(
            new OpenIddictApplicationDescriptor
            {
                ClientId = request.ClientId,
                DisplayName = request.DisplayName,
            },
            ct
        );

        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteClientAsync(
        string clientId,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return OperationResult.Failure("Client ID is required.");
        }

        var app = await applicationManager.FindByClientIdAsync(clientId, ct);
        if (app is null)
        {
            return OperationResult.Failure("Client not found.");
        }

        await applicationManager.DeleteAsync(app, ct);
        return OperationResult.Success();
    }
}
