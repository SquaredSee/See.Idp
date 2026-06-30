using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using See.Idp.Core.Dtos.Clients;
using See.Idp.Core.Services.Clients;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

public sealed partial class ClientQueryService(
    ApplicationDbContext dbContext,
    IOpenIddictApplicationManager applicationManager,
    ILogger<ClientQueryService> logger
) : IClientQueryService
{
    public async Task<IReadOnlyList<ClientSummaryDto>> ListClientsAsync(
        ListClientsQuery query,
        CancellationToken ct = default
    )
    {
        var q = dbContext
            .Set<OpenIddictEntityFrameworkCoreApplication>()
            .AsNoTracking()
            .Where(a => a.ClientId != null && a.ClientId != string.Empty);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToLower();
            q = q.Where(a =>
                (a.ClientId != null && a.ClientId.ToLower().Contains(term))
                || (a.DisplayName != null && a.DisplayName.ToLower().Contains(term))
            );
        }

        q = q.OrderBy(a => a.ClientId);

        if (query.Skip > 0)
            q = q.Skip(query.Skip);
        if (query.Take is > 0)
            q = q.Take(query.Take.Value);

        var result = await q.Select(a => new ClientSummaryDto(a.ClientId!, a.DisplayName))
            .ToListAsync(ct);

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

        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, app, ct);

        var permissions = descriptor
            .Permissions.Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        var redirectUris = descriptor
            .RedirectUris.Select(uri => uri.AbsoluteUri)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(uri => uri, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var postLogoutRedirectUris = descriptor
            .PostLogoutRedirectUris.Select(uri => uri.AbsoluteUri)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(uri => uri, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var hasClientSecret = !string.IsNullOrWhiteSpace(descriptor.ClientSecret);
        var isConfidential =
            hasClientSecret
            || string.Equals(
                descriptor.ClientType,
                OpenIddictConstants.ClientTypes.Confidential,
                StringComparison.Ordinal
            );

        return new ClientDetailsDto(
            clientId,
            descriptor.DisplayName,
            ClientPermissionConventions.HasAuthorizationCodeFlow(permissions),
            ClientPermissionConventions.HasClientCredentialsFlow(permissions),
            ClientPermissionConventions.HasRefreshTokenFlow(permissions),
            redirectUris,
            postLogoutRedirectUris,
            permissions
                .Where(p => !ClientPermissionConventions.IsFlowControlledPermission(p))
                .ToList(),
            isConfidential,
            hasClientSecret
        );
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
