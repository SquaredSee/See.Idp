using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using See.Idp.Web.Auth;
using See.Idp.Web.Logging;

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

        const string email = "test@seeidp.com";

        if (!await roleManager.RoleExistsAsync(Roles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
        }

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

            await userManager.CreateAsync(user, "Test1234!");
            LogUserSeeded(email);
        }
        else
        {
            LogUserAlreadyExists(email);
        }

        if (!await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            await userManager.AddToRoleAsync(user, Roles.Admin);
            LogUserAddedToRole(email, Roles.Admin);
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
}
