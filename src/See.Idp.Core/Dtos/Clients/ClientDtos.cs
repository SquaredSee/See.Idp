using System.Collections.Generic;

namespace See.Idp.Core.Dtos.Clients;

/// <summary>Represents a summarized view of a client.</summary>
/// <param name="ClientId">The unique client identifier.</param>
/// <param name="DisplayName">The display name.</param>
public sealed record ClientSummaryDto(string ClientId, string? DisplayName);

/// <summary>Represents detailed client information.</summary>
public sealed record ClientDetailsDto(
    string ClientId,
    string? DisplayName,
    bool AllowAuthorizationCodeFlow,
    bool AllowClientCredentialsFlow,
    bool AllowRefreshTokenFlow,
    IReadOnlyList<string> RedirectUris,
    IReadOnlyList<string> PostLogoutRedirectUris,
    IReadOnlyList<string> Permissions,
    bool IsConfidential,
    bool HasClientSecret
);
