using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Reference to an image related to an OCPI object.
/// </summary>
public sealed record Image
{
    /// <summary>URL to the image. Must be HTTPS.</summary>
    [Required]
    [StringLength(255)]
    public required string Url { get; init; }

    /// <summary>URL to a thumbnail version of the image.</summary>
    [StringLength(255)]
    public string? Thumbnail { get; init; }

    /// <summary>Category of the image.</summary>
    [Required]
    public required ImageCategory Category { get; init; }

    /// <summary>File type (e.g. "png", "jpeg").</summary>
    [Required]
    [StringLength(4)]
    public required CiString Type { get; init; }

    /// <summary>Width in pixels.</summary>
    public int? Width { get; init; }

    /// <summary>Height in pixels.</summary>
    public int? Height { get; init; }
}
