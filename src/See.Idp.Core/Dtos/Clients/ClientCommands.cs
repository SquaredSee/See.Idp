using System.Collections.Generic;

namespace See.Idp.Core.Dtos.Clients;

/// <summary>
///     Represents a command to create a client.
/// </summary>
/// <param name="ClientId">The unique client identifier.</param>
/// <param name="DisplayName">The display name shown to administrators and users.</param>
public sealed record CreateClientCommand(string ClientId, string? DisplayName);

/// <summary>
///     Represents a command to update an existing client.
/// </summary>
/// <param name="ClientId">The unique client identifier.</param>
/// <param name="DisplayName">The updated display name.</param>
public sealed record UpdateClientCommand(string ClientId, string? DisplayName);

/// <summary>
///     Represents a command to delete a client.
/// </summary>
/// <param name="ClientId">The unique client identifier.</param>
public sealed record DeleteClientCommand(string ClientId);

/// <summary>
///     Represents a command to create a client when it does not already exist.
/// </summary>
/// <param name="ClientId">The unique client identifier.</param>
/// <param name="ClientSecret">The client secret used for confidential clients.</param>
/// <param name="DisplayName">The display name shown to administrators and users.</param>
/// <param name="RedirectUris">The redirect URIs configured for authorization flows.</param>
/// <param name="Permissions">The permissions granted to the client.</param>
public sealed record CreateClientIfMissingCommand(
    string ClientId,
    string? ClientSecret,
    string? DisplayName,
    IReadOnlyList<string> RedirectUris,
    IReadOnlyList<string> Permissions
);
