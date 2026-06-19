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

    // Hosted initialization orchestration: 1400 - 1499
    public const int InitializationHostedServiceStarting = 1400;
    public const int InitializationHostedServiceCompleted = 1401;
    public const int InitializationHostedServiceFailed = 1402;
    public const int InitializationDisabled = 1403;

    // Client query/command service: 1500 - 1599
    public const int ClientListRetrieved = 1500;
    public const int ClientLookupNotFound = 1501;
    public const int ClientCreated = 1502;
    public const int ClientUpdated = 1503;
    public const int ClientDeleted = 1504;
    public const int ClientManagementAlreadyExists = 1505;
    public const int ClientCommandRejected = 1506;
    public const int ClientSecretRotated = 1507;
    public const int ClientPromotedToConfidential = 1508;
    public const int ClientSecretGeneratedOnCreate = 1509;

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
    public const int UserPhoneNumberUpdated = 1613;
    public const int UserPasswordResetTokenGenerated = 1614;

    // Authentication command service: 1700 - 1799
    public const int AuthenticationSignInAttempt = 1700;
    public const int AuthenticationSignInSucceeded = 1701;
    public const int AuthenticationSignInFailed = 1702;
    public const int AuthenticationSignInLockedOut = 1703;
    public const int AuthenticationSignOut = 1704;
    public const int AuthenticationPasswordResetTokenGenerated = 1705;
    public const int AuthenticationPasswordResetSucceeded = 1706;
    public const int AuthenticationPasswordResetFailed = 1707;
    public const int AuthenticationPasswordChanged = 1708;
    public const int AuthenticationPasswordChangeFailed = 1709;
    public const int AuthenticationSignInRefreshed = 1710;
    public const int TwoFactorSignInSucceeded = 1711;
    public const int TwoFactorSignInFailed = 1712;
    public const int TwoFactorSignInLockedOut = 1713;
    public const int RecoveryCodeSignInSucceeded = 1714;
    public const int RecoveryCodeSignInFailed = 1715;
    public const int TwoFactorEnabled = 1716;
    public const int TwoFactorDisabled = 1717;
    public const int AuthenticatorKeyReset = 1718;
    public const int RecoveryCodesGenerated = 1719;

    // User registration service: 1800 - 1899
    public const int UserRegistered = 1800;
    public const int UserRegistrationFailed = 1801;
    public const int UserEmailConfirmed = 1802;
    public const int UserEmailConfirmationFailed = 1803;
    public const int UserEmailConfirmationTokenGenerated = 1804;

    // Rate limiting: 1900 - 1999
    public const int RateLimitExceeded = 1900;

    // Data protection: 2000 - 2099
    public const int DataProtectionRedisUnavailable = 2000;
}
