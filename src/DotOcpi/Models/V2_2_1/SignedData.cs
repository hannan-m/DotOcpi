using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// Signed meter data for calibration law compliance (e.g. German Eichrecht).
/// </summary>
public sealed record SignedData
{
    /// <summary>The encoding method used.</summary>
    [Required]
    [StringLength(36)]
    public required CiString EncodingMethod { get; init; }

    /// <summary>Version of the encoding method.</summary>
    public int? EncodingMethodVersion { get; init; }

    /// <summary>Public key used to sign the meter values.</summary>
    [StringLength(512)]
    public string? PublicKey { get; init; }

    /// <summary>Signed values. At least one required.</summary>
    [Required]
    public required IReadOnlyList<SignedValue> SignedValues { get; init; }

    /// <summary>URL to a verification service.</summary>
    [StringLength(512)]
    public CiString? Url { get; init; }
}
