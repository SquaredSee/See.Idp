using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using See.Idp.Core.Logging;
using See.Idp.Web.Auth;

namespace See.Idp.Web.Services;

/// <summary>
///     Hosted service that seeds a test user into the Identity database if it doesn't already exist.
/// </summary>
public partial class UserSeeder(IServiceProvider serviceProvider, ILogger<UserSeeder> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await SeedRolesAsync(roleManager, [Roles.Admin]);
        await SeedUserAsync(userManager, "test@seeidp.com", [], "Test123!");
        await SeedUserAsync(userManager, "colton.crouch96@gmail.com", [Roles.Admin], "Test123!");
    }

    private async Task SeedUserAsync(
        UserManager<IdentityUser> userManager,
        string email,
        List<string> roles,
        string? password = null
    )
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            LogSeedingUser(email);

            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
            };

            var createResult = string.IsNullOrWhiteSpace(password)
                ? await userManager.CreateAsync(user)
                : await userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed user '{email}': {string.Join("; ", createResult.Errors.Select(e => e.Description))}"
                );
            }
            LogUserSeeded(email);
        }
        else
        {
            LogUserAlreadyExists(email);
        }

        foreach (var role in roles)
        {
            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
                LogUserAddedToRole(email, role);
            }
        }
    }

    private async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, List<string> roles)
    {
        foreach (var role in roles)
        {
            LogSeedingRole(role);
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                LogRoleSeeded(role);
            }
            else
            {
                LogRoleAlreadyExists(role);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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
