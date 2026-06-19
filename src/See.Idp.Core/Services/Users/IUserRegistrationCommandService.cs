using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos.Common;
using See.Idp.Core.Dtos.Users;

namespace See.Idp.Core.Services.Users;

/// <summary>
///     Provides commands for registering users and confirming their email addresses.
/// </summary>
public interface IUserRegistrationCommandService
{
    /// <summary>
    ///     Registers a new user account.
    /// </summary>
    /// <param name="command">The command containing the email and password for the new account.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    ///     A result containing the user ID and email confirmation token on success,
    ///     or a list of errors on failure.
    /// </returns>
    Task<RegisterUserResult> RegisterAsync(
        RegisterUserCommand command,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Confirms a user's email address using the provided token.
    /// </summary>
    /// <param name="command">The command containing the user ID and Base64Url-encoded token.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result of the confirmation.</returns>
    Task<CommandResult> ConfirmEmailAsync(
        ConfirmEmailCommand command,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Generates a password reset token for the user with the given email address.
    ///     Returns <see langword="null"/> if the user does not exist or their email is not confirmed.
    /// </summary>
    /// <param name="command">The command containing the email address of the user requesting a password reset.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    ///     A Base64Url-encoded password reset token, or <see langword="null"/> if the user was
    ///     not found or their email has not been confirmed.
    /// </returns>
    Task<string?> GeneratePasswordResetTokenAsync(
        GeneratePasswordResetTokenCommand command,
        CancellationToken ct = default
    );
}
