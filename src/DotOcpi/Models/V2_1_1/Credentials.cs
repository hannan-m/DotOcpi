using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_1_1;

/// <summary>
/// Credentials exchanged during the registration handshake.
/// In 2.1.1: flat structure with business_name, party_id, and country_code directly
/// on the object (no roles array, no CredentialsRole).
/// </summary>
public sealed record Credentials
{
    /// <summary>Token for the receiving party to use when calling the sender.</summary>
    [Required]
    [StringLength(64)]
    public required string Token { get; init; }

    /// <summary>URL to the sender's versions endpoint.</summary>
    [Required]
    [StringLength(255)]
    public required string Url { get; init; }

    /// <summary>Business name of the party.</summary>
    [Required]
    [StringLength(100)]
    public required string BusinessName { get; init; }

    /// <summary>Party ID (CPO or eMSP).</summary>
    [Required]
    [StringLength(3)]
    public required string PartyId { get; init; }

    /// <summary>ISO 3166-1 alpha-2 country code.</summary>
    [Required]
    [StringLength(2)]
    public required string CountryCode { get; init; }

    /// <summary>Company logo.</summary>
    public Image? BusinessLogo { get; init; }

    /// <summary>Company website URL.</summary>
    [StringLength(255)]
    public string? Website { get; init; }
}
