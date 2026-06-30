using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;
using See.Idp.Core.Dtos.Clients;
using See.Idp.Infrastructure;
using See.Idp.Infrastructure.Services;
using See.Idp.Tests.Support;

#pragma warning disable CA2012

namespace See.Idp.Tests.Services;

[TestClass]
public sealed class ClientQueryServiceTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task ListClientsAsync_ExcludesEntriesWithNullClientId()
    {
        await using var db = CreateDbContext();

        db.Set<OpenIddictEntityFrameworkCoreApplication>()
            .AddRange(
                new OpenIddictEntityFrameworkCoreApplication
                {
                    ClientId = "client-1",
                    DisplayName = "Client One",
                },
                new OpenIddictEntityFrameworkCoreApplication
                {
                    ClientId = null,
                    DisplayName = "Ignored",
                }
            );
        await db.SaveChangesAsync(Ct);

        var result = await CreateSut(db).ListClientsAsync(new ListClientsQuery(), Ct);

        Assert.HasCount(1, result);
        Assert.AreEqual("client-1", result[0].ClientId);
        Assert.AreEqual("Client One", result[0].DisplayName);
    }

    [TestMethod]
    public async Task ListClientsAsync_FiltersBySearchTerm()
    {
        await using var db = CreateDbContext();

        db.Set<OpenIddictEntityFrameworkCoreApplication>()
            .AddRange(
                new OpenIddictEntityFrameworkCoreApplication
                {
                    ClientId = "client-1",
                    DisplayName = "First Client",
                },
                new OpenIddictEntityFrameworkCoreApplication
                {
                    ClientId = "client-2",
                    DisplayName = "Second Client",
                }
            );
        await db.SaveChangesAsync(Ct);

        var result = await CreateSut(db)
            .ListClientsAsync(new ListClientsQuery(SearchTerm: "SECOND"), Ct);

        Assert.HasCount(1, result);
        Assert.AreEqual("client-2", result[0].ClientId);
    }

    [TestMethod]
    public async Task ListClientsAsync_AppliesSkipAndTake()
    {
        await using var db = CreateDbContext();

        db.Set<OpenIddictEntityFrameworkCoreApplication>()
            .AddRange(
                new OpenIddictEntityFrameworkCoreApplication
                {
                    ClientId = "client-a",
                    DisplayName = "Client A",
                },
                new OpenIddictEntityFrameworkCoreApplication
                {
                    ClientId = "client-b",
                    DisplayName = "Client B",
                },
                new OpenIddictEntityFrameworkCoreApplication
                {
                    ClientId = "client-c",
                    DisplayName = "Client C",
                }
            );
        await db.SaveChangesAsync(Ct);

        var result = await CreateSut(db)
            .ListClientsAsync(new ListClientsQuery(Skip: 1, Take: 1), Ct);

        Assert.HasCount(1, result);
        Assert.AreEqual("client-b", result[0].ClientId);
    }

    [TestMethod]
    public async Task GetClientByIdAsync_ReturnsFailure_WhenClientIdMissing()
    {
        var applicationManager = CreateApplicationManager();
        var sut = CreateSut(applicationManager: applicationManager);

        var result = await sut.GetClientByIdAsync(new GetClientByIdQuery(""), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.IsFalse(result.NotFound);
        Assert.IsNotNull(result.Error);
        await applicationManager.DidNotReceive().FindByClientIdAsync(Arg.Any<string>(), Ct);
    }

    [TestMethod]
    public async Task GetClientByIdAsync_ReturnsMissing_WhenClientNotFound()
    {
        var applicationManager = CreateApplicationManager();
        applicationManager
            .FindByClientIdAsync("missing", Ct)
            .Returns(new ValueTask<object?>((object?)null));

        var sut = CreateSut(applicationManager: applicationManager);

        var result = await sut.GetClientByIdAsync(new GetClientByIdQuery("missing"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.NotFound);
    }

    [TestMethod]
    public async Task GetClientByIdAsync_ReturnsFailure_WhenResolvedClientIdIsEmpty()
    {
        var app = new object();

        var applicationManager = CreateApplicationManager();
        applicationManager.FindByClientIdAsync("client-1", Ct).Returns(new ValueTask<object?>(app));
        applicationManager.GetClientIdAsync(app, Ct).Returns(new ValueTask<string?>(""));

        var sut = CreateSut(applicationManager: applicationManager);

        var result = await sut.GetClientByIdAsync(new GetClientByIdQuery("client-1"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.IsFalse(result.NotFound);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task GetClientByIdAsync_ReturnsDetails_WhenClientExists()
    {
        var app = new object();

        var applicationManager = CreateApplicationManager();
        applicationManager.FindByClientIdAsync("client-1", Ct).Returns(new ValueTask<object?>(app));
        applicationManager.GetClientIdAsync(app, Ct).Returns(new ValueTask<string?>("client-1"));
        applicationManager
            .When(x => x.PopulateAsync(Arg.Any<OpenIddictApplicationDescriptor>(), app, Ct))
            .Do(callInfo =>
            {
                var descriptor = callInfo.Arg<OpenIddictApplicationDescriptor>();
                descriptor.DisplayName = "Client One";
                descriptor.Permissions.Add("gt:authorization_code");
                descriptor.Permissions.Add("gt:client_credentials");
                descriptor.Permissions.Add("scp:profile");
                descriptor.RedirectUris.Add(new Uri("https://localhost/callback"));
                descriptor.RedirectUris.Add(new Uri("https://localhost/callback"));
            });

        var sut = CreateSut(applicationManager: applicationManager);

        var result = await sut.GetClientByIdAsync(new GetClientByIdQuery("client-1"), Ct);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.Client);
        Assert.AreEqual("client-1", result.Client.ClientId);
        Assert.AreEqual("Client One", result.Client.DisplayName);
        Assert.IsTrue(result.Client.AllowAuthorizationCodeFlow);
        Assert.IsTrue(result.Client.AllowClientCredentialsFlow);
        Assert.IsFalse(result.Client.AllowRefreshTokenFlow);
        Assert.IsFalse(result.Client.IsConfidential);
        Assert.IsFalse(result.Client.HasClientSecret);
        Assert.HasCount(1, result.Client.RedirectUris);
        Assert.AreEqual("https://localhost/callback", result.Client.RedirectUris[0]);
        Assert.IsTrue(result.Client.Permissions.Contains("scp:profile"));
    }

    [TestMethod]
    public async Task GetClientByIdAsync_SetsConfidentialAndSecretFlags_WhenConfigured()
    {
        var app = new object();

        var applicationManager = CreateApplicationManager();
        applicationManager.FindByClientIdAsync("client-2", Ct).Returns(new ValueTask<object?>(app));
        applicationManager.GetClientIdAsync(app, Ct).Returns(new ValueTask<string?>("client-2"));
        applicationManager
            .When(x => x.PopulateAsync(Arg.Any<OpenIddictApplicationDescriptor>(), app, Ct))
            .Do(callInfo =>
            {
                var descriptor = callInfo.Arg<OpenIddictApplicationDescriptor>();
                descriptor.DisplayName = "Client Two";
                descriptor.ClientType = OpenIddictConstants.ClientTypes.Confidential;
                descriptor.ClientSecret = "hashed-or-placeholder";
            });

        var sut = CreateSut(applicationManager: applicationManager);

        var result = await sut.GetClientByIdAsync(new GetClientByIdQuery("client-2"), Ct);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Client!.IsConfidential);
        Assert.IsTrue(result.Client.HasClientSecret);
    }

    [TestMethod]
    public async Task GetClientByIdAsync_ReturnsPostLogoutRedirectUris_WhenConfigured()
    {
        var app = new object();

        var applicationManager = CreateApplicationManager();
        applicationManager.FindByClientIdAsync("client-1", Ct).Returns(new ValueTask<object?>(app));
        applicationManager.GetClientIdAsync(app, Ct).Returns(new ValueTask<string?>("client-1"));
        applicationManager
            .When(x => x.PopulateAsync(Arg.Any<OpenIddictApplicationDescriptor>(), app, Ct))
            .Do(callInfo =>
            {
                var descriptor = callInfo.Arg<OpenIddictApplicationDescriptor>();
                descriptor.PostLogoutRedirectUris.Add(new Uri("https://localhost/"));
                descriptor.PostLogoutRedirectUris.Add(new Uri("https://localhost/"));
            });

        var sut = CreateSut(applicationManager: applicationManager);

        var result = await sut.GetClientByIdAsync(new GetClientByIdQuery("client-1"), Ct);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(1, result.Client!.PostLogoutRedirectUris);
        Assert.AreEqual("https://localhost/", result.Client.PostLogoutRedirectUris[0]);
    }

    private static ApplicationDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .UseOpenIddict()
                .Options
        );

    private static IOpenIddictApplicationManager CreateApplicationManager() =>
        Substitute.For<IOpenIddictApplicationManager>();

    private static ClientQueryService CreateSut(
        ApplicationDbContext? dbContext = null,
        IOpenIddictApplicationManager? applicationManager = null
    ) =>
        new(
            dbContext ?? CreateDbContext(),
            applicationManager ?? CreateApplicationManager(),
            Substitute.For<ILogger<ClientQueryService>>()
        );
}

#pragma warning restore CA2012
