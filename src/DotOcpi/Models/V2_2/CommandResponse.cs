using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Synchronous response to a command request (before the async result arrives).
/// </summary>
public sealed record CommandResponse
{
    /// <summary>Whether the command was accepted.</summary>
    [Required]
    public required CommandResponseType Result { get; init; }

    /// <summary>Timeout in seconds to wait for the async result.</summary>
    [Required]
    public required int Timeout { get; init; }

    /// <summary>Human-readable messages (2.2+ only).</summary>
    public IReadOnlyList<DisplayText>? Message { get; init; }
}
