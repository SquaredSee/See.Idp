using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Infrastructure;
using See.Idp.Infrastructure.Services;
using See.Idp.Tests.Support;

namespace See.Idp.Tests.Services;

[TestClass]
public sealed class TwoFactorQueryServiceTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task GetTwoFactorInfoAsync_ReturnsNull_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("missing").Returns(Task.FromResult<ApplicationUser?>(null));

        var result = await CreateSut(userManager)
            .GetTwoFactorInfoAsync(new GetTwoFactorInfoQuery("missing"), Ct);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetTwoFactorInfoAsync_ReturnsInfo_WhenUserExists()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        var signInManager = IdentityTestFactory.CreateSignInManager(userManager);
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.GetTwoFactorEnabledAsync(user).Returns(Task.FromResult(true));
        userManager.GetAuthenticatorKeyAsync(user).Returns(Task.FromResult<string?>("SOMEKEY"));
        userManager.CountRecoveryCodesAsync(user).Returns(Task.FromResult(8));
        signInManager.IsTwoFactorClientRememberedAsync(user).Returns(Task.FromResult(false));

        var result = await CreateSut(userManager, signInManager)
            .GetTwoFactorInfoAsync(new GetTwoFactorInfoQuery("u1"), Ct);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsTwoFactorEnabled);
        Assert.IsTrue(result.HasAuthenticator);
        Assert.AreEqual(8, result.RecoveryCodesLeft);
        Assert.IsFalse(result.IsMachineRemembered);
    }

    [TestMethod]
    public async Task GetAuthenticatorSetupAsync_ReturnsNull_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("missing").Returns(Task.FromResult<ApplicationUser?>(null));

        var result = await CreateSut(userManager)
            .GetAuthenticatorSetupAsync(new GetAuthenticatorSetupQuery("missing"), Ct);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetAuthenticatorSetupAsync_ReturnsNull_WhenNoKeyProvisioned()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.GetAuthenticatorKeyAsync(user).Returns(Task.FromResult<string?>(null));

        var result = await CreateSut(userManager)
            .GetAuthenticatorSetupAsync(new GetAuthenticatorSetupQuery("u1"), Ct);

        Assert.IsNull(result);
        await userManager.DidNotReceive().ResetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>());
    }

    [TestMethod]
    public async Task GetAuthenticatorSetupAsync_ReturnsSetupInfo_WhenKeyExists()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .GetAuthenticatorKeyAsync(user)
            .Returns(Task.FromResult<string?>("JBSWY3DPEHPK3PXP"));
        userManager.GetEmailAsync(user).Returns(Task.FromResult<string?>("user@test.com"));

        var result = await CreateSut(userManager)
            .GetAuthenticatorSetupAsync(new GetAuthenticatorSetupQuery("u1"), Ct);

        Assert.IsNotNull(result);
        Assert.AreEqual("JBSW Y3DP EHPK 3PXP", result.SharedKey);
        StringAssert.Contains(result.AuthenticatorUri, "otpauth://totp/");
        await userManager.DidNotReceive().ResetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>());
    }

    private static TwoFactorQueryService CreateSut(
        UserManager<ApplicationUser>? userManager = null,
        SignInManager<ApplicationUser>? signInManager = null
    )
    {
        var um = userManager ?? IdentityTestFactory.CreateUserManager();
        return new TwoFactorQueryService(
            um,
            signInManager ?? IdentityTestFactory.CreateSignInManager(um),
            Substitute.For<ILogger<TwoFactorQueryService>>()
        );
    }
}
