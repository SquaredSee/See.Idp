namespace See.Idp.Core.Dtos;

public sealed record UserManagementActionResult(
    bool Succeeded,
    string? Message = null,
    string? Error = null
)
{
    public static UserManagementActionResult Success(string message) => new(true, message, null);

    public static UserManagementActionResult Failure(string error) => new(false, null, error);
}
