using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using See.Idp.Web.Logging;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace See.Idp.Web.Services;

/// <summary>
///     Hosted Service that seeds a client application into the OpenIddict database on application startup.
/// </summary>
public partial class UiClientSeeder(
    IServiceProvider serviceProvider,
    ILogger<UiClientSeeder> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        const string clientId = "seeid-razor-client";

        if (await manager.FindByClientIdAsync(clientId, cancellationToken) is null)
        {
            LogSeedingClient(logger, clientId);

            await manager.CreateAsync(
                new OpenIddictApplicationDescriptor
                {
                    ClientId = clientId,
                    ClientSecret = "seeid-razor-client-secret",
                    DisplayName = "Razor Pages Client",
                    RedirectUris = { new Uri("https://localhost:7001/callback") },
                    Permissions =
                    {
                        Permissions.Endpoints.Authorization,
                        Permissions.Endpoints.Token,
                        Permissions.GrantTypes.AuthorizationCode,
                        Permissions.ResponseTypes.Code,
                        Permissions.Scopes.Profile,
                        Permissions.Scopes.Email,
                        Permissions.Scopes.Roles,
                    },
                },
                cancellationToken
            );

            LogClientSeeded(logger, clientId);
        }
        else
        {
            LogClientAlreadyExists(logger, clientId);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        EventId = EventIds.SeedingClient,
        Level = LogLevel.Information,
        Message = "Seeding client: {ClientId}"
    )]
    private static partial void LogSeedingClient(ILogger logger, string clientId);

    [LoggerMessage(
        EventId = EventIds.ClientSeeded,
        Level = LogLevel.Information,
        Message = "Client seeded successfully: {ClientId}"
    )]
    private static partial void LogClientSeeded(ILogger logger, string clientId);

    [LoggerMessage(
        EventId = EventIds.ClientAlreadyExists,
        Level = LogLevel.Information,
        Message = "Client already exists, skipping: {ClientId}"
    )]
    private static partial void LogClientAlreadyExists(ILogger logger, string clientId);
}
