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

        var result = await sut.CreateClientAsync(
            new CreateClientCommand("", "Display", false, false, false, false, [], [], []),
            Ct
        );

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
            new CreateClientCommand("client-1", "Display", false, false, false, false, [], [], []),
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
            new CreateClientCommand(
                "client-new",
                "New Client",
                true,
                false,
                false,
                false,
                ["https://localhost/signin-oidc"],
                [],
                ["scp:profile"]
            ),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        await applicationManager
            .Received(1)
            .CreateAsync(
                Arg.Is<OpenIddictApplicationDescriptor>(d =>
                    d.ClientId == "client-new"
                    && d.DisplayName == "New Client"
                    && d.ClientType == OpenIddictConstants.ClientTypes.Public
                    && d.ClientSecret == null
                    && d.RedirectUris.Contains(new Uri("https://localhost/signin-oidc"))
                    && d.Permissions.Contains("scp:profile")
                    && d.Permissions.Contains("ept:authorization")
                    && d.Permissions.Contains("gt:authorization_code")
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

        var result = await sut.UpdateClientAsync(
            new UpdateClientCommand("missing", "Updated", false, false, false, [], [], []),
            Ct
        );

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
            new UpdateClientCommand(
                "client-1",
                "Updated Name",
                false,
                true,
                true,
                ["https://localhost/callback"],
                [],
                ["scp:roles", "gt:authorization_code"]
            ),
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
                Arg.Is<OpenIddictApplicationDescriptor>(d =>
                    d.DisplayName == "Updated Name"
                    && d.RedirectUris.Contains(new Uri("https://localhost/callback"))
                    && d.Permissions.Contains("scp:roles")
                    && d.Permissions.Contains("gt:client_credentials")
                    && d.Permissions.Contains("gt:refresh_token")
                    && !d.Permissions.Contains("gt:authorization_code")
                ),
                Ct
            );
    }

    [TestMethod]
    public async Task CreateClientAsync_SetsPostLogoutRedirectUris_WhenProvided()
    {
        var applicationManager = CreateApplicationManager();
        applicationManager
            .FindByClientIdAsync("client-new", Ct)
            .Returns(new ValueTask<object?>((object?)null));

        var sut = CreateSut(applicationManager);

        var result = await sut.CreateClientAsync(
            new CreateClientCommand(
                "client-new",
                "New Client",
                true,
                false,
                false,
                false,
                ["https://localhost/signin-oidc"],
                ["https://localhost/"],
                []
            ),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        await applicationManager
            .Received(1)
            .CreateAsync(
                Arg.Is<OpenIddictApplicationDescriptor>(d =>
                    d.PostLogoutRedirectUris.Contains(new Uri("https://localhost/"))
                ),
                Ct
            );
    }

    [TestMethod]
    public async Task CreateClientAsync_ReturnsFailure_WhenPostLogoutRedirectUriInvalid()
    {
        var applicationManager = CreateApplicationManager();
        applicationManager
            .FindByClientIdAsync("client-invalid", Ct)
            .Returns(new ValueTask<object?>((object?)null));

        var sut = CreateSut(applicationManager);

        var result = await sut.CreateClientAsync(
            new CreateClientCommand(
                "client-invalid",
                "Invalid",
                false,
                false,
                false,
                false,
                [],
                ["not-a-uri"],
                []
            ),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
        StringAssert.Contains(result.Error, "Invalid post-logout redirect URI");
        await applicationManager
            .DidNotReceive()
            .CreateAsync(Arg.Any<OpenIddictApplicationDescriptor>(), Ct);
    }

    [TestMethod]
    public async Task UpdateClientAsync_ReplacesPostLogoutRedirectUris()
    {
        var app = new object();

        var applicationManager = CreateApplicationManager();
        applicationManager.FindByClientIdAsync("client-1", Ct).Returns(new ValueTask<object?>(app));
        applicationManager
            .When(x => x.PopulateAsync(Arg.Any<OpenIddictApplicationDescriptor>(), app, Ct))
            .Do(callInfo =>
            {
                var descriptor = callInfo.Arg<OpenIddictApplicationDescriptor>();
                descriptor.PostLogoutRedirectUris.Add(new Uri("https://old.example.com/"));
            });

        var sut = CreateSut(applicationManager);

        var result = await sut.UpdateClientAsync(
            new UpdateClientCommand(
                "client-1",
                "Updated",
                false,
                false,
                false,
                [],
                ["https://new.example.com/"],
                []
            ),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        await applicationManager
            .Received(1)
            .UpdateAsync(
                app,
                Arg.Is<OpenIddictApplicationDescriptor>(d =>
                    d.PostLogoutRedirectUris.Contains(new Uri("https://new.example.com/"))
                    && !d.PostLogoutRedirectUris.Contains(new Uri("https://old.example.com/"))
                ),
                Ct
            );
    }

    [TestMethod]
    public async Task CreateClientAsync_ReturnsFailure_WhenRedirectUriInvalid()
    {
        var applicationManager = CreateApplicationManager();
        applicationManager
            .FindByClientIdAsync("client-invalid", Ct)
            .Returns(new ValueTask<object?>((object?)null));

        var sut = CreateSut(applicationManager);

        var result = await sut.CreateClientAsync(
            new CreateClientCommand(
                "client-invalid",
                "Invalid",
                true,
                false,
                false,
                false,
                ["not-a-uri"],
                [],
                []
            ),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
        StringAssert.Contains(result.Error, "Invalid redirect URI");
        await applicationManager
            .DidNotReceive()
            .CreateAsync(Arg.Any<OpenIddictApplicationDescriptor>(), Ct);
    }

    [TestMethod]
    public async Task RotateClientSecretAsync_ReturnsFailure_WhenClientNotFound()
    {
        var applicationManager = CreateApplicationManager();
        applicationManager
            .FindByClientIdAsync("missing", Ct)
            .Returns(new ValueTask<object?>((object?)null));

        var sut = CreateSut(applicationManager);

        var result = await sut.RotateClientSecretAsync(
            new RotateClientSecretCommand("missing"),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Client not found.", result.Error);
    }

    [TestMethod]
    public async Task RotateClientSecretAsync_UpdatesDescriptor_WhenClientExists()
    {
        var app = new object();

        var applicationManager = CreateApplicationManager();
        applicationManager.FindByClientIdAsync("client-1", Ct).Returns(new ValueTask<object?>(app));

        var sut = CreateSut(applicationManager);

        var result = await sut.RotateClientSecretAsync(
            new RotateClientSecretCommand("client-1"),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.PromotedToConfidential);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ClientSecret));
        await applicationManager
            .Received(1)
            .UpdateAsync(
                app,
                Arg.Is<OpenIddictApplicationDescriptor>(d =>
                    d.ClientType == OpenIddictConstants.ClientTypes.Confidential
                    && !string.IsNullOrWhiteSpace(d.ClientSecret)
                ),
                Ct
            );
    }

    [TestMethod]
    public async Task RotateClientSecretAsync_DoesNotPromote_WhenAlreadyConfidential()
    {
        var app = new object();

        var applicationManager = CreateApplicationManager();
        applicationManager.FindByClientIdAsync("client-1", Ct).Returns(new ValueTask<object?>(app));
        applicationManager
            .When(x => x.PopulateAsync(Arg.Any<OpenIddictApplicationDescriptor>(), app, Ct))
            .Do(callInfo =>
            {
                var descriptor = callInfo.Arg<OpenIddictApplicationDescriptor>();
                descriptor.ClientType = OpenIddictConstants.ClientTypes.Confidential;
            });

        var sut = CreateSut(applicationManager);

        var result = await sut.RotateClientSecretAsync(
            new RotateClientSecretCommand("client-1"),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(result.PromotedToConfidential);
    }

    [TestMethod]
    public async Task CreateClientAsync_GeneratesSecret_WhenRequested()
    {
        var applicationManager = CreateApplicationManager();
        applicationManager
            .FindByClientIdAsync("client-secret", Ct)
            .Returns(new ValueTask<object?>((object?)null));

        var sut = CreateSut(applicationManager);

        var result = await sut.CreateClientAsync(
            new CreateClientCommand(
                "client-secret",
                "Secret Client",
                false,
                true,
                false,
                true,
                [],
                [],
                []
            ),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ClientSecret));
        await applicationManager
            .Received(1)
            .CreateAsync(
                Arg.Is<OpenIddictApplicationDescriptor>(d =>
                    d.ClientId == "client-secret"
                    && d.ClientType == OpenIddictConstants.ClientTypes.Confidential
                    && !string.IsNullOrWhiteSpace(d.ClientSecret)
                ),
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
                Array.Empty<string>(),
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
                Array.Empty<string>(),
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
