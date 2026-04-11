using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using See.Idp.Core.Logging;
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

        // Register the Razor Pages client application if it doesn't exist already.
        // The client represents the user-facing web application, which uses authorization code flow with PKCE.

        const string razorClientId = "seeid-razor-client";

        if (await manager.FindByClientIdAsync(razorClientId, cancellationToken) is null)
        {
            LogSeedingClient(logger, razorClientId);

            await manager.CreateAsync(
                new OpenIddictApplicationDescriptor
                {
                    ClientId = razorClientId,
                    ClientSecret = "seeid-razor-client-secret",
                    DisplayName = "SeeId User Interface",
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

            LogClientSeeded(logger, razorClientId);
        }
        else
        {
            LogClientAlreadyExists(logger, razorClientId);
        }

        // Register the service worker client application if it doesn't exist already.
        // The client represents a machine-to-machine application, which uses client credentials flow.

        const string serviceWorkerClientId = "seeid-service-worker-client";

        if (await manager.FindByClientIdAsync(serviceWorkerClientId, cancellationToken) is null)
        {
            LogSeedingClient(logger, serviceWorkerClientId);

            await manager.CreateAsync(
                new OpenIddictApplicationDescriptor
                {
                    ClientId = serviceWorkerClientId,
                    ClientSecret = "seeid-service-worker-client-secret",
                    DisplayName = "SeeId Service Worker",
                    Permissions =
                    {
                        Permissions.Endpoints.Token,
                        Permissions.GrantTypes.ClientCredentials,
                    },
                },
                cancellationToken
            );

            LogClientSeeded(logger, serviceWorkerClientId);
        }
        else
        {
            LogClientAlreadyExists(logger, serviceWorkerClientId);
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
