# 23 — Missing test coverage: four gaps from the #17 refactor

## Context

The #17 refactor left four untested paths across three service classes.

## Gaps

### 1 — `UserCommandService.UpdatePhoneNumberAsync` — zero tests

No test exists for this method at all. Add to `UserCommandServiceTests.cs`:

| Test                                                            | Setup                                                 |
| --------------------------------------------------------------- | ----------------------------------------------------- |
| `UpdatePhoneNumberAsync_ReturnsFailure_WhenUserIdIsEmpty`       | empty `UserId`                                        |
| `UpdatePhoneNumberAsync_ReturnsFailure_WhenUserNotFound`        | `FindByIdAsync` returns `null`                        |
| `UpdatePhoneNumberAsync_UpdatesPhoneNumber_WhenUserExists`      | `SetPhoneNumberAsync` returns success                 |
| `UpdatePhoneNumberAsync_ReturnsFailure_WhenSetPhoneNumberFails` | `SetPhoneNumberAsync` returns failed `IdentityResult` |

### 2 — `AuthenticationCommandService.SignOutAsync` — not tested

Add to `AuthenticationCommandServiceTests.cs`:

| Test                                     | Assertion                                              |
| ---------------------------------------- | ------------------------------------------------------ |
| `SignOutAsync_CallsSignInManagerSignOut` | `signInManager.SignOutAsync()` was called exactly once |

### 3 — `TwoFactorCommandService.DisableTwoFactorAsync` — failure path missing

Only the success path is tested. Add to `TwoFactorCommandServiceTests.cs`:

| Test                                                                | Setup                                                                     |
| ------------------------------------------------------------------- | ------------------------------------------------------------------------- |
| `DisableTwoFactorAsync_ReturnsFailure_WhenSetTwoFactorEnabledFails` | `SetTwoFactorEnabledAsync(user, false)` returns a failed `IdentityResult` |

### 4 — `UserCommandService.ToggleAdminAsync` — grant success path missing

`ReturnsFailure_WhenGrantAdminFails` exists but the success branch is untested.
Add to `UserCommandServiceTests.cs`:

| Test                                                  | Setup                                                                                                  |
| ----------------------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| `ToggleAdminAsync_GrantsAdminRole_WhenUserIsNotAdmin` | `IsInRoleAsync` returns `false`; `AddToRoleAsync` returns success; assert `result.Succeeded` is `true` |

## Acceptance Criteria

- All four gaps above have the listed test methods
- `dotnet test` passes clean
