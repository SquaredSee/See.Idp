using System.Collections.Generic;

namespace See.Idp.Core.Dtos.Clients;

/// <summary>
///     Represents a query to list clients.
/// </summary>
/// <param name="SearchTerm">Optional search term applied to client id and display name.</param>
/// <param name="Skip">Optional number of results to skip.</param>
/// <param name="Take">Optional maximum number of results to return.</param>
public sealed record ListClientsQuery(string? SearchTerm = null, int Skip = 0, int? Take = null);

/// <summary>
///     Represents a query to retrieve a client by identifier.
/// </summary>
/// <param name="ClientId">The unique client identifier.</param>
public sealed record GetClientByIdQuery(string ClientId);

/// <summary>
///     Represents a summarized view of a client.
/// </summary>
/// <param name="ClientId">The unique client identifier.</param>
/// <param name="DisplayName">The display name.</param>
public sealed record ClientSummaryDto(string ClientId, string? DisplayName);

/// <summary>
///     Represents detailed client information.
/// </summary>
/// <param name="ClientId">The unique client identifier.</param>
/// <param name="DisplayName">The display name.</param>
/// <param name="AllowAuthorizationCodeFlow">Whether authorization code flow is enabled.</param>
/// <param name="AllowClientCredentialsFlow">Whether client credentials flow is enabled.</param>
/// <param name="AllowRefreshTokenFlow">Whether refresh token flow is enabled.</param>
/// <param name="RedirectUris">Configured redirect URIs for this client.</param>
/// <param name="Permissions">Permissions assigned to this client.</param>
/// <param name="IsConfidential">Whether the client is configured as confidential.</param>
/// <param name="HasClientSecret">Whether a client secret is configured.</param>
public sealed record ClientDetailsDto(
    string ClientId,
    string? DisplayName,
    bool AllowAuthorizationCodeFlow,
    bool AllowClientCredentialsFlow,
    bool AllowRefreshTokenFlow,
    IReadOnlyList<string> RedirectUris,
    IReadOnlyList<string> Permissions,
    bool IsConfidential,
    bool HasClientSecret
);
