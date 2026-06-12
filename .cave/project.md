# See.Idp — Cave Project Instructions

Custom Identity Provider built on ASP.NET Core + OpenIddict (OIDC/OAuth 2.0).

## Tech Stack

- **Runtime**: .NET 10, C#, nullable enabled, no implicit usings
- **Web**: ASP.NET Core Razor Pages
- **Auth/OIDC**: OpenIddict 7.4 (server + EF Core stores)
- **Identity**: ASP.NET Core Identity (`ApplicationUser` / `ApplicationRole`)
- **ORM**: EF Core 10 + Npgsql (PostgreSQL)
- **CSS**: Tailwind CSS v4 (built via MSBuild → npm)
- **Observability**: OpenTelemetry → OTLP (Grafana LGTM stack)
- **Formatter (C#)**: CSharpier 1.2.6 (`dotnet csharpier`)
- **Formatter (JS/TS/CSS/HTML/JSON/etc.)**: Prettier (`npx prettier`)
- **Tests**: MSTest + NSubstitute

## Architecture

Three-project layered structure:

```
src/
  See.Idp.Core/           # Interfaces, DTOs, config models — zero external deps
  See.Idp.Infrastructure/ # EF Core, Identity entities, service impls, migrations
  See.Idp.Web/            # ASP.NET Core host, Razor Pages, DI composition root
tests/
  See.Idp.Tests/          # Unit tests (Core + Infrastructure only)
```

**Dependency rule**: Core ← Infrastructure ← Web. Never reference Web from Infrastructure or Core.

### Core
- Service interfaces live in `See.Idp.Core/Services/` (split by domain: `Auth/`, `Clients/`, `Users/`)
- Configuration models in `See.Idp.Core/Configuration/`
- Role constants in `See.Idp.Core/Auth/Roles.cs`

### Infrastructure
- `ApplicationDbContext` (EF Core, registered with `UseOpenIddict()`)
- `ApplicationUser` / `ApplicationRole` — ASP.NET Core Identity entities
- Service implementations in `See.Idp.Infrastructure/Services/`
- EF migrations in `See.Idp.Infrastructure/Migrations/`

### Web
- `Program.cs` — composition root (DI, middleware, OpenIddict config)
- `Areas/Admin/Pages/` — protected admin portal (Clients, Users)
- `Areas/Identity/Pages/Account/` — scaffolded Identity UI
- `Auth/Policies.cs` — authorization policy definitions
- `wwwroot/css/` — two Tailwind entry points: `identity.css`, `admin.css`

## Key Commands

```bash
# Build
dotnet build

# Run tests
dotnet test

# Format (always run after changes)
dotnet csharpier .

# Add EF migration
dotnet ef migrations add <MigrationName> \
  --project src/See.Idp.Infrastructure \
  --startup-project src/See.Idp.Web

# Apply migrations
dotnet ef database update \
  --project src/See.Idp.Infrastructure \
  --startup-project src/See.Idp.Web

# Start local deps (Postgres, Redis, Grafana LGTM)
docker compose up -d

# Run the app
dotnet run --project src/See.Idp.Web
```

## CQRS Conventions

The project uses **direct-injection CQRS** — no MediatR. Razor Pages inject specific command/query service interfaces; there is no dispatcher.

### Interface segregation
- Every service interface is either a command service (`IXxxCommandService`) or a query service (`IXxxQueryService`) — never mixed
- Query interfaces: read-only, no side effects, no state mutation
- Command interfaces: mutating operations only; must never be called from `OnGet` handlers
- If an operation has side effects it belongs on a command interface, even if it returns data

### Command and query objects
- Every service method takes a **typed record** as its sole meaningful parameter — never bare primitives (`string`, `Guid`, etc.)
- Commands: `sealed record XxxCommand(...)` in `See.Idp.Core/Dtos/`
- Queries: `sealed record XxxQuery(...)` in `See.Idp.Core/Dtos/`
- Results: strongly-typed result records (e.g. `CommandResult`, `CreateClientResult`) — not `bool` or raw strings

### File layout for new domains
```
See.Idp.Core/
  Services/<Domain>/
    IXxxCommandService.cs
    IXxxQueryService.cs
  Dtos/<Domain>/
    XxxCommand.cs        # one file per command record
    XxxQuery.cs          # one file per query record
    XxxResult.cs         # result records

See.Idp.Infrastructure/Services/
    XxxCommandService.cs
    XxxQueryService.cs
```

### Razor Pages
- `OnGet` / `OnGetAsync` → inject and call **query services only**
- `OnPost` / `OnPostAsync` → construct a typed command record inline and call a **command service**
- A page may inject both interfaces; a handler may not use the wrong kind

## Code Conventions

- **No implicit usings** — every `using` must be explicit at the top of the file
- **Nullable reference types enabled** — handle nullability; don't suppress warnings with `!` unless genuinely safe
- **Indentation**: 4 spaces for C#; 2 spaces for JSON, YAML, CSS, HTML, cshtml, csproj
- **CSharpier** formats C# — run `dotnet csharpier .` before committing; don't manually adjust formatting CSharpier would fix
- **Prettier** formats everything else (JS, TS, CSS, HTML, JSON, YAML, cshtml, etc.) — run `npx prettier --write .` before committing
- Services follow interface → implementation pattern; interfaces live in Core, implementations in Infrastructure
- Use `IQueryable`-based async patterns in data services (see existing service tests for patterns)

## Commit Conventions

All commits must use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>[optional scope]: <description>
```

Common types:
- `feat` — new feature or behaviour
- `fix` — bug fix
- `chore` — maintenance, config, tooling, dependencies
- `docs` — documentation only
- `refactor` — code change that neither fixes a bug nor adds a feature
- `test` — adding or updating tests
- `style` — formatting changes only

Examples:
```
feat(auth): add authorization controller and claims
docs: add issues backlog
test(users): add UserQueryService and UserRegistrationService tests
fix(cqrs): wrap bare string params in typed query records
```

## TDD

Test-driven development is the **primary mode of development**. Write tests before (or alongside) production code — never after the fact.

- Before adding a new method or behaviour: write a failing test first, then make it pass
- Before fixing a bug: write a test that reproduces it, then fix it
- New service methods without tests are considered incomplete
- PRs that add untested logic should be rejected

## Test Conventions

- Framework: MSTest (`[TestClass]`, `[TestMethod]`, `[DataRow]`)
- Mocking: NSubstitute (`Substitute.For<T>()`)
- Test helpers: `AsyncEnumerableTestFactory`, `AsyncQueryableTestFactory`, `IdentityTestFactory` in `tests/See.Idp.Tests/`
- Test files mirror production namespaces, suffixed with `Tests`
- Tests cover Core + Infrastructure layers only; Web layer is not unit-tested

## Local Infrastructure (docker-compose.yml)

| Service | Port | Purpose |
|---|---|---|
| postgres:16 | 5432 | Primary DB (`seeidp`) |
| redis:7 | 6379 | Present, not yet wired |
| grafana/otel-lgtm | 3000 / 4317 / 4318 | Grafana + OTLP collector |

Connection string key: `ConnectionStrings:DefaultConnection`
OTLP endpoint key: `OTEL_EXPORTER_OTLP_ENDPOINT` (default: `http://localhost:4317`)
