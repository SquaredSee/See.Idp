using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Infrastructure;
using See.Idp.Infrastructure.Services;
using See.Idp.Tests.Support;

namespace See.Idp.Tests.Services;

[TestClass]
public sealed class PasswordCommandServiceTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task GeneratePasswordResetTokenAsync_ReturnsSuccess_WhenUserExistsAndEmailConfirmed()
    {
        const string rawToken = "reset-token";
        var user = new ApplicationUser { Id = "user-1", Email = "user@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager
            .FindByEmailAsync("user@example.com")
            .Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.IsEmailConfirmedAsync(user).Returns(Task.FromResult(true));
        userManager.GeneratePasswordResetTokenAsync(user).Returns(Task.FromResult(rawToken));

        var result = await CreateSut(userManager)
            .GeneratePasswordResetTokenAsync(
                new GeneratePasswordResetTokenCommand("user@example.com"),
                Ct
            );

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.Token);
        var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(result.Token));
        Assert.AreEqual(rawToken, decoded);
    }

    [TestMethod]
    public async Task GeneratePasswordResetTokenAsync_ReturnsFailed_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager
            .FindByEmailAsync("missing@example.com")
            .Returns(Task.FromResult<ApplicationUser?>(null));

        var result = await CreateSut(userManager)
            .GeneratePasswordResetTokenAsync(
                new GeneratePasswordResetTokenCommand("missing@example.com"),
                Ct
            );

        Assert.IsFalse(result.Succeeded);
        Assert.IsNull(result.Token);
        Assert.IsNull(result.Error);
    }

    [TestMethod]
    public async Task GeneratePasswordResetTokenAsync_ReturnsFailed_WhenEmailNotConfirmed()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "unconfirmed@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager
            .FindByEmailAsync("unconfirmed@example.com")
            .Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.IsEmailConfirmedAsync(user).Returns(Task.FromResult(false));

        var result = await CreateSut(userManager)
            .GeneratePasswordResetTokenAsync(
                new GeneratePasswordResetTokenCommand("unconfirmed@example.com"),
                Ct
            );

        Assert.IsFalse(result.Succeeded);
        Assert.IsNull(result.Token);
        await userManager
            .DidNotReceive()
            .GeneratePasswordResetTokenAsync(Arg.Any<ApplicationUser>());
    }

    [TestMethod]
    public async Task ResetPasswordAsync_ReturnsSuccess_WhenTokenIsValid()
    {
        const string rawCode = "valid-token";
        var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawCode));
        var user = new ApplicationUser { Id = "user-1", Email = "user@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager
            .FindByEmailAsync("user@example.com")
            .Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .ResetPasswordAsync(user, rawCode, "NewPass1!")
            .Returns(Task.FromResult(IdentityResult.Success));

        var result = await CreateSut(userManager)
            .ResetPasswordAsync(
                new ResetPasswordCommand("user@example.com", encodedCode, "NewPass1!"),
                Ct
            );

        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public async Task ResetPasswordAsync_ReturnsFailure_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager
            .FindByEmailAsync("missing@example.com")
            .Returns(Task.FromResult<ApplicationUser?>(null));

        var result = await CreateSut(userManager)
            .ResetPasswordAsync(
                new ResetPasswordCommand("missing@example.com", "any-code", "NewPass1!"),
                Ct
            );

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task ChangePasswordAsync_ReturnsSuccess_WhenPasswordIsCorrect()
    {
        var user = new ApplicationUser { Id = "user-1" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("user-1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .ChangePasswordAsync(user, "OldPass1!", "NewPass1!")
            .Returns(Task.FromResult(IdentityResult.Success));

        var signInManager = IdentityTestFactory.CreateSignInManager(userManager);

        var result = await CreateSut(userManager, signInManager)
            .ChangePasswordAsync(new ChangePasswordCommand("user-1", "OldPass1!", "NewPass1!"), Ct);

        Assert.IsTrue(result.Succeeded);
        await signInManager.Received(1).RefreshSignInAsync(user);
    }

    [TestMethod]
    public async Task ChangePasswordAsync_ReturnsFailure_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("missing").Returns(Task.FromResult<ApplicationUser?>(null));

        var result = await CreateSut(userManager)
            .ChangePasswordAsync(
                new ChangePasswordCommand("missing", "OldPass1!", "NewPass1!"),
                Ct
            );

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
    }

    private static PasswordCommandService CreateSut(
        UserManager<ApplicationUser>? userManager = null,
        SignInManager<ApplicationUser>? signInManager = null
    )
    {
        var um = userManager ?? IdentityTestFactory.CreateUserManager();
        return new PasswordCommandService(
            um,
            signInManager ?? IdentityTestFactory.CreateSignInManager(um),
            Substitute.For<ILogger<PasswordCommandService>>()
        );
    }
}
