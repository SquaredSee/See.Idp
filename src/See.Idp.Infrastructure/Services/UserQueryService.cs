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
        var usersQuery = userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.Trim();
            usersQuery = usersQuery.Where(u =>
                (u.Email != null && u.Email.Contains(searchTerm))
                || (u.UserName != null && u.UserName.Contains(searchTerm))
            );
        }

        usersQuery = usersQuery.OrderBy(u => u.Email).ThenBy(u => u.UserName);

        if (query.Skip > 0)
        {
            usersQuery = usersQuery.Skip(query.Skip);
        }

        if (query.Take is > 0)
        {
            usersQuery = usersQuery.Take(query.Take.Value);
        }

        var users = await usersQuery.ToListAsync(ct);
        var adminUsers = await userManager.GetUsersInRoleAsync(Roles.Admin);
        var adminUserIds = new HashSet<string>(
            adminUsers.Select(u => u.Id),
            StringComparer.Ordinal
        );

        var result = new List<UserSummaryDto>(users.Count);

        foreach (var user in users)
        {
            var isAdmin = adminUserIds.Contains(user.Id);
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
