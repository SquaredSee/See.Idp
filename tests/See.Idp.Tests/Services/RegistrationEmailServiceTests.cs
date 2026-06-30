using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using See.Idp.Infrastructure;
using See.Idp.Infrastructure.Services;

namespace See.Idp.Tests.Services;

[TestClass]
public sealed class RegistrationEmailServiceTests
{
    [TestMethod]
    public async Task SendConfirmationLinkAsync_DelegatesToEmailSender()
    {
        var emailSender = Substitute.For<IEmailSender<ApplicationUser>>();
        var sut = new RegistrationEmailService(emailSender);

        await sut.SendConfirmationLinkAsync("user@example.com", "https://example.com/confirm");

        await emailSender
            .Received(1)
            .SendConfirmationLinkAsync(
                Arg.Is<ApplicationUser>(u => u.Email == "user@example.com"),
                "user@example.com",
                "https://example.com/confirm"
            );
    }

    [TestMethod]
    public async Task SendPasswordResetLinkAsync_DelegatesToEmailSender()
    {
        var emailSender = Substitute.For<IEmailSender<ApplicationUser>>();
        var sut = new RegistrationEmailService(emailSender);

        await sut.SendPasswordResetLinkAsync("user@example.com", "https://example.com/reset");

        await emailSender
            .Received(1)
            .SendPasswordResetLinkAsync(
                Arg.Is<ApplicationUser>(u => u.Email == "user@example.com"),
                "user@example.com",
                "https://example.com/reset"
            );
    }

    [TestMethod]
    public async Task SendConfirmationLinkAsync_PropagatesException_WhenSenderFails()
    {
        var emailSender = Substitute.For<IEmailSender<ApplicationUser>>();
        emailSender
            .SendConfirmationLinkAsync(
                Arg.Any<ApplicationUser>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            )
            .ThrowsAsync(new InvalidOperationException("Sender failed"));
        var sut = new RegistrationEmailService(emailSender);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            sut.SendConfirmationLinkAsync("user@example.com", "https://example.com/confirm")
        );
    }

    [TestMethod]
    public async Task SendPasswordResetLinkAsync_PropagatesException_WhenSenderFails()
    {
        var emailSender = Substitute.For<IEmailSender<ApplicationUser>>();
        emailSender
            .SendPasswordResetLinkAsync(
                Arg.Any<ApplicationUser>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            )
            .ThrowsAsync(new InvalidOperationException("Sender failed"));
        var sut = new RegistrationEmailService(emailSender);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            sut.SendPasswordResetLinkAsync("user@example.com", "https://example.com/reset")
        );
    }
}
