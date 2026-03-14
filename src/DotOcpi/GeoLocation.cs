namespace DotOcpi;

/// <summary>
/// Geographic coordinates per OCPI specification.
/// Latitude and longitude are stored as strings to preserve exact precision from the wire format.
/// </summary>
/// <param name="Latitude">Latitude in decimal degrees as a string (e.g., "51.0472").</param>
/// <param name="Longitude">Longitude in decimal degrees as a string (e.g., "3.7294").</param>
public readonly record struct GeoLocation(string Latitude, string Longitude);
