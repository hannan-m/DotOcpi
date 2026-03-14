using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Credentials exchanged during the registration handshake.
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

    /// <summary>Roles the sender plays. At least one required.</summary>
    [Required]
    public required IReadOnlyList<CredentialsRole> Roles { get; init; }
}
