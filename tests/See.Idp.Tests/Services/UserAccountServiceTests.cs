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
public sealed class UserAccountServiceTests
{
    [TestMethod]
    public async Task PasswordSignInAsync_ReturnsTwoFactorRequired_WhenSignInRequiresTwoFactor()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        var signInManager = IdentityTestFactory.CreateSignInManager(userManager);
        signInManager
            .PasswordSignInAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<bool>()
            )
            .Returns(SignInResult.TwoFactorRequired);

        var result = await CreateSut(userManager, signInManager)
            .PasswordSignInAsync(new PasswordSignInCommand("u@t.com", "pw", false));

        Assert.IsTrue(result.RequiresTwoFactor);
        Assert.IsFalse(result.Succeeded);
        Assert.IsFalse(result.IsLockedOut);
    }

    [TestMethod]
    public async Task TwoFactorSignInAsync_ReturnsSuccess_WhenCodeIsValid()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        var signInManager = IdentityTestFactory.CreateSignInManager(userManager);
        signInManager
            .TwoFactorAuthenticatorSignInAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(SignInResult.Success);

        var result = await CreateSut(userManager, signInManager)
            .TwoFactorSignInAsync(new TwoFactorSignInCommand("123456", false, false));

        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public async Task TwoFactorSignInAsync_ReturnsLockedOut_WhenUserIsLockedOut()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        var signInManager = IdentityTestFactory.CreateSignInManager(userManager);
        signInManager
            .TwoFactorAuthenticatorSignInAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(SignInResult.LockedOut);

        var result = await CreateSut(userManager, signInManager)
            .TwoFactorSignInAsync(new TwoFactorSignInCommand("123456", false, false));

        Assert.IsTrue(result.IsLockedOut);
        Assert.IsFalse(result.Succeeded);
    }

    [TestMethod]
    public async Task TwoFactorSignInAsync_ReturnsFailure_WhenCodeIsInvalid()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        var signInManager = IdentityTestFactory.CreateSignInManager(userManager);
        signInManager
            .TwoFactorAuthenticatorSignInAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(SignInResult.Failed);

        var result = await CreateSut(userManager, signInManager)
            .TwoFactorSignInAsync(new TwoFactorSignInCommand("000000", false, false));

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task RecoveryCodeSignInAsync_ReturnsSuccess_WhenCodeIsValid()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        var signInManager = IdentityTestFactory.CreateSignInManager(userManager);
        signInManager
            .TwoFactorRecoveryCodeSignInAsync(Arg.Any<string>())
            .Returns(SignInResult.Success);

        var result = await CreateSut(userManager, signInManager)
            .RecoveryCodeSignInAsync(new RecoveryCodeSignInCommand("abc-def-ghi"));

        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public async Task RecoveryCodeSignInAsync_ReturnsFailure_WhenCodeIsInvalid()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        var signInManager = IdentityTestFactory.CreateSignInManager(userManager);
        signInManager
            .TwoFactorRecoveryCodeSignInAsync(Arg.Any<string>())
            .Returns(SignInResult.Failed);

        var result = await CreateSut(userManager, signInManager)
            .RecoveryCodeSignInAsync(new RecoveryCodeSignInCommand("bad-code"));

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
    }

    private static UserAccountService CreateSut(
        UserManager<ApplicationUser>? userManager = null,
        SignInManager<ApplicationUser>? signInManager = null
    ) =>
        new(
            userManager ?? IdentityTestFactory.CreateUserManager(),
            signInManager ?? IdentityTestFactory.CreateSignInManager(),
            Substitute.For<ILogger<UserAccountService>>()
        );
}
