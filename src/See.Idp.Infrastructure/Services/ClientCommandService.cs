using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using See.Idp.Core.Dtos.Clients;
using See.Idp.Core.Dtos.Common;
using See.Idp.Core.Services.Clients;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

public sealed partial class ClientCommandService(
    IOpenIddictApplicationManager applicationManager,
    ILogger<ClientCommandService> logger
) : IClientCommandService
{
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
