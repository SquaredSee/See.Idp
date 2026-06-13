using System.Collections.Generic;
using System.Linq;
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
public sealed class TwoFactorServiceTests
{
    [TestMethod]
    public async Task GetTwoFactorInfoAsync_ReturnsNull_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(Arg.Any<string>()).Returns((ApplicationUser?)null);

        var result = await CreateSut(userManager)
            .GetTwoFactorInfoAsync(new GetTwoFactorInfoQuery("missing-id"));

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetTwoFactorInfoAsync_ReturnsInfo_WhenUserExists()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        var signInManager = IdentityTestFactory.CreateSignInManager(userManager);
        userManager.FindByIdAsync("u1").Returns(user);
        userManager.GetTwoFactorEnabledAsync(user).Returns(true);
        userManager.GetAuthenticatorKeyAsync(user).Returns("SOMEKEY");
        userManager.CountRecoveryCodesAsync(user).Returns(8);
        signInManager.IsTwoFactorClientRememberedAsync(user).Returns(false);

        var result = await CreateSut(userManager, signInManager)
            .GetTwoFactorInfoAsync(new GetTwoFactorInfoQuery("u1"));

        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsTwoFactorEnabled);
        Assert.IsTrue(result.HasAuthenticator);
        Assert.AreEqual(8, result.RecoveryCodesLeft);
    }

    [TestMethod]
    public async Task GetAuthenticatorSetupAsync_ReturnsNull_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(Arg.Any<string>()).Returns((ApplicationUser?)null);

        var result = await CreateSut(userManager)
            .GetAuthenticatorSetupAsync(new GetAuthenticatorSetupQuery("missing-id"));

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetAuthenticatorSetupAsync_ResetsKey_WhenNoKeyExists()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(user);
        userManager.GetAuthenticatorKeyAsync(user).Returns((string?)null, "JBSWY3DPEHPK3PXP");
        userManager.ResetAuthenticatorKeyAsync(user).Returns(IdentityResult.Success);
        userManager.GetEmailAsync(user).Returns("user@test.com");

        var result = await CreateSut(userManager)
            .GetAuthenticatorSetupAsync(new GetAuthenticatorSetupQuery("u1"));

        Assert.IsNotNull(result);
        await userManager.Received(1).ResetAuthenticatorKeyAsync(user);
        Assert.IsFalse(string.IsNullOrEmpty(result.SharedKey));
        StringAssert.Contains(result.AuthenticatorUri, "otpauth://totp/");
    }

    [TestMethod]
    public async Task GetAuthenticatorSetupAsync_ReturnsExistingKey_WhenKeyAlreadySet()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(user);
        userManager.GetAuthenticatorKeyAsync(user).Returns("JBSWY3DPEHPK3PXP");
        userManager.GetEmailAsync(user).Returns("user@test.com");

        var result = await CreateSut(userManager)
            .GetAuthenticatorSetupAsync(new GetAuthenticatorSetupQuery("u1"));

        Assert.IsNotNull(result);
        await userManager.DidNotReceive().ResetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>());
        Assert.AreEqual("JBSW Y3DP EHPK 3PXP", result.SharedKey);
    }

    [TestMethod]
    public async Task EnableTwoFactorAsync_ReturnsFailure_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(Arg.Any<string>()).Returns((ApplicationUser?)null);

        var result = await CreateSut(userManager)
            .EnableTwoFactorAsync(new EnableTwoFactorCommand("missing-id", "123456"));

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task EnableTwoFactorAsync_ReturnsFailure_WhenCodeIsInvalid()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(user);
        userManager
            .VerifyTwoFactorTokenAsync(user, Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        var result = await CreateSut(userManager)
            .EnableTwoFactorAsync(new EnableTwoFactorCommand("u1", "000000"));

        Assert.IsFalse(result.Succeeded);
        StringAssert.Contains(result.Error, "invalid");
    }

    [TestMethod]
    public async Task EnableTwoFactorAsync_ReturnsRecoveryCodes_WhenCodeIsValid()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(user);
        userManager
            .VerifyTwoFactorTokenAsync(user, Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);
        userManager.SetTwoFactorEnabledAsync(user, true).Returns(IdentityResult.Success);
        userManager
            .GenerateNewTwoFactorRecoveryCodesAsync(user, 10)
            .Returns(
                (IEnumerable<string>)
                    new[]
                    {
                        "code1",
                        "code2",
                        "code3",
                        "code4",
                        "code5",
                        "code6",
                        "code7",
                        "code8",
                        "code9",
                        "code10",
                    }
            );

        var result = await CreateSut(userManager)
            .EnableTwoFactorAsync(new EnableTwoFactorCommand("u1", "123456"));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(10, result.Codes.Count());
    }

    [TestMethod]
    public async Task DisableTwoFactorAsync_ReturnsSuccess_WhenUserExists()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(user);
        userManager.SetTwoFactorEnabledAsync(user, false).Returns(IdentityResult.Success);

        var result = await CreateSut(userManager)
            .DisableTwoFactorAsync(new DisableTwoFactorCommand("u1"));

        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public async Task GenerateRecoveryCodesAsync_ReturnsCodes_WhenUserExists()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(user);
        userManager
            .GenerateNewTwoFactorRecoveryCodesAsync(user, 10)
            .Returns(
                (IEnumerable<string>)new[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j" }
            );

        var result = await CreateSut(userManager)
            .GenerateRecoveryCodesAsync(new GenerateRecoveryCodesCommand("u1"));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(10, result.Codes.Count());
    }

    [TestMethod]
    public async Task ResetAuthenticatorKeyAsync_DisablesTwoFactorAndResetsKey()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(user);
        userManager.SetTwoFactorEnabledAsync(user, false).Returns(IdentityResult.Success);
        userManager.ResetAuthenticatorKeyAsync(user).Returns(IdentityResult.Success);

        var result = await CreateSut(userManager)
            .ResetAuthenticatorKeyAsync(new ResetAuthenticatorKeyCommand("u1"));

        Assert.IsTrue(result.Succeeded);
        await userManager.Received(1).SetTwoFactorEnabledAsync(user, false);
        await userManager.Received(1).ResetAuthenticatorKeyAsync(user);
    }

    private static TwoFactorService CreateSut(
        UserManager<ApplicationUser>? userManager = null,
        SignInManager<ApplicationUser>? signInManager = null
    ) =>
        new(
            userManager ?? IdentityTestFactory.CreateUserManager(),
            signInManager ?? IdentityTestFactory.CreateSignInManager(),
            Substitute.For<ILogger<TwoFactorService>>()
        );
}
