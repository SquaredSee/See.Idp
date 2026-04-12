using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using See.Idp.Core.Dtos.Clients;
using See.Idp.Core.Dtos.Common;
using See.Idp.Core.Services.Clients;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

// TODO: Consider splitting this into separate query and command services if the implementation grows more complex.
public sealed partial class ClientApplicationService(
    IOpenIddictApplicationManager applicationManager,
    ILogger<ClientApplicationService> logger
) : IClientQueryService, IClientCommandService
{
    public async Task<IReadOnlyList<ClientSummaryDto>> ListClientsAsync(
        ListClientsQuery query,
        CancellationToken ct = default
    )
    {
        var clients = new List<ClientSummaryDto>();

        await foreach (var app in applicationManager.ListAsync(cancellationToken: ct))
        {
            var clientId = await applicationManager.GetClientIdAsync(app, ct);
            var displayName = await applicationManager.GetDisplayNameAsync(app, ct);

            if (!string.IsNullOrWhiteSpace(clientId))
            {
                clients.Add(new ClientSummaryDto(clientId, displayName));
            }
        }

        LogClientListRetrieved(clients.Count);
        return clients;
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

    public async Task<CommandResult> CreateClientAsync(
        CreateClientCommand command,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.ClientId))
        {
            LogClientCommandRejected(nameof(CreateClientAsync), "Client ID is required.");
            return CommandResult.Failure("Client ID is required.");
        }

        if (await applicationManager.FindByClientIdAsync(command.ClientId, ct) is not null)
        {
            LogClientAlreadyExists(command.ClientId);
            return CommandResult.Failure("Client ID already exists.");
        }

        await applicationManager.CreateAsync(
            new OpenIddictApplicationDescriptor
            {
                ClientId = command.ClientId,
                DisplayName = command.DisplayName,
            },
            ct
        );

        LogClientCreated(command.ClientId);
        return CommandResult.Success();
    }

    public async Task<CommandResult> UpdateClientAsync(
        UpdateClientCommand command,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.ClientId))
        {
            LogClientCommandRejected(nameof(UpdateClientAsync), "Client ID is required.");
            return CommandResult.Failure("Client ID is required.");
        }

        var app = await applicationManager.FindByClientIdAsync(command.ClientId, ct);
        if (app is null)
        {
            LogClientLookupNotFound(command.ClientId);
            return CommandResult.Failure("Client not found.");
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, app, ct);

        descriptor.DisplayName = command.DisplayName;

        await applicationManager.UpdateAsync(app, descriptor, ct);
        LogClientUpdated(command.ClientId);
        return CommandResult.Success();
    }

    public async Task<CommandResult> DeleteClientAsync(
        DeleteClientCommand command,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.ClientId))
        {
            LogClientCommandRejected(nameof(DeleteClientAsync), "Client ID is required.");
            return CommandResult.Failure("Client ID is required.");
        }

        var app = await applicationManager.FindByClientIdAsync(command.ClientId, ct);
        if (app is null)
        {
            LogClientLookupNotFound(command.ClientId);
            return CommandResult.Failure("Client not found.");
        }

        await applicationManager.DeleteAsync(app, ct);
        LogClientDeleted(command.ClientId);
        return CommandResult.Success();
    }

    public async Task<CreateIfMissingResult> CreateClientIfMissingAsync(
        CreateClientIfMissingCommand command,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.ClientId))
        {
            LogClientCommandRejected(nameof(CreateClientIfMissingAsync), "Client ID is required.");
            return CreateIfMissingResult.Failure("Client ID is required.");
        }

        if (await applicationManager.FindByClientIdAsync(command.ClientId, ct) is not null)
        {
            LogClientAlreadyExists(command.ClientId);
            return CreateIfMissingResult.AlreadyExists();
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = command.ClientId,
            ClientSecret = string.IsNullOrWhiteSpace(command.ClientSecret)
                ? null
                : command.ClientSecret,
            DisplayName = command.DisplayName,
        };

        foreach (var redirectUri in command.RedirectUris)
        {
            if (string.IsNullOrWhiteSpace(redirectUri))
            {
                continue;
            }

            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
            {
                LogClientCommandRejected(
                    nameof(CreateClientIfMissingAsync),
                    $"Invalid redirect URI '{redirectUri}' for client '{command.ClientId}'."
                );
                return CreateIfMissingResult.Failure(
                    $"Invalid redirect URI '{redirectUri}' for client '{command.ClientId}'."
                );
            }

            descriptor.RedirectUris.Add(uri);
        }

        foreach (var permission in command.Permissions)
        {
            if (!string.IsNullOrWhiteSpace(permission))
            {
                descriptor.Permissions.Add(permission);
            }
        }

        await applicationManager.CreateAsync(descriptor, ct);
        LogClientCreated(command.ClientId);
        return CreateIfMissingResult.CreatedNew();
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
        EventId = EventIds.ClientCreated,
        Level = LogLevel.Information,
        Message = "Client created: {ClientId}"
    )]
    private partial void LogClientCreated(string clientId);

    [LoggerMessage(
        EventId = EventIds.ClientUpdated,
        Level = LogLevel.Information,
        Message = "Client updated: {ClientId}"
    )]
    private partial void LogClientUpdated(string clientId);

    [LoggerMessage(
        EventId = EventIds.ClientDeleted,
        Level = LogLevel.Information,
        Message = "Client deleted: {ClientId}"
    )]
    private partial void LogClientDeleted(string clientId);

    [LoggerMessage(
        EventId = EventIds.ClientManagementAlreadyExists,
        Level = LogLevel.Information,
        Message = "Client already exists: {ClientId}"
    )]
    private partial void LogClientAlreadyExists(string clientId);

    [LoggerMessage(
        EventId = EventIds.ClientCommandRejected,
        Level = LogLevel.Warning,
        Message = "Client command {CommandName} rejected: {Reason}"
    )]
    private partial void LogClientCommandRejected(string commandName, string reason);
}
