namespace DotOcpi;

/// <summary>
/// OCPI status code carried in the response envelope.
/// 1xxx = success, 2xxx = client error, 3xxx = server error.
/// </summary>
/// <param name="Value">The numeric status code.</param>
public readonly record struct OcpiStatusCode(int Value)
{
    /// <summary>Generic success (1000).</summary>
    public static readonly OcpiStatusCode Success = new(1000);

    /// <summary>Generic client error (2000).</summary>
    public static readonly OcpiStatusCode GenericClientError = new(2000);

    /// <summary>Invalid or missing parameters (2001).</summary>
    public static readonly OcpiStatusCode InvalidParameters = new(2001);

    /// <summary>Not enough information, e.g., missing auth token (2002).</summary>
    public static readonly OcpiStatusCode NotEnoughInformation = new(2002);

    /// <summary>Unknown location (2003).</summary>
    public static readonly OcpiStatusCode UnknownLocation = new(2003);

    /// <summary>Unknown token (2004).</summary>
    public static readonly OcpiStatusCode UnknownToken = new(2004);

    /// <summary>Generic server error (3000).</summary>
    public static readonly OcpiStatusCode GenericServerError = new(3000);

    /// <summary>Unable to use the client's API (3001).</summary>
    public static readonly OcpiStatusCode UnableToUseClientApi = new(3001);

    /// <summary>Unsupported version (3002).</summary>
    public static readonly OcpiStatusCode UnsupportedVersion = new(3002);

    /// <summary>No matching endpoints (3003).</summary>
    public static readonly OcpiStatusCode NoMatchingEndpoints = new(3003);

    /// <summary>True if this is a success code (1xxx).</summary>
    public bool IsSuccess => Value is >= 1000 and < 2000;

    /// <summary>True if this is a client error code (2xxx).</summary>
    public bool IsClientError => Value is >= 2000 and < 3000;

    /// <summary>True if this is a server error code (3xxx).</summary>
    public bool IsServerError => Value is >= 3000 and < 4000;

    /// <inheritdoc/>
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
