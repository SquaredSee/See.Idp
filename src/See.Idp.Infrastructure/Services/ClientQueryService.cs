using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using See.Idp.Core.Dtos.Clients;
using See.Idp.Core.Services.Clients;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

public sealed partial class ClientQueryService(
    IOpenIddictApplicationManager applicationManager,
    ILogger<ClientQueryService> logger
) : IClientQueryService
{
    public async Task<IReadOnlyList<ClientSummaryDto>> ListClientsAsync(
        ListClientsQuery query,
        CancellationToken ct = default
    )
    {
        // TODO: Filtering and Paging is currently done in-memory which is not ideal for large datasets. Consider EF Core replacement.
        var clients = StreamClientsAsync(ct);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.Trim();
            clients = clients.Where(c =>
                c.ClientId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                || (
                    !string.IsNullOrWhiteSpace(c.DisplayName)
                    && c.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                )
            );
        }

        clients = clients.OrderBy(c => c.ClientId, StringComparer.OrdinalIgnoreCase);

        if (query.Skip > 0)
            clients = clients.Skip(query.Skip);
        if (query.Take is > 0)
            clients = clients.Take(query.Take.Value);

        var result = await clients.ToListAsync(ct);
        LogClientListRetrieved(result.Count);
        return result;
    }

    public async Task<ClientDetailsDto?> GetClientByIdAsync(
        GetClientByIdQuery query,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(query.ClientId))
        {
            LogClientCommandRejected(nameof(GetClientByIdAsync), "Client ID is required.");
            return null;
        }

        var app = await applicationManager.FindByClientIdAsync(query.ClientId, ct);
        if (app is null)
        {
            LogClientLookupNotFound(query.ClientId);
            return null;
        }

        var clientId = await applicationManager.GetClientIdAsync(app, ct);
        if (string.IsNullOrWhiteSpace(clientId))
        {
            LogClientCommandRejected(
                nameof(GetClientByIdAsync),
                $"Client '{query.ClientId}' returned an empty client id."
            );
            return null;
        }

        var displayName = await applicationManager.GetDisplayNameAsync(app, ct);
        return new ClientDetailsDto(clientId, displayName);
    }

    private async IAsyncEnumerable<ClientSummaryDto> StreamClientsAsync(
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        await foreach (var app in applicationManager.ListAsync(cancellationToken: ct))
        {
            var clientId = await applicationManager.GetClientIdAsync(app, ct);
            var displayName = await applicationManager.GetDisplayNameAsync(app, ct);

            if (!string.IsNullOrWhiteSpace(clientId))
            {
                yield return new ClientSummaryDto(clientId, displayName);
            }
        }
    }

    [LoggerMessage(
        EventId = EventIds.ClientListRetrieved,
        Level = LogLevel.Information,
        Message = "Retrieved {Count} clients"
    )]
    private partial void LogClientListRetrieved(int count);

    [LoggerMessage(
        EventId = EventIds.ClientLookupNotFound,
        Level = LogLevel.Warning,
        Message = "Client not found: {ClientId}"
    )]
    private partial void LogClientLookupNotFound(string clientId);

    [LoggerMessage(
        EventId = EventIds.ClientCommandRejected,
        Level = LogLevel.Warning,
        Message = "Client command {CommandName} rejected: {Reason}"
    )]
    private partial void LogClientCommandRejected(string commandName, string reason);
}
