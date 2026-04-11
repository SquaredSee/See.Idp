namespace See.Idp.Core.Dtos;

public sealed record ClientApplicationActionResult(bool Succeeded, string? Error = null)
{
    public static ClientApplicationActionResult Success() => new(true);

    public static ClientApplicationActionResult Failure(string error) => new(false, error);
}
