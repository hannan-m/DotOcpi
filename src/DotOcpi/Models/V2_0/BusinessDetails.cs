using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_0;

/// <summary>
/// Business details of a company (operator, suboperator, owner, etc.).
/// In 2.0: no logo field (logo was added in 2.1.1).
/// </summary>
public sealed record BusinessDetails
{
    /// <summary>Company name.</summary>
    [Required]
    [StringLength(100)]
    public required string Name { get; init; }

    /// <summary>Company website URL.</summary>
    [StringLength(255)]
    public string? Website { get; init; }
}
