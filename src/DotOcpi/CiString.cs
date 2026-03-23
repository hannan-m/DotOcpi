using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace DotOcpi;

/// <summary>
/// Case-insensitive string type per OCPI specification.
/// Preserves original case but compares using <see cref="StringComparison.OrdinalIgnoreCase"/>.
/// Used for identifiers in OCPI 2.2+ (e.g., location_id, evse_uid, connector_id).
/// <para>
/// <c>default(CiString)</c> produces an empty string, not null — all members are safe
/// to call without null checks.
/// </para>
/// </summary>
public readonly record struct CiString : IEquatable<CiString>
{
    private readonly string? _value;

    /// <summary>
    /// The original string value with preserved casing.
    /// Guaranteed non-null: returns <see cref="string.Empty"/> for default-constructed instances.
    /// </summary>
    public string Value => _value ?? "";

    /// <summary>
    /// Creates a new <see cref="CiString"/> from a string value.
    /// </summary>
    /// <param name="value">The string value. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public CiString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
    }

    /// <summary>
    /// Implicit conversion from <see cref="string"/> to <see cref="CiString"/>.
    /// </summary>
    public static implicit operator CiString(string value) => new(value);

    /// <summary>
    /// Implicit conversion from <see cref="CiString"/> to <see cref="string"/>.
    /// </summary>
    public static implicit operator string(CiString ciString) => ciString.Value;

    /// <inheritdoc/>
    public bool Equals(CiString other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>
    /// Validates that the string length does not exceed the specified maximum.
    /// </summary>
    /// <param name="maxLength">Maximum allowed length.</param>
    /// <returns>True if the value length is within the limit.</returns>
    public bool IsWithinMaxLength(int maxLength) => Value.Length <= maxLength;

    /// <summary>
    /// Returns true if the value is empty or the struct was default-constructed.
    /// </summary>
    public bool IsEmpty => Value.Length == 0;
}
