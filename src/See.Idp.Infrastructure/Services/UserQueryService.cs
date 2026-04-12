using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using See.Idp.Core.Dtos.Users;
using See.Idp.Core.Services.Users;
using See.Idp.Infrastructure.Auth;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

public sealed partial class UserQueryService(
    UserManager<IdentityUser> userManager,
    ILogger<UserQueryService> logger
) : IUserQueryService
{
    public async Task<IReadOnlyList<UserSummaryDto>> ListUsersAsync(
        ListUsersQuery query,
        CancellationToken ct = default
    )
    {
        var users = await userManager
            .Users.OrderBy(u => u.Email)
            .ThenBy(u => u.UserName)
            .ToListAsync(ct);

        var result = new List<UserSummaryDto>(users.Count);

        foreach (var user in users)
        {
            var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);
            var isLocked =
                user.LockoutEnabled
                && user.LockoutEnd.HasValue
                && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

            result.Add(
                new UserSummaryDto(
                    UserId: user.Id,
                    UserName: user.UserName,
                    Email: user.Email,
                    EmailConfirmed: user.EmailConfirmed,
                    IsAdmin: isAdmin,
                    IsLockedOut: isLocked
                )
            );
        }

        LogUserListRetrieved(result.Count);
        return result;
    }

    [LoggerMessage(
        EventId = EventIds.UserListRetrieved,
        Level = LogLevel.Information,
        Message = "Retrieved {Count} users"
    )]
    private partial void LogUserListRetrieved(int count);
}
