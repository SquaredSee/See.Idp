using System.Collections.Generic;

namespace See.Idp.Core.Dtos.Users;

/// <summary>
///     Represents a command to register a new user account.
/// </summary>
/// <param name="Email">The email address for the new account.</param>
/// <param name="Password">The password for the new account.</param>
public sealed record RegisterUserCommand(string Email, string Password);

/// <summary>
///     Represents the result of a user registration attempt.
/// </summary>
/// <param name="Succeeded">Whether the registration succeeded.</param>
/// <param name="UserId">The ID of the newly created user, when successful.</param>
/// <param name="EmailConfirmationToken">The Base64Url-encoded email confirmation token, when successful.</param>
/// <param name="Errors">The error descriptions, when unsuccessful.</param>
public sealed record RegisterUserResult(
    bool Succeeded,
    string? UserId,
    string? EmailConfirmationToken,
    IReadOnlyList<string> Errors
)
{
    /// <summary>Creates a successful registration result.</summary>
    public static RegisterUserResult Success(string userId, string emailConfirmationToken) =>
        new(true, userId, emailConfirmationToken, []);

    /// <summary>Creates a failed registration result.</summary>
    public static RegisterUserResult Failure(IEnumerable<string> errors) =>
        new(false, null, null, new List<string>(errors));
}

/// <summary>
///     Represents a command to confirm a user's email address.
/// </summary>
/// <param name="UserId">The ID of the user whose email is being confirmed.</param>
/// <param name="EncodedToken">The Base64Url-encoded confirmation token.</param>
public sealed record ConfirmEmailCommand(string UserId, string EncodedToken);
