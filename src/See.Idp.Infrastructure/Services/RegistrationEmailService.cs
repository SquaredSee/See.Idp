using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using See.Idp.Core.Services.Auth;

namespace See.Idp.Infrastructure.Services;

public sealed class RegistrationEmailService(IEmailSender<ApplicationUser> emailSender)
    : IRegistrationEmailService
{
    public Task SendConfirmationLinkAsync(
        string email,
        string confirmationLink,
        CancellationToken ct = default
    ) =>
        emailSender.SendConfirmationLinkAsync(
            new ApplicationUser { Email = email, UserName = email },
            email,
            confirmationLink
        );

    public Task SendPasswordResetLinkAsync(
        string email,
        string resetLink,
        CancellationToken ct = default
    ) =>
        emailSender.SendPasswordResetLinkAsync(
            new ApplicationUser { Email = email, UserName = email },
            email,
            resetLink
        );
}
