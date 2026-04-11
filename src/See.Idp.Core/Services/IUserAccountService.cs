using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos;

namespace See.Idp.Core.Services;

public interface IUserAccountService
{
    Task<AccountSignInResult> PasswordSignInAsync(
        string email,
        string password,
        bool rememberMe,
        CancellationToken ct = default
    );

    Task SignOutAsync(CancellationToken ct = default);
}
