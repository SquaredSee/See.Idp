namespace See.Idp.Infrastructure.Logging;

/// <summary>
///     Event IDs for structured logging in See.Idp.
/// </summary>
public static class EventIds
{
    // Client initialization: 1000 - 1099
    public const int SeedingClient = 1000;
    public const int ClientSeeded = 1001;
    public const int ClientAlreadyExists = 1002;

    // User/role initialization: 1100 - 1199
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

    // Hosted initialization orchestration: 1400 - 1499
    public const int InitializationHostedServiceStarting = 1400;
    public const int InitializationHostedServiceCompleted = 1401;
    public const int InitializationHostedServiceFailed = 1402;

    // Client query/command service: 1500 - 1599
    public const int ClientListRetrieved = 1500;
    public const int ClientLookupNotFound = 1501;
    public const int ClientCreated = 1502;
    public const int ClientUpdated = 1503;
    public const int ClientDeleted = 1504;
    public const int ClientManagementAlreadyExists = 1505;
    public const int ClientCommandRejected = 1506;

    // User query/command service: 1600 - 1699
    public const int UserListRetrieved = 1600;
    public const int RoleCreated = 1601;
    public const int RoleManagementAlreadyExists = 1602;
    public const int UserCreated = 1603;
    public const int UserManagementAlreadyExists = 1604;
    public const int UserAlreadyInRole = 1605;
    public const int UserRoleAdded = 1606;
    public const int UserAdminGranted = 1607;
    public const int UserAdminRemoved = 1608;
    public const int UserLocked = 1609;
    public const int UserUnlocked = 1610;
    public const int UserDeleted = 1611;
    public const int UserCommandRejected = 1612;

    // Authentication command service: 1700 - 1799
    public const int AuthenticationSignInAttempt = 1700;
    public const int AuthenticationSignInSucceeded = 1701;
    public const int AuthenticationSignInFailed = 1702;
    public const int AuthenticationSignInLockedOut = 1703;
    public const int AuthenticationSignOut = 1704;
}
