using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using See.Idp.Core.Auth;
using See.Idp.Core.Dtos.Common;
using See.Idp.Core.Dtos.Users;
using See.Idp.Core.Services.Users;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

public sealed partial class UserCommandService(
    UserManager<ApplicationUser> userManager,
    ILogger<UserCommandService> logger
) : IUserCommandService
{
    private static DateTimeOffset LockoutUntil => DateTimeOffset.UtcNow.AddYears(100);

    public async Task<CommandResult> ToggleAdminAsync(
        ToggleUserAdminCommand command,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.TargetUserId))
        {
            LogUserCommandRejected(nameof(ToggleAdminAsync), "User id is required.");
            return CommandResult.Failure("User id is required.");
        }

        var user = await userManager.FindByIdAsync(command.TargetUserId);
        if (user is null)
        {
            LogUserCommandRejected(nameof(ToggleAdminAsync), "User not found.");
            return CommandResult.Failure("User not found.");
        }

        var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);

        if (isAdmin && string.Equals(user.Id, command.CurrentUserId, StringComparison.Ordinal))
        {
            LogUserCommandRejected(nameof(ToggleAdminAsync), "Cannot remove your own admin role.");
            return CommandResult.Failure("You cannot remove your own admin role.");
        }

        if (isAdmin)
        {
            var removeResult = await userManager.RemoveFromRoleAsync(user, Roles.Admin);
            if (removeResult.Succeeded)
            {
                LogUserAdminRemoved(user.Id);
                return CommandResult.Success($"Removed admin role from {DisplayName(user)}.");
            }

            LogUserCommandRejected(nameof(ToggleAdminAsync), JoinErrors(removeResult));
            return CommandResult.Failure(JoinErrors(removeResult));
        }
        else
        {
            var addResult = await userManager.AddToRoleAsync(user, Roles.Admin);
            if (addResult.Succeeded)
            {
                LogUserAdminGranted(user.Id);
                return CommandResult.Success($"Granted admin role to {DisplayName(user)}.");
            }

            LogUserCommandRejected(nameof(ToggleAdminAsync), JoinErrors(addResult));
            return CommandResult.Failure(JoinErrors(addResult));
        }
    }

    public async Task<CommandResult> ToggleLockAsync(
        ToggleUserLockCommand command,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.TargetUserId))
        {
            LogUserCommandRejected(nameof(ToggleLockAsync), "User id is required.");
            return CommandResult.Failure("User id is required.");
        }

        var user = await userManager.FindByIdAsync(command.TargetUserId);
        if (user is null)
        {
            LogUserCommandRejected(nameof(ToggleLockAsync), "User not found.");
            return CommandResult.Failure("User not found.");
        }

        if (string.Equals(user.Id, command.CurrentUserId, StringComparison.Ordinal))
        {
            LogUserCommandRejected(nameof(ToggleLockAsync), "Cannot lock your own account.");
            return CommandResult.Failure("You cannot lock your own account.");
        }

        if (!user.LockoutEnabled)
        {
            user.LockoutEnabled = true;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                LogUserCommandRejected(nameof(ToggleLockAsync), JoinErrors(updateResult));
                return CommandResult.Failure(JoinErrors(updateResult));
            }
        }

        var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
        var result = await userManager.SetLockoutEndDateAsync(user, isLocked ? null : LockoutUntil);

        if (result.Succeeded)
        {
            if (isLocked)
                LogUserUnlocked(user.Id);
            else
                LogUserLocked(user.Id);

            return CommandResult.Success(
                isLocked ? $"Unlocked {DisplayName(user)}." : $"Locked {DisplayName(user)}."
            );
        }

        LogUserCommandRejected(nameof(ToggleLockAsync), JoinErrors(result));
        return CommandResult.Failure(JoinErrors(result));
    }

    public async Task<CommandResult> DeleteUserAsync(
        DeleteUserCommand command,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.TargetUserId))
        {
            LogUserCommandRejected(nameof(DeleteUserAsync), "User id is required.");
            return CommandResult.Failure("User id is required.");
        }

        var user = await userManager.FindByIdAsync(command.TargetUserId);
        if (user is null)
        {
            LogUserCommandRejected(nameof(DeleteUserAsync), "User not found.");
            return CommandResult.Failure("User not found.");
        }

        if (string.Equals(user.Id, command.CurrentUserId, StringComparison.Ordinal))
        {
            LogUserCommandRejected(nameof(DeleteUserAsync), "Cannot delete your own account.");
            return CommandResult.Failure("You cannot delete your own account.");
        }

        var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);
        if (isAdmin)
        {
            var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
            if (admins.Count <= 1)
            {
                LogUserCommandRejected(
                    nameof(DeleteUserAsync),
                    "Cannot delete the last admin user."
                );
                return CommandResult.Failure("Cannot delete the last admin user.");
            }
        }

        var result = await userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            LogUserDeleted(user.Id);
            return CommandResult.Success($"Deleted {DisplayName(user)}.");
        }

        LogUserCommandRejected(nameof(DeleteUserAsync), JoinErrors(result));
        return CommandResult.Failure(JoinErrors(result));
    }

    public async Task<CommandResult> UpdatePhoneNumberAsync(
        UpdatePhoneNumberCommand command,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
        {
            LogUserCommandRejected(
                nameof(UpdatePhoneNumberAsync),
                $"User '{command.UserId}' not found."
            );
            return CommandResult.Failure($"Unable to load user with ID '{command.UserId}'.");
        }

        var result = await userManager.SetPhoneNumberAsync(user, command.PhoneNumber);
        if (result.Succeeded)
        {
            LogUserPhoneNumberUpdated(command.UserId);
            return CommandResult.Success();
        }

        var reason = JoinErrors(result);
        LogUserCommandRejected(nameof(UpdatePhoneNumberAsync), reason);
        return CommandResult.Failure("Unable to update phone number.");
    }

    private static string DisplayName(IdentityUser user) => user.Email ?? user.UserName ?? user.Id;

    private static string JoinErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(e => e.Description));

    [LoggerMessage(
        EventId = EventIds.UserAdminGranted,
        Level = LogLevel.Information,
        Message = "Admin role granted to user: {UserId}"
    )]
    private partial void LogUserAdminGranted(string userId);

    [LoggerMessage(
        EventId = EventIds.UserAdminRemoved,
        Level = LogLevel.Information,
        Message = "Admin role removed from user: {UserId}"
    )]
    private partial void LogUserAdminRemoved(string userId);

    [LoggerMessage(
        EventId = EventIds.UserAlreadyInRole,
        Level = LogLevel.Information,
        Message = "User already in role: {UserId} -> {RoleName}"
    )]
    private partial void LogUserAlreadyInRole(string userId, string roleName);

    [LoggerMessage(
        EventId = EventIds.UserLocked,
        Level = LogLevel.Information,
        Message = "User locked: {UserId}"
    )]
    private partial void LogUserLocked(string userId);

    [LoggerMessage(
        EventId = EventIds.UserUnlocked,
        Level = LogLevel.Information,
        Message = "User unlocked: {UserId}"
    )]
    private partial void LogUserUnlocked(string userId);

    [LoggerMessage(
        EventId = EventIds.UserDeleted,
        Level = LogLevel.Information,
        Message = "User deleted: {UserId}"
    )]
    private partial void LogUserDeleted(string userId);

    [LoggerMessage(
        EventId = EventIds.UserCommandRejected,
        Level = LogLevel.Warning,
        Message = "User command {CommandName} rejected: {Reason}"
    )]
    private partial void LogUserCommandRejected(string commandName, string reason);

    [LoggerMessage(
        EventId = EventIds.UserPhoneNumberUpdated,
        Level = LogLevel.Information,
        Message = "Phone number updated for user {UserId}"
    )]
    private partial void LogUserPhoneNumberUpdated(string userId);
}
