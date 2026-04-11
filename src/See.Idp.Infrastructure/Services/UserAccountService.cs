using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using See.Idp.Core.Dtos;
using See.Idp.Core.Services;

namespace See.Idp.Infrastructure.Services;

// TODO: implement cancellation token support in SignInManager
public sealed class UserAccountService(SignInManager<IdentityUser> signInManager)
    : IUserAccountService
{
    public async Task<AccountSignInResult> PasswordSignInAsync(
        string email,
        string password,
        bool rememberMe,
        CancellationToken ct = default
    )
    {
        var result = await signInManager.PasswordSignInAsync(
            email,
            password,
            rememberMe,
            lockoutOnFailure: true
        );

        if (result.Succeeded)
        {
            return AccountSignInResult.Success();
        }

        if (result.IsLockedOut)
        {
            return AccountSignInResult.LockedOut();
        }

        return AccountSignInResult.Failure(
            "Login failed. Please check your credentials and try again."
        );
    }

    public Task SignOutAsync(CancellationToken ct = default)
    {
        return signInManager.SignOutAsync();
    }
}
