using System.Text;
using DotOcpi.Registry;
using DotOcpi.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DotOcpi.AspNetCore.Tests.Handlers;

/// <summary>
/// Shared test infrastructure for handler endpoint tests.
/// Eliminates boilerplate duplication across module test classes.
/// </summary>
internal static class OcpiEndpointTestHelper
{
    internal static readonly CpoConnection TestConnection = new()
    {
        CpoCountryCode = "DE",
        CpoPartyId = "ALL",
        EmspCountryCode = "NL",
        EmspPartyId = "TNM",
        Version = OcpiVersion.V2_2_1,
        ModuleEndpoints = new Dictionary<string, string>(),
        TokenBHash = "hash",
        Status = ConnectionStatus.Connected,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    internal static OcpiRequestContext CreateContext(OcpiVersion version, string moduleId) =>
        new()
        {
            Connection = TestConnection with { Version = version },
            RequestId = "req-1",
            CorrelationId = "corr-1",
            CpoId = "DE_ALL",
            CpoIdentity = new PartyIdentity("DE", "ALL"),
            EmspIdentity = new PartyIdentity("NL", "TNM"),
            NegotiatedVersion = version,
            ModuleId = moduleId,
        };

    internal static DefaultHttpContext CreateHttpContext<TService>(
        TService service,
        OcpiVersion version,
        string moduleId,
        string? body = null
    )
        where TService : class
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = new ServiceCollection().AddSingleton(service).BuildServiceProvider();
        httpContext.SetOcpiContext(CreateContext(version, moduleId));

        if (body is not null)
        {
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            httpContext.Request.ContentType = "application/json";
        }

        return httpContext;
    }

    internal static string ReadResponseBody(DefaultHttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body);
        return reader.ReadToEnd();
    }

    internal static OcpiRegistrationContext CreateRegistrationContext(OcpiVersion version) =>
        new()
        {
            RequestId = "req-1",
            CorrelationId = "corr-1",
            Version = version,
            TokenAEntry = new TokenEntry("token-a-hash", TokenPurpose.TokenA, "NL:TNM"),
        };

    internal static DefaultHttpContext CreateRegistrationHttpContext<TService>(
        TService service,
        OcpiVersion version,
        string? body = null
    )
        where TService : class
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = new ServiceCollection().AddSingleton(service).BuildServiceProvider();
        httpContext.SetRegistrationContext(CreateRegistrationContext(version));

        if (body is not null)
        {
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            httpContext.Request.ContentType = "application/json";
        }

        return httpContext;
    }

    internal static IEnumerable<object[]> AllVersions =>
        [
            [OcpiVersion.V2_0],
            [OcpiVersion.V2_1_1],
            [OcpiVersion.V2_2],
            [OcpiVersion.V2_2_1],
        ];
}
