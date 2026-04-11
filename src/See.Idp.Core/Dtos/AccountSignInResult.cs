namespace See.Idp.Core.Dtos;

public sealed record AccountSignInResult(bool Succeeded, bool IsLockedOut, string? Error = null)
{
    public static AccountSignInResult Success() => new(true, false);

    public static AccountSignInResult LockedOut() => new(false, true);

    public static AccountSignInResult Failure(string error) => new(false, false, error);
}
