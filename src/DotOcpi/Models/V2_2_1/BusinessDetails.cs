using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// Business details of a company (operator, suboperator, owner, etc.).
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

    /// <summary>Company logo.</summary>
    public Image? Logo { get; init; }
}
