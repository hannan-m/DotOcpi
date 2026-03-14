using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_0;

/// <summary>
/// Response to a command request. In 2.0, CommandResponseType is used for both
/// synchronous and asynchronous responses (there is no separate CommandResult type).
/// </summary>
public sealed record CommandResponse
{
    /// <summary>Whether the command was accepted, rejected, timed out, etc.</summary>
    [Required]
    public required CommandResponseType Result { get; init; }

    /// <summary>Timeout in seconds to wait for the async result.</summary>
    [Required]
    public required int Timeout { get; init; }
}
