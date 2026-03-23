using System.Collections.Concurrent;

namespace DotOcpi.Sample.Services;

/// <summary>
/// Tracks command history for display in the UI.
/// </summary>
public sealed class CommandHistory
{
    private readonly ConcurrentQueue<CommandRecord> _records = new();

    public void Add(string cpoId, string commandType, string result)
    {
        _records.Enqueue(new CommandRecord(
            DateTimeOffset.UtcNow,
            cpoId,
            commandType,
            result
        ));

        // Keep last 50
        while (_records.Count > 50)
            _records.TryDequeue(out _);
    }

    public IReadOnlyList<CommandRecord> GetRecent(int count = 20) =>
        _records.Reverse().Take(count).ToList();
}

public sealed record CommandRecord(
    DateTimeOffset Time,
    string CpoId,
    string CommandType,
    string Result
);
