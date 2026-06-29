using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using See.Idp.Core.Dtos.Clients;
using See.Idp.Core.Dtos.Common;
using See.Idp.Core.Dtos.Users;
using See.Idp.Core.Services;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

public sealed partial class ApplicationSeedCommandService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IOpenIddictApplicationManager applicationManager,
    ILogger<ApplicationSeedCommandService> logger
) : IApplicationSeedCommandService
{
    public async Task<CreateIfMissingResult> CreateRoleIfMissingAsync(
        CreateRoleIfMissingCommand command,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.RoleName))
        {
            LogSeedCommandRejected(nameof(CreateRoleIfMissingAsync), "Role is required.");
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

        var error = JoinErrors(result);
        LogSeedCommandRejected(nameof(CreateRoleIfMissingAsync), error);
        return CreateIfMissingResult.Failure(error);
    }

    public async Task<CreateUserIfMissingResult> CreateUserIfMissingAsync(
        CreateUserIfMissingCommand command,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            LogSeedCommandRejected(nameof(CreateUserIfMissingAsync), "Email is required.");
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

        var error = JoinErrors(createResult);
        LogSeedCommandRejected(nameof(CreateUserIfMissingAsync), error);
        return CreateUserIfMissingResult.Failure(error);
    }

    public async Task<CreateIfMissingResult> AddUserToRoleIfMissingAsync(
        AddUserToRoleIfMissingCommand command,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            LogSeedCommandRejected(nameof(AddUserToRoleIfMissingAsync), "User id is required.");
            return CreateIfMissingResult.Failure("User id is required.");
        }

        if (string.IsNullOrWhiteSpace(command.RoleName))
        {
            LogSeedCommandRejected(nameof(AddUserToRoleIfMissingAsync), "Role is required.");
            return CreateIfMissingResult.Failure("Role is required.");
        }

        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
        {
            LogSeedCommandRejected(
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

        var addResult = await userManager.AddToRoleAsync(user, command.RoleName);
        if (addResult.Succeeded)
        {
            LogUserAddedToRole(user.Id, command.RoleName);
            return CreateIfMissingResult.CreatedNew();
        }

        var error = JoinErrors(addResult);
        LogSeedCommandRejected(nameof(AddUserToRoleIfMissingAsync), error);
        return CreateIfMissingResult.Failure(error);
    }

    public async Task<CreateIfMissingResult> CreateClientIfMissingAsync(
        CreateClientIfMissingCommand command,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.ClientId))
        {
            LogSeedCommandRejected(nameof(CreateClientIfMissingAsync), "Client ID is required.");
            return CreateIfMissingResult.Failure("Client ID is required.");
        }

        if (await applicationManager.FindByClientIdAsync(command.ClientId, ct) is not null)
        {
            LogClientAlreadyExists(command.ClientId);
            return CreateIfMissingResult.AlreadyExists();
        }

        var normalizedSecret = string.IsNullOrWhiteSpace(command.ClientSecret)
            ? null
            : command.ClientSecret;

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = command.ClientId,
            ClientSecret = normalizedSecret,
            ClientType = normalizedSecret is null
                ? OpenIddictConstants.ClientTypes.Public
                : OpenIddictConstants.ClientTypes.Confidential,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            DisplayName = command.DisplayName,
        };

        foreach (var redirectUri in command.RedirectUris)
        {
            if (string.IsNullOrWhiteSpace(redirectUri))
                continue;

            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
            {
                var error =
                    $"Invalid redirect URI '{redirectUri}' for client '{command.ClientId}'.";
                LogSeedCommandRejected(nameof(CreateClientIfMissingAsync), error);
                return CreateIfMissingResult.Failure(error);
            }

            descriptor.RedirectUris.Add(uri);
        }

        foreach (var uri in command.PostLogoutRedirectUris)
        {
            if (
                !string.IsNullOrWhiteSpace(uri)
                && Uri.TryCreate(uri, UriKind.Absolute, out var parsed)
            )
                descriptor.PostLogoutRedirectUris.Add(parsed);
        }

        foreach (var permission in command.AdditionalPermissions)
        {
            if (!string.IsNullOrWhiteSpace(permission))
                descriptor.Permissions.Add(permission);
        }

        await applicationManager.CreateAsync(descriptor, ct);
        LogClientCreated(command.ClientId);
        return CreateIfMissingResult.CreatedNew();
    }

    private static string JoinErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(e => e.Description));

    [LoggerMessage(
        EventId = EventIds.SeedingRole,
        Level = LogLevel.Information,
        Message = "Seeding role: {RoleName}"
    )]
    private partial void LogSeedingRole(string roleName);

    [LoggerMessage(
        EventId = EventIds.RoleSeeded,
        Level = LogLevel.Information,
        Message = "Role seeded: {RoleName}"
    )]
    private partial void LogRoleCreated(string roleName);

    [LoggerMessage(
        EventId = EventIds.RoleAlreadyExists,
        Level = LogLevel.Information,
        Message = "Role already exists: {RoleName}"
    )]
    private partial void LogRoleAlreadyExists(string roleName);

    [LoggerMessage(
        EventId = EventIds.UserSeeded,
        Level = LogLevel.Information,
        Message = "User seeded: {UserId} ({Email})"
    )]
    private partial void LogUserCreated(string userId, string email);

    [LoggerMessage(
        EventId = EventIds.UserAlreadyExists,
        Level = LogLevel.Information,
        Message = "User already exists: {UserId} ({Email})"
    )]
    private partial void LogUserAlreadyExists(string userId, string email);

    [LoggerMessage(
        EventId = EventIds.UserAddedToRole,
        Level = LogLevel.Information,
        Message = "User {UserId} added to role {RoleName}"
    )]
    private partial void LogUserAddedToRole(string userId, string roleName);

    [LoggerMessage(
        EventId = EventIds.UserAlreadyInRole,
        Level = LogLevel.Information,
        Message = "User already in role: {UserId} -> {RoleName}"
    )]
    private partial void LogUserAlreadyInRole(string userId, string roleName);

    [LoggerMessage(
        EventId = EventIds.ClientSeeded,
        Level = LogLevel.Information,
        Message = "Client seeded: {ClientId}"
    )]
    private partial void LogClientCreated(string clientId);

    [LoggerMessage(
        EventId = EventIds.ClientAlreadyExists,
        Level = LogLevel.Information,
        Message = "Client already exists: {ClientId}"
    )]
    private partial void LogClientAlreadyExists(string clientId);

    [LoggerMessage(
        EventId = EventIds.UserCommandRejected,
        Level = LogLevel.Warning,
        Message = "Seed command {CommandName} rejected: {Reason}"
    )]
    private partial void LogSeedCommandRejected(string commandName, string reason);
}
