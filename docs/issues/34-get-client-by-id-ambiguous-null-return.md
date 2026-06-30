# 34 — GetClientByIdAsync returns null for three distinct failure conditions

## Context

`ClientQueryService.GetClientByIdAsync` returns `null` on three structurally different
failure paths:

```csharp
// src/See.Idp.Infrastructure/Services/ClientQueryService.cs

// 1. Programming error — precondition violated by the caller
if (string.IsNullOrWhiteSpace(query.ClientId))
{
    LogClientCommandRejected(...);
    return null;
}

// 2. Expected domain state — client does not exist
var app = await applicationManager.FindByClientIdAsync(query.ClientId, ct);
if (app is null)
{
    LogClientLookupNotFound(query.ClientId);
    return null;
}

// 3. Data integrity problem — client exists but its ID is empty in the store
var clientId = await applicationManager.GetClientIdAsync(app, ct);
if (string.IsNullOrWhiteSpace(clientId))
{
    LogClientCommandRejected(...);
    return null;
}
```

The caller (e.g. `Admin/Clients/Edit.cshtml.cs`) cannot distinguish between these cases.
All three produce the same response: a redirect or a 404. This means:

- A blank `ClientId` query parameter (caller bug) is indistinguishable from "client not
  found" (normal domain state).
- A corrupt store record (data integrity problem) silently surfaces as "not found" with
  no escalation.

## Fix

Introduce a result type that names the three outcomes:

```csharp
// See.Idp.Core/Dtos/Clients/ClientResults.cs
public sealed record GetClientResult
{
    public bool Succeeded { get; init; }
    public ClientDetailsDto? Client { get; init; }
    public bool NotFound { get; init; }
    public string? Error { get; init; }

    public static GetClientResult Success(ClientDetailsDto client) =>
        new() { Succeeded = true, Client = client };

    public static GetClientResult Missing() =>
        new() { Succeeded = false, NotFound = true };

    public static GetClientResult Failure(string error) =>
        new() { Succeeded = false, Error = error };
}
```

Update `IClientQueryService.GetClientByIdAsync` to return `GetClientResult` instead of
`ClientDetailsDto?`. Update `ClientQueryService` to return the appropriate result variant
for each path. Update all callers.

For the precondition-violation case (blank `ClientId`), consider whether this should
throw `ArgumentException` instead, since it represents a caller contract violation rather
than a domain state — consistent with how other guard clauses in the codebase behave.

## Acceptance Criteria

- `IClientQueryService.GetClientByIdAsync` returns `GetClientResult` (not
  `ClientDetailsDto?`)
- `GetClientResult` exposes distinct success, not-found, and failure states
- Callers (Razor Pages) handle the not-found and failure cases separately
- The data-integrity failure path (empty client ID from store) is logged at `Error` level,
  not `Warning`
- `dotnet build` and `dotnet test` pass clean
- Tests cover all three return paths

## Dependencies

None.
