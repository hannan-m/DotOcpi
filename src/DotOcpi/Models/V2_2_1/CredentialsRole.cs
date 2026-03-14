using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// A role a party plays in the OCPI ecosystem, with business details.
/// </summary>
public sealed record CredentialsRole
{
    /// <summary>The role this party plays.</summary>
    [Required]
    public required Role Role { get; init; }

    /// <summary>Business details of the party.</summary>
    [Required]
    public required BusinessDetails BusinessDetails { get; init; }

    /// <summary>CPO or eMSP party ID.</summary>
    [Required]
    [StringLength(3)]
    public required CiString PartyId { get; init; }

    /// <summary>ISO 3166-1 alpha-2 country code.</summary>
    [Required]
    [StringLength(2)]
    public required CiString CountryCode { get; init; }
}
