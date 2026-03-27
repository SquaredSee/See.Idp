namespace See.Idp.Web.Logging;

/// <summary>
///     Event IDs for structured logging in See.Idp.Web.
/// </summary>
public static class EventIds
{
    // UiClientSeeder: 1000 - 1099

    /// <summary>
    ///     Indicates that the client seeding process has started for the user interface.
    /// </summary>
    public const int SeedingClient = 1000;

    /// <summary>
    ///     Indicates that the client has been successfully seeded for the user interface.
    /// </summary>
    public const int ClientSeeded = 1001;

    /// <summary>
    ///     Indicates that the client already exists and seeding is skipped for the user interface.
    /// </summary>
    public const int ClientAlreadyExists = 1002;
}
