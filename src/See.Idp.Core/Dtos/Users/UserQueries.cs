namespace See.Idp.Core.Dtos.Users;

/// <summary>Represents a query to list users.</summary>
public sealed record ListUsersQuery(string? SearchTerm = null, int Skip = 0, int? Take = null);

/// <summary>Represents a query to retrieve a user ID by email address.</summary>
/// <param name="Email">The email address to look up.</param>
public sealed record FindUserByEmailQuery(string Email);

/// <summary>Represents a query to retrieve a user's profile by their ID.</summary>
/// <param name="UserId">The ID of the user.</param>
public sealed record GetUserProfileQuery(string UserId);
