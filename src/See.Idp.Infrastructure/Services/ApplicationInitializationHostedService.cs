using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using See.Idp.Core.Services;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

public sealed partial class ApplicationInitializationHostedService(
    IServiceProvider serviceProvider,
    ILogger<ApplicationInitializationHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogInitializationHostedServiceStarting();

        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var initializer = scope.ServiceProvider.GetRequiredService<IApplicationInitializer>();

            await initializer.InitializeAsync(cancellationToken);
            LogInitializationHostedServiceCompleted();
        }
        catch (Exception ex)
        {
            LogInitializationHostedServiceFailed(ex);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        EventId = EventIds.InitializationHostedServiceStarting,
        Level = LogLevel.Information,
        Message = "Application initialization hosted service starting"
    )]
    private partial void LogInitializationHostedServiceStarting();

    [LoggerMessage(
        EventId = EventIds.InitializationHostedServiceCompleted,
        Level = LogLevel.Information,
        Message = "Application initialization hosted service completed"
    )]
    private partial void LogInitializationHostedServiceCompleted();

    [LoggerMessage(
        EventId = EventIds.InitializationHostedServiceFailed,
        Level = LogLevel.Error,
        Message = "Application initialization hosted service failed"
    )]
    private partial void LogInitializationHostedServiceFailed(Exception ex);
}
