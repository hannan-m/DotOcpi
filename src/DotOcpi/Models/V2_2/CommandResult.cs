using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Asynchronous result of a command execution, posted to the response_url.
/// </summary>
public sealed record CommandResult
{
    /// <summary>Result of the command execution.</summary>
    [Required]
    public required CommandResultType Result { get; init; }

    /// <summary>Human-readable messages.</summary>
    public IReadOnlyList<DisplayText>? Message { get; init; }
}
