namespace See.Idp.Core.Logging;

/// <summary>
///     Event IDs for structured logging in See.Idp.
/// </summary>
public static class EventIds
{
    // UiClientSeeder: 1000 - 1099
    public const int SeedingClient = 1000;
    public const int ClientSeeded = 1001;
    public const int ClientAlreadyExists = 1002;

    // UserSeeder: 1100 - 1199
    public const int SeedingUser = 1100;
    public const int UserSeeded = 1101;
    public const int UserAlreadyExists = 1102;
    public const int UserAddedToRole = 1103;
    public const int SeedingRole = 1104;
    public const int RoleSeeded = 1105;
    public const int RoleAlreadyExists = 1106;

    // Login: 1200 - 1299
    public const int LoginAttempt = 1200;
    public const int LoginSuccess = 1201;
    public const int LoginFailed = 1202;
    public const int LoginLockedOut = 1203;

    // Logout: 1300 - 1399
    public const int Logout = 1300;
}
