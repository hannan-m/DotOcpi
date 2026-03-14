using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_1_1;

/// <summary>
/// Details of a specific OCPI version, including its module endpoints.
/// </summary>
public sealed record VersionDetail
{
    /// <summary>OCPI version number.</summary>
    [Required]
    [StringLength(10)]
    public required string Version { get; init; }

    /// <summary>Module endpoints for this version. At least one required.</summary>
    [Required]
    public required IReadOnlyList<Endpoint> Endpoints { get; init; }
}
