# 30 — UserQueryService and ClientQueryService paginate in memory

## Context

Both list query services acknowledge the problem with a TODO comment:

```csharp
// src/See.Idp.Infrastructure/Services/UserQueryService.cs:27
// TODO: Filtering and Paging is currently done in-memory which is not ideal for
// large datasets. Consider EF Core replacement.

// src/See.Idp.Infrastructure/Services/ClientQueryService.cs:25
// TODO: Filtering and Paging is currently done in-memory which is not ideal for
// large datasets. Consider EF Core replacement.
```

### UserQueryService

`ListUsersAsync` streams the entire `AspNetUsers` table into the application process via
`AsAsyncEnumerable()`, applies the search term filter, sorts, and pages in-process. Before
streaming it also calls `GetUsersInRoleAsync(Roles.Admin)` — a separate full-table read
— to build an admin ID set. Every admin page load performs two full table scans regardless
of how many results are displayed.

At 1‬000 users the page is slow. At 10‬000 it is visibly broken. At 100‬000 the process
runs out of memory under concurrent load.

### ClientQueryService

`ListClientsAsync` iterates all OpenIddict applications via `applicationManager.ListAsync()`
— another full enumeration — then filters, sorts, and pages in-process. Because the
OpenIddict abstraction layer (`IOpenIddictApplicationManager`) does not expose arbitrary
`IQueryable` predicates, server-side filtering on the OpenIddict-managed client store
requires a different approach.

## Fix

### UserQueryService (EF Core path)

Replace the `StreamUsersAsync` async-enumerable approach with a composed `IQueryable`
pipeline on `ApplicationDbContext.Users`:

```csharp
public async Task<IReadOnlyList<UserSummaryDto>> ListUsersAsync(
    ListUsersQuery query,
    CancellationToken ct = default
)
{
    var q = dbContext.Users.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(query.SearchTerm))
    {
        var term = query.SearchTerm.Trim();
        q = q.Where(u =>
            (u.Email != null && EF.Functions.ILike(u.Email, $"%{term}%")) ||
            (u.UserName != null && EF.Functions.ILike(u.UserName, $"%{term}%"))
        );
    }

    q = q.OrderBy(u => u.Email).ThenBy(u => u.UserName);

    if (query.Skip > 0)
        q = q.Skip(query.Skip);
    if (query.Take is > 0)
        q = q.Take(query.Take.Value);

    // Project to summary DTO and resolve admin flag via a subquery join
    var users = await q
        .Select(u => new {
            u.Id, u.UserName, u.Email, u.EmailConfirmed,
            u.LockoutEnabled, u.LockoutEnd,
            IsAdmin = dbContext.UserRoles
                .Join(dbContext.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new { ur.UserId, r.NormalizedName })
                .Any(x => x.UserId == u.Id
                    && x.NormalizedName == Roles.Admin.ToUpperInvariant())
        })
        .ToListAsync(ct);

    return users.Select(u => new UserSummaryDto(
        u.Id, u.UserName, u.Email, u.EmailConfirmed, u.IsAdmin,
        u.LockoutEnabled && u.LockoutEnd.HasValue
            && u.LockoutEnd.Value > DateTimeOffset.UtcNow
    )).ToList();
}
```

Inject `ApplicationDbContext` into `UserQueryService`.

Remove the `StreamUsersAsync` method and its `[EnumeratorCancellation]` parameter.

### ClientQueryService (two options)

**Option A — OpenIddict EF Core direct query (recommended)**

OpenIddict EF Core stores expose the application entity as a `DbSet` on
`ApplicationDbContext` (via the `UseOpenIddict()` registration). Inject
`ApplicationDbContext` into `ClientQueryService` and query `OpenIddictEntityFrameworkCoreApplication`
directly:

```csharp
var q = dbContext.Set<OpenIddictEntityFrameworkCoreApplication>()
    .AsNoTracking();

if (!string.IsNullOrWhiteSpace(query.SearchTerm))
    q = q.Where(a =>
        a.ClientId != null && EF.Functions.ILike(a.ClientId, $"%{term}%") ||
        a.DisplayName != null && EF.Functions.ILike(a.DisplayName, $"%{term}%")
    );

q = q.OrderBy(a => a.ClientId);
// Skip / Take ...
```

**Option B — Retain `IOpenIddictApplicationManager` with server-side count**

Use `applicationManager.CountAsync()` for total counts and
`applicationManager.ListAsync(count, offset, ct)` for pagination, accepting that
text-search filtering still happens in memory for the current page only.

Option A is preferred for this codebase because the EF Core dependency already exists and
Option B still requires in-memory filtering.

## Acceptance Criteria

- `UserQueryService.ListUsersAsync` issues no more than two SQL queries (one for users
    - admin flag, one count or combined) regardless of total user count
- `UserQueryService` does not call `GetUsersInRoleAsync`
- `UserQueryService` does not use `AsAsyncEnumerable()`
- `ClientQueryService.ListClientsAsync` does not load the full client list into memory
  when `Take` is set
- All filtering and sorting are applied at the database level (verified by query logs or
  EF Core test helpers)
- TODO comments are removed from both services
- `dotnet build` and `dotnet test` pass clean

## Dependencies

None. Can be implemented independently.
