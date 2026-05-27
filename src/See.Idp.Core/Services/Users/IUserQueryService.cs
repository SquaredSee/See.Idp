using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos.Users;

namespace See.Idp.Core.Services.Users;

/// <summary>
///     Provides read operations for user data.
/// </summary>
public interface IUserQueryService
{
    /// <summary>
    ///     Lists users that match the supplied query criteria.
    /// </summary>
    /// <param name="query">The query containing filter and paging options.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A read-only list of matching users.</returns>
    Task<IReadOnlyList<UserSummaryDto>> ListUsersAsync(
        ListUsersQuery query,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Returns the profile of a user by their ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The user's profile, or <see langword="null"/> if not found.</returns>
    Task<UserProfileDto?> GetUserProfileAsync(string userId, CancellationToken ct = default);

    /// <summary>
    ///     Returns the user ID for the account with the given email, or <see langword="null"/> if
    ///     no such account exists.
    /// </summary>
    /// <param name="email">The email address to look up.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The user ID, or <see langword="null"/> if not found.</returns>
    Task<string?> FindUserIdByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    ///     Generates a password reset token for the user with the given email address.
    ///     Returns <see langword="null"/> if the user does not exist or their email is not confirmed.
    /// </summary>
    /// <param name="email">The email address of the user requesting a password reset.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    ///     A Base64Url-encoded password reset token, or <see langword="null"/> if the user was
    ///     not found or their email has not been confirmed.
    /// </returns>
    Task<string?> GeneratePasswordResetTokenAsync(string email, CancellationToken ct = default);

    /// <summary>
    ///     Generates a new email confirmation token for the given user.
    ///     Returns <see langword="null"/> if no user with the given ID was found.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    ///     The Base64Url-encoded confirmation token, or <see langword="null"/> if no user with
    ///     the given ID was found.
    /// </returns>
    Task<string?> GenerateEmailConfirmationTokenAsync(
        string userId,
        CancellationToken ct = default
    );
}
