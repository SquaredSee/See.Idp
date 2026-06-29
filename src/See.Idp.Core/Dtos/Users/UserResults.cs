namespace See.Idp.Core.Dtos.Users;

/// <summary>Represents the result of a create-user-if-missing command.</summary>
public sealed record CreateUserIfMissingResult(
    bool Succeeded,
    bool Created,
    string? UserId = null,
    string? Error = null
)
{
    /// <summary>Creates a successful result indicating a user was created.</summary>
    public static CreateUserIfMissingResult CreatedNew(string userId) =>
        new(true, true, userId, null);

    /// <summary>Creates a successful result indicating the user already existed.</summary>
    public static CreateUserIfMissingResult AlreadyExists(string userId) =>
        new(true, false, userId, null);

    /// <summary>Creates a failed create-user-if-missing result.</summary>
    public static CreateUserIfMissingResult Failure(string error) => new(false, false, null, error);
}
