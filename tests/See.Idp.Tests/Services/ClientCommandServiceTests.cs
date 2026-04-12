using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using OpenIddict.Abstractions;
using See.Idp.Core.Dtos.Clients;
using See.Idp.Infrastructure.Services;

#pragma warning disable CA2012

namespace See.Idp.Tests.Services;

[TestClass]
public sealed class ClientCommandServiceTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

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

    private static ClientCommandService CreateSut(
        IOpenIddictApplicationManager? applicationManager = null
    )
    {
        var manager = applicationManager ?? CreateApplicationManager();
        var logger = Substitute.For<ILogger<ClientCommandService>>();
        return new ClientCommandService(manager, logger);
    }
}

#pragma warning restore CA2012
