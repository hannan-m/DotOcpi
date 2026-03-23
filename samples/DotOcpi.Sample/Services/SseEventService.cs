using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace DotOcpi.Sample.Services;

/// <summary>
/// Manages SSE connections and broadcasts events to all connected clients.
/// </summary>
public sealed class SseEventService
{
    private readonly ConcurrentDictionary<string, Stream> _clients = new();

    public string AddClient(Stream responseStream)
    {
        var clientId = Guid.NewGuid().ToString("N");
        _clients[clientId] = responseStream;
        return clientId;
    }

    public void RemoveClient(string clientId)
    {
        _clients.TryRemove(clientId, out _);
    }

    public async Task BroadcastAsync(string eventType, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var message = $"event: {eventType}\ndata: {json}\n\n";
        var bytes = Encoding.UTF8.GetBytes(message);

        foreach (var (id, stream) in _clients)
        {
            try
            {
                await stream.WriteAsync(bytes).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
            }
            catch
            {
                _clients.TryRemove(id, out _);
            }
        }
    }
}
