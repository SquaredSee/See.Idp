namespace See.Idp.Core.Dtos;

public sealed record AdminUserDto(
    string UserId,
    string? UserName,
    string? Email,
    bool EmailConfirmed,
    bool IsAdmin,
    bool IsLockedOut,
    bool IsCurrentUser
);
