namespace See.Idp.Core.Dtos.Clients;

/// <summary>Represents the result of creating a client.</summary>
public sealed record CreateClientResult(
    bool Succeeded,
    string? ClientSecret = null,
    string? Error = null
)
{
    /// <summary>Creates a successful create-client result.</summary>
    public static CreateClientResult Success(string? clientSecret = null) =>
        new(true, clientSecret, null);

    /// <summary>Creates a failed create-client result.</summary>
    public static CreateClientResult Failure(string error) => new(false, null, error);
}

/// <summary>Represents the result of rotating a client secret.</summary>
public sealed record RotateClientSecretResult(
    bool Succeeded,
    string? ClientSecret = null,
    string? Error = null,
    bool PromotedToConfidential = false
)
{
    /// <summary>Creates a successful result containing the generated client secret.</summary>
    public static RotateClientSecretResult Success(
        string clientSecret,
        bool promotedToConfidential = false
    ) => new(true, clientSecret, null, promotedToConfidential);

    /// <summary>Creates a failed rotate-client-secret result.</summary>
    public static RotateClientSecretResult Failure(string error) => new(false, null, error);
}
