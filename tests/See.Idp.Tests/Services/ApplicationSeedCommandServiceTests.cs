using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using OpenIddict.Abstractions;
using See.Idp.Core.Dtos.Clients;
using See.Idp.Core.Dtos.Users;
using See.Idp.Infrastructure;
using See.Idp.Infrastructure.Services;
using See.Idp.Tests.Support;

namespace See.Idp.Tests.Services;

[TestClass]
public sealed class ApplicationSeedCommandServiceTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task CreateRoleIfMissingAsync_ReturnsFailure_WhenRoleNameEmpty()
    {
        var sut = CreateSut();

        var result = await sut.CreateRoleIfMissingAsync(new CreateRoleIfMissingCommand(""), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task CreateRoleIfMissingAsync_ReturnsAlreadyExists_WhenRoleExists()
    {
        var roleManager = IdentityTestFactory.CreateRoleManager();
        roleManager.RoleExistsAsync(Arg.Any<string>()).Returns(Task.FromResult(true));

        var result = await CreateSut(roleManager: roleManager)
            .CreateRoleIfMissingAsync(new CreateRoleIfMissingCommand("Admin"), Ct);

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(result.Created);
    }

    [TestMethod]
    public async Task CreateRoleIfMissingAsync_CreatesRole_WhenMissing()
    {
        const string roleName = "NewRole";
        var roleManager = IdentityTestFactory.CreateRoleManager();
        roleManager.RoleExistsAsync(roleName).Returns(Task.FromResult(false));
        roleManager
            .CreateAsync(Arg.Any<ApplicationRole>())
            .Returns(Task.FromResult(IdentityResult.Success));

        var result = await CreateSut(roleManager: roleManager)
            .CreateRoleIfMissingAsync(new CreateRoleIfMissingCommand(roleName), Ct);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Created);
    }

    [TestMethod]
    public async Task CreateUserIfMissingAsync_ReturnsFailure_WhenEmailEmpty()
    {
        var result = await CreateSut()
            .CreateUserIfMissingAsync(new CreateUserIfMissingCommand("", "pass"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task CreateUserIfMissingAsync_ReturnsAlreadyExists_WhenUserExists()
    {
        var existingUser = new ApplicationUser { Id = "existing-id", Email = "user@example.com" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager
            .FindByEmailAsync("user@example.com")
            .Returns(Task.FromResult<ApplicationUser?>(existingUser));

        var result = await CreateSut(userManager: userManager)
            .CreateUserIfMissingAsync(
                new CreateUserIfMissingCommand("user@example.com", "pass"),
                Ct
            );

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(result.Created);
        Assert.AreEqual("existing-id", result.UserId);
    }

    [TestMethod]
    public async Task CreateUserIfMissingAsync_CreatesUser_WhenMissing()
    {
        const string email = "new@example.com";
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByEmailAsync(email).Returns(Task.FromResult<ApplicationUser?>(null));
        userManager
            .CreateAsync(Arg.Any<ApplicationUser>(), "pass")
            .Returns(Task.FromResult(IdentityResult.Success));
        userManager.GetUserIdAsync(Arg.Any<ApplicationUser>()).Returns(Task.FromResult("new-id"));

        var result = await CreateSut(userManager: userManager)
            .CreateUserIfMissingAsync(new CreateUserIfMissingCommand(email, "pass"), Ct);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Created);
    }

    [TestMethod]
    public async Task AddUserToRoleIfMissingAsync_ReturnsAlreadyExists_WhenAlreadyInRole()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.IsInRoleAsync(user, "Admin").Returns(Task.FromResult(true));

        var result = await CreateSut(userManager: userManager)
            .AddUserToRoleIfMissingAsync(new AddUserToRoleIfMissingCommand("u1", "Admin"), Ct);

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(result.Created);
    }

    [TestMethod]
    public async Task AddUserToRoleIfMissingAsync_AddsRole_WhenNotInRole()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.IsInRoleAsync(user, "Admin").Returns(Task.FromResult(false));
        userManager.AddToRoleAsync(user, "Admin").Returns(Task.FromResult(IdentityResult.Success));

        var result = await CreateSut(userManager: userManager)
            .AddUserToRoleIfMissingAsync(new AddUserToRoleIfMissingCommand("u1", "Admin"), Ct);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Created);
    }

    [TestMethod]
    public async Task CreateClientIfMissingAsync_ReturnsAlreadyExists_WhenClientExists()
    {
        var appManager = CreateApplicationManager();
        appManager
            .FindByClientIdAsync("client-1", Ct)
            .Returns(new ValueTask<object?>(new object()));

        var result = await CreateSut(applicationManager: appManager)
            .CreateClientIfMissingAsync(
                new CreateClientIfMissingCommand(
                    "client-1",
                    null,
                    "Display",
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    Array.Empty<string>()
                ),
                Ct
            );

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(result.Created);
    }

    [TestMethod]
    public async Task CreateClientIfMissingAsync_ReturnsFailure_WhenInvalidRedirectUri()
    {
        var appManager = CreateApplicationManager();
        appManager
            .FindByClientIdAsync("client-1", Ct)
            .Returns(new ValueTask<object?>((object?)null));

        var result = await CreateSut(applicationManager: appManager)
            .CreateClientIfMissingAsync(
                new CreateClientIfMissingCommand(
                    "client-1",
                    null,
                    "Display",
                    new[] { "not-a-valid-uri" },
                    Array.Empty<string>(),
                    Array.Empty<string>()
                ),
                Ct
            );

        Assert.IsFalse(result.Succeeded);
        StringAssert.Contains(result.Error, "Invalid redirect URI");
    }

    [TestMethod]
    public async Task CreateClientIfMissingAsync_CreatesClient_WhenMissing()
    {
        var appManager = CreateApplicationManager();
        appManager
            .FindByClientIdAsync("client-1", Ct)
            .Returns(new ValueTask<object?>((object?)null));

        var result = await CreateSut(applicationManager: appManager)
            .CreateClientIfMissingAsync(
                new CreateClientIfMissingCommand(
                    "client-1",
                    null,
                    "Display",
                    new[] { "https://localhost/cb" },
                    Array.Empty<string>(),
                    new[] { "perm-1" }
                ),
                Ct
            );

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Created);
        await appManager
            .Received(1)
            .CreateAsync(
                Arg.Is<OpenIddictApplicationDescriptor>(d =>
                    d.ClientId == "client-1" && d.DisplayName == "Display"
                ),
                Ct
            );
    }

    [TestMethod]
    public async Task CreateRoleIfMissingAsync_ReturnsFailure_WhenCreateFails()
    {
        const string roleName = "FailRole";
        var roleManager = IdentityTestFactory.CreateRoleManager();
        roleManager.RoleExistsAsync(roleName).Returns(Task.FromResult(false));
        roleManager
            .CreateAsync(Arg.Any<ApplicationRole>())
            .Returns(Task.FromResult(IdentityTestFactory.FailedResult("Create failed.")));

        var result = await CreateSut(roleManager: roleManager)
            .CreateRoleIfMissingAsync(new CreateRoleIfMissingCommand(roleName), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task CreateUserIfMissingAsync_CreatesUserWithoutPassword_WhenPasswordBlank()
    {
        const string email = "no-password@example.com";
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByEmailAsync(email).Returns(Task.FromResult<ApplicationUser?>(null));
        userManager
            .CreateAsync(Arg.Any<ApplicationUser>())
            .Returns(Task.FromResult(IdentityResult.Success));

        var result = await CreateSut(userManager: userManager)
            .CreateUserIfMissingAsync(new CreateUserIfMissingCommand(email, " "), Ct);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Created);
        await userManager.Received(1).CreateAsync(Arg.Is<ApplicationUser>(u => u.Email == email));
        await userManager
            .DidNotReceive()
            .CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
    }

    [TestMethod]
    public async Task CreateUserIfMissingAsync_ReturnsFailure_WhenCreateFails()
    {
        const string email = "fail@example.com";
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByEmailAsync(email).Returns(Task.FromResult<ApplicationUser?>(null));
        userManager
            .CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(Task.FromResult(IdentityTestFactory.FailedResult("Password too weak.")));

        var result = await CreateSut(userManager: userManager)
            .CreateUserIfMissingAsync(new CreateUserIfMissingCommand(email, "weak"), Ct);

        Assert.IsFalse(result.Succeeded);
        StringAssert.Contains(result.Error, "Password too weak.");
    }

    [TestMethod]
    public async Task AddUserToRoleIfMissingAsync_ReturnsFailure_WhenUserIdMissing()
    {
        var result = await CreateSut()
            .AddUserToRoleIfMissingAsync(new AddUserToRoleIfMissingCommand("", "Admin"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task AddUserToRoleIfMissingAsync_ReturnsFailure_WhenRoleNameMissing()
    {
        var result = await CreateSut()
            .AddUserToRoleIfMissingAsync(new AddUserToRoleIfMissingCommand("u1", ""), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task AddUserToRoleIfMissingAsync_ReturnsFailure_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("missing").Returns(Task.FromResult<ApplicationUser?>(null));

        var result = await CreateSut(userManager: userManager)
            .AddUserToRoleIfMissingAsync(new AddUserToRoleIfMissingCommand("missing", "Admin"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task AddUserToRoleIfMissingAsync_ReturnsFailure_WhenAddToRoleFails()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.IsInRoleAsync(user, "Admin").Returns(Task.FromResult(false));
        userManager
            .AddToRoleAsync(user, "Admin")
            .Returns(Task.FromResult(IdentityTestFactory.FailedResult("Add role failed.")));

        var result = await CreateSut(userManager: userManager)
            .AddUserToRoleIfMissingAsync(new AddUserToRoleIfMissingCommand("u1", "Admin"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Add role failed.", result.Error);
    }

    private static IOpenIddictApplicationManager CreateApplicationManager() =>
        Substitute.For<IOpenIddictApplicationManager>();

    private static ApplicationSeedCommandService CreateSut(
        UserManager<ApplicationUser>? userManager = null,
        RoleManager<ApplicationRole>? roleManager = null,
        IOpenIddictApplicationManager? applicationManager = null
    ) =>
        new(
            userManager ?? IdentityTestFactory.CreateUserManager(),
            roleManager ?? IdentityTestFactory.CreateRoleManager(),
            applicationManager ?? CreateApplicationManager(),
            Substitute.For<ILogger<ApplicationSeedCommandService>>()
        );
}
