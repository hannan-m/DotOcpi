using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Channels;
using DotOcpi.Simulator.Infrastructure;
using DotOcpi.Simulator.State;

namespace DotOcpi.Simulator.Push;

/// <summary>
/// Channel-based push engine that delivers OCPI push notifications to the eMSP.
/// Sequential per-connection to preserve ordering (session PUT before PATCH).
/// </summary>
internal sealed class PushEngine : IAsyncDisposable
{
    private readonly Channel<PushRequest> _channel = Channel.CreateBounded<PushRequest>(100);
    private readonly HttpClient _client = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processingTask;
    private readonly CpoSimulatorConfiguration _config;
    private readonly SimulatorState _state;
    private int _inFlight;

    public PushEngine(CpoSimulatorConfiguration config, SimulatorState state)
    {
        _config = config;
        _state = state;
        _processingTask = ProcessAsync();
    }

    /// <summary>
    /// Enqueues a command callback to the response_url.
    /// </summary>
    public void EnqueueCommandCallback(string responseUrl, string resultStatus, TimeSpan delay)
    {
        _channel.Writer.TryWrite(
            new PushRequest
            {
                Type = PushType.CommandCallback,
                Url = responseUrl,
                Payload = resultStatus,
                Delay = delay,
            }
        );
    }

    /// <summary>
    /// Enqueues a session push (PUT or PATCH) to the eMSP.
    /// </summary>
    public void EnqueueSessionPush(string sessionId, object sessionModel, string method)
    {
        if (!_config.PushEnabled || !_config.PushSessionUpdates)
            return;

        var conn = _state.DefaultConnection;
        if (conn?.ReceivedTokenC is null)
            return;

        _channel.Writer.TryWrite(
            new PushRequest
            {
                Type = PushType.SessionPush,
                SessionId = sessionId,
                Payload = sessionModel,
                Method = method,
                TokenC = conn.ReceivedTokenC,
                Version = conn.NegotiatedVersion,
            }
        );
    }

    /// <summary>
    /// Enqueues a CDR push (POST) to the eMSP.
    /// </summary>
    public void EnqueueCdrPush(object cdrModel)
    {
        if (!_config.PushEnabled || !_config.PushCdrOnCompletion)
            return;

        var conn = _state.DefaultConnection;
        if (conn?.ReceivedTokenC is null)
            return;

        _channel.Writer.TryWrite(
            new PushRequest
            {
                Type = PushType.CdrPush,
                Payload = cdrModel,
                TokenC = conn.ReceivedTokenC,
                Version = conn.NegotiatedVersion,
            }
        );
    }

    /// <summary>Waits for all pending and in-flight push requests to complete.</summary>
    public async Task WaitForPendingAsync(TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while ((_channel.Reader.Count > 0 || Volatile.Read(ref _inFlight) > 0) && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(50).ConfigureAwait(false);
        }
    }

    private async Task ProcessAsync()
    {
        var ct = _cts.Token;
        try
        {
            await foreach (var request in _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                Interlocked.Increment(ref _inFlight);
                try
                {
                    if (request.Delay > TimeSpan.Zero)
                        await Task.Delay(request.Delay, ct).ConfigureAwait(false);

                    switch (request.Type)
                    {
                        case PushType.CommandCallback:
                            await SendCommandCallbackAsync(request, ct).ConfigureAwait(false);
                            break;
                        case PushType.SessionPush:
                            await SendSessionPushAsync(request, ct).ConfigureAwait(false);
                            break;
                        case PushType.CdrPush:
                            await SendCdrPushAsync(request, ct).ConfigureAwait(false);
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                { /* Best effort delivery */
                }
                finally
                {
                    Interlocked.Decrement(ref _inFlight);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task SendCommandCallbackAsync(PushRequest request, CancellationToken ct)
    {
        if (request.Url is null)
            return;
        if (!_config.AllowLoopbackCallbacks && !SsrfGuard.IsUrlSafeForOutbound(request.Url))
            return;

        var json = JsonSerializer.SerializeToUtf8Bytes(new { result = (string)request.Payload! });
        using var content = new ByteArrayContent(json);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        await _client.PostAsync(request.Url, content, ct).ConfigureAwait(false);
    }

    private async Task SendSessionPushAsync(PushRequest request, CancellationToken ct)
    {
        var emspBaseUrl = _config.EmspBaseUrl;
        if (emspBaseUrl is null)
            return;
        if (!_config.AllowLoopbackCallbacks && !SsrfGuard.IsUrlSafeForOutbound(emspBaseUrl))
            return;

        var conn = _state.DefaultConnection;
        if (conn is null)
            return;

        var cc = _config.CpoIdentity.CountryCode;
        var pid = _config.CpoIdentity.PartyId;
        var versionStr = request.Version.ToVersionString();
        var url = request.Version.UsesPartyIdInUrls()
            ? $"{emspBaseUrl.TrimEnd('/')}/{versionStr}/sessions/{cc}/{pid}/{request.SessionId}"
            : $"{emspBaseUrl.TrimEnd('/')}/{versionStr}/sessions/{request.SessionId}";

        var options = Serialization.OcpiJsonOptions.GetOptions(request.Version);
        var json = JsonSerializer.SerializeToUtf8Bytes(request.Payload!, request.Payload!.GetType(), options);
        var content = new ByteArrayContent(json);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var httpRequest = new HttpRequestMessage(
            request.Method == "PUT" ? HttpMethod.Put : HttpMethod.Patch,
            url
        );
        httpRequest.Content = content;
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Token", request.TokenC);
        await _client.SendAsync(httpRequest, ct).ConfigureAwait(false);
    }

    private async Task SendCdrPushAsync(PushRequest request, CancellationToken ct)
    {
        var emspBaseUrl = _config.EmspBaseUrl;
        if (emspBaseUrl is null)
            return;
        if (!_config.AllowLoopbackCallbacks && !SsrfGuard.IsUrlSafeForOutbound(emspBaseUrl))
            return;

        var versionStr = request.Version.ToVersionString();
        var url = $"{emspBaseUrl.TrimEnd('/')}/{versionStr}/cdrs";

        var options = Serialization.OcpiJsonOptions.GetOptions(request.Version);
        var json = JsonSerializer.SerializeToUtf8Bytes(request.Payload!, request.Payload!.GetType(), options);
        var content = new ByteArrayContent(json);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = content;
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Token", request.TokenC);
        await _client.SendAsync(httpRequest, ct).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        await _cts.CancelAsync().ConfigureAwait(false);
        try
        {
            await _processingTask.WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
        }
        catch
        { /* Shutting down */
        }
        _client.Dispose();
        _cts.Dispose();
    }
}

internal sealed class PushRequest
{
    public PushType Type { get; init; }
    public string? Url { get; init; }
    public string? SessionId { get; init; }
    public object? Payload { get; init; }
    public string? Method { get; init; }
    public string? TokenC { get; init; }
    public OcpiVersion Version { get; init; }
    public TimeSpan Delay { get; init; }
}

internal enum PushType
{
    CommandCallback,
    SessionPush,
    CdrPush,
}
