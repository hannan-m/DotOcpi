namespace DotOcpi;

/// <summary>
/// A paginated result set from an OCPI list endpoint.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
public sealed class PaginatedResult<T>
{
    /// <summary>The items in the current page.</summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>Total count of items across all pages (from X-Total-Count header).</summary>
    public required int TotalCount { get; init; }

    /// <summary>The offset of the first item in this page.</summary>
    public required int Offset { get; init; }

    /// <summary>The maximum number of items per page.</summary>
    public required int Limit { get; init; }
}
