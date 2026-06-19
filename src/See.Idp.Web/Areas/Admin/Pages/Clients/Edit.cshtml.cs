using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using See.Idp.Core.Dtos.Clients;
using See.Idp.Core.Services.Clients;

namespace See.Idp.Web.Areas.Admin.Pages.Clients;

public sealed class EditModel(
    IClientQueryService clientQueryService,
    IClientCommandService clientCommandService
) : PageModel
{
    [TempData]
    public string? RotatedClientSecret { get; set; }

    [TempData]
    public bool SecretRotationPromotedClient { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return NotFound();
        }

        if (!await TryLoadInputAsync(clientId))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await clientCommandService.UpdateClientAsync(
            new UpdateClientCommand(
                Input.ClientId,
                Input.DisplayName,
                Input.AllowAuthorizationCodeFlow,
                Input.AllowClientCredentialsFlow,
                Input.AllowRefreshTokenFlow,
                SplitLines(Input.RedirectUrisText),
                SplitLines(Input.PermissionsText)
            )
        );

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Unable to update client.");
            return Page();
        }

        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostRotateSecretAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.ClientId))
        {
            return NotFound();
        }

        var result = await clientCommandService.RotateClientSecretAsync(
            new RotateClientSecretCommand(Input.ClientId)
        );

        if (!result.Succeeded)
        {
            if (!await TryLoadInputAsync(Input.ClientId))
            {
                return NotFound();
            }

            ModelState.AddModelError(
                string.Empty,
                result.Error ?? "Unable to rotate client secret."
            );
            return Page();
        }

        RotatedClientSecret = result.ClientSecret;
        SecretRotationPromotedClient = result.PromotedToConfidential;
        return RedirectToPage(new { clientId = Input.ClientId });
    }

    private async Task<bool> TryLoadInputAsync(string clientId)
    {
        var client = await clientQueryService.GetClientByIdAsync(new GetClientByIdQuery(clientId));
        if (client is null)
        {
            return false;
        }

        Input = new InputModel
        {
            ClientId = client.ClientId,
            DisplayName = client.DisplayName,
            AllowAuthorizationCodeFlow = client.AllowAuthorizationCodeFlow,
            AllowClientCredentialsFlow = client.AllowClientCredentialsFlow,
            AllowRefreshTokenFlow = client.AllowRefreshTokenFlow,
            RedirectUrisText = string.Join(Environment.NewLine, client.RedirectUris),
            PermissionsText = string.Join(Environment.NewLine, client.Permissions),
            IsConfidential = client.IsConfidential,
            HasClientSecret = client.HasClientSecret,
        };

        return true;
    }

    private static List<string> SplitLines(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return
        [
            .. value
                .Split(
                    ['\r', '\n'],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                )
                .Where(line => !string.IsNullOrWhiteSpace(line)),
        ];
    }

    public sealed class InputModel
    {
        [Required]
        public string ClientId { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        [Display(Name = "Allow authorization code flow")]
        public bool AllowAuthorizationCodeFlow { get; set; }

        [Display(Name = "Allow client credentials flow")]
        public bool AllowClientCredentialsFlow { get; set; }

        [Display(Name = "Allow refresh token flow")]
        public bool AllowRefreshTokenFlow { get; set; }

        [Display(Name = "Redirect URIs (one per line)")]
        public string? RedirectUrisText { get; set; }

        [Display(Name = "Additional permissions (one per line)")]
        public string? PermissionsText { get; set; }

        public bool IsConfidential { get; set; }

        public bool HasClientSecret { get; set; }
    }
}
