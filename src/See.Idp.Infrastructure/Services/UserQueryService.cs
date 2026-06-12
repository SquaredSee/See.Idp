using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using See.Idp.Core.Auth;
using See.Idp.Core.Dtos.Users;
using See.Idp.Core.Services.Users;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

public sealed partial class UserQueryService(
    UserManager<ApplicationUser> userManager,
    ILogger<UserQueryService> logger
) : IUserQueryService
{
    public async Task<IReadOnlyList<UserSummaryDto>> ListUsersAsync(
        ListUsersQuery query,
        CancellationToken ct = default
    )
    {
        // TODO: Filtering and Paging is currently done in-memory which is not ideal for large datasets. Consider EF Core replacement.
        var users = StreamUsersAsync(ct);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.Trim();
            users = users.Where(u =>
                (
                    u.Email != null
                    && u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                )
                || (
                    u.UserName != null
                    && u.UserName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                )
            );
        }

        users = users.OrderBy(u => u.Email).ThenBy(u => u.UserName);

        if (query.Skip > 0)
            users = users.Skip(query.Skip);
        if (query.Take is > 0)
            users = users.Take(query.Take.Value);

        var result = await users.ToListAsync(ct);
        LogUserListRetrieved(result.Count);
        return result;
    }

    private async IAsyncEnumerable<UserSummaryDto> StreamUsersAsync(
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        var adminUsers = await userManager.GetUsersInRoleAsync(Roles.Admin);
        var adminUserIds = new HashSet<string>(
            adminUsers.Select(u => u.Id),
            StringComparer.Ordinal
        );

        await foreach (
            var user in userManager.Users.AsNoTracking().AsAsyncEnumerable().WithCancellation(ct)
        )
        {
            var isAdmin = adminUserIds.Contains(user.Id);
            var isLocked =
                user.LockoutEnabled
                && user.LockoutEnd.HasValue
                && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

            yield return new UserSummaryDto(
                UserId: user.Id,
                UserName: user.UserName,
                Email: user.Email,
                EmailConfirmed: user.EmailConfirmed,
                IsAdmin: isAdmin,
                IsLockedOut: isLocked
            );
        }
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

    public async Task<string?> FindUserIdByEmailAsync(
        FindUserByEmailQuery query,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByEmailAsync(query.Email);
        return user is null ? null : await userManager.GetUserIdAsync(user);
    }

    public async Task<string?> GenerateEmailConfirmationTokenAsync(
        GenerateEmailConfirmationTokenQuery query,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(query.UserId);
        if (user is null)
            return null;

        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        LogUserEmailConfirmationTokenGenerated(query.UserId);
        return encodedCode;
    }

    [LoggerMessage(
        EventId = EventIds.UserEmailConfirmationTokenGenerated,
        Level = LogLevel.Information,
        Message = "Email confirmation token generated for user {UserId}"
    )]
    private partial void LogUserEmailConfirmationTokenGenerated(string userId);
}
