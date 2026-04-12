using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using OpenIddict.Abstractions;
using See.Idp.Core.Dtos.Clients;
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
    public async Task ListClientsAsync_ExcludesEntriesWithEmptyClientId()
    {
        var app1 = new object();
        var app2 = new object();

        var applicationManager = CreateApplicationManager();
        applicationManager
            .ListAsync(cancellationToken: Ct)
            .Returns(AsyncEnumerableTestFactory.Create(app1, app2));
        applicationManager.GetClientIdAsync(app1, Ct).Returns(new ValueTask<string?>("client-1"));
        applicationManager
            .GetDisplayNameAsync(app1, Ct)
            .Returns(new ValueTask<string?>("Client One"));
        applicationManager.GetClientIdAsync(app2, Ct).Returns(new ValueTask<string?>(" "));
        applicationManager.GetDisplayNameAsync(app2, Ct).Returns(new ValueTask<string?>("Ignored"));

        var sut = CreateSut(applicationManager);

        var result = await sut.ListClientsAsync(new ListClientsQuery(), Ct);

        Assert.HasCount(1, result);
        Assert.AreEqual("client-1", result[0].ClientId);
        Assert.AreEqual("Client One", result[0].DisplayName);
    }

    [TestMethod]
    public async Task ListClientsAsync_FiltersBySearchTerm()
    {
        var app1 = new object();
        var app2 = new object();

        var applicationManager = CreateApplicationManager();
        applicationManager
            .ListAsync(cancellationToken: Ct)
            .Returns(AsyncEnumerableTestFactory.Create(app1, app2));

        applicationManager.GetClientIdAsync(app1, Ct).Returns(new ValueTask<string?>("client-1"));
        applicationManager
            .GetDisplayNameAsync(app1, Ct)
            .Returns(new ValueTask<string?>("First Client"));

        applicationManager.GetClientIdAsync(app2, Ct).Returns(new ValueTask<string?>("client-2"));
        applicationManager
            .GetDisplayNameAsync(app2, Ct)
            .Returns(new ValueTask<string?>("Second Client"));

        var sut = CreateSut(applicationManager);

        var result = await sut.ListClientsAsync(new ListClientsQuery(SearchTerm: "SECOND"), Ct);

        Assert.HasCount(1, result);
        Assert.AreEqual("client-2", result[0].ClientId);
    }

    [TestMethod]
    public async Task ListClientsAsync_AppliesSkipAndTake()
    {
        var appA = new object();
        var appB = new object();
        var appC = new object();

        var applicationManager = CreateApplicationManager();
        applicationManager
            .ListAsync(cancellationToken: Ct)
            .Returns(AsyncEnumerableTestFactory.Create(appC, appA, appB));

        applicationManager.GetClientIdAsync(appA, Ct).Returns(new ValueTask<string?>("client-a"));
        applicationManager
            .GetDisplayNameAsync(appA, Ct)
            .Returns(new ValueTask<string?>("Client A"));

        applicationManager.GetClientIdAsync(appB, Ct).Returns(new ValueTask<string?>("client-b"));
        applicationManager
            .GetDisplayNameAsync(appB, Ct)
            .Returns(new ValueTask<string?>("Client B"));

        applicationManager.GetClientIdAsync(appC, Ct).Returns(new ValueTask<string?>("client-c"));
        applicationManager
            .GetDisplayNameAsync(appC, Ct)
            .Returns(new ValueTask<string?>("Client C"));

        var sut = CreateSut(applicationManager);

        var result = await sut.ListClientsAsync(new ListClientsQuery(Skip: 1, Take: 1), Ct);

        Assert.HasCount(1, result);
        Assert.AreEqual("client-b", result[0].ClientId);
    }

    [TestMethod]
    public async Task GetClientByIdAsync_ReturnsNull_WhenClientIdMissing()
    {
        var applicationManager = CreateApplicationManager();
        var sut = CreateSut(applicationManager);

        var result = await sut.GetClientByIdAsync(new GetClientByIdQuery(""), Ct);

        Assert.IsNull(result);
        await applicationManager.DidNotReceive().FindByClientIdAsync(Arg.Any<string>(), Ct);
    }

    [TestMethod]
    public async Task GetClientByIdAsync_ReturnsNull_WhenClientNotFound()
    {
        var applicationManager = CreateApplicationManager();
        applicationManager
            .FindByClientIdAsync("missing", Ct)
            .Returns(new ValueTask<object?>((object?)null));

        var sut = CreateSut(applicationManager);

        var result = await sut.GetClientByIdAsync(new GetClientByIdQuery("missing"), Ct);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetClientByIdAsync_ReturnsNull_WhenResolvedClientIdIsEmpty()
    {
        var app = new object();

        var applicationManager = CreateApplicationManager();
        applicationManager.FindByClientIdAsync("client-1", Ct).Returns(new ValueTask<object?>(app));
        applicationManager.GetClientIdAsync(app, Ct).Returns(new ValueTask<string?>(""));

        var sut = CreateSut(applicationManager);

        var result = await sut.GetClientByIdAsync(new GetClientByIdQuery("client-1"), Ct);

        Assert.IsNull(result);
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

        var sut = CreateSut(applicationManager);

        var result = await sut.GetClientByIdAsync(new GetClientByIdQuery("client-1"), Ct);

        Assert.IsNotNull(result);
        Assert.AreEqual("client-1", result.ClientId);
        Assert.AreEqual("Client One", result.DisplayName);
        Assert.IsTrue(result.AllowAuthorizationCodeFlow);
        Assert.IsTrue(result.AllowClientCredentialsFlow);
        Assert.IsFalse(result.AllowRefreshTokenFlow);
        Assert.HasCount(1, result.RedirectUris);
        Assert.AreEqual("https://localhost/callback", result.RedirectUris[0]);
        Assert.IsTrue(result.Permissions.Contains("scp:profile"));
    }

    private static IOpenIddictApplicationManager CreateApplicationManager()
    {
        return Substitute.For<IOpenIddictApplicationManager>();
    }

    private static ClientQueryService CreateSut(
        IOpenIddictApplicationManager? applicationManager = null
    )
    {
        var manager = applicationManager ?? CreateApplicationManager();
        var logger = Substitute.For<ILogger<ClientQueryService>>();
        return new ClientQueryService(manager, logger);
    }
}

#pragma warning restore CA2012
