using System.Collections.Generic;

namespace See.Idp.Core.Dtos.Clients;

/// <summary>Represents a command to create a client when it does not already exist.</summary>
public sealed record CreateClientIfMissingCommand(
    string ClientId,
    string? ClientSecret,
    string? DisplayName,
    IReadOnlyList<string> RedirectUris,
    IReadOnlyList<string> PostLogoutRedirectUris,
    IReadOnlyList<string> AdditionalPermissions
);
