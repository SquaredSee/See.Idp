namespace See.Idp.Core.Dtos.Common;

/// <summary>Represents the result of a command execution.</summary>
/// <param name="Succeeded">Indicates whether the command succeeded.</param>
/// <param name="Message">An optional informational message.</param>
/// <param name="Error">An optional error message.</param>
public sealed record CommandResult(bool Succeeded, string? Message = null, string? Error = null)
{
    /// <summary>Creates a successful command result.</summary>
    public static CommandResult Success(string? message = null) => new(true, message, null);

    /// <summary>Creates a failed command result.</summary>
    public static CommandResult Failure(string error) => new(false, null, error);
}

/// <summary>Represents the result of a create-if-missing command.</summary>
/// <param name="Succeeded">Indicates whether the command succeeded.</param>
/// <param name="Created">Indicates whether a new entity was created.</param>
/// <param name="Error">An optional error message.</param>
public sealed record CreateIfMissingResult(bool Succeeded, bool Created, string? Error = null)
{
    /// <summary>Creates a successful result indicating a new entity was created.</summary>
    public static CreateIfMissingResult CreatedNew() => new(true, true, null);

    /// <summary>Creates a successful result indicating the entity already existed.</summary>
    public static CreateIfMissingResult AlreadyExists() => new(true, false, null);

    /// <summary>Creates a failed create-if-missing result.</summary>
    public static CreateIfMissingResult Failure(string error) => new(false, false, error);
}
