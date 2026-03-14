using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DotOcpi.Testing;

/// <summary>
/// In-memory OCPI-compliant CPO test server. Serves version discovery,
/// credentials exchange, and configurable module data. Intended for
/// consumer integration tests.
/// </summary>
public sealed class OcpiTestCpoServer : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly TestCpoConfiguration _config;
    private string? _issuedTokenB;
    private string? _receivedTokenC;

    private OcpiTestCpoServer(WebApplication app, TestCpoConfiguration config, string tokenA)
    {
        _app = app;
        _config = config;
        TokenA = tokenA;
    }

    /// <summary>The pre-shared Token A for initial registration.</summary>
    public string TokenA { get; }

    /// <summary>The base URL of the test server.</summary>
    public Uri BaseUrl => new(_app.Urls.First());

    /// <summary>Returns the Token B issued during the last credentials exchange.</summary>
    public string GetIssuedTokenB() =>
        _issuedTokenB ?? throw new InvalidOperationException("No credentials exchange has occurred yet.");

    /// <summary>Returns the Token C received from the eMSP during the last credentials exchange.</summary>
    public string? GetReceivedTokenC() => _receivedTokenC;

    /// <summary>Creates and starts a new test CPO server.</summary>
    public static async Task<OcpiTestCpoServer> CreateAsync(Action<TestCpoConfiguration>? configure = null)
    {
        var config = new TestCpoConfiguration();
        configure?.Invoke(config);

        var tokenA = GenerateToken();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenLocalhost(0);
        });

        var app = builder.Build();
        var server = new OcpiTestCpoServer(app, config, tokenA);
        server.MapEndpoints(app);

        await app.StartAsync().ConfigureAwait(false);

        return server;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync().ConfigureAwait(false);
        await _app.DisposeAsync().ConfigureAwait(false);
    }

    private void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/ocpi/versions", HandleVersions);
        endpoints.MapGet("/ocpi/versions/{versionId}", HandleVersionDetail);
        endpoints.MapPost("/ocpi/credentials", HandlePostCredentials);
        endpoints.MapPut("/ocpi/credentials", HandlePutCredentials);
        endpoints.MapDelete("/ocpi/credentials", HandleDeleteCredentials);
        endpoints.MapGet("/ocpi/locations", HandleGetLocations);
        endpoints.MapGet("/ocpi/tariffs", HandleGetTariffs);
        endpoints.MapGet("/ocpi/sessions", HandleGetSessions);
        endpoints.MapGet("/ocpi/cdrs", HandleGetCdrs);
    }

    private async Task HandleVersions(HttpContext ctx)
    {
        await ApplyFailureInjection(ctx).ConfigureAwait(false);

        var baseUrl = BaseUrl.ToString().TrimEnd('/');
        var versions = _config
            .SupportedVersions.Select(v => new
            {
                version = v.ToVersionString(),
                url = $"{baseUrl}/ocpi/versions/{v.ToVersionString()}",
            })
            .ToArray();

        await WriteOcpiResponse(ctx, versions).ConfigureAwait(false);
    }

    private async Task HandleVersionDetail(HttpContext ctx, string versionId)
    {
        await ApplyFailureInjection(ctx).ConfigureAwait(false);

        if (
            !OcpiVersionExtensions.TryParse(versionId, out var version)
            || !_config.SupportedVersions.Contains(version.Value)
        )
        {
            ctx.Response.StatusCode = 404;
            await WriteOcpiError(ctx, 2001, $"Version {versionId} not supported.").ConfigureAwait(false);
            return;
        }

        var baseUrl = BaseUrl.ToString().TrimEnd('/');
        var detail = new
        {
            version = versionId,
            endpoints = new object[]
            {
                new { identifier = "credentials", url = $"{baseUrl}/ocpi/credentials" },
                new { identifier = "locations", url = $"{baseUrl}/ocpi/locations" },
                new { identifier = "tariffs", url = $"{baseUrl}/ocpi/tariffs" },
                new { identifier = "sessions", url = $"{baseUrl}/ocpi/sessions" },
                new { identifier = "cdrs", url = $"{baseUrl}/ocpi/cdrs" },
            },
        };

        await WriteOcpiResponse(ctx, detail).ConfigureAwait(false);
    }

    private async Task HandlePostCredentials(HttpContext ctx)
    {
        await ApplyFailureInjection(ctx).ConfigureAwait(false);

        if (!ValidateAuth(ctx, TokenA))
        {
            return;
        }

        if (_config.RejectRegistration)
        {
            ctx.Response.StatusCode = 400;
            await WriteOcpiError(ctx, 3001, "Registration rejected.").ConfigureAwait(false);
            return;
        }

        var body = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body).ConfigureAwait(false);

        if (body.TryGetProperty("token", out var tokenProp))
        {
            _receivedTokenC = tokenProp.GetString();
        }

        _issuedTokenB = GenerateToken();
        await WriteCredentialsResponse(ctx).ConfigureAwait(false);
    }

    private async Task HandlePutCredentials(HttpContext ctx)
    {
        await ApplyFailureInjection(ctx).ConfigureAwait(false);

        if (_issuedTokenB is null || !ValidateAuth(ctx, _issuedTokenB))
        {
            return;
        }

        var body = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body).ConfigureAwait(false);

        if (body.TryGetProperty("token", out var tokenProp))
        {
            _receivedTokenC = tokenProp.GetString();
        }

        _issuedTokenB = GenerateToken();
        await WriteCredentialsResponse(ctx).ConfigureAwait(false);
    }

    private async Task HandleDeleteCredentials(HttpContext ctx)
    {
        await ApplyFailureInjection(ctx).ConfigureAwait(false);

        if (_issuedTokenB is null || !ValidateAuth(ctx, _issuedTokenB))
        {
            return;
        }

        _issuedTokenB = null;
        _receivedTokenC = null;

        await WriteOcpiResponse(ctx, (object?)null).ConfigureAwait(false);
    }

    private async Task HandleGetLocations(HttpContext ctx)
    {
        await ApplyFailureInjection(ctx).ConfigureAwait(false);
        await WriteOcpiListResponse(ctx, _config.Locations).ConfigureAwait(false);
    }

    private async Task HandleGetTariffs(HttpContext ctx)
    {
        await ApplyFailureInjection(ctx).ConfigureAwait(false);
        await WriteOcpiListResponse(ctx, _config.Tariffs).ConfigureAwait(false);
    }

    private async Task HandleGetSessions(HttpContext ctx)
    {
        await ApplyFailureInjection(ctx).ConfigureAwait(false);
        await WriteOcpiListResponse(ctx, _config.Sessions).ConfigureAwait(false);
    }

    private async Task HandleGetCdrs(HttpContext ctx)
    {
        await ApplyFailureInjection(ctx).ConfigureAwait(false);
        await WriteOcpiListResponse(ctx, _config.Cdrs).ConfigureAwait(false);
    }

    private static bool ValidateAuth(HttpContext ctx, string expectedToken)
    {
        var authHeader = ctx.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Token ", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Response.StatusCode = 401;
            WriteOcpiError(ctx, 2002, "Missing or invalid Authorization header.").GetAwaiter().GetResult();
            return false;
        }

        var token = authHeader["Token ".Length..];
        if (token != expectedToken)
        {
            ctx.Response.StatusCode = 401;
            WriteOcpiError(ctx, 2002, "Invalid token.").GetAwaiter().GetResult();
            return false;
        }

        return true;
    }

    private async Task ApplyFailureInjection(HttpContext ctx)
    {
        if (_config.SimulateTimeout)
        {
            throw new TaskCanceledException("Simulated timeout.");
        }

        if (_config.ResponseDelay is { } delay)
        {
            await Task.Delay(delay, ctx.RequestAborted).ConfigureAwait(false);
        }
    }

    private async Task WriteCredentialsResponse(HttpContext ctx)
    {
        var response = new
        {
            token = _issuedTokenB,
            url = $"{BaseUrl.ToString().TrimEnd('/')}/ocpi/versions",
            roles = new[]
            {
                new
                {
                    role = "CPO",
                    country_code = _config.CpoIdentity.CountryCode,
                    party_id = _config.CpoIdentity.PartyId,
                    business_details = new { name = "Test CPO" },
                },
            },
        };

        await WriteOcpiResponse(ctx, response).ConfigureAwait(false);
    }

    private async Task WriteOcpiResponse(HttpContext ctx, object? data)
    {
        var statusCode = _config.ForceStatusCode?.Value ?? 1000;
        var response = new
        {
            status_code = statusCode,
            status_message = statusCode == 1000 ? "Success" : "Forced error",
            timestamp = FormatTimestamp(),
            data,
        };

        ctx.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, response).ConfigureAwait(false);
    }

    private async Task WriteOcpiListResponse(HttpContext ctx, List<object> items)
    {
        ctx.Response.Headers["X-Total-Count"] = items.Count.ToString(CultureInfo.InvariantCulture);
        ctx.Response.Headers["X-Limit"] = "1000";

        var statusCode = _config.ForceStatusCode?.Value ?? 1000;
        var response = new
        {
            status_code = statusCode,
            status_message = statusCode == 1000 ? "Success" : "Forced error",
            timestamp = FormatTimestamp(),
            data = items,
        };

        ctx.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, response).ConfigureAwait(false);
    }

    private static async Task WriteOcpiError(HttpContext ctx, int statusCode, string message)
    {
        var response = new
        {
            status_code = statusCode,
            status_message = message,
            timestamp = FormatTimestamp(),
        };

        ctx.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, response).ConfigureAwait(false);
    }

    private static string FormatTimestamp() =>
        DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private static string GenerateToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
