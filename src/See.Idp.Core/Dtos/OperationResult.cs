namespace See.Idp.Core.Dtos;

public sealed record OperationResult(bool Succeeded, string? Error = null)
{
    public static OperationResult Success() => new(true);

    public static OperationResult Failure(string error) => new(false, error);
}
