using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace See.Idp.Infrastructure.Services;

/// <summary>
///     A no-op email sender that silently discards all outgoing identity emails.
///     Replace with a real implementation when email delivery is needed.
/// </summary>
public sealed class NoOpEmailSender : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(
        ApplicationUser user,
        string email,
        string confirmationLink
    ) => Task.CompletedTask;

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        Task.CompletedTask;

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        Task.CompletedTask;
}
