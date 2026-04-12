namespace See.Idp.Core.Dtos.Clients;

/// <summary>
///     Represents a query to list clients.
/// </summary>
public sealed record ListClientsQuery;

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
public sealed record ClientDetailsDto(string ClientId, string? DisplayName);
