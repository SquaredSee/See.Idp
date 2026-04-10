using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using See.Idp.Infrastructure;
using See.Idp.Web.Auth;
using See.Idp.Web.Services;

var builder = WebApplication.CreateBuilder(args);

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
        // Enable the token endpoint.
        options.SetTokenEndpointUris("connect/token");

        // Enable the client credentials flow.
        options.AllowClientCredentialsFlow();

        // Register the signing and encryption credentials.
        options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();

        // Register the ASP.NET Core host and configure the ASP.NET Core options.
        var openIddictAspNetCoreOptions = options.UseAspNetCore().EnableTokenEndpointPassthrough();
        if (builder.Environment.IsDevelopment())
        {
            openIddictAspNetCoreOptions.DisableTransportSecurityRequirement();
        }
    });

builder
    .Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorizationBuilder().AddAdminPortalPolicy();

builder.Services.ConfigureApplicationCookie(options =>
{
    // Default paths for login, logout, and access denied.
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddHostedService<UiClientSeeder>();
builder.Services.AddHostedService<UserSeeder>();

// builder.Services.AddControllers();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Admin", "/", Policies.AdminPortal);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// app.MapControllers();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();
