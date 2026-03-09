using System.Text.Json;

namespace FrankenTui.Runtime;

public sealed class ReplayTape<TMessage>
{
    private readonly List<ReplayEntry<TMessage>> _entries = [];

    public IReadOnlyList<ReplayEntry<TMessage>> Entries => _entries;

    public string Fingerprint => RuntimeTraceHash.Compute(_entries.Select(static entry => entry.Fingerprint));

    public void Add(TMessage message, string frame) => Add(_entries.Count, message, [], frame, string.Empty);

    public void Add(
        int stepIndex,
        TMessage message,
        IReadOnlyList<TMessage> emittedMessages,
        string screenText,
        string output)
    {
        ArgumentNullException.ThrowIfNull(screenText);
        ArgumentNullException.ThrowIfNull(output);

        var emitted = emittedMessages ?? [];
        _entries.Add(
            new ReplayEntry<TMessage>(
                stepIndex,
                message,
                emitted,
                screenText,
                output,
                RuntimeTraceHash.Compute(stepIndex, message, emitted, screenText, output)));
    }

    public string ToJson() =>
        JsonSerializer.Serialize(
            _entries,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

    public static ReplayTape<TMessage> FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var entries = JsonSerializer.Deserialize<List<ReplayEntry<TMessage>>>(json) ?? [];
        var tape = new ReplayTape<TMessage>();
        foreach (var entry in entries)
        {
            tape._entries.Add(entry);
        }

        return tape;
    }

    public bool IsDeterministicMatch(ReplayTape<TMessage> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return string.Equals(Fingerprint, other.Fingerprint, StringComparison.Ordinal);
    }

    internal static ReplayTape<TMessage> FromTrace(IReadOnlyList<RuntimeTraceEntry<TMessage>> entries)
    {
        var tape = new ReplayTape<TMessage>();
        foreach (var entry in entries)
        {
            tape._entries.Add(
                new ReplayEntry<TMessage>(
                    entry.StepIndex,
                    entry.Message,
                    entry.EmittedMessages,
                    entry.ScreenText,
                    entry.Output,
                    entry.Fingerprint));
        }

        return tape;
    }
}

public readonly record struct ReplayEntry<TMessage>(
    int StepIndex,
    TMessage Message,
    IReadOnlyList<TMessage> EmittedMessages,
    string ScreenText,
    string Output,
    string Fingerprint);
