using System;
using System.Collections.Generic;
using System.Security.Cryptography;
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
    public async Task<CreateClientResult> CreateClientAsync(
        CreateClientCommand command,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.ClientId))
        {
            LogClientCommandRejected(nameof(CreateClientAsync), "Client ID is required.");
            return CreateClientResult.Failure("Client ID is required.");
        }

        if (await applicationManager.FindByClientIdAsync(command.ClientId, ct) is not null)
        {
            LogClientAlreadyExists(command.ClientId);
            return CreateClientResult.Failure("Client ID already exists.");
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = command.ClientId,
            DisplayName = command.DisplayName,
        };

        if (
            !TryConfigureClient(
                descriptor,
                command.ClientId,
                command.AllowAuthorizationCodeFlow,
                command.AllowClientCredentialsFlow,
                command.AllowRefreshTokenFlow,
                command.RedirectUris,
                command.AdditionalPermissions,
                out var configurationError
            )
        )
        {
            LogClientCommandRejected(nameof(CreateClientAsync), configurationError);
            return CreateClientResult.Failure(configurationError);
        }

        string? generatedSecret = null;
        if (command.GenerateClientSecret)
        {
            generatedSecret = GenerateClientSecret();
            descriptor.ClientSecret = generatedSecret;
            descriptor.ClientType = OpenIddictConstants.ClientTypes.Confidential;
            LogClientSecretGeneratedOnCreate(command.ClientId);
        }
        else
        {
            descriptor.ClientSecret = null;
            descriptor.ClientType = OpenIddictConstants.ClientTypes.Public;
        }

        await applicationManager.CreateAsync(descriptor, ct);

        LogClientCreated(command.ClientId);
        return CreateClientResult.Success(generatedSecret);
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

        if (
            !TryConfigureClient(
                descriptor,
                command.ClientId,
                command.AllowAuthorizationCodeFlow,
                command.AllowClientCredentialsFlow,
                command.AllowRefreshTokenFlow,
                command.RedirectUris,
                command.AdditionalPermissions,
                out var configurationError
            )
        )
        {
            LogClientCommandRejected(nameof(UpdateClientAsync), configurationError);
            return CommandResult.Failure(configurationError);
        }

        await applicationManager.UpdateAsync(app, descriptor, ct);
        LogClientUpdated(command.ClientId);
        return CommandResult.Success();
    }

    public async Task<RotateClientSecretResult> RotateClientSecretAsync(
        RotateClientSecretCommand command,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.ClientId))
        {
            LogClientCommandRejected(nameof(RotateClientSecretAsync), "Client ID is required.");
            return RotateClientSecretResult.Failure("Client ID is required.");
        }

        var app = await applicationManager.FindByClientIdAsync(command.ClientId, ct);
        if (app is null)
        {
            LogClientLookupNotFound(command.ClientId);
            return RotateClientSecretResult.Failure("Client not found.");
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, app, ct);

        var promotedToConfidential = EnsureConfidentialClient(descriptor);
        if (promotedToConfidential)
        {
            LogClientPromotedToConfidential(command.ClientId, nameof(RotateClientSecretAsync));
        }

        var generatedSecret = GenerateClientSecret();
        descriptor.ClientSecret = generatedSecret;

        try
        {
            await applicationManager.UpdateAsync(app, descriptor, ct);
        }
        catch (Exception ex)
        {
            LogClientCommandRejected(nameof(RotateClientSecretAsync), ex.Message);
            return RotateClientSecretResult.Failure("Unable to rotate client secret.");
        }

        LogClientSecretRotated(command.ClientId);
        return RotateClientSecretResult.Success(generatedSecret, promotedToConfidential);
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

        var normalizedClientSecret = string.IsNullOrWhiteSpace(command.ClientSecret)
            ? null
            : command.ClientSecret;

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = command.ClientId,
            ClientSecret = normalizedClientSecret,
            ClientType = normalizedClientSecret is null
                ? OpenIddictConstants.ClientTypes.Public
                : OpenIddictConstants.ClientTypes.Confidential,
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

        foreach (var permission in command.AdditionalPermissions)
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

    private static bool TryConfigureClient(
        OpenIddictApplicationDescriptor descriptor,
        string clientId,
        bool allowAuthorizationCodeFlow,
        bool allowClientCredentialsFlow,
        bool allowRefreshTokenFlow,
        IReadOnlyList<string> redirectUris,
        IReadOnlyList<string> additionalPermissions,
        out string configurationError
    )
    {
        if (
            !TryParseRedirectUris(
                redirectUris,
                clientId,
                out var parsedRedirectUris,
                out configurationError
            )
        )
        {
            return false;
        }

        descriptor.RedirectUris.Clear();
        foreach (var redirectUri in parsedRedirectUris)
        {
            descriptor.RedirectUris.Add(redirectUri);
        }

        descriptor.Permissions.Clear();
        foreach (
            var permission in ClientPermissionConventions.BuildPermissions(
                allowAuthorizationCodeFlow,
                allowClientCredentialsFlow,
                allowRefreshTokenFlow,
                additionalPermissions
            )
        )
        {
            descriptor.Permissions.Add(permission);
        }

        configurationError = string.Empty;
        return true;
    }

    private static bool TryParseRedirectUris(
        IReadOnlyList<string> redirectUris,
        string clientId,
        out List<Uri> parsedRedirectUris,
        out string validationError
    )
    {
        parsedRedirectUris = [];
        var uniqueUris = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var redirectUri in redirectUris)
        {
            if (string.IsNullOrWhiteSpace(redirectUri))
            {
                continue;
            }

            if (!Uri.TryCreate(redirectUri.Trim(), UriKind.Absolute, out var uri))
            {
                validationError = $"Invalid redirect URI '{redirectUri}' for client '{clientId}'.";
                return false;
            }

            if (uniqueUris.Add(uri.AbsoluteUri))
            {
                parsedRedirectUris.Add(uri);
            }
        }

        validationError = string.Empty;
        return true;
    }

    private static bool EnsureConfidentialClient(OpenIddictApplicationDescriptor descriptor)
    {
        if (
            string.Equals(
                descriptor.ClientType,
                OpenIddictConstants.ClientTypes.Confidential,
                StringComparison.Ordinal
            )
        )
        {
            return false;
        }

        descriptor.ClientType = OpenIddictConstants.ClientTypes.Confidential;
        return true;
    }

    private static string GenerateClientSecret()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(48);
        var base64Secret = Convert.ToBase64String(randomBytes);
        return base64Secret.TrimEnd('=').Replace('+', '-').Replace('/', '_');
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
        EventId = EventIds.ClientSecretRotated,
        Level = LogLevel.Information,
        Message = "Client secret rotated: {ClientId}"
    )]
    private partial void LogClientSecretRotated(string clientId);

    [LoggerMessage(
        EventId = EventIds.ClientSecretGeneratedOnCreate,
        Level = LogLevel.Information,
        Message = "Client secret generated on create: {ClientId}"
    )]
    private partial void LogClientSecretGeneratedOnCreate(string clientId);

    [LoggerMessage(
        EventId = EventIds.ClientPromotedToConfidential,
        Level = LogLevel.Information,
        Message = "Client promoted to confidential: {ClientId} via {Operation}"
    )]
    private partial void LogClientPromotedToConfidential(string clientId, string operation);

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
