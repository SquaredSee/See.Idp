namespace See.Idp.Core.Dtos.Clients;

/// <summary>Represents a query to list clients.</summary>
public sealed record ListClientsQuery(string? SearchTerm = null, int Skip = 0, int? Take = null);

/// <summary>Represents a query to retrieve a client by identifier.</summary>
/// <param name="ClientId">The unique client identifier.</param>
public sealed record GetClientByIdQuery(string ClientId);
