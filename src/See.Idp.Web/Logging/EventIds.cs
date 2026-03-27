namespace See.Idp.Web.Logging;

/// <summary>
///     Event IDs for structured logging in See.Idp.Web.
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
}
