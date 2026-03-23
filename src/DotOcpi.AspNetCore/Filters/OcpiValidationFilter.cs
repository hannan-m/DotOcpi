using DotOcpi.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DotOcpi.AspNetCore.Filters;

/// <summary>
/// Endpoint filter that validates incoming OCPI model payloads using
/// <see cref="IOcpiValidator{T}"/>. Returns OCPI status 2001 (invalid
/// or missing parameters) when validation fails.
/// </summary>
/// <typeparam name="T">The model type to validate.</typeparam>
public sealed class OcpiValidationFilter<T> : IEndpointFilter
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IOcpiValidator<T>>();
        if (validator is null)
        {
            return await next(context).ConfigureAwait(false);
        }

        // Find the first argument of type T in the endpoint parameters
        T? model = default;
        foreach (var arg in context.Arguments)
        {
            if (arg is T typed)
            {
                model = typed;
                break;
            }
        }

        if (model is null)
        {
            return await next(context).ConfigureAwait(false);
        }

        var result = validator.Validate(model);
        if (result.IsValid)
        {
            return await next(context).ConfigureAwait(false);
        }

        return OcpiResponseWriter.ValidationErrorResult(result.Errors);
    }
}
