using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using See.Idp.Core.Dtos;
using See.Idp.Core.Services;

namespace See.Idp.Infrastructure.Services;

public sealed class UserManagementService(UserManager<IdentityUser> userManager)
    : IUserManagementService
{
    private const string AdminRole = "admin";
    private static readonly DateTimeOffset LockoutUntil = DateTimeOffset.UtcNow.AddYears(100);

    public async Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(
        string? currentUserId,
        CancellationToken ct = default
    )
    {
        var users = await userManager
            .Users.OrderBy(u => u.Email)
            .ThenBy(u => u.UserName)
            .ToListAsync(ct);

        var result = new List<AdminUserDto>(users.Count);

        foreach (var user in users)
        {
            var isAdmin = await userManager.IsInRoleAsync(user, AdminRole);
            var isLocked =
                user.LockoutEnabled
                && user.LockoutEnd.HasValue
                && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

            result.Add(
                new AdminUserDto(
                    UserId: user.Id,
                    UserName: user.UserName,
                    Email: user.Email,
                    EmailConfirmed: user.EmailConfirmed,
                    IsAdmin: isAdmin,
                    IsLockedOut: isLocked,
                    IsCurrentUser: string.Equals(user.Id, currentUserId, StringComparison.Ordinal)
                )
            );
        }

        return result;
    }

    public async Task<UserManagementActionResult> ToggleAdminAsync(
        string targetUserId,
        string? currentUserId,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            return UserManagementActionResult.Failure("User id is required.");
        }

        var user = await userManager.FindByIdAsync(targetUserId);
        if (user is null)
        {
            return UserManagementActionResult.Failure("User not found.");
        }

        var isAdmin = await userManager.IsInRoleAsync(user, AdminRole);

        if (isAdmin && string.Equals(user.Id, currentUserId, StringComparison.Ordinal))
        {
            return UserManagementActionResult.Failure("You cannot remove your own admin role.");
        }

        IdentityResult result;
        if (isAdmin)
        {
            result = await userManager.RemoveFromRoleAsync(user, AdminRole);
            if (result.Succeeded)
            {
                return UserManagementActionResult.Success(
                    $"Removed admin role from {DisplayName(user)}."
                );
            }
        }
        else
        {
            result = await userManager.AddToRoleAsync(user, AdminRole);
            if (result.Succeeded)
            {
                return UserManagementActionResult.Success(
                    $"Granted admin role to {DisplayName(user)}."
                );
            }
        }

        return UserManagementActionResult.Failure(
            string.Join(" ", result.Errors.Select(e => e.Description))
        );
    }

    public async Task<UserManagementActionResult> ToggleLockAsync(
        string targetUserId,
        string? currentUserId,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            return UserManagementActionResult.Failure("User id is required.");
        }

        var user = await userManager.FindByIdAsync(targetUserId);
        if (user is null)
        {
            return UserManagementActionResult.Failure("User not found.");
        }

        if (string.Equals(user.Id, currentUserId, StringComparison.Ordinal))
        {
            return UserManagementActionResult.Failure("You cannot lock your own account.");
        }

        if (!user.LockoutEnabled)
        {
            user.LockoutEnabled = true;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return UserManagementActionResult.Failure(
                    string.Join(" ", updateResult.Errors.Select(e => e.Description))
                );
            }
        }

        var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
        var result = await userManager.SetLockoutEndDateAsync(user, isLocked ? null : LockoutUntil);

        if (result.Succeeded)
        {
            return UserManagementActionResult.Success(
                isLocked ? $"Unlocked {DisplayName(user)}." : $"Locked {DisplayName(user)}."
            );
        }

        return UserManagementActionResult.Failure(
            string.Join(" ", result.Errors.Select(e => e.Description))
        );
    }

    public async Task<UserManagementActionResult> DeleteUserAsync(
        string targetUserId,
        string? currentUserId,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            return UserManagementActionResult.Failure("User id is required.");
        }

        var user = await userManager.FindByIdAsync(targetUserId);
        if (user is null)
        {
            return UserManagementActionResult.Failure("User not found.");
        }

        if (string.Equals(user.Id, currentUserId, StringComparison.Ordinal))
        {
            return UserManagementActionResult.Failure("You cannot delete your own account.");
        }

        var isAdmin = await userManager.IsInRoleAsync(user, AdminRole);
        if (isAdmin)
        {
            var admins = await userManager.GetUsersInRoleAsync(AdminRole);
            if (admins.Count <= 1)
            {
                return UserManagementActionResult.Failure("Cannot delete the last admin user.");
            }
        }

        var result = await userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            return UserManagementActionResult.Success($"Deleted {DisplayName(user)}.");
        }

        return UserManagementActionResult.Failure(
            string.Join(" ", result.Errors.Select(e => e.Description))
        );
    }

    private static string DisplayName(IdentityUser user) => user.Email ?? user.UserName ?? user.Id;
}
