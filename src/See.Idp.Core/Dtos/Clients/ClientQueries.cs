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
public sealed record ClientDetailsDto(string ClientId, string? DisplayName);
