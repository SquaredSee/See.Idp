using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using See.Idp.Web.Logging;

namespace See.Idp.Web.Services;

public partial class UserSeeder(IServiceProvider serviceProvider, ILogger<UserSeeder> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        const string email = "test@seeidp.com";

        if (await userManager.FindByEmailAsync(email) is null)
        {
            LogSeedingUser(email);

            var user = new IdentityUser
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
}
