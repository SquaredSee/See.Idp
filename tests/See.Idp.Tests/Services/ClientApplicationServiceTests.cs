using System;
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
public sealed class ClientApplicationServiceTests
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
            .GetDisplayNameAsync(app, Ct)
            .Returns(new ValueTask<string?>("Client One"));

        var sut = CreateSut(applicationManager);

        var result = await sut.GetClientByIdAsync(new GetClientByIdQuery("client-1"), Ct);

        Assert.IsNotNull(result);
        Assert.AreEqual("client-1", result.ClientId);
        Assert.AreEqual("Client One", result.DisplayName);
    }

    [TestMethod]
    public async Task CreateClientAsync_ReturnsFailure_WhenClientIdMissing()
    {
        var applicationManager = CreateApplicationManager();
        var sut = CreateSut(applicationManager);

        var result = await sut.CreateClientAsync(new CreateClientCommand("", "Display"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Client ID is required.", result.Error);
        await applicationManager
            .DidNotReceive()
            .CreateAsync(Arg.Any<OpenIddictApplicationDescriptor>(), Ct);
    }

    [TestMethod]
    public async Task CreateClientAsync_ReturnsFailure_WhenClientAlreadyExists()
    {
        var existing = new object();

        var applicationManager = CreateApplicationManager();
        applicationManager
            .FindByClientIdAsync("client-1", Ct)
            .Returns(new ValueTask<object?>(existing));

        var sut = CreateSut(applicationManager);

        var result = await sut.CreateClientAsync(
            new CreateClientCommand("client-1", "Display"),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Client ID already exists.", result.Error);
        await applicationManager
            .DidNotReceive()
            .CreateAsync(Arg.Any<OpenIddictApplicationDescriptor>(), Ct);
    }

    [TestMethod]
    public async Task CreateClientAsync_CreatesDescriptor_WhenClientIsNew()
    {
        var applicationManager = CreateApplicationManager();
        applicationManager
            .FindByClientIdAsync("client-new", Ct)
            .Returns(new ValueTask<object?>((object?)null));

        var sut = CreateSut(applicationManager);

        var result = await sut.CreateClientAsync(
            new CreateClientCommand("client-new", "New Client"),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        await applicationManager
            .Received(1)
            .CreateAsync(
                Arg.Is<OpenIddictApplicationDescriptor>(d =>
                    d.ClientId == "client-new" && d.DisplayName == "New Client"
                ),
                Ct
            );
    }

    [TestMethod]
    public async Task UpdateClientAsync_ReturnsFailure_WhenClientNotFound()
    {
        var applicationManager = CreateApplicationManager();
        applicationManager
            .FindByClientIdAsync("missing", Ct)
            .Returns(new ValueTask<object?>((object?)null));

        var sut = CreateSut(applicationManager);

        var result = await sut.UpdateClientAsync(new UpdateClientCommand("missing", "Updated"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Client not found.", result.Error);
    }

    [TestMethod]
    public async Task UpdateClientAsync_UpdatesDescriptor_WhenClientExists()
    {
        var app = new object();

        var applicationManager = CreateApplicationManager();
        applicationManager.FindByClientIdAsync("client-1", Ct).Returns(new ValueTask<object?>(app));

        var sut = CreateSut(applicationManager);

        var result = await sut.UpdateClientAsync(
            new UpdateClientCommand("client-1", "Updated Name"),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        await applicationManager
            .Received(1)
            .PopulateAsync(Arg.Any<OpenIddictApplicationDescriptor>(), app, Ct);
        await applicationManager
            .Received(1)
            .UpdateAsync(
                app,
                Arg.Is<OpenIddictApplicationDescriptor>(d => d.DisplayName == "Updated Name"),
                Ct
            );
    }

    [TestMethod]
    public async Task DeleteClientAsync_ReturnsFailure_WhenClientNotFound()
    {
        var applicationManager = CreateApplicationManager();
        applicationManager
            .FindByClientIdAsync("missing", Ct)
            .Returns(new ValueTask<object?>((object?)null));

        var sut = CreateSut(applicationManager);

        var result = await sut.DeleteClientAsync(new DeleteClientCommand("missing"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Client not found.", result.Error);
    }

    [TestMethod]
    public async Task DeleteClientAsync_DeletesClient_WhenFound()
    {
        var app = new object();

        var applicationManager = CreateApplicationManager();
        applicationManager.FindByClientIdAsync("client-1", Ct).Returns(new ValueTask<object?>(app));

        var sut = CreateSut(applicationManager);

        var result = await sut.DeleteClientAsync(new DeleteClientCommand("client-1"), Ct);

        Assert.IsTrue(result.Succeeded);
        await applicationManager.Received(1).DeleteAsync(app, Ct);
    }

    [TestMethod]
    public async Task CreateClientIfMissingAsync_ReturnsFailure_WhenInvalidRedirectUri()
    {
        var applicationManager = CreateApplicationManager();
        applicationManager
            .FindByClientIdAsync("client-1", Ct)
            .Returns(new ValueTask<object?>((object?)null));

        var sut = CreateSut(applicationManager);

        var result = await sut.CreateClientIfMissingAsync(
            new CreateClientIfMissingCommand(
                "client-1",
                "secret",
                "Display",
                new[] { "not-a-valid-uri" },
                new[] { "perm-1" }
            ),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
        Assert.IsFalse(result.Created);
        StringAssert.Contains(result.Error, "Invalid redirect URI");
        await applicationManager
            .DidNotReceive()
            .CreateAsync(Arg.Any<OpenIddictApplicationDescriptor>(), Ct);
    }

    [TestMethod]
    public async Task CreateClientIfMissingAsync_CreatesClient_WhenInputIsValid()
    {
        var applicationManager = CreateApplicationManager();
        applicationManager
            .FindByClientIdAsync("client-1", Ct)
            .Returns(new ValueTask<object?>((object?)null));

        var sut = CreateSut(applicationManager);

        var result = await sut.CreateClientIfMissingAsync(
            new CreateClientIfMissingCommand(
                "client-1",
                " ",
                "Display",
                new[] { "https://localhost/signin-oidc", "" },
                new[] { "perm-1", " " }
            ),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Created);
        await applicationManager
            .Received(1)
            .CreateAsync(
                Arg.Is<OpenIddictApplicationDescriptor>(d =>
                    d.ClientId == "client-1"
                    && d.ClientSecret == null
                    && d.DisplayName == "Display"
                    && d.RedirectUris.Count == 1
                    && d.RedirectUris.Contains(new Uri("https://localhost/signin-oidc"))
                    && d.Permissions.Count == 1
                    && d.Permissions.Contains("perm-1")
                ),
                Ct
            );
    }

    private static IOpenIddictApplicationManager CreateApplicationManager()
    {
        return Substitute.For<IOpenIddictApplicationManager>();
    }

    private static ClientApplicationService CreateSut(
        IOpenIddictApplicationManager? applicationManager = null
    )
    {
        var manager = applicationManager ?? CreateApplicationManager();
        var logger = Substitute.For<ILogger<ClientApplicationService>>();
        return new ClientApplicationService(manager, logger);
    }
}

#pragma warning restore CA2012
