using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Human-readable text in a specific language.
/// </summary>
public sealed record DisplayText
{
    /// <summary>ISO 639-1 language code.</summary>
    [Required]
    [StringLength(2)]
    public required string Language { get; init; }

    /// <summary>Human-readable text.</summary>
    [Required]
    [StringLength(512)]
    public required string Text { get; init; }
}
