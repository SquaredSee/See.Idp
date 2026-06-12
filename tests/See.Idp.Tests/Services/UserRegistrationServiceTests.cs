using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using See.Idp.Core.Dtos.Users;
using See.Idp.Infrastructure;
using See.Idp.Infrastructure.Services;
using See.Idp.Tests.Support;

namespace See.Idp.Tests.Services;

[TestClass]
public sealed class UserRegistrationServiceTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task RegisterAsync_ReturnsSuccess_WithUserIdAndEncodedToken()
    {
        const string rawToken = "raw-confirmation-token";

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager
            .CreateAsync(Arg.Any<ApplicationUser>(), "Password1!")
            .Returns(Task.FromResult(IdentityResult.Success));
        userManager.GetUserIdAsync(Arg.Any<ApplicationUser>()).Returns(Task.FromResult("new-id"));
        userManager
            .GenerateEmailConfirmationTokenAsync(Arg.Any<ApplicationUser>())
            .Returns(Task.FromResult(rawToken));

        var sut = CreateSut(userManager);

        var result = await sut.RegisterAsync(
            new RegisterUserCommand("new@example.com", "Password1!"),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("new-id", result.UserId);
        Assert.IsNotNull(result.EmailConfirmationToken);
        Assert.AreEqual(0, result.Errors.Count);

        var decoded = Encoding.UTF8.GetString(
            WebEncoders.Base64UrlDecode(result.EmailConfirmationToken)
        );
        Assert.AreEqual(rawToken, decoded);
    }

    [TestMethod]
    public async Task RegisterAsync_ReturnsFailure_WhenIdentityFails()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager
            .CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(
                Task.FromResult(
                    IdentityTestFactory.FailedResult("Password too weak.", "Email already taken.")
                )
            );

        var sut = CreateSut(userManager);

        var result = await sut.RegisterAsync(
            new RegisterUserCommand("taken@example.com", "weak"),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
        Assert.IsNull(result.UserId);
        Assert.IsNull(result.EmailConfirmationToken);
        Assert.HasCount(2, result.Errors);
        CollectionAssert.Contains(
            (System.Collections.ICollection)result.Errors,
            "Password too weak."
        );
        CollectionAssert.Contains(
            (System.Collections.ICollection)result.Errors,
            "Email already taken."
        );
    }

    [TestMethod]
    public async Task ConfirmEmailAsync_ReturnsSuccess_WhenTokenIsValid()
    {
        const string rawToken = "raw-token";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
        var user = new ApplicationUser { Id = "user-1" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("user-1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .ConfirmEmailAsync(user, rawToken)
            .Returns(Task.FromResult(IdentityResult.Success));

        var sut = CreateSut(userManager);

        var result = await sut.ConfirmEmailAsync(
            new ConfirmEmailCommand("user-1", encodedToken),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public async Task ConfirmEmailAsync_ReturnsFailure_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("missing").Returns(Task.FromResult<ApplicationUser?>(null));

        var sut = CreateSut(userManager);

        var result = await sut.ConfirmEmailAsync(
            new ConfirmEmailCommand("missing", "any-token"),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
    }

    [TestMethod]
    public async Task ConfirmEmailAsync_ReturnsFailure_WhenTokenIsInvalid()
    {
        const string rawToken = "bad-token";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
        var user = new ApplicationUser { Id = "user-1" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("user-1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .ConfirmEmailAsync(user, rawToken)
            .Returns(Task.FromResult(IdentityTestFactory.FailedResult("Invalid token.")));

        var sut = CreateSut(userManager);

        var result = await sut.ConfirmEmailAsync(
            new ConfirmEmailCommand("user-1", encodedToken),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
    }

    [TestMethod]
    public async Task GeneratePasswordResetTokenAsync_ReturnsEncodedToken_WhenUserExistsAndEmailConfirmed()
    {
        const string rawToken = "reset-token";
        var user = new ApplicationUser { Id = "user-1", Email = "user@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager
            .FindByEmailAsync("user@example.com")
            .Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.IsEmailConfirmedAsync(user).Returns(Task.FromResult(true));
        userManager.GeneratePasswordResetTokenAsync(user).Returns(Task.FromResult(rawToken));

        var sut = CreateSut(userManager);

        var result = await sut.GeneratePasswordResetTokenAsync(
            new GeneratePasswordResetTokenCommand("user@example.com"),
            Ct
        );

        Assert.IsNotNull(result);
        var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(result));
        Assert.AreEqual(rawToken, decoded);
    }

    [TestMethod]
    public async Task GeneratePasswordResetTokenAsync_ReturnsNull_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager
            .FindByEmailAsync("missing@example.com")
            .Returns(Task.FromResult<ApplicationUser?>(null));

        var sut = CreateSut(userManager);

        var result = await sut.GeneratePasswordResetTokenAsync(
            new GeneratePasswordResetTokenCommand("missing@example.com"),
            Ct
        );

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GeneratePasswordResetTokenAsync_ReturnsNull_WhenEmailNotConfirmed()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "unconfirmed@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager
            .FindByEmailAsync("unconfirmed@example.com")
            .Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.IsEmailConfirmedAsync(user).Returns(Task.FromResult(false));

        var sut = CreateSut(userManager);

        var result = await sut.GeneratePasswordResetTokenAsync(
            new GeneratePasswordResetTokenCommand("unconfirmed@example.com"),
            Ct
        );

        Assert.IsNull(result);
        await userManager
            .DidNotReceive()
            .GeneratePasswordResetTokenAsync(Arg.Any<ApplicationUser>());
    }

    private static UserRegistrationService CreateSut(
        UserManager<ApplicationUser>? userManager = null
    )
    {
        var effectiveUserManager = userManager ?? IdentityTestFactory.CreateUserManager();
        var logger = Substitute.For<ILogger<UserRegistrationService>>();

        return new UserRegistrationService(effectiveUserManager, logger);
    }
}
