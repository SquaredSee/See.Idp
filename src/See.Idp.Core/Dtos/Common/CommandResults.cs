namespace See.Idp.Core.Dtos.Common;

/// <summary>
///     Represents the result of a command execution.
/// </summary>
/// <param name="Succeeded">Indicates whether the command succeeded.</param>
/// <param name="Message">An optional informational message.</param>
/// <param name="Error">An optional error message.</param>
public sealed record CommandResult(bool Succeeded, string? Message = null, string? Error = null)
{
    /// <summary>
    ///     Creates a successful command result.
    /// </summary>
    /// <param name="message">An optional informational message.</param>
    /// <returns>A successful command result.</returns>
    public static CommandResult Success(string? message = null) => new(true, message, null);

    /// <summary>
    ///     Creates a failed command result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed command result.</returns>
    public static CommandResult Failure(string error) => new(false, null, error);
}

/// <summary>
///     Represents the result of a create-if-missing command.
/// </summary>
/// <param name="Succeeded">Indicates whether the command succeeded.</param>
/// <param name="Created">Indicates whether a new entity was created.</param>
/// <param name="Error">An optional error message.</param>
public sealed record CreateIfMissingResult(bool Succeeded, bool Created, string? Error = null)
{
    /// <summary>
    ///     Creates a successful result indicating a new entity was created.
    /// </summary>
    /// <returns>A successful create-if-missing result with created set to true.</returns>
    public static CreateIfMissingResult CreatedNew() => new(true, true, null);

    /// <summary>
    ///     Creates a successful result indicating the entity already existed.
    /// </summary>
    /// <returns>A successful create-if-missing result with created set to false.</returns>
    public static CreateIfMissingResult AlreadyExists() => new(true, false, null);

    /// <summary>
    ///     Creates a failed create-if-missing result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed create-if-missing result.</returns>
    public static CreateIfMissingResult Failure(string error) => new(false, false, error);
}

/// <summary>
///     Represents the result of a create-user-if-missing command.
/// </summary>
/// <param name="Succeeded">Indicates whether the command succeeded.</param>
/// <param name="Created">Indicates whether a new user was created.</param>
/// <param name="UserId">The identifier of the existing or newly created user, if available.</param>
/// <param name="Error">An optional error message.</param>
public sealed record CreateUserIfMissingResult(
    bool Succeeded,
    bool Created,
    string? UserId = null,
    string? Error = null
)
{
    /// <summary>
    ///     Creates a successful result indicating a user was created.
    /// </summary>
    /// <param name="userId">The identifier of the created user.</param>
    /// <returns>A successful create-user-if-missing result with created set to true.</returns>
    public static CreateUserIfMissingResult CreatedNew(string userId) =>
        new(true, true, userId, null);

    /// <summary>
    ///     Creates a successful result indicating the user already existed.
    /// </summary>
    /// <param name="userId">The identifier of the existing user.</param>
    /// <returns>A successful create-user-if-missing result with created set to false.</returns>
    public static CreateUserIfMissingResult AlreadyExists(string userId) =>
        new(true, false, userId, null);

    /// <summary>
    ///     Creates a failed create-user-if-missing result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed create-user-if-missing result.</returns>
    public static CreateUserIfMissingResult Failure(string error) => new(false, false, null, error);
}
