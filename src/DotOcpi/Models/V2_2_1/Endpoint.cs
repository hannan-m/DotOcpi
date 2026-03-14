using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// An endpoint for a specific OCPI module and interface role.
/// </summary>
public sealed record Endpoint
{
    /// <summary>Module identifier.</summary>
    [Required]
    public required ModuleId Identifier { get; init; }

    /// <summary>Whether this endpoint is for the Sender or Receiver interface.</summary>
    [Required]
    public required InterfaceRole Role { get; init; }

    /// <summary>URL of the endpoint. Must be HTTPS.</summary>
    [Required]
    [StringLength(255)]
    public required string Url { get; init; }
}
