# 24 — Bug: `TwoFactorCommandService` silently ignores `IdentityResult` failures

## Context

Three methods in `TwoFactorCommandService` discard the `IdentityResult` returned by `UserManager`
calls and unconditionally return `CommandResult.Success()`. This is inconsistent with
`DisableTwoFactorAsync` in the same class, which correctly checks its result and propagates
failures.

## Affected methods

### `ProvisionAuthenticatorKeyAsync`

```csharp
await userManager.ResetAuthenticatorKeyAsync(user);  // result ignored
return CommandResult.Success();
```

If key provisioning fails, the method reports success and the caller proceeds as though a key
exists.

### `EnableTwoFactorAsync`

```csharp
await userManager.SetTwoFactorEnabledAsync(user, true);  // result ignored
var codes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
return EnableTwoFactorResult.Success(codes ?? Array.Empty<string>());
```

If enabling 2FA fails, recovery codes are still generated and success is returned — leaving the
account in a partially-mutated state with 2FA not actually enabled.

### `ResetAuthenticatorKeyAsync`

```csharp
await userManager.SetTwoFactorEnabledAsync(user, false);   // result ignored
await userManager.ResetAuthenticatorKeyAsync(user);        // result ignored
return CommandResult.Success();
```

Both steps can fail silently.

## Fix

Capture each `IdentityResult`, check `.Succeeded`, and return `Failure(...)` on error — matching
the pattern already used in `DisableTwoFactorAsync`.

## Acceptance Criteria

- All three methods check `IdentityResult` and return a failure result when the underlying
  Identity call fails
- Tests cover the new failure paths for each method
- `dotnet test` passes clean
