using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using See.Idp.Core.Configuration;
using See.Idp.Core.Logging;
using See.Idp.Core.Services;

namespace See.Idp.Infrastructure.Services;

public partial class ConfigurationApplicationInitializer(
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOpenIddictApplicationManager applicationManager,
    IOptions<InitializationOptions> options,
    ILogger<ConfigurationApplicationInitializer> logger
) : IApplicationInitializer
{
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var config = options.Value;

        if (!config.Enabled)
        {
            logger.LogInformation("Initialization is disabled. Skipping startup seeding.");
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

        await SeedRolesAsync([.. allRoles]);

        foreach (var user in config.Users)
        {
            await SeedUserAsync(user);
        }

        foreach (var client in config.Clients)
        {
            await SeedClientAsync(client, ct);
        }
    }

    private async Task SeedRolesAsync(List<string> roles)
    {
        foreach (var role in roles)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                continue;
            }

            LogSeedingRole(role);

            if (await roleManager.RoleExistsAsync(role))
            {
                LogRoleAlreadyExists(role);
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed role '{role}': {JoinErrors(result)}"
                );
            }

            LogRoleSeeded(role);
        }
    }

    private async Task SeedUserAsync(SeedUserOptions user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("Initialization users require a non-empty email.");
        }

        var existingUser = await userManager.FindByEmailAsync(user.Email);
        if (existingUser is null)
        {
            LogSeedingUser(user.Email);

            var newUser = new IdentityUser
            {
                UserName = user.Email,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
            };

            var createResult = string.IsNullOrWhiteSpace(user.Password)
                ? await userManager.CreateAsync(newUser)
                : await userManager.CreateAsync(newUser, user.Password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed user '{user.Email}': {JoinErrors(createResult)}"
                );
            }

            existingUser = newUser;
            LogUserSeeded(user.Email);
        }
        else
        {
            LogUserAlreadyExists(user.Email);
        }

        foreach (var role in user.Roles)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                continue;
            }

            if (await userManager.IsInRoleAsync(existingUser, role))
            {
                continue;
            }

            var addRoleResult = await userManager.AddToRoleAsync(existingUser, role);
            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed adding role '{role}' to '{user.Email}': {JoinErrors(addRoleResult)}"
                );
            }

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

        if (await applicationManager.FindByClientIdAsync(client.ClientId, ct) is not null)
        {
            LogClientAlreadyExists(client.ClientId);
            return;
        }

        LogSeedingClient(client.ClientId);

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = client.ClientId,
            ClientSecret = string.IsNullOrWhiteSpace(client.ClientSecret)
                ? null
                : client.ClientSecret,
            DisplayName = client.DisplayName,
        };

        foreach (var redirectUri in client.RedirectUris)
        {
            if (string.IsNullOrWhiteSpace(redirectUri))
            {
                continue;
            }

            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException(
                    $"Invalid redirect URI '{redirectUri}' for client '{client.ClientId}'."
                );
            }

            descriptor.RedirectUris.Add(uri);
        }

        foreach (var permission in client.Permissions)
        {
            if (!string.IsNullOrWhiteSpace(permission))
            {
                descriptor.Permissions.Add(permission);
            }
        }

        await applicationManager.CreateAsync(descriptor, ct);
        LogClientSeeded(client.ClientId);
    }

    private static string JoinErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(e => e.Description));

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
