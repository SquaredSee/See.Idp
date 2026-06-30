using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos.Users;

namespace See.Idp.Core.Services.Users;

/// <summary>Provides read operations for user data.</summary>
public interface IUserQueryService
{
    /// <summary>Lists users that match the supplied query criteria.</summary>
    Task<IReadOnlyList<UserSummaryDto>> ListUsersAsync(
        ListUsersQuery query,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Returns the profile of a user by their ID, or <see langword="null"/> if not found.
    /// </summary>
    Task<UserProfileDto?> GetUserProfileAsync(
        GetUserProfileQuery query,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Returns the user ID for the account with the given email, or <see langword="null"/>
    ///     if no such account exists.
    /// </summary>
    Task<FindUserByEmailDto> FindUserIdByEmailAsync(
        FindUserByEmailQuery query,
        CancellationToken ct = default
    );
}
