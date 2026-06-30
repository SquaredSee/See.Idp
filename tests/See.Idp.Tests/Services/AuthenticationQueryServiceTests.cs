using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using OpenIddict.Abstractions;
using See.Idp.Infrastructure;
using See.Idp.Infrastructure.Services;
using See.Idp.Tests.Support;

namespace See.Idp.Tests.Services;

[TestClass]
public sealed class AuthenticationQueryServiceTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task BuildUserIdentityAsync_ReturnsNull_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("missing").Returns(Task.FromResult<ApplicationUser?>(null));

        var result = await CreateSut(userManager)
            .BuildUserIdentityAsync("missing", ImmutableArray<string>.Empty, Ct);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task BuildUserIdentityAsync_ReturnsNull_WhenUserCannotSignIn()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        var signInManager = IdentityTestFactory.CreateSignInManager(userManager);
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        signInManager.CanSignInAsync(user).Returns(Task.FromResult(false));

        var result = await CreateSut(userManager, signInManager)
            .BuildUserIdentityAsync("u1", ImmutableArray<string>.Empty, Ct);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task BuildUserIdentityAsync_ReturnsIdentity_WithEmailClaims()
    {
        var (sut, user) = ArrangeWithUser(
            "u1",
            email: "test@example.com",
            emailConfirmed: true,
            userName: "testuser",
            roles: []
        );

        var result = await sut.BuildUserIdentityAsync("u1", [OpenIddictConstants.Scopes.Email], Ct);

        Assert.IsNotNull(result);
        Assert.AreEqual("test@example.com", result.GetClaim(OpenIddictConstants.Claims.Email));
        Assert.AreEqual("True", result.FindFirst(OpenIddictConstants.Claims.EmailVerified)?.Value);
    }

    [TestMethod]
    public async Task BuildUserIdentityAsync_ReturnsIdentity_WithProfileClaims()
    {
        var (sut, user) = ArrangeWithUser(
            "u1",
            email: "test@example.com",
            emailConfirmed: false,
            userName: "testuser",
            roles: []
        );

        var result = await sut.BuildUserIdentityAsync(
            "u1",
            [OpenIddictConstants.Scopes.Profile],
            Ct
        );

        Assert.IsNotNull(result);
        Assert.AreEqual("testuser", result.GetClaim(OpenIddictConstants.Claims.Name));
    }

    [TestMethod]
    public async Task BuildUserIdentityAsync_ReturnsIdentity_WithRoleClaims()
    {
        var (sut, user) = ArrangeWithUser(
            "u1",
            email: "test@example.com",
            emailConfirmed: false,
            userName: "testuser",
            roles: ["Admin", "User"]
        );

        var result = await sut.BuildUserIdentityAsync("u1", [OpenIddictConstants.Scopes.Roles], Ct);

        Assert.IsNotNull(result);
        var roles = result.GetClaims(OpenIddictConstants.Claims.Role).ToArray();
        CollectionAssert.AreEquivalent(new[] { "Admin", "User" }, roles);
    }

    [TestMethod]
    public async Task BuildUserIdentityAsync_SetsEmailDestinationToIdentityToken_WhenEmailScopeGranted()
    {
        var (sut, user) = ArrangeWithUser(
            "u1",
            email: "test@example.com",
            emailConfirmed: true,
            userName: "testuser",
            roles: []
        );

        var result = await sut.BuildUserIdentityAsync("u1", [OpenIddictConstants.Scopes.Email], Ct);

        Assert.IsNotNull(result);
        var emailClaim = result.FindFirst(OpenIddictConstants.Claims.Email);
        Assert.IsNotNull(emailClaim);
        CollectionAssert.Contains(
            emailClaim.GetDestinations().ToArray(),
            OpenIddictConstants.Destinations.IdentityToken
        );
    }

    [TestMethod]
    public async Task BuildUserIdentityAsync_SetsProfileDestinationToIdentityToken_WhenProfileScopeGranted()
    {
        var (sut, user) = ArrangeWithUser(
            "u1",
            email: "test@example.com",
            emailConfirmed: false,
            userName: "testuser",
            roles: []
        );

        var result = await sut.BuildUserIdentityAsync(
            "u1",
            [OpenIddictConstants.Scopes.Profile],
            Ct
        );

        Assert.IsNotNull(result);
        var nameClaim = result.FindFirst(OpenIddictConstants.Claims.Name);
        Assert.IsNotNull(nameClaim);
        CollectionAssert.Contains(
            nameClaim.GetDestinations().ToArray(),
            OpenIddictConstants.Destinations.IdentityToken
        );
    }

    [TestMethod]
    public async Task BuildUserIdentityAsync_SetsRolesDestinationToIdentityToken_WhenRolesScopeGranted()
    {
        var (sut, user) = ArrangeWithUser(
            "u1",
            email: "test@example.com",
            emailConfirmed: false,
            userName: "testuser",
            roles: ["Admin"]
        );

        var result = await sut.BuildUserIdentityAsync("u1", [OpenIddictConstants.Scopes.Roles], Ct);

        Assert.IsNotNull(result);
        var roleClaim = result.FindFirst(OpenIddictConstants.Claims.Role);
        Assert.IsNotNull(roleClaim);
        CollectionAssert.Contains(
            roleClaim.GetDestinations().ToArray(),
            OpenIddictConstants.Destinations.IdentityToken
        );
    }

    private static AuthenticationQueryService CreateSut(
        UserManager<ApplicationUser>? userManager = null,
        SignInManager<ApplicationUser>? signInManager = null
    )
    {
        userManager ??= IdentityTestFactory.CreateUserManager();
        signInManager ??= IdentityTestFactory.CreateSignInManager(userManager);
        return new AuthenticationQueryService(
            userManager,
            signInManager,
            Substitute.For<ILogger<AuthenticationQueryService>>()
        );
    }

    private (AuthenticationQueryService Sut, ApplicationUser User) ArrangeWithUser(
        string userId,
        string email,
        bool emailConfirmed,
        string userName,
        IList<string> roles
    )
    {
        var user = new ApplicationUser { Id = userId };
        var userManager = IdentityTestFactory.CreateUserManager();
        var signInManager = IdentityTestFactory.CreateSignInManager(userManager);

        userManager.FindByIdAsync(userId).Returns(Task.FromResult<ApplicationUser?>(user));
        signInManager.CanSignInAsync(user).Returns(Task.FromResult(true));
        userManager.GetUserIdAsync(user).Returns(Task.FromResult(userId));
        userManager.GetEmailAsync(user).Returns(Task.FromResult<string?>(email));
        userManager.IsEmailConfirmedAsync(user).Returns(Task.FromResult(emailConfirmed));
        userManager.GetUserNameAsync(user).Returns(Task.FromResult<string?>(userName));
        userManager.GetRolesAsync(user).Returns(Task.FromResult(roles));

        return (CreateSut(userManager, signInManager), user);
    }
}
