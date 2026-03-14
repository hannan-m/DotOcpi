using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Restricts which tokens can see a Location when publish is false.
/// At least one of <see cref="Uid"/>, <see cref="VisualNumber"/>, or <see cref="GroupId"/> must be set.
/// </summary>
public sealed record PublishTokenType
{
    /// <summary>Token UID. When set, <see cref="Type"/> is required.</summary>
    [StringLength(36)]
    public CiString? Uid { get; init; }

    /// <summary>Token type. Required when <see cref="Uid"/> is set.</summary>
    public TokenType? Type { get; init; }

    /// <summary>Visual number printed on the token.</summary>
    [StringLength(64)]
    public string? VisualNumber { get; init; }

    /// <summary>Token issuer name.</summary>
    [StringLength(64)]
    public string? Issuer { get; init; }

    /// <summary>Token group ID for fleet identification.</summary>
    [StringLength(36)]
    public CiString? GroupId { get; init; }
}
