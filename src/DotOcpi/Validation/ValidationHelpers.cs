namespace DotOcpi.Validation;

/// <summary>
/// Shared validation helpers for OCPI model validators.
/// Span-based to avoid LINQ enumerator allocations on hot paths.
/// Methods accept primitives so they can be called from any version-specific validator.
/// </summary>
internal static class ValidationHelpers
{
    /// <summary>
    /// Returns true if every character in the string is a letter (A-Z, a-z).
    /// Uses span iteration — no enumerator allocation.
    /// </summary>
    internal static bool IsAllLetters(string value)
    {
        foreach (var ch in value.AsSpan())
        {
            if (!char.IsLetter(ch))
                return false;
        }
        return true;
    }

    internal static void ValidateCoordinates(
        GeoLocation coordinates,
        string propertyPath,
        List<OcpiValidationError> errors
    )
    {
        if (
            !decimal.TryParse(
                coordinates.Latitude,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var lat
            )
            || lat < -90m
            || lat > 90m
        )
        {
            errors.Add(
                new OcpiValidationError(
                    "LOCATION_INVALID_LATITUDE",
                    $"Latitude '{coordinates.Latitude}' is not a valid decimal between -90 and 90.",
                    "Provide a decimal latitude in the range [-90, 90]."
                )
                {
                    PropertyPath = $"{propertyPath}.Latitude",
                }
            );
        }

        if (
            !decimal.TryParse(
                coordinates.Longitude,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var lon
            )
            || lon < -180m
            || lon > 180m
        )
        {
            errors.Add(
                new OcpiValidationError(
                    "LOCATION_INVALID_LONGITUDE",
                    $"Longitude '{coordinates.Longitude}' is not a valid decimal between -180 and 180.",
                    "Provide a decimal longitude in the range [-180, 180]."
                )
                {
                    PropertyPath = $"{propertyPath}.Longitude",
                }
            );
        }
    }

    internal static void ValidateCountryAlpha3(string country, List<OcpiValidationError> errors)
    {
        if (country.Length != 3 || !IsAllLetters(country))
        {
            errors.Add(
                new OcpiValidationError(
                    "LOCATION_INVALID_COUNTRY",
                    $"Country '{country}' is not a valid ISO 3166-1 alpha-3 code.",
                    "Provide a 3-letter country code (e.g. 'NLD', 'DEU')."
                )
                {
                    PropertyPath = "Country",
                }
            );
        }
    }

    internal static void ValidateCountryAlpha2(
        string countryCode,
        string errorCode,
        string propertyPath,
        List<OcpiValidationError> errors
    )
    {
        if (countryCode.Length != 2 || !IsAllLetters(countryCode))
        {
            errors.Add(
                new OcpiValidationError(
                    errorCode,
                    $"CountryCode '{countryCode}' is not a valid ISO 3166-1 alpha-2 code.",
                    "Provide a 2-letter country code (e.g. 'NL', 'DE')."
                )
                {
                    PropertyPath = propertyPath,
                }
            );
        }
    }

    internal static void ValidateCurrency(string currency, string errorCode, List<OcpiValidationError> errors)
    {
        if (currency.Length != 3 || !IsAllLetters(currency))
        {
            errors.Add(
                new OcpiValidationError(
                    errorCode,
                    $"Currency '{currency}' is not a valid ISO 4217 code.",
                    "Provide a 3-letter ISO 4217 currency code (e.g. 'EUR', 'USD')."
                )
                {
                    PropertyPath = "Currency",
                }
            );
        }
    }

    internal static void ValidateLanguage(string? language, string errorCode, List<OcpiValidationError> errors)
    {
        if (language is not null && (language.Length != 2 || !IsAllLetters(language)))
        {
            errors.Add(
                new OcpiValidationError(
                    errorCode,
                    $"Language '{language}' is not a valid ISO 639-1 code.",
                    "Provide a 2-letter language code (e.g. 'en', 'nl')."
                )
                {
                    PropertyPath = "Language",
                }
            );
        }
    }

    internal static void ValidateHttpsUrl(
        string url,
        string errorCode,
        string message,
        string suggestion,
        string propertyPath,
        List<OcpiValidationError> errors
    )
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            errors.Add(new OcpiValidationError(errorCode, message, suggestion) { PropertyPath = propertyPath });
        }
    }

    /// <summary>
    /// Pre-DNS SSRF check: rejects URLs whose hostname is a literal private, loopback,
    /// or link-local IP address. This catches obvious SSRF attempts at validation time;
    /// the post-DNS <c>SsrfGuard</c> in the Client package catches DNS-rebinding attacks
    /// at connection time.
    /// </summary>
    internal static bool IsLiteralPrivateHost(Uri uri)
    {
        if (!System.Net.IPAddress.TryParse(uri.Host, out var ip))
            return false; // hostname, not an IP literal — can't check without DNS

        if (System.Net.IPAddress.IsLoopback(ip))
            return true;

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal)
                return true;
            var bytes = ip.GetAddressBytes();
            if ((bytes[0] & 0xFE) == 0xFC) // fc00::/7
                return true;
            if (ip.IsIPv4MappedToIPv6)
                return IsLiteralPrivateHost(new Uri($"https://{ip.MapToIPv4()}/"));
            return false;
        }

        var ipBytes = ip.GetAddressBytes();
        return ipBytes[0] switch
        {
            0 => true, // 0.0.0.0/8
            10 => true,
            100 when ipBytes[1] >= 64 && ipBytes[1] <= 127 => true, // 100.64.0.0/10 CGNAT
            127 => true,
            169 when ipBytes[1] == 254 => true,
            172 when ipBytes[1] >= 16 && ipBytes[1] <= 31 => true,
            192 when ipBytes[1] == 0 && ipBytes[2] == 0 => true, // 192.0.0.0/24
            192 when ipBytes[1] == 168 => true,
            198 when ipBytes[1] >= 18 && ipBytes[1] <= 19 => true, // 198.18.0.0/15
            >= 240 => true, // 240.0.0.0/4 reserved + broadcast
            _ => false,
        };
    }
}
