using System.Buffers;
using System.Text.Json;
using DotOcpi.Simulator.Infrastructure;
using DotOcpi.Simulator.State;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.Simulator.Endpoints;

internal static class TokensHandler
{
    public static async Task HandlePut(
        HttpContext ctx,
        string tokenUid,
        SimulatorState state,
        CpoSimulatorConfiguration config
    )
    {
        await FailureInjectionHelper.ApplyAsync(ctx, config).ConfigureAwait(false);
        if (!await AuthHelper.ValidateModuleAuthAsync(ctx, state, config).ConfigureAwait(false))
            return;

        var body = RequestRecordingMiddleware.GetParsedBody(ctx) ?? default;
        state.ReceivedTokens[tokenUid] = body;

        await OcpiResponseWriter.WriteSuccessAsync(ctx).ConfigureAwait(false);
    }

    public static async Task HandlePatch(
        HttpContext ctx,
        string tokenUid,
        SimulatorState state,
        CpoSimulatorConfiguration config
    )
    {
        await FailureInjectionHelper.ApplyAsync(ctx, config).ConfigureAwait(false);
        if (!await AuthHelper.ValidateModuleAuthAsync(ctx, state, config).ConfigureAwait(false))
            return;

        var patch = RequestRecordingMiddleware.GetParsedBody(ctx) ?? default;
        if (patch.ValueKind != JsonValueKind.Object)
        {
            await OcpiResponseWriter
                .WriteErrorAsync(ctx, 400, 2001, "PATCH body must be a JSON object.")
                .ConfigureAwait(false);
            return;
        }

        if (!state.ReceivedTokens.TryGetValue(tokenUid, out var existing))
        {
            await OcpiResponseWriter
                .WriteErrorAsync(ctx, 404, 2003, $"Token '{tokenUid}' not found.")
                .ConfigureAwait(false);
            return;
        }

        state.ReceivedTokens[tokenUid] = MergeJsonPatch(existing, patch);

        await OcpiResponseWriter.WriteSuccessAsync(ctx).ConfigureAwait(false);
    }

    public static async Task HandleAuthorize(
        HttpContext ctx,
        string tokenUid,
        SimulatorState state,
        CpoSimulatorConfiguration config
    )
    {
        await FailureInjectionHelper.ApplyAsync(ctx, config).ConfigureAwait(false);
        if (!await AuthHelper.ValidateModuleAuthAsync(ctx, state, config).ConfigureAwait(false))
            return;

        var result = config.TokenAuthorizations.TryGetValue(tokenUid, out var perToken)
            ? perToken
            : config.AuthorizationResult;

        await OcpiResponseWriter
            .WriteSuccessAsync(
                ctx,
                static (writer, s) =>
                {
                    writer.WriteStartObject("data"u8);
                    writer.WriteString("allowed"u8, s.result);
                    writer.WriteEndObject();
                },
                (result, tokenUid)
            )
            .ConfigureAwait(false);
    }

    /// <summary>RFC 7386 JSON Merge Patch.</summary>
    internal static JsonElement MergeJsonPatch(JsonElement target, JsonElement patch)
    {
        if (patch.ValueKind != JsonValueKind.Object || target.ValueKind != JsonValueKind.Object)
            return patch;

        var buffer = new ArrayBufferWriter<byte>(256);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            foreach (var prop in target.EnumerateObject())
            {
                if (patch.TryGetProperty(prop.Name, out var patchValue))
                {
                    // RFC 7386: null in patch means remove the field
                    if (patchValue.ValueKind == JsonValueKind.Null)
                        continue;
                    // Both objects: recurse
                    if (prop.Value.ValueKind == JsonValueKind.Object && patchValue.ValueKind == JsonValueKind.Object)
                    {
                        writer.WritePropertyName(prop.Name);
                        var merged = MergeJsonPatch(prop.Value, patchValue);
                        merged.WriteTo(writer);
                        continue;
                    }
                    // Scalar replacement: write the patch value
                    writer.WritePropertyName(prop.Name);
                    patchValue.WriteTo(writer);
                    continue;
                }
                prop.WriteTo(writer);
            }
            // Write new properties from the patch that don't exist in the target.
            // Properties that exist in both were already handled in the first loop.
            foreach (var prop in patch.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Null)
                    continue;
                if (target.TryGetProperty(prop.Name, out _))
                    continue;
                prop.WriteTo(writer);
            }
            writer.WriteEndObject();
        }

        using var doc = JsonDocument.Parse(buffer.WrittenMemory);
        return doc.RootElement.Clone();
    }
}
