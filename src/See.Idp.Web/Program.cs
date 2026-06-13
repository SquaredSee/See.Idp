using System;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Resend;
using Scalar.AspNetCore;
using See.Idp.Core.Configuration;
using See.Idp.Core.Services;
using See.Idp.Core.Services.Auth;
using See.Idp.Core.Services.Clients;
using See.Idp.Core.Services.Users;
using See.Idp.Infrastructure;
using See.Idp.Infrastructure.Logging;
using See.Idp.Infrastructure.Services;
using See.Idp.Web.Auth;
using See.Idp.Web.Cors;
using See.Idp.Web.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Observability
builder
    .Services.AddOpenTelemetry()
    .ConfigureResource(r =>
        r.AddService(
            serviceName: "See.Idp",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString()
        )
    )
    .WithTracing(t =>
        t.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter()
    )
    .WithMetrics(m => m.AddAspNetCoreInstrumentation().AddOtlpExporter())
    .WithLogging(l => l.AddOtlpExporter());

builder.Services.Configure<OpenTelemetryLoggerOptions>(o =>
{
    o.IncludeFormattedMessage = true;
    o.IncludeScopes = true;
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Configure Entity Framework Core to use PostgreSQL.
    var connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    options.UseNpgsql(connectionString);

    // Register the entity sets needed by OpenIddict.
    // Note: use the generic overload if you need to replace the default OpenIddict entities.
    options.UseOpenIddict();
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder
    .Services.AddOpenIddict()
    // Register the OpenIddict core components.
    .AddCore(options =>
    {
        // Configure OpenIddict to use the Entity Framework Core stores and models.
        // Note: call ReplaceDefaultEntities() to replace the default entities.
        options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>();
    })
    // Register the OpenIddict server components.
    .AddServer(options =>
    {
        // Set the endpoints for the OpenIddict server.
        options
            .SetAuthorizationEndpointUris("connect/authorize")
            .SetTokenEndpointUris("connect/token")
            .SetEndSessionEndpointUris("connect/logout")
            .SetUserInfoEndpointUris("connect/userinfo");

        // Enable the supported flows.
        options
            .AllowAuthorizationCodeFlow()
            .RequireProofKeyForCodeExchange()
            .AllowClientCredentialsFlow()
            .AllowRefreshTokenFlow();

        // Register scopes that can be used in the authorization process.
        options.RegisterScopes(
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.Email,
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.OpenId,
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.Profile,
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.Roles
        );

        var aspNetCoreBuilder = options
            .UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough()
            .EnableUserInfoEndpointPassthrough();

        if (builder.Environment.IsDevelopment())
        {
            options
                .AddDevelopmentEncryptionCertificate()
                .AddDevelopmentSigningCertificate()
                .DisableAccessTokenEncryption();
            aspNetCoreBuilder.DisableTransportSecurityRequirement();
        }
        else
        {
            var signingKeyXml =
                builder.Configuration["OpenIddict:SigningKey"]
                ?? throw new InvalidOperationException(
                    "OpenIddict:SigningKey is required in production. "
                        + "Set it via the OPENIDDICT__SIGNINGKEY environment variable."
                );

            var encryptionKeyXml =
                builder.Configuration["OpenIddict:EncryptionKey"]
                ?? throw new InvalidOperationException(
                    "OpenIddict:EncryptionKey is required in production. "
                        + "Set it via the OPENIDDICT__ENCRYPTIONKEY environment variable."
                );

            using var signingRsa = RSA.Create();
            signingRsa.FromXmlString(signingKeyXml);
            options.AddSigningKey(
                new RsaSecurityKey(signingRsa.ExportParameters(includePrivateParameters: true))
            );

            using var encryptionRsa = RSA.Create();
            encryptionRsa.FromXmlString(encryptionKeyXml);
            options.AddEncryptionKey(
                new RsaSecurityKey(encryptionRsa.ExportParameters(includePrivateParameters: true))
            );
        }
    });

builder
    .Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorizationBuilder().AddAdminPortalPolicy();

// builder.Services.ConfigureApplicationCookie(options =>
// {
//     // Default paths for login, logout, and access denied.
//     options.LoginPath = "/Account/Login";
//     options.LogoutPath = "/Account/Logout";
//     options.AccessDeniedPath = "/Account/AccessDenied";
// });

// Options pattern configuration
builder.Services.Configure<InitializationOptions>(
    builder.Configuration.GetSection("Initialization")
);

// Application initialization
builder.Services.AddScoped<IApplicationInitializer, ConfigurationApplicationInitializer>();
builder.Services.AddHostedService<ApplicationInitializationHostedService>();

// Application services
builder.Services.AddScoped<IClientQueryService, ClientQueryService>();
builder.Services.AddScoped<IClientCommandService, ClientCommandService>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<IUserCommandService, UserCommandService>();
builder.Services.AddScoped<UserAccountService>();
builder.Services.AddScoped<IUserAuthenticationCommandService>(sp =>
    sp.GetRequiredService<UserAccountService>()
);
builder.Services.AddScoped<IUserPasswordCommandService>(sp =>
    sp.GetRequiredService<UserAccountService>()
);
builder.Services.AddScoped<IUserRegistrationCommandService, UserRegistrationService>();
builder.Services.AddScoped<TwoFactorService>();
builder.Services.AddScoped<ITwoFactorCommandService>(sp =>
    sp.GetRequiredService<TwoFactorService>()
);
builder.Services.AddScoped<ITwoFactorQueryService>(sp => sp.GetRequiredService<TwoFactorService>());
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IEmailSender<ApplicationUser>, NoOpEmailSender>();
}
else
{
    var emailApiKey =
        builder.Configuration["Email:ApiKey"]
        ?? throw new InvalidOperationException("Email:ApiKey configuration is required.");
    builder.Services.AddResend(options => options.ApiToken = emailApiKey);
    builder.Services.AddScoped<IEmailSender<ApplicationUser>, ResendEmailSender>();
}

builder.Services.AddCors();
builder.Services.AddSingleton<ICorsPolicyProvider, DynamicCorsPolicyProvider>();

// Data Protection — persist keys to Redis when available; fall back to in-memory.
var redisCs = builder.Configuration.GetConnectionString("Redis");
var redisDataProtectionReady = false;
if (!string.IsNullOrEmpty(redisCs))
{
    try
    {
        var redis = ConnectionMultiplexer.Connect(redisCs);
        builder
            .Services.AddDataProtection()
            .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys")
            .SetApplicationName("See.Idp");
        redisDataProtectionReady = true;
    }
    catch (RedisConnectionException)
    {
        builder.Services.AddDataProtection().SetApplicationName("See.Idp");
    }
}
else
{
    builder.Services.AddDataProtection().SetApplicationName("See.Idp");
}

builder.Services.AddRateLimiter(options =>
{
    var loginPermitLimit = builder.Configuration.GetValue("RateLimiting:Login:PermitLimit", 10);
    var loginWindowSeconds = builder.Configuration.GetValue("RateLimiting:Login:WindowSeconds", 60);
    var tokenPermitLimit = builder.Configuration.GetValue("RateLimiting:Token:PermitLimit", 30);
    var tokenWindowSeconds = builder.Configuration.GetValue("RateLimiting:Token:WindowSeconds", 60);

    options.AddPolicy(
        "login",
        context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = loginPermitLimit,
                    Window = TimeSpan.FromSeconds(loginWindowSeconds),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                }
            )
    );

    options.AddPolicy(
        "token",
        context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = tokenPermitLimit,
                    Window = TimeSpan.FromSeconds(tokenWindowSeconds),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                }
            )
    );

    options.OnRejected = async (context, ct) =>
    {
        var logger = context
            .HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("RateLimiting");

        logger.LogWarning(
            new EventId(EventIds.RateLimitExceeded, nameof(EventIds.RateLimitExceeded)),
            "Rate limit exceeded for {Path} from {IP}",
            context.HttpContext.Request.Path,
            context.HttpContext.Connection.RemoteIpAddress
        );

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please try again later.",
            ct
        );
    };
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Admin", "/", Policies.AdminPortal);
});

var app = builder.Build();

if (!string.IsNullOrEmpty(redisCs) && !redisDataProtectionReady)
{
    app.Logger.LogWarning(
        new EventId(
            EventIds.DataProtectionRedisUnavailable,
            nameof(EventIds.DataProtectionRedisUnavailable)
        ),
        "Redis unavailable at startup; Data Protection keys are in-memory and will not survive restarts."
    );
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();

app.UseRateLimiter();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages().WithStaticAssets();

app.Run();
