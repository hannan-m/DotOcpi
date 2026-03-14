using System.Diagnostics.CodeAnalysis;

namespace DotOcpi;

/// <summary>
/// Discriminated result type for OCPI operations that return data.
/// Forces callers to handle success and failure explicitly.
/// </summary>
/// <typeparam name="T">The type of the data payload on success.</typeparam>
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Factory methods are the standard pattern for result types")]
public sealed class OcpiResult<T>
{
    private OcpiResult(bool isSuccess, T? data, OcpiStatusCode statusCode, string? statusMessage)
    {
        IsSuccess = isSuccess;
        Data = data;
        StatusCode = statusCode;
        StatusMessage = statusMessage;
    }

    /// <summary>True if the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>The data payload. Non-null on success, null on failure.</summary>
    public T? Data { get; }

    /// <summary>The OCPI status code.</summary>
    public OcpiStatusCode StatusCode { get; }

    /// <summary>Optional status message.</summary>
    public string? StatusMessage { get; }

    /// <summary>
    /// Creates a successful result with data.
    /// </summary>
    public static OcpiResult<T> Success(T data, string? message = null) =>
        new(true, data, OcpiStatusCode.Success, message);

    /// <summary>
    /// Creates a failure result with an error code and message.
    /// </summary>
    public static OcpiResult<T> Failure(OcpiStatusCode code, string message) =>
        new(false, default, code, message);
}

/// <summary>
/// Discriminated result type for OCPI operations that return no data (void operations).
/// </summary>
public sealed class OcpiResult
{
    private OcpiResult(bool isSuccess, OcpiStatusCode statusCode, string? statusMessage)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        StatusMessage = statusMessage;
    }

    /// <summary>True if the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>The OCPI status code.</summary>
    public OcpiStatusCode StatusCode { get; }

    /// <summary>Optional status message.</summary>
    public string? StatusMessage { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static OcpiResult Success(string? message = null) =>
        new(true, OcpiStatusCode.Success, message);

    /// <summary>
    /// Creates a failure result with an error code and message.
    /// </summary>
    public static OcpiResult Failure(OcpiStatusCode code, string message) =>
        new(false, code, message);
}
