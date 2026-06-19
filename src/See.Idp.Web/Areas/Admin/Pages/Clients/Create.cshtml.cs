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

public sealed class CreateModel(IClientCommandService clientCommandService) : PageModel
{
    [TempData]
    public string? CreatedClientSecret { get; set; }

    [TempData]
    public string? CreatedClientId { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
        Input = new InputModel { AllowAuthorizationCodeFlow = true };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await clientCommandService.CreateClientAsync(
            new CreateClientCommand(
                Input.ClientId,
                Input.DisplayName,
                Input.AllowAuthorizationCodeFlow,
                Input.AllowClientCredentialsFlow,
                Input.AllowRefreshTokenFlow,
                Input.GenerateClientSecret,
                SplitLines(Input.RedirectUrisText),
                SplitLines(Input.PermissionsText)
            )
        );

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Client creation failed.");
            return Page();
        }

        CreatedClientId = Input.ClientId;
        CreatedClientSecret = result.ClientSecret;

        if (!string.IsNullOrWhiteSpace(result.ClientSecret))
        {
            return RedirectToPage();
        }

        return RedirectToPage("./Index");
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
        public bool AllowAuthorizationCodeFlow { get; set; } = true;

        [Display(Name = "Allow client credentials flow")]
        public bool AllowClientCredentialsFlow { get; set; }

        [Display(Name = "Allow refresh token flow")]
        public bool AllowRefreshTokenFlow { get; set; }

        [Display(Name = "Generate client secret")]
        public bool GenerateClientSecret { get; set; }

        [Display(Name = "Redirect URIs (one per line)")]
        public string? RedirectUrisText { get; set; }

        [Display(Name = "Additional permissions (one per line)")]
        public string? PermissionsText { get; set; }
    }
}
