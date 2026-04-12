using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Dtos.Users;
using See.Idp.Core.Services.Users;

namespace See.Idp.Web.Areas.Admin.Pages.Users;

public sealed class IndexModel(
    IUserQueryService userQueryService,
    IUserCommandService userCommandService
) : PageModel
{
    private const int PageSize = 10;

    public List<UserRow> Users { get; } = [];

    public int CurrentPage { get; private set; } = 1;

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? StatusKind { get; set; }

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = NormalizePage(pageNumber);
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostToggleAdminAsync(string userId, int pageNumber = 1)
    {
        var targetPage = NormalizePage(pageNumber);
        var currentUserId = GetCurrentUserId();
        var result = await userCommandService.ToggleAdminAsync(
            new ToggleUserAdminCommand(userId, currentUserId)
        );

        if (result.Succeeded)
            SetStatusSuccess(result.Message ?? "Admin role updated.");
        else
            SetStatusError(result.Error ?? "Unable to update admin role.");

        return RedirectToPage(new { pageNumber = targetPage });
    }

    public async Task<IActionResult> OnPostToggleLockAsync(string userId, int pageNumber = 1)
    {
        var targetPage = NormalizePage(pageNumber);
        var currentUserId = GetCurrentUserId();
        var result = await userCommandService.ToggleLockAsync(
            new ToggleUserLockCommand(userId, currentUserId)
        );

        if (result.Succeeded)
            SetStatusSuccess(result.Message ?? "Lock state updated.");
        else
            SetStatusError(result.Error ?? "Unable to update lock state.");

        return RedirectToPage(new { pageNumber = targetPage });
    }

    public async Task<IActionResult> OnPostDeleteAsync(string userId, int pageNumber = 1)
    {
        var targetPage = NormalizePage(pageNumber);
        var currentUserId = GetCurrentUserId();
        var result = await userCommandService.DeleteUserAsync(
            new DeleteUserCommand(userId, currentUserId)
        );

        if (result.Succeeded)
            SetStatusSuccess(result.Message ?? "User deleted.");
        else
            SetStatusError(result.Error ?? "Unable to delete user.");

        return RedirectToPage(new { pageNumber = targetPage });
    }

    private async Task LoadAsync()
    {
        var currentUserId = GetCurrentUserId();
        var users = await userQueryService.ListUsersAsync(
            new ListUsersQuery(Skip: (CurrentPage - 1) * PageSize, Take: PageSize + 1)
        );

        HasNextPage = users.Count > PageSize;

        foreach (var user in users.Take(PageSize))
        {
            Users.Add(
                new UserRow(
                    UserId: user.UserId,
                    UserName: user.UserName,
                    Email: user.Email,
                    EmailConfirmed: user.EmailConfirmed,
                    IsAdmin: user.IsAdmin,
                    IsLockedOut: user.IsLockedOut,
                    IsCurrentUser: string.Equals(
                        user.UserId,
                        currentUserId,
                        StringComparison.Ordinal
                    )
                )
            );
        }
    }

    private void SetStatusSuccess(string message)
    {
        StatusKind = "success";
        StatusMessage = message;
    }

    private void SetStatusError(string message)
    {
        StatusKind = "error";
        StatusMessage = message;
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
    }

    private static int NormalizePage(int page)
    {
        return page < 1 ? 1 : page;
    }

    public sealed record UserRow(
        string UserId,
        string? UserName,
        string? Email,
        bool EmailConfirmed,
        bool IsAdmin,
        bool IsLockedOut,
        bool IsCurrentUser
    );
}
