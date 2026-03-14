using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// A single signed meter value for calibration law compliance.
/// </summary>
public sealed record SignedValue
{
    /// <summary>Nature of the value (e.g. "START", "END", "INTERMEDIATE").</summary>
    [Required]
    [StringLength(32)]
    public required CiString Nature { get; init; }

    /// <summary>Plain data before signing.</summary>
    [Required]
    [StringLength(512)]
    public required string PlainData { get; init; }

    /// <summary>Signed representation of the data.</summary>
    [Required]
    [StringLength(5000)]
    public required string SignedData { get; init; }
}
