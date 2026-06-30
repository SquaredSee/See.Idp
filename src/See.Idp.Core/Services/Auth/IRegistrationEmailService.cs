using System.Threading;
using System.Threading.Tasks;

namespace See.Idp.Core.Services.Auth;

/// <summary>Sends registration-related emails without exposing Infrastructure types.</summary>
public interface IRegistrationEmailService
{
    /// <summary>Sends an email confirmation link to the specified address.</summary>
    Task SendConfirmationLinkAsync(
        string email,
        string confirmationLink,
        CancellationToken ct = default
    );

    /// <summary>Sends a password reset link to the specified address.</summary>
    Task SendPasswordResetLinkAsync(string email, string resetLink, CancellationToken ct = default);
}
