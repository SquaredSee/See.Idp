# 29 — ClientCommandService has inconsistent exception handling

## Context

`ClientCommandService` has four mutation methods. Only one of them —
`RotateClientSecretAsync` — wraps its OpenIddict call in a try/catch:

```csharp
// src/See.Idp.Infrastructure/Services/ClientCommandService.cs:157
try
{
    await applicationManager.UpdateAsync(app, descriptor, ct);
}
catch (Exception ex)
{
    LogClientCommandRejected(nameof(RotateClientSecretAsync), ex.Message);
    return RotateClientSecretResult.Failure("Unable to rotate client secret.");
}
```

`CreateClientAsync`, `UpdateClientAsync`, and `DeleteClientAsync` call the corresponding
OpenIddict manager methods without any exception boundary. OpenIddict's manager
implementations validate descriptors internally and can throw `OpenIddictExceptions` (e.g.
for duplicate client IDs, constraint violations, or validation failures). When they do, the
exception propagates unhandled through the service layer to the global error handler,
producing a 500 response instead of a `CommandResult.Failure`.

The result is an inconsistent contract: callers can reason that `RotateClientSecret` either
succeeds or returns a failure result, but `CreateClient`, `UpdateClient`, and `DeleteClient`
may either return a result or throw. Razor Pages injecting these methods cannot handle
failure uniformly.

## Fix

Apply the same defensive catch to each of the three unprotected mutation methods:

```csharp
public async Task<CreateClientResult> CreateClientAsync(
    CreateClientCommand command,
    CancellationToken ct = default
)
{
    // ... validation + descriptor setup ...
    try
    {
        await applicationManager.CreateAsync(descriptor, ct);
    }
    catch (Exception ex)
    {
        LogClientCommandRejected(nameof(CreateClientAsync), ex.Message);
        return CreateClientResult.Failure("Unable to create client.");
    }
    // ... return success ...
}
```

Apply the same pattern to `UpdateClientAsync` (wrapping `UpdateAsync`) and
`DeleteClientAsync` (wrapping `DeleteAsync`).

Do not catch inside the guard-clause blocks (null checks, not-found checks) — only around
the manager mutation call itself.

## Acceptance Criteria

- `CreateClientAsync`, `UpdateClientAsync`, and `DeleteClientAsync` each wrap their
  respective OpenIddict manager call in a try/catch block
- All four mutation methods return a typed result on exception rather than propagating
- The exception message is logged via `LogClientCommandRejected` in each catch block
- The exposed error message to callers is generic (does not echo the internal exception
  message to the client)
- `dotnet build` and `dotnet test` pass clean
- Tests cover the catch path for at least `CreateClientAsync` and `UpdateClientAsync`

## Dependencies

None.
