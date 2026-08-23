// SPDX-License-Identifier: Apache-2.0
// Managed persistence extension for the input-macro port at upstream basis
// 15cc6543f76b814394c590f9e7719dedd6684e4c. The Rust source has no wire format.

using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using FrankenTui.Core;

namespace FrankenTui.Runtime;

/// <summary>Stable JSON persistence for <see cref="InputMacro"/>.</summary>
public static class InputMacroJson
{
    public const string SchemaName = "frankentui.input-macro";
    public const int SchemaVersion = 1;

    public static string Serialize(InputMacro macro, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(macro);
        var output = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = indented }))
        {
            writer.WriteStartObject();
            writer.WriteString("schema", SchemaName);
            writer.WriteNumber("version", SchemaVersion);
            writer.WritePropertyName("metadata");
            writer.WriteStartObject();
            writer.WriteString("name", macro.Metadata.Name);
            writer.WritePropertyName("terminalSize");
            writer.WriteStartObject();
            writer.WriteNumber("width", macro.Metadata.TerminalSize.Width);
            writer.WriteNumber("height", macro.Metadata.TerminalSize.Height);
            writer.WriteEndObject();
            writer.WriteNumber("totalDurationTicks", macro.TotalDuration.Ticks);
            writer.WriteEndObject();
            writer.WritePropertyName("events");
            writer.WriteStartArray();

            foreach (var timed in macro.Events)
            {
                writer.WriteStartObject();
                writer.WriteNumber("delayTicks", timed.Delay.Ticks);
                writer.WritePropertyName("event");
                WriteEvent(writer, timed.Event);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(output.WrittenSpan);
    }

    public static InputMacro Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (ReadString(root, "schema") != SchemaName)
            {
                throw new JsonException("Unsupported input-macro schema.");
            }

            var version = ReadInt32(root, "version");
            if (version != SchemaVersion)
            {
                throw new JsonException($"Unsupported input-macro schema version {version}.");
            }

            var metadataElement = ReadObject(root, "metadata");
            var sizeElement = ReadObject(metadataElement, "terminalSize");
            var durationTicks = ReadNonNegativeInt64(metadataElement, "totalDurationTicks");
            var metadata = new MacroMetadata(
                ReadString(metadataElement, "name"),
                (ReadUInt16(sizeElement, "width"), ReadUInt16(sizeElement, "height")),
                TimeSpan.FromTicks(durationTicks));

            var eventElements = root.GetProperty("events");
            if (eventElements.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException("Property 'events' must be an array.");
            }

            var events = new List<TimedEvent>(eventElements.GetArrayLength());
            foreach (var element in eventElements.EnumerateArray())
            {
                var delay = TimeSpan.FromTicks(ReadNonNegativeInt64(element, "delayTicks"));
                events.Add(new TimedEvent(ReadEvent(ReadObject(element, "event")), delay));
            }

            return new InputMacro(events, metadata);
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is KeyNotFoundException
                                          or InvalidOperationException
                                          or FormatException
                                          or OverflowException
                                          or ArgumentException)
        {
            throw new JsonException("Invalid input-macro document.", exception);
        }
    }

    private static void WriteEvent(Utf8JsonWriter writer, TerminalEvent @event)
    {
        writer.WriteStartObject();
        switch (@event)
        {
            case KeyTerminalEvent key:
                WriteCommon(writer, "key", key.Timestamp);
                writer.WriteString("key", key.Gesture.Key.ToString());
                writer.WriteNumber("modifiers", (byte)key.Gesture.Modifiers);
                if (key.Gesture.Character is { } character)
                {
                    writer.WriteNumber("character", character.Value);
                }
                else
                {
                    writer.WriteNull("character");
                }

                writer.WriteString("kind", key.Kind.ToString());
                break;

            case MouseTerminalEvent mouse:
                WriteCommon(writer, "mouse", mouse.Timestamp);
                writer.WriteNumber("column", mouse.Gesture.Column);
                writer.WriteNumber("row", mouse.Gesture.Row);
                writer.WriteString("button", mouse.Gesture.Button.ToString());
                writer.WriteString("kind", mouse.Gesture.Kind.ToString());
                writer.WriteNumber("modifiers", (byte)mouse.Gesture.Modifiers);
                break;

            case ResizeTerminalEvent resize:
                WriteCommon(writer, "resize", resize.Timestamp);
                writer.WriteNumber("width", resize.Size.Width);
                writer.WriteNumber("height", resize.Size.Height);
                break;

            case FocusTerminalEvent focus:
                WriteCommon(writer, "focus", focus.Timestamp);
                writer.WriteBoolean("focused", focus.Focused);
                break;

            case PasteTerminalEvent paste:
                WriteCommon(writer, "paste", paste.Timestamp);
                writer.WriteString("text", paste.Text);
                break;

            case HoverTerminalEvent hover:
                WriteCommon(writer, "hover", hover.Timestamp);
                writer.WriteNumber("column", hover.Column);
                writer.WriteNumber("row", hover.Row);
                writer.WriteBoolean("stable", hover.Stable);
                break;

            default:
                throw new NotSupportedException(
                    $"Terminal event type '{@event.GetType().FullName}' is not supported by schema v{SchemaVersion}.");
        }

        writer.WriteEndObject();
    }

    private static TerminalEvent ReadEvent(JsonElement element)
    {
        var type = ReadString(element, "type");
        var timestamp = ReadTimestamp(element);
        return type switch
        {
            "key" => ReadKey(element, timestamp),
            "mouse" => ReadMouse(element, timestamp),
            "resize" => TerminalEvent.Resize(
                new Size(ReadUInt16(element, "width"), ReadUInt16(element, "height")),
                timestamp),
            "focus" => TerminalEvent.Focus(ReadBoolean(element, "focused"), timestamp),
            "paste" => TerminalEvent.Paste(ReadString(element, "text"), timestamp),
            "hover" => TerminalEvent.Hover(
                ReadUInt16(element, "column"),
                ReadUInt16(element, "row"),
                ReadBoolean(element, "stable"),
                timestamp),
            _ => throw new JsonException($"Unknown terminal-event type '{type}'."),
        };
    }

    private static KeyTerminalEvent ReadKey(JsonElement element, DateTimeOffset timestamp)
    {
        var key = ReadEnum<TerminalKey>(element, "key");
        var kind = ReadEnum<TerminalKeyEventKind>(element, "kind");
        var modifiers = (TerminalModifiers)ReadByte(element, "modifiers");
        Rune? character = null;
        var characterElement = element.GetProperty("character");
        if (characterElement.ValueKind != JsonValueKind.Null)
        {
            var scalar = characterElement.GetInt32();
            if (!Rune.IsValid(scalar))
            {
                throw new JsonException($"Invalid Unicode scalar value {scalar}.");
            }

            character = new Rune(scalar);
        }

        return TerminalEvent.Key(new KeyGesture(key, modifiers, character), timestamp, kind);
    }

    private static MouseTerminalEvent ReadMouse(JsonElement element, DateTimeOffset timestamp) =>
        TerminalEvent.Mouse(
            new MouseGesture(
                ReadUInt16(element, "column"),
                ReadUInt16(element, "row"),
                ReadEnum<TerminalMouseButton>(element, "button"),
                ReadEnum<TerminalMouseKind>(element, "kind"),
                (TerminalModifiers)ReadByte(element, "modifiers")),
            timestamp);

    private static void WriteCommon(Utf8JsonWriter writer, string type, DateTimeOffset timestamp)
    {
        writer.WriteString("type", type);
        writer.WriteString("timestamp", timestamp.ToString("O", CultureInfo.InvariantCulture));
    }

    private static DateTimeOffset ReadTimestamp(JsonElement element)
    {
        var text = ReadString(element, "timestamp");
        if (!DateTimeOffset.TryParseExact(
                text,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var timestamp))
        {
            throw new JsonException($"Invalid round-trip timestamp '{text}'.");
        }

        return timestamp;
    }

    private static JsonElement ReadObject(JsonElement parent, string propertyName)
    {
        var value = parent.GetProperty(propertyName);
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"Property '{propertyName}' must be an object.");
        }

        return value;
    }

    private static string ReadString(JsonElement parent, string propertyName)
    {
        var value = parent.GetProperty(propertyName);
        return value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new JsonException($"Property '{propertyName}' must be a string.");
    }

    private static bool ReadBoolean(JsonElement parent, string propertyName)
    {
        var value = parent.GetProperty(propertyName);
        return value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : throw new JsonException($"Property '{propertyName}' must be a boolean.");
    }

    private static int ReadInt32(JsonElement parent, string propertyName) =>
        parent.GetProperty(propertyName).GetInt32();

    private static long ReadNonNegativeInt64(JsonElement parent, string propertyName)
    {
        var value = parent.GetProperty(propertyName).GetInt64();
        return value >= 0
            ? value
            : throw new JsonException($"Property '{propertyName}' cannot be negative.");
    }

    private static ushort ReadUInt16(JsonElement parent, string propertyName)
    {
        var value = parent.GetProperty(propertyName).GetInt32();
        return value is >= ushort.MinValue and <= ushort.MaxValue
            ? (ushort)value
            : throw new JsonException($"Property '{propertyName}' is outside UInt16 range.");
    }

    private static byte ReadByte(JsonElement parent, string propertyName)
    {
        var value = parent.GetProperty(propertyName).GetInt32();
        return value is >= byte.MinValue and <= byte.MaxValue
            ? (byte)value
            : throw new JsonException($"Property '{propertyName}' is outside Byte range.");
    }

    private static TEnum ReadEnum<TEnum>(JsonElement parent, string propertyName)
        where TEnum : struct, Enum
    {
        var text = ReadString(parent, propertyName);
        return Enum.TryParse<TEnum>(text, ignoreCase: false, out var value)
               && Enum.IsDefined(value)
            ? value
            : throw new JsonException($"Unknown {typeof(TEnum).Name} value '{text}'.");
    }
}
