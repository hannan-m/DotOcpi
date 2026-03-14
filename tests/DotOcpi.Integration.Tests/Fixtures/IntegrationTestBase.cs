using DotOcpi.Testing;
using Xunit;

namespace DotOcpi.Integration.Tests.Fixtures;

/// <summary>
/// Base class for integration tests that use an in-memory test CPO server.
/// Creates a fresh server per test class and disposes it after all tests complete.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private OcpiTestCpoServer? _server;

    /// <summary>
    /// The test CPO server instance. Available after <see cref="InitializeAsync"/>.
    /// </summary>
    protected OcpiTestCpoServer Server => _server!;

    /// <summary>
    /// Override to customize the test CPO server configuration.
    /// </summary>
    protected virtual void ConfigureServer(TestCpoConfiguration config) { }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> configured with the server's base address.
    /// </summary>
    protected HttpClient CreateHttpClient()
    {
        return new HttpClient { BaseAddress = Server.BaseUrl };
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> with the given token in the Authorization header.
    /// </summary>
    protected HttpClient CreateHttpClient(string token)
    {
        var client = CreateHttpClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Token",
            token
        );
        return client;
    }

    public async Task InitializeAsync()
    {
        _server = await OcpiTestCpoServer.CreateAsync(ConfigureServer);
    }

    public async Task DisposeAsync()
    {
        if (_server is not null)
        {
            await _server.DisposeAsync();
        }
    }
}
