using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using See.Idp.Core.Auth;
using See.Idp.Core.Dtos.Common;
using See.Idp.Core.Dtos.Users;
using See.Idp.Core.Models;
using See.Idp.Core.Services.Users;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

public sealed partial class UserCommandService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ILogger<UserCommandService> logger
) : IUserCommandService
{
    private static readonly DateTimeOffset LockoutUntil = DateTimeOffset.UtcNow.AddYears(100);

    public async Task<CreateIfMissingResult> CreateRoleIfMissingAsync(
        CreateRoleIfMissingCommand command,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.RoleName))
        {
            LogUserCommandRejected(nameof(CreateRoleIfMissingAsync), "Role is required.");
            return CreateIfMissingResult.Failure("Role is required.");
        }

        if (await roleManager.RoleExistsAsync(command.RoleName))
        {
            LogRoleAlreadyExists(command.RoleName);
            return CreateIfMissingResult.AlreadyExists();
        }

        var result = await roleManager.CreateAsync(new ApplicationRole { Name = command.RoleName });
        if (result.Succeeded)
        {
            LogRoleCreated(command.RoleName);
            return CreateIfMissingResult.CreatedNew();
        }

        LogUserCommandRejected(nameof(CreateRoleIfMissingAsync), JoinErrors(result));
        return CreateIfMissingResult.Failure(JoinErrors(result));
    }

    public async Task<CreateUserIfMissingResult> CreateUserIfMissingAsync(
        CreateUserIfMissingCommand command,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            LogUserCommandRejected(nameof(CreateUserIfMissingAsync), "Email is required.");
            return CreateUserIfMissingResult.Failure("Email is required.");
        }

        var existingUser = await userManager.FindByEmailAsync(command.Email);
        if (existingUser is not null)
        {
            LogUserAlreadyExists(existingUser.Id, command.Email);
            return CreateUserIfMissingResult.AlreadyExists(existingUser.Id);
        }

        var user = new ApplicationUser
        {
            UserName = command.Email,
            Email = command.Email,
            EmailConfirmed = command.EmailConfirmed,
        };

        var createResult = string.IsNullOrWhiteSpace(command.Password)
            ? await userManager.CreateAsync(user)
            : await userManager.CreateAsync(user, command.Password);

        if (createResult.Succeeded)
        {
            LogUserCreated(user.Id, command.Email);
            return CreateUserIfMissingResult.CreatedNew(user.Id);
        }

        LogUserCommandRejected(nameof(CreateUserIfMissingAsync), JoinErrors(createResult));
        return CreateUserIfMissingResult.Failure(JoinErrors(createResult));
    }

    public async Task<CreateIfMissingResult> AddUserToRoleIfMissingAsync(
        AddUserToRoleIfMissingCommand command,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            LogUserCommandRejected(nameof(AddUserToRoleIfMissingAsync), "User id is required.");
            return CreateIfMissingResult.Failure("User id is required.");
        }

        if (string.IsNullOrWhiteSpace(command.RoleName))
        {
            LogUserCommandRejected(nameof(AddUserToRoleIfMissingAsync), "Role is required.");
            return CreateIfMissingResult.Failure("Role is required.");
        }

        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
        {
            LogUserCommandRejected(
                nameof(AddUserToRoleIfMissingAsync),
                $"User '{command.UserId}' not found."
            );
            return CreateIfMissingResult.Failure("User not found.");
        }

        if (await userManager.IsInRoleAsync(user, command.RoleName))
        {
            LogUserAlreadyInRole(user.Id, command.RoleName);
            return CreateIfMissingResult.AlreadyExists();
        }

        var addRoleResult = await userManager.AddToRoleAsync(user, command.RoleName);
        if (addRoleResult.Succeeded)
        {
            LogUserAddedToRole(user.Id, command.RoleName);
            return CreateIfMissingResult.CreatedNew();
        }

        LogUserCommandRejected(nameof(AddUserToRoleIfMissingAsync), JoinErrors(addRoleResult));
        return CreateIfMissingResult.Failure(JoinErrors(addRoleResult));
    }

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

        IdentityResult result;
        if (isAdmin)
        {
            result = await userManager.RemoveFromRoleAsync(user, Roles.Admin);
            if (result.Succeeded)
            {
                LogUserAdminRemoved(user.Id);
                return CommandResult.Success($"Removed admin role from {DisplayName(user)}.");
            }

            LogUserCommandRejected(nameof(ToggleAdminAsync), JoinErrors(result));
        }
        else
        {
            var addRoleResult = await AddUserToRoleIfMissingAsync(
                new AddUserToRoleIfMissingCommand(user.Id, Roles.Admin),
                ct
            );

            if (!addRoleResult.Succeeded)
            {
                LogUserCommandRejected(
                    nameof(ToggleAdminAsync),
                    addRoleResult.Error ?? "Unable to grant admin role."
                );
                return CommandResult.Failure(addRoleResult.Error ?? "Unable to grant admin role.");
            }

            if (addRoleResult.Created)
            {
                LogUserAdminGranted(user.Id);
                return CommandResult.Success($"Granted admin role to {DisplayName(user)}.");
            }

            LogUserAlreadyInRole(user.Id, Roles.Admin);
            return CommandResult.Success($"{DisplayName(user)} already has the admin role.");
        }

        return CommandResult.Failure(JoinErrors(result));
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

    private static string DisplayName(IdentityUser user) => user.Email ?? user.UserName ?? user.Id;

    private static string JoinErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(e => e.Description));

    [LoggerMessage(
        EventId = EventIds.RoleCreated,
        Level = LogLevel.Information,
        Message = "Role created: {RoleName}"
    )]
    private partial void LogRoleCreated(string roleName);

    [LoggerMessage(
        EventId = EventIds.RoleManagementAlreadyExists,
        Level = LogLevel.Information,
        Message = "Role already exists: {RoleName}"
    )]
    private partial void LogRoleAlreadyExists(string roleName);

    [LoggerMessage(
        EventId = EventIds.UserCreated,
        Level = LogLevel.Information,
        Message = "User created: {UserId} ({Email})"
    )]
    private partial void LogUserCreated(string userId, string email);

    [LoggerMessage(
        EventId = EventIds.UserManagementAlreadyExists,
        Level = LogLevel.Information,
        Message = "User already exists: {UserId} ({Email})"
    )]
    private partial void LogUserAlreadyExists(string userId, string email);

    [LoggerMessage(
        EventId = EventIds.UserAlreadyInRole,
        Level = LogLevel.Information,
        Message = "User already in role: {UserId} -> {RoleName}"
    )]
    private partial void LogUserAlreadyInRole(string userId, string roleName);

    [LoggerMessage(
        EventId = EventIds.UserRoleAdded,
        Level = LogLevel.Information,
        Message = "User added to role: {UserId} -> {RoleName}"
    )]
    private partial void LogUserAddedToRole(string userId, string roleName);

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

    [LoggerMessage(
        EventId = EventIds.UserPhoneNumberUpdated,
        Level = LogLevel.Information,
        Message = "Phone number updated for user {UserId}"
    )]
    private partial void LogUserPhoneNumberUpdated(string userId);
}
