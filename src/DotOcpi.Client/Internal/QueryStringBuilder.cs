using System.Text;

namespace DotOcpi.Client.Internal;

/// <summary>
/// Builds OCPI pagination query strings with date_from/date_to parameters.
/// </summary>
internal static class QueryStringBuilder
{
    internal static string? BuildDateFilter(DateTimeOffset? dateFrom, DateTimeOffset? dateTo)
    {
        if (dateFrom is null && dateTo is null)
            return null;

        var sb = new StringBuilder();

        if (dateFrom.HasValue)
        {
            sb.Append("date_from=");
            sb.Append(Uri.EscapeDataString(FormatDateTime(dateFrom.Value)));
        }

        if (dateTo.HasValue)
        {
            if (sb.Length > 0)
                sb.Append('&');
            sb.Append("date_to=");
            sb.Append(Uri.EscapeDataString(FormatDateTime(dateTo.Value)));
        }

        return sb.ToString();
    }

    private static string FormatDateTime(DateTimeOffset value) => OcpiDateTime.Format(value);
}
