using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using See.Idp.Core.Auth;
using See.Idp.Core.Dtos.Users;
using See.Idp.Core.Services.Users;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

public sealed partial class UserQueryService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ILogger<UserQueryService> logger
) : IUserQueryService
{
    public async Task<IReadOnlyList<UserSummaryDto>> ListUsersAsync(
        ListUsersQuery query,
        CancellationToken ct = default
    )
    {
        var adminRoleName = Roles.Admin.ToUpperInvariant();
        var adminUserIdList = await dbContext
            .UserRoles.Join(
                dbContext.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, r.NormalizedName }
            )
            .Where(x => x.NormalizedName == adminRoleName)
            .Select(x => x.UserId)
            .ToListAsync(ct);
        var adminUserIds = new HashSet<string>(adminUserIdList, StringComparer.Ordinal);

        var q = dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToLower();
            q = q.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(term))
                || (u.UserName != null && u.UserName.ToLower().Contains(term))
            );
        }

        q = q.OrderBy(u => u.Email).ThenBy(u => u.UserName);

        if (query.Skip > 0)
            q = q.Skip(query.Skip);
        if (query.Take is > 0)
            q = q.Take(query.Take.Value);

        var users = await q.ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var result = users
            .Select(u => new UserSummaryDto(
                u.Id,
                u.UserName,
                u.Email,
                u.EmailConfirmed,
                adminUserIds.Contains(u.Id),
                u.LockoutEnabled && u.LockoutEnd.HasValue && u.LockoutEnd.Value > now
            ))
            .ToList();

        LogUserListRetrieved(result.Count);
        return result;
    }

    [LoggerMessage(
        EventId = EventIds.UserListRetrieved,
        Level = LogLevel.Information,
        Message = "Retrieved {Count} users"
    )]
    private partial void LogUserListRetrieved(int count);

    public async Task<UserProfileDto?> GetUserProfileAsync(
        GetUserProfileQuery query,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(query.UserId);
        if (user is null)
            return null;

        return new UserProfileDto(user.Email, user.PhoneNumber);
    }

    public async Task<FindUserByEmailDto> FindUserIdByEmailAsync(
        FindUserByEmailQuery query,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByEmailAsync(query.Email);
        if (user is null)
            return new FindUserByEmailDto(null);
        return new FindUserByEmailDto(await userManager.GetUserIdAsync(user));
    }
}
