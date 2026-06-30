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

/// <summary>Represents the result of a get-client-by-id query.</summary>
public sealed record GetClientResult
{
    public bool Succeeded { get; init; }
    public ClientDetailsDto? Client { get; init; }
    public bool NotFound { get; init; }
    public string? Error { get; init; }

    /// <summary>Creates a successful result containing the client details.</summary>
    public static GetClientResult Success(ClientDetailsDto client) =>
        new() { Succeeded = true, Client = client };

    /// <summary>Creates a not-found result indicating no client exists with that ID.</summary>
    public static GetClientResult Missing() => new() { Succeeded = false, NotFound = true };

    /// <summary>Creates a failure result indicating a data or precondition error.</summary>
    public static GetClientResult Failure(string error) =>
        new() { Succeeded = false, Error = error };
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
