using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// Result of a real-time token authorization request.
/// </summary>
public sealed record AuthorizationInfo
{
    /// <summary>Whether the token is allowed to charge.</summary>
    [Required]
    public required AllowedType Allowed { get; init; }

    /// <summary>The token that was authorized (2.2+ only).</summary>
    [Required]
    public required Token Token { get; init; }

    /// <summary>Location where authorization is valid.</summary>
    public LocationReferences? Location { get; init; }

    /// <summary>Reference to correlate with the authorization response.</summary>
    [StringLength(36)]
    public CiString? AuthorizationReference { get; init; }

    /// <summary>Additional information for the driver.</summary>
    public DisplayText? Info { get; init; }
}
