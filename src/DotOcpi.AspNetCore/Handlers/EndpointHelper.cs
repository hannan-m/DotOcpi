using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using DotOcpi.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DotOcpi.AspNetCore.Handlers;

/// <summary>
/// Shared helpers for OCPI module endpoint handlers.
/// </summary>
internal static class EndpointHelper
{
    /// <summary>
    /// Deserializes the request body using the given handler, then validates
    /// the result against a registered <see cref="IOcpiValidator{T}"/> if available.
    /// Returns null and writes a 400 error if the body is missing, invalid JSON,
    /// or fails protocol-level validation.
    /// </summary>
    internal static async ValueTask<object?> DeserializeOrRejectAsync(HttpContext httpContext, IModuleHandler handler)
    {
        try
        {
            var data = await handler
                .DeserializeAsync(httpContext.Request.Body, httpContext.RequestAborted)
                .ConfigureAwait(false);
            if (data is null)
            {
                await OcpiResponseWriter
                    .WriteErrorAsync(
                        httpContext,
                        400,
                        OcpiStatusCode.InvalidParameters.Value,
                        "Request body is required.",
                        httpContext.RequestAborted
                    )
                    .ConfigureAwait(false);
                return null;
            }

            // Protocol-level validation via IOcpiValidator<T> if registered.
            // Uses the runtime type of the deserialized model to resolve the correct
            // validator (e.g., IOcpiValidator<Location> for a V2_2_1 Location).
            var validationError = TryValidate(httpContext.RequestServices, data);
            if (validationError is not null)
            {
                await OcpiResponseWriter
                    .WriteErrorAsync(
                        httpContext,
                        400,
                        OcpiStatusCode.InvalidParameters.Value,
                        validationError,
                        httpContext.RequestAborted
                    )
                    .ConfigureAwait(false);
                return null;
            }

            return data;
        }
        catch (JsonException)
        {
            await OcpiResponseWriter
                .WriteErrorAsync(
                    httpContext,
                    400,
                    OcpiStatusCode.InvalidParameters.Value,
                    "Request body contains invalid JSON.",
                    httpContext.RequestAborted
                )
                .ConfigureAwait(false);
            return null;
        }
    }

    /// <summary>
    /// Reads the request body as a <see cref="JsonElement"/> for PATCH operations.
    /// Returns null and writes a 400 error if the body is missing or invalid.
    /// </summary>
    internal static async ValueTask<JsonElement?> ReadPatchAsync(HttpContext httpContext)
    {
        if (httpContext.Request.ContentLength == 0)
        {
            await OcpiResponseWriter
                .WriteErrorAsync(
                    httpContext,
                    400,
                    OcpiStatusCode.InvalidParameters.Value,
                    "Request body is required.",
                    httpContext.RequestAborted
                )
                .ConfigureAwait(false);
            return null;
        }

        try
        {
            using var doc = await JsonDocument
                .ParseAsync(httpContext.Request.Body, cancellationToken: httpContext.RequestAborted)
                .ConfigureAwait(false);
            var element = doc.RootElement.Clone();

            if (element.ValueKind != JsonValueKind.Object)
            {
                await OcpiResponseWriter
                    .WriteErrorAsync(
                        httpContext,
                        400,
                        OcpiStatusCode.InvalidParameters.Value,
                        "PATCH body must be a JSON object.",
                        httpContext.RequestAborted
                    )
                    .ConfigureAwait(false);
                return null;
            }

            return element;
        }
        catch (JsonException)
        {
            await OcpiResponseWriter
                .WriteErrorAsync(
                    httpContext,
                    400,
                    OcpiStatusCode.InvalidParameters.Value,
                    "Request body contains invalid JSON.",
                    httpContext.RequestAborted
                )
                .ConfigureAwait(false);
            return null;
        }
    }

    /// <summary>
    /// Two-layer validation: DataAnnotations first ([Required], [StringLength], etc.),
    /// then IOcpiValidator for protocol-level rules.
    /// Returns an error message if validation fails, or null if valid.
    /// </summary>
    internal static string? TryValidate(IServiceProvider services, object model)
    {
        // Layer 1: DataAnnotations — enforces [Required], [StringLength], etc.
        // Standard Validator.TryValidateObject throws InvalidCastException on CiString
        // properties with [StringLength], so we validate manually to handle CiString.
        var annotationError = ValidateDataAnnotations(model);
        if (annotationError is not null)
            return annotationError;

        // Layer 2: IOcpiValidator<T> — protocol-level rules (coordinates, enum values, etc.)
        // Uses a static map from model Type to service Type — no MakeGenericType, AOT-safe.
        if (!ValidatorServiceTypes.TryGetValue(model.GetType(), out var validatorServiceType))
            return null;
        if (services.GetService(validatorServiceType) is not IOcpiValidator validator)
            return null;

        var result = validator.Validate(model);

        if (result.IsValid)
            return null;

        return string.Join("; ", result.Errors.Select(static e => e.Message));
    }

    private static readonly FrozenDictionary<Type, Type> ValidatorServiceTypes = new Dictionary<Type, Type>
    {
        // V2_0
        [typeof(Models.V2_0.Location)] = typeof(IOcpiValidator<Models.V2_0.Location>),
        [typeof(Models.V2_0.Session)] = typeof(IOcpiValidator<Models.V2_0.Session>),
        [typeof(Models.V2_0.Cdr)] = typeof(IOcpiValidator<Models.V2_0.Cdr>),
        [typeof(Models.V2_0.Tariff)] = typeof(IOcpiValidator<Models.V2_0.Tariff>),
        [typeof(Models.V2_0.Token)] = typeof(IOcpiValidator<Models.V2_0.Token>),
        [typeof(Models.V2_0.Credentials)] = typeof(IOcpiValidator<Models.V2_0.Credentials>),
        // V2_1_1
        [typeof(Models.V2_1_1.Location)] = typeof(IOcpiValidator<Models.V2_1_1.Location>),
        [typeof(Models.V2_1_1.Session)] = typeof(IOcpiValidator<Models.V2_1_1.Session>),
        [typeof(Models.V2_1_1.Cdr)] = typeof(IOcpiValidator<Models.V2_1_1.Cdr>),
        [typeof(Models.V2_1_1.Tariff)] = typeof(IOcpiValidator<Models.V2_1_1.Tariff>),
        [typeof(Models.V2_1_1.Token)] = typeof(IOcpiValidator<Models.V2_1_1.Token>),
        [typeof(Models.V2_1_1.Credentials)] = typeof(IOcpiValidator<Models.V2_1_1.Credentials>),
        // V2_2
        [typeof(Models.V2_2.Location)] = typeof(IOcpiValidator<Models.V2_2.Location>),
        [typeof(Models.V2_2.Session)] = typeof(IOcpiValidator<Models.V2_2.Session>),
        [typeof(Models.V2_2.Cdr)] = typeof(IOcpiValidator<Models.V2_2.Cdr>),
        [typeof(Models.V2_2.Tariff)] = typeof(IOcpiValidator<Models.V2_2.Tariff>),
        [typeof(Models.V2_2.Token)] = typeof(IOcpiValidator<Models.V2_2.Token>),
        [typeof(Models.V2_2.Credentials)] = typeof(IOcpiValidator<Models.V2_2.Credentials>),
        // V2_2_1
        [typeof(Models.V2_2_1.Location)] = typeof(IOcpiValidator<Models.V2_2_1.Location>),
        [typeof(Models.V2_2_1.Session)] = typeof(IOcpiValidator<Models.V2_2_1.Session>),
        [typeof(Models.V2_2_1.Cdr)] = typeof(IOcpiValidator<Models.V2_2_1.Cdr>),
        [typeof(Models.V2_2_1.Tariff)] = typeof(IOcpiValidator<Models.V2_2_1.Tariff>),
        [typeof(Models.V2_2_1.Token)] = typeof(IOcpiValidator<Models.V2_2_1.Token>),
        [typeof(Models.V2_2_1.Credentials)] = typeof(IOcpiValidator<Models.V2_2_1.Credentials>),
    }.ToFrozenDictionary();

    private static readonly ConcurrentDictionary<Type, CachedPropertyValidation[]> ValidationCache = new();

    /// <summary>
    /// Validates [Required] and [StringLength] attributes on model properties,
    /// handling both <see cref="string"/> and <see cref="CiString"/> values.
    /// Standard <see cref="Validator.TryValidateObject"/> throws on CiString
    /// because <see cref="StringLengthAttribute"/> expects string.
    /// Property metadata is cached per model type to avoid repeated reflection.
    /// </summary>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2070:DynamicallyAccessedMembers",
        Justification = "Model types are preserved via [JsonSerializable] source-gen context registrations."
    )]
    private static string? ValidateDataAnnotations(object model)
    {
        var entries = ValidationCache.GetOrAdd(
            model.GetType(),
            static type =>
            {
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var list = new List<CachedPropertyValidation>(props.Length);
                foreach (var prop in props)
                {
                    var isRequired = prop.IsDefined(typeof(RequiredAttribute), true);
                    var sl = prop.GetCustomAttribute<StringLengthAttribute>(true);
                    if (isRequired || sl is not null)
                        list.Add(new CachedPropertyValidation(prop, isRequired, sl));
                }
                return list.ToArray();
            }
        );

        if (entries.Length == 0)
            return null;

        var errors = new List<string>();
        foreach (var entry in entries)
        {
            var value = entry.Property.GetValue(model);

            if (entry.IsRequired && value is null)
            {
                errors.Add($"The {entry.Property.Name} field is required.");
                continue;
            }

            if (entry.StringLength is not null && value is not null)
            {
                var strValue = value switch
                {
                    string s => s,
                    CiString ci => ci.Value,
                    _ => null,
                };

                if (strValue is not null && !entry.StringLength.IsValid(strValue))
                    errors.Add(entry.StringLength.FormatErrorMessage(entry.Property.Name));
            }
        }

        return errors.Count > 0 ? string.Join("; ", errors) : null;
    }

    private sealed record CachedPropertyValidation(
        PropertyInfo Property,
        bool IsRequired,
        StringLengthAttribute? StringLength
    );
}
