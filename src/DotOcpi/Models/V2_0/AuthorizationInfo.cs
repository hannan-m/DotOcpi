using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_0;

/// <summary>
/// Result of a real-time token authorization request.
/// In 2.0: no token field, no authorization_reference.
/// </summary>
public sealed record AuthorizationInfo
{
    /// <summary>Whether the token is allowed to charge.</summary>
    [Required]
    public required AllowedType Allowed { get; init; }

    /// <summary>Location where authorization is valid.</summary>
    public LocationReferences? Location { get; init; }

    /// <summary>Additional information for the driver.</summary>
    public DisplayText? Info { get; init; }
}
