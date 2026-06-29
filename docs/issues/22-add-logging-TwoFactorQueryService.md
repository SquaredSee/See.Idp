# 22 — Add structured logging to `TwoFactorQueryService`

## Context

`TwoFactorQueryService` accepts an `ILogger<TwoFactorQueryService>` constructor parameter
but never uses it, producing compiler warning `CS9113`. Every other query and command
service in the codebase logs key events using the `[LoggerMessage]` source-generator
pattern. `TwoFactorQueryService` is the only service that is completely silent.

## Fix

Implement `[LoggerMessage]` methods for the observable events in each query method and
add the corresponding event IDs to `EventIds.cs`.

### New event IDs — `EventIds.cs`

Extend the authentication / two-factor block (1700–1799):

```csharp
// Two-factor query service
public const int TwoFactorInfoRetrieved = 1721;
public const int TwoFactorSetupRetrieved = 1722;
public const int TwoFactorUserNotFound = 1723;
public const int TwoFactorKeyNotProvisioned = 1724;
```

### `TwoFactorQueryService` changes

Add log calls in each method and `[LoggerMessage]` partial method declarations following
the existing pattern in `TwoFactorCommandService`:

```csharp
// GetTwoFactorInfoAsync:
if (user is null) { LogTwoFactorUserNotFound(query.UserId); return null; }
// ... build result ...
LogTwoFactorInfoRetrieved(query.UserId);
return result;

// GetAuthenticatorSetupAsync:
if (user is null) { LogTwoFactorUserNotFound(query.UserId); return null; }
if (string.IsNullOrEmpty(key)) { LogTwoFactorKeyNotProvisioned(query.UserId); return null; }
// ... build result ...
LogTwoFactorSetupRetrieved(query.UserId);
return result;

[LoggerMessage(EventId = EventIds.TwoFactorUserNotFound, Level = LogLevel.Warning,
    Message = "Two-factor query failed: user {UserId} not found")]
private partial void LogTwoFactorUserNotFound(string userId);

[LoggerMessage(EventId = EventIds.TwoFactorInfoRetrieved, Level = LogLevel.Debug,
    Message = "Two-factor info retrieved for user {UserId}")]
private partial void LogTwoFactorInfoRetrieved(string userId);

[LoggerMessage(EventId = EventIds.TwoFactorKeyNotProvisioned, Level = LogLevel.Debug,
    Message = "Authenticator setup requested but no key provisioned for user {UserId}")]
private partial void LogTwoFactorKeyNotProvisioned(string userId);

[LoggerMessage(EventId = EventIds.TwoFactorSetupRetrieved, Level = LogLevel.Debug,
    Message = "Authenticator setup retrieved for user {UserId}")]
private partial void LogTwoFactorSetupRetrieved(string userId);
```

## Acceptance Criteria

- `TwoFactorQueryService` logs at `Warning` when the user is not found
- `TwoFactorQueryService` logs at `Debug` for all other observable outcomes
- `EventIds.cs` contains the four new constants in the 1721–1724 range
- `dotnet build` produces no `CS9113` warning for `TwoFactorQueryService.cs`
- `dotnet test` passes clean
