# 17c — User, Client & Seed + DTO Cleanup

Sub-issue of #17. Implement all changes in the User, Client, and Seed domains, plus all
remaining DTO file reorganisation. See issue #17 for full context and DTO field
definitions.

## Scope

### DTOs (Core)

Create/update per the target layout in issue #17:

**Users domain — `Dtos/Users/`**

- `UserCommands.cs` — `ToggleUserAdminCommand`, `ToggleUserLockCommand`,
  `DeleteUserCommand`, `UpdatePhoneNumberCommand` (moved from `UserProfileCommands.cs`)
- `UserSeedCommands.cs` — new file: `CreateRoleIfMissingCommand`,
  `CreateUserIfMissingCommand`, `AddUserToRoleIfMissingCommand`
- `UserQueries.cs` — `ListUsersQuery`, `FindUserByEmailQuery`, `GetUserProfileQuery`
  only (remove any remaining stale records)
- `UserDtos.cs` — new file: `UserSummaryDto`, `UserProfileDto`, `FindUserByEmailResult`
  (move DTOs out of `UserQueries.cs`)
- `UserResults.cs` — new file: `CreateUserIfMissingResult` (moved from `Common/`)
- Delete `UserProfileCommands.cs`

**Clients domain — `Dtos/Clients/`**

- `ClientCommands.cs` — remove `CreateClientIfMissingCommand`
- `ClientSeedCommands.cs` — new file: `CreateClientIfMissingCommand`
- `ClientQueries.cs` — `ListClientsQuery`, `GetClientByIdQuery` only (no response DTOs)
- `ClientDtos.cs` — new file: `ClientSummaryDto`, `ClientDetailsDto` (moved from
  `ClientQueries.cs`)
- `ClientResults.cs` — new file: `CreateClientResult`, `RotateClientSecretResult` (moved
  from `Common/`)

**Common — `Dtos/Common/`**

- `CommandResults.cs` — `CommandResult` and `CreateIfMissingResult` only; remove
  `CreateClientResult`, `RotateClientSecretResult`, `CreateUserIfMissingResult`

### Interfaces (Core)

- `IUserCommandService` — remove `CreateRoleIfMissingAsync`, `CreateUserIfMissingAsync`,
  `AddUserToRoleIfMissingAsync`; keep `UpdatePhoneNumber`, `ToggleUserAdmin`,
  `ToggleUserLock`, `DeleteUser`
- `IUserQueryService` — update `FindUserByEmailAsync` return type to
  `FindUserByEmailResult`; remove any remaining `Generate*` methods
- `IClientCommandService` — remove `CreateClientIfMissingAsync`
- `IApplicationSeedCommandService` — new interface:
  `CreateRoleIfMissingAsync`, `CreateUserIfMissingAsync`, `AddUserToRoleIfMissingAsync`,
  `CreateClientIfMissingAsync`

### Implementations (Infrastructure)

- `UserCommandService` — remove seeding methods
- `UserQueryService` — update `FindUserByEmailAsync` return type to `FindUserByEmailResult`
- `ClientCommandService` — remove `CreateClientIfMissingAsync`
- `ApplicationSeedCommandService` — new class implementing `IApplicationSeedCommandService`;
  move seeding logic from `UserCommandService` and `CreateClientIfMissingAsync` from
  `ClientCommandService` here. Dependencies: `UserManager`, `RoleManager`,
  `IOpenIddictApplicationManager`, `ILogger`.
- `ConfigurationApplicationInitializer` — inject `IApplicationSeedCommandService`
  instead of `IUserCommandService` and `IClientCommandService`
- Update `Program.cs` DI registrations

### Pages (Web)

- `Admin/Users/Edit.cshtml.cs` — inject `IRegistrationCommandService` for
  confirmation token generation (depends on 17a being merged, or stub the interface
  until then); rename `OnPostGenerateConfirmationLinkAsync` →
  `OnGetGenerateConfirmationLinkAsync`
- `Admin/Users/Edit.cshtml` — replace `<form method="post">` for confirmation link with
  `<a asp-page-handler="GenerateConfirmationLink" asp-route-userId="@Model.UserId">`
- All other Admin pages — update any `using` statements affected by DTO moves

### Tests

- `UserCommandServiceTests.cs` — remove tests for deleted seeding methods
- `UserQueryServiceTests.cs` — update `FindUserByEmailAsync` tests for new return type
- `ClientCommandServiceTests.cs` — remove `CreateClientIfMissingAsync` tests if present
- Create `ApplicationSeedCommandServiceTests.cs` — cover `CreateRoleIfMissing`,
  `CreateUserIfMissing`, `AddUserToRoleIfMissing`, `CreateClientIfMissing`

## Acceptance Criteria

- `IApplicationSeedCommandService` exists with all four seeding methods
- `ApplicationSeedCommandService` exists; `ConfigurationApplicationInitializer` injects it
- `IUserCommandService` has exactly: `UpdatePhoneNumber`, `ToggleUserAdmin`,
  `ToggleUserLock`, `DeleteUser`
- `IClientCommandService` has no `CreateClientIfMissing`
- `FindUserByEmailAsync` returns `FindUserByEmailResult`, not `string?`
- `UserProfileCommands.cs` does not exist
- `UserDtos.cs`, `UserResults.cs`, `UserSeedCommands.cs` exist in `Dtos/Users/`
- `ClientDtos.cs`, `ClientResults.cs`, `ClientSeedCommands.cs` exist in `Dtos/Clients/`
- `Common/CommandResults.cs` contains only `CommandResult` and `CreateIfMissingResult`
- `Admin/Users/Edit` uses `OnGetGenerateConfirmationLinkAsync`
- `dotnet build` and `dotnet test` pass clean

## Dependencies

17a should be merged first (Admin/Users/Edit injects `IRegistrationCommandService`).
17b can be implemented in parallel with this issue.
