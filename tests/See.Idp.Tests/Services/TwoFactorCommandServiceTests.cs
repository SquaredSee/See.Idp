using System;
using System.Collections.Generic;
using System.Linq;
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
public sealed class TwoFactorCommandServiceTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task ProvisionAuthenticatorKeyAsync_ProvisionesKey_WhenNoneExists()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.GetAuthenticatorKeyAsync(user).Returns(Task.FromResult<string?>(null));
        userManager
            .ResetAuthenticatorKeyAsync(user)
            .Returns(Task.FromResult(IdentityResult.Success));

        var result = await CreateSut(userManager)
            .ProvisionAuthenticatorKeyAsync(new ProvisionAuthenticatorKeyCommand("u1"), Ct);

        Assert.IsTrue(result.Succeeded);
        await userManager.Received(1).ResetAuthenticatorKeyAsync(user);
    }

    [TestMethod]
    public async Task ProvisionAuthenticatorKeyAsync_IsNoOp_WhenKeyAlreadyExists()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.GetAuthenticatorKeyAsync(user).Returns(Task.FromResult<string?>("EXISTINGKEY"));

        var result = await CreateSut(userManager)
            .ProvisionAuthenticatorKeyAsync(new ProvisionAuthenticatorKeyCommand("u1"), Ct);

        Assert.IsTrue(result.Succeeded);
        await userManager.DidNotReceive().ResetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>());
    }

    [TestMethod]
    public async Task ProvisionAuthenticatorKeyAsync_ReturnsFailure_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("missing").Returns(Task.FromResult<ApplicationUser?>(null));

        var result = await CreateSut(userManager)
            .ProvisionAuthenticatorKeyAsync(new ProvisionAuthenticatorKeyCommand("missing"), Ct);

        Assert.IsFalse(result.Succeeded);
    }

    [TestMethod]
    public async Task ProvisionAuthenticatorKeyAsync_ReturnsFailure_WhenResetAuthenticatorKeyFails()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.GetAuthenticatorKeyAsync(user).Returns(Task.FromResult<string?>(null));
        userManager
            .ResetAuthenticatorKeyAsync(user)
            .Returns(Task.FromResult(IdentityTestFactory.FailedResult("Key reset failed.")));

        var result = await CreateSut(userManager)
            .ProvisionAuthenticatorKeyAsync(new ProvisionAuthenticatorKeyCommand("u1"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task EnableTwoFactorAsync_ReturnsFailure_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("missing").Returns(Task.FromResult<ApplicationUser?>(null));

        var result = await CreateSut(userManager)
            .EnableTwoFactorAsync(new EnableTwoFactorCommand("missing", "123456"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task EnableTwoFactorAsync_ReturnsFailure_WhenCodeIsInvalid()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .VerifyTwoFactorTokenAsync(user, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await CreateSut(userManager)
            .EnableTwoFactorAsync(new EnableTwoFactorCommand("u1", "000000"), Ct);

        Assert.IsFalse(result.Succeeded);
        StringAssert.Contains(result.Error, "invalid");
    }

    [TestMethod]
    public async Task EnableTwoFactorAsync_ReturnsFailure_WhenSetTwoFactorEnabledFails()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .VerifyTwoFactorTokenAsync(user, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        userManager
            .SetTwoFactorEnabledAsync(user, true)
            .Returns(
                Task.FromResult(IdentityTestFactory.FailedResult("Cannot enable two-factor."))
            );

        var result = await CreateSut(userManager)
            .EnableTwoFactorAsync(new EnableTwoFactorCommand("u1", "123456"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
        await userManager
            .DidNotReceive()
            .GenerateNewTwoFactorRecoveryCodesAsync(Arg.Any<ApplicationUser>(), Arg.Any<int>());
    }

    [TestMethod]
    public async Task EnableTwoFactorAsync_ReturnsRecoveryCodes_WhenCodeIsValid()
    {
        var user = new ApplicationUser { Id = "u1" };
        var codes = new[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .VerifyTwoFactorTokenAsync(user, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        userManager
            .SetTwoFactorEnabledAsync(user, true)
            .Returns(Task.FromResult(IdentityResult.Success));
        userManager
            .GenerateNewTwoFactorRecoveryCodesAsync(user, 10)
            .Returns(Task.FromResult<IEnumerable<string>?>(codes));

        var result = await CreateSut(userManager)
            .EnableTwoFactorAsync(new EnableTwoFactorCommand("u1", "123456"), Ct);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(10, result.RecoveryCodes.Count());
    }

    [TestMethod]
    public async Task DisableTwoFactorAsync_ReturnsSuccess_WhenUserExists()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .SetTwoFactorEnabledAsync(user, false)
            .Returns(Task.FromResult(IdentityResult.Success));

        var result = await CreateSut(userManager)
            .DisableTwoFactorAsync(new DisableTwoFactorCommand("u1"), Ct);

        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public async Task ResetAuthenticatorKeyAsync_DisablesTwoFactorAndResetsKey()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .SetTwoFactorEnabledAsync(user, false)
            .Returns(Task.FromResult(IdentityResult.Success));
        userManager
            .ResetAuthenticatorKeyAsync(user)
            .Returns(Task.FromResult(IdentityResult.Success));

        var result = await CreateSut(userManager)
            .ResetAuthenticatorKeyAsync(new ResetAuthenticatorKeyCommand("u1"), Ct);

        Assert.IsTrue(result.Succeeded);
        await userManager.Received(1).SetTwoFactorEnabledAsync(user, false);
        await userManager.Received(1).ResetAuthenticatorKeyAsync(user);
    }

    [TestMethod]
    public async Task ResetAuthenticatorKeyAsync_ReturnsFailure_WhenSetTwoFactorEnabledFails()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .SetTwoFactorEnabledAsync(user, false)
            .Returns(
                Task.FromResult(IdentityTestFactory.FailedResult("Cannot disable two-factor."))
            );

        var result = await CreateSut(userManager)
            .ResetAuthenticatorKeyAsync(new ResetAuthenticatorKeyCommand("u1"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
        await userManager.DidNotReceive().ResetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>());
    }

    [TestMethod]
    public async Task ResetAuthenticatorKeyAsync_ReturnsFailure_WhenResetAuthenticatorKeyFails()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .SetTwoFactorEnabledAsync(user, false)
            .Returns(Task.FromResult(IdentityResult.Success));
        userManager
            .ResetAuthenticatorKeyAsync(user)
            .Returns(Task.FromResult(IdentityTestFactory.FailedResult("Key reset failed.")));

        var result = await CreateSut(userManager)
            .ResetAuthenticatorKeyAsync(new ResetAuthenticatorKeyCommand("u1"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task GenerateRecoveryCodesAsync_ReturnsCodes_WhenUserExists()
    {
        var user = new ApplicationUser { Id = "u1" };
        var codes = new[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .GenerateNewTwoFactorRecoveryCodesAsync(user, 10)
            .Returns(Task.FromResult<IEnumerable<string>?>(codes));

        var result = await CreateSut(userManager)
            .GenerateRecoveryCodesAsync(new GenerateRecoveryCodesCommand("u1"), Ct);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(10, result.Codes.Count());
    }

    [TestMethod]
    public async Task DisableTwoFactorAsync_ReturnsFailure_WhenSetTwoFactorEnabledFails()
    {
        var user = new ApplicationUser { Id = "u1" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("u1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .SetTwoFactorEnabledAsync(user, false)
            .Returns(
                Task.FromResult(IdentityTestFactory.FailedResult("Cannot disable two-factor."))
            );

        var result = await CreateSut(userManager)
            .DisableTwoFactorAsync(new DisableTwoFactorCommand("u1"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
    }

    private static TwoFactorCommandService CreateSut(
        UserManager<ApplicationUser>? userManager = null
    ) =>
        new(
            userManager ?? IdentityTestFactory.CreateUserManager(),
            Substitute.For<ILogger<TwoFactorCommandService>>()
        );
}
