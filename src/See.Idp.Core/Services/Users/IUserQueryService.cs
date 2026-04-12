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
}
