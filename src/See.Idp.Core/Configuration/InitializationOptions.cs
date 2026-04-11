using System.Collections.Generic;

namespace See.Idp.Core.Configuration;

public sealed class InitializationOptions
{
    public bool Enabled { get; set; }

    public List<string> Roles { get; set; } = [];

    public List<SeedUserOptions> Users { get; set; } = [];

    public List<SeedClientOptions> Clients { get; set; } = [];
}

public sealed class SeedUserOptions
{
    public string Email { get; set; } = string.Empty;

    public string? Password { get; set; }

    public bool EmailConfirmed { get; set; } = true;

    public List<string> Roles { get; set; } = [];
}

public sealed class SeedClientOptions
{
    public string ClientId { get; set; } = string.Empty;

    public string? ClientSecret { get; set; }

    public string? DisplayName { get; set; }

    public List<string> RedirectUris { get; set; } = [];

    public List<string> Permissions { get; set; } = [];
}
