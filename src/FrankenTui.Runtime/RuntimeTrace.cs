using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FrankenTui.Runtime;

public sealed class RuntimeTrace<TMessage>
{
    private readonly List<RuntimeTraceEntry<TMessage>> _entries = [];

    public IReadOnlyList<RuntimeTraceEntry<TMessage>> Entries => _entries;

    public IReadOnlyList<string> Frames => _entries.Select(static entry => entry.ScreenText).ToArray();

    public IReadOnlyList<TMessage> Messages => _entries.Select(static entry => entry.Message).ToArray();

    public string Fingerprint => RuntimeTraceHash.Compute(_entries.Select(static entry => entry.Fingerprint));

    public void Record(
        int stepIndex,
        TMessage message,
        IEnumerable<TMessage> emittedMessages,
        string screenText,
        string output)
    {
        ArgumentNullException.ThrowIfNull(emittedMessages);
        ArgumentNullException.ThrowIfNull(screenText);
        ArgumentNullException.ThrowIfNull(output);

        var emitted = emittedMessages.ToArray();
        _entries.Add(
            new RuntimeTraceEntry<TMessage>(
                stepIndex,
                message,
                emitted,
                screenText,
                output,
                RuntimeTraceHash.Compute(stepIndex, message, emitted, screenText, output)));
    }

    public ReplayTape<TMessage> ToReplayTape() => ReplayTape<TMessage>.FromTrace(_entries);
}

public sealed record RuntimeTraceEntry<TMessage>(
    int StepIndex,
    TMessage Message,
    IReadOnlyList<TMessage> EmittedMessages,
    string ScreenText,
    string Output,
    string Fingerprint);

internal static class RuntimeTraceHash
{
    public static string Compute<TMessage>(
        int stepIndex,
        TMessage message,
        IReadOnlyList<TMessage> emittedMessages,
        string screenText,
        string output)
    {
        var builder = new StringBuilder();
        builder.Append(stepIndex).Append('\n');
        builder.Append(JsonSerializer.Serialize(message)).Append('\n');
        builder.Append(JsonSerializer.Serialize(emittedMessages)).Append('\n');
        builder.Append(screenText).Append('\n');
        builder.Append(output);
        return Compute(builder.ToString());
    }

    public static string Compute(IEnumerable<string> values)
    {
        var builder = new StringBuilder();
        foreach (var value in values)
        {
            builder.Append(value).Append('\n');
        }

        return Compute(builder.ToString());
    }

    private static string Compute(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
}
