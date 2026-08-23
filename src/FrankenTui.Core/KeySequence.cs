// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-core/src/key_sequence.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

namespace FrankenTui.Core;

/// <summary>Configuration for multi-key sequence detection.</summary>
public sealed record KeySequenceConfig
{
    public static readonly TimeSpan DefaultSequenceTimeout = TimeSpan.FromMilliseconds(250);

    /// <summary>Window measured from the first buffered key.</summary>
    public TimeSpan SequenceTimeout { get; init; } = DefaultSequenceTimeout;

    /// <summary>Whether an unmodified Escape followed by Escape is recognized.</summary>
    public bool DetectDoubleEscape { get; init; } = true;

    public static KeySequenceConfig WithTimeout(TimeSpan timeout) =>
        new() { SequenceTimeout = ValidateTimeout(timeout) };

    internal static TimeSpan ValidateTimeout(TimeSpan timeout)
    {
        if (timeout < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "A sequence timeout cannot be negative.");
        }

        return timeout;
    }
}

/// <summary>A recognized multi-key sequence.</summary>
public enum KeySequenceKind
{
    DoubleEscape,
}

public static class KeySequenceKindExtensions
{
    public static string Name(this KeySequenceKind kind) => kind switch
    {
        KeySequenceKind.DoubleEscape => "Esc Esc",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}

/// <summary>The result of feeding a key to a <see cref="KeySequenceInterpreter"/>.</summary>
public abstract record KeySequenceAction
{
    public virtual bool IsPending => false;

    public virtual bool IsSequence => false;

    public sealed record Emit(KeyTerminalEvent Key) : KeySequenceAction;

    public sealed record EmitSequence(
        KeySequenceKind Kind,
        IReadOnlyList<KeyTerminalEvent> Keys) : KeySequenceAction
    {
        public override bool IsSequence => true;
    }

    public sealed record Pending : KeySequenceAction
    {
        public static Pending Instance { get; } = new();

        private Pending()
        {
        }

        public override bool IsPending => true;
    }
}

/// <summary>
/// Stateful, non-blocking interpreter for multi-key sequences.
/// </summary>
/// <remarks>
/// Rust's low-level <c>KeyEventKind</c> is not present after conversion to the .NET
/// semantic input stream: every <see cref="KeyTerminalEvent"/> represents a key press.
/// Release and repeat filtering therefore remains the responsibility of the terminal
/// adapter that creates the semantic event.
/// </remarks>
public sealed class KeySequenceInterpreter
{
    private readonly List<KeyTerminalEvent> _buffer = new(4);
    private KeySequenceConfig _config;
    private DateTimeOffset? _bufferStart;

    public KeySequenceInterpreter(KeySequenceConfig? config = null)
    {
        _config = ValidateConfig(config ?? new KeySequenceConfig());
    }

    public KeySequenceConfig Config => _config;

    public bool HasPending => _bufferStart is not null;

    /// <summary>Feeds a key using the event's own timestamp as the clock coordinate.</summary>
    public KeySequenceAction Feed(KeyTerminalEvent key) => Feed(key, key.Timestamp);

    public KeySequenceAction Feed(KeyTerminalEvent key, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(key);

        switch (TrySequence(key))
        {
            case SequenceResult.Complete:
                _buffer.Add(key);
                var keys = _buffer.ToArray();
                _buffer.Clear();
                _bufferStart = null;
                return new KeySequenceAction.EmitSequence(KeySequenceKind.DoubleEscape, keys);

            case SequenceResult.Continue:
                if (_buffer.Count == 0)
                {
                    _bufferStart = now;
                }

                _buffer.Add(key);
                return KeySequenceAction.Pending.Instance;

            default:
                // Match upstream's graceful-degradation contract: callers must poll
                // CheckTimeout before feeding a non-matching key if they need the
                // previously buffered key to be emitted.
                _buffer.Clear();
                _bufferStart = null;
                return new KeySequenceAction.Emit(key);
        }
    }

    /// <summary>Flushes buffered keys when the sequence window has expired.</summary>
    public IReadOnlyList<KeySequenceAction>? CheckTimeout(DateTimeOffset now)
    {
        if (_bufferStart is not { } start || ElapsedSince(start, now) < _config.SequenceTimeout)
        {
            return null;
        }

        var actions = Flush();
        return actions.Count == 0 ? null : actions;
    }

    public TimeSpan? TimeUntilTimeout(DateTimeOffset now)
    {
        if (_bufferStart is not { } start)
        {
            return null;
        }

        var remaining = _config.SequenceTimeout - ElapsedSince(start, now);
        return remaining <= TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    public void Reset()
    {
        _buffer.Clear();
        _bufferStart = null;
    }

    public IReadOnlyList<KeySequenceAction> Flush()
    {
        if (_buffer.Count == 0)
        {
            _bufferStart = null;
            return Array.Empty<KeySequenceAction>();
        }

        var actions = new KeySequenceAction[_buffer.Count];
        for (var index = 0; index < _buffer.Count; index++)
        {
            actions[index] = new KeySequenceAction.Emit(_buffer[index]);
        }

        _buffer.Clear();
        _bufferStart = null;
        return actions;
    }

    public void SetConfig(KeySequenceConfig config) => _config = ValidateConfig(config);

    private SequenceResult TrySequence(KeyTerminalEvent key)
    {
        if (!_config.DetectDoubleEscape ||
            key.Gesture.Key != TerminalKey.Escape ||
            key.Gesture.Modifiers != TerminalModifiers.None)
        {
            return SequenceResult.NoMatch;
        }

        if (_buffer.Count == 1 &&
            _buffer[0].Gesture.Key == TerminalKey.Escape &&
            _buffer[0].Gesture.Modifiers == TerminalModifiers.None)
        {
            return SequenceResult.Complete;
        }

        return _buffer.Count == 0 ? SequenceResult.Continue : SequenceResult.NoMatch;
    }

    private static KeySequenceConfig ValidateConfig(KeySequenceConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        KeySequenceConfig.ValidateTimeout(config.SequenceTimeout);
        return config;
    }

    private static TimeSpan ElapsedSince(DateTimeOffset start, DateTimeOffset now) =>
        now <= start ? TimeSpan.Zero : now - start;

    private enum SequenceResult
    {
        Complete,
        Continue,
        NoMatch,
    }
}
