using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using See.Idp.Core.Configuration;
using See.Idp.Core.Dtos.Clients;
using See.Idp.Core.Dtos.Users;
using See.Idp.Core.Services;
using See.Idp.Core.Services.Clients;
using See.Idp.Core.Services.Users;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

public sealed partial class ConfigurationApplicationInitializer(
    IUserCommandService userCommandService,
    IClientCommandService clientCommandService,
    IOptions<InitializationOptions> options,
    ILogger<ConfigurationApplicationInitializer> logger
) : IApplicationInitializer
{
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var config = options.Value;

        if (!config.Enabled)
        {
            LogInitializationDisabled();
            return;
        }

        var allRoles = new HashSet<string>(config.Roles, StringComparer.OrdinalIgnoreCase);
        foreach (var user in config.Users)
        {
            foreach (var role in user.Roles)
            {
                allRoles.Add(role);
            }
        }

        await SeedRolesAsync([.. allRoles], ct);

        foreach (var user in config.Users)
        {
            await SeedUserAsync(user, ct);
        }

        foreach (var client in config.Clients)
        {
            await SeedClientAsync(client, ct);
        }
    }

    private async Task SeedRolesAsync(List<string> roles, CancellationToken ct)
    {
        foreach (var role in roles)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                continue;
            }

            LogSeedingRole(role);

            var result = await userCommandService.CreateRoleIfMissingAsync(
                new CreateRoleIfMissingCommand(role),
                ct
            );
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed role '{role}': {result.Error ?? "Unknown error."}"
                );
            }

            if (result.Created)
                LogRoleSeeded(role);
            else
                LogRoleAlreadyExists(role);
        }
    }

    private async Task SeedUserAsync(SeedUserOptions user, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("Initialization users require a non-empty email.");
        }

        LogSeedingUser(user.Email);

        var ensureUserResult = await userCommandService.CreateUserIfMissingAsync(
            new CreateUserIfMissingCommand(user.Email, user.Password, user.EmailConfirmed),
            ct
        );

        if (!ensureUserResult.Succeeded || string.IsNullOrWhiteSpace(ensureUserResult.UserId))
        {
            throw new InvalidOperationException(
                $"Failed to seed user '{user.Email}': {ensureUserResult.Error ?? "Unknown error."}"
            );
        }

        if (ensureUserResult.Created)
            LogUserSeeded(user.Email);
        else
            LogUserAlreadyExists(user.Email);

        foreach (var role in user.Roles)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                continue;
            }

            var addRoleResult = await userCommandService.AddUserToRoleIfMissingAsync(
                new AddUserToRoleIfMissingCommand(ensureUserResult.UserId, role),
                ct
            );

            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed adding role '{role}' to '{user.Email}': {addRoleResult.Error ?? "Unknown error."}"
                );
            }

            if (addRoleResult.Created)
                LogUserAddedToRole(user.Email, role);
        }
    }

    private async Task SeedClientAsync(SeedClientOptions client, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(client.ClientId))
        {
            throw new InvalidOperationException(
                "Initialization clients require a non-empty client id."
            );
        }

        LogSeedingClient(client.ClientId);

        var ensureClientResult = await clientCommandService.CreateClientIfMissingAsync(
            new CreateClientIfMissingCommand(
                client.ClientId,
                client.ClientSecret,
                client.DisplayName,
                client.RedirectUris,
                client.PostLogoutRedirectUris,
                client.Permissions
            ),
            ct
        );

        if (!ensureClientResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed client '{client.ClientId}': {ensureClientResult.Error ?? "Unknown error."}"
            );
        }

        if (ensureClientResult.Created)
            LogClientSeeded(client.ClientId);
        else
            LogClientAlreadyExists(client.ClientId);
    }

    [LoggerMessage(
        EventId = EventIds.InitializationDisabled,
        Level = LogLevel.Information,
        Message = "Initialization is disabled. Skipping startup seeding."
    )]
    private partial void LogInitializationDisabled();

    [LoggerMessage(
        EventId = EventIds.SeedingClient,
        Level = LogLevel.Information,
        Message = "Seeding client: {ClientId}"
    )]
    private partial void LogSeedingClient(string clientId);

    [LoggerMessage(
        EventId = EventIds.ClientSeeded,
        Level = LogLevel.Information,
        Message = "Client seeded successfully: {ClientId}"
    )]
    private partial void LogClientSeeded(string clientId);

    [LoggerMessage(
        EventId = EventIds.ClientAlreadyExists,
        Level = LogLevel.Information,
        Message = "Client already exists, skipping: {ClientId}"
    )]
    private partial void LogClientAlreadyExists(string clientId);

    [LoggerMessage(
        EventId = EventIds.SeedingUser,
        Level = LogLevel.Information,
        Message = "Seeding user: {Email}"
    )]
    private partial void LogSeedingUser(string email);

    [LoggerMessage(
        EventId = EventIds.UserSeeded,
        Level = LogLevel.Information,
        Message = "User seeded successfully: {Email}"
    )]
    private partial void LogUserSeeded(string email);

    [LoggerMessage(
        EventId = EventIds.UserAlreadyExists,
        Level = LogLevel.Information,
        Message = "User already exists, skipping: {Email}"
    )]
    private partial void LogUserAlreadyExists(string email);

    [LoggerMessage(
        EventId = EventIds.UserAddedToRole,
        Level = LogLevel.Information,
        Message = "User {Email} added to role {Role}"
    )]
    private partial void LogUserAddedToRole(string email, string role);

    [LoggerMessage(
        EventId = EventIds.SeedingRole,
        Level = LogLevel.Information,
        Message = "Seeding role: {Role}"
    )]
    private partial void LogSeedingRole(string role);

    [LoggerMessage(
        EventId = EventIds.RoleSeeded,
        Level = LogLevel.Information,
        Message = "Role seeded successfully: {Role}"
    )]
    private partial void LogRoleSeeded(string role);

    [LoggerMessage(
        EventId = EventIds.RoleAlreadyExists,
        Level = LogLevel.Information,
        Message = "Role already exists, skipping: {Role}"
    )]
    private partial void LogRoleAlreadyExists(string role);
}
