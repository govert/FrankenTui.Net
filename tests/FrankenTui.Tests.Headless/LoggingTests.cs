using FrankenTui.Extras;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class LoggingTests
{
    [Fact] public void DefaultConfig()
    {
        var c = new LogConfig();
        Assert.True(c.ShowTime);
        Assert.True(c.ShowLevel);
        Assert.True(c.ShowTarget);
        Assert.True(c.ShowFields);
        Assert.False(c.ShowSource);
    }

    [Fact] public void LogLevelStrings()
    {
        Assert.Equal("ERROR", FxLog.LevelString(LogLevel.Error));
        Assert.Equal("WARN ", FxLog.LevelString(LogLevel.Warn));
        Assert.Equal("INFO ", FxLog.LevelString(LogLevel.Info));
        Assert.Equal("DEBUG", FxLog.LevelString(LogLevel.Debug));
        Assert.Equal("TRACE", FxLog.LevelString(LogLevel.Trace));
    }

    [Fact] public void LevelColorForAllLevels()
    {
        foreach (var level in new[] { LogLevel.Trace, LogLevel.Debug, LogLevel.Info, LogLevel.Warn, LogLevel.Error })
            Assert.NotNull(FxLog.LevelColor(level));
    }

    [Fact] public void OnLogFiresForAllLevels()
    {
        int count = 0;
        Action<LogEntry> handler = _ => count++;
        FxLog.OnLog += handler;
        FxLog.Trace("t"); FxLog.Debug("d"); FxLog.Info("i"); FxLog.Warn("w"); FxLog.Error("e");
        Assert.Equal(5, count);
        FxLog.OnLog -= handler;
    }

    [Fact] public void LogEntryCarriesLevel()
    {
        LogEntry? captured = null;
        Action<LogEntry> handler = e => captured = e;
        FxLog.OnLog += handler;
        FxLog.Warn("warning");
        Assert.NotNull(captured);
        Assert.Equal(LogLevel.Warn, captured!.Level);
        Assert.Equal("warning", captured.Message);
        FxLog.OnLog -= handler;
    }

    [Fact] public void LogEntryCarriesTarget()
    {
        LogEntry? captured = null;
        Action<LogEntry> handler = e => captured = e;
        FxLog.OnLog += handler;
        FxLog.Info("msg", "test-target");
        Assert.NotNull(captured);
        Assert.Equal("test-target", captured!.Target);
        FxLog.OnLog -= handler;
    }

    [Fact] public void LogEntryCarriesFields()
    {
        LogEntry? captured = null;
        Action<LogEntry> handler = e => captured = e;
        FxLog.OnLog += handler;
        var fields = new Dictionary<string, string> { ["key"] = "val" };
        FxLog.Log(LogLevel.Debug, "with fields", fields: fields);
        Assert.NotNull(captured);
        Assert.Equal("val", captured!.Fields?["key"]);
        FxLog.OnLog -= handler;
    }

    [Fact] public void LogWithoutHandlerDoesNotThrow()
    {
        FxLog.Info("no handler — should not throw");
    }

    [Fact] public void LogEntryHasTimestamp()
    {
        LogEntry? captured = null;
        Action<LogEntry> handler = e => captured = e;
        FxLog.OnLog += handler;
        FxLog.Info("timed");
        Assert.NotNull(captured);
        Assert.True(captured!.Timestamp > DateTime.MinValue);
        FxLog.OnLog -= handler;
    }

    [Fact] public void ConfigCanToggleTime()
    {
        FxLog.Config.ShowTime = false;
        Assert.False(FxLog.Config.ShowTime);
        FxLog.Config.ShowTime = true;
    }

    [Fact] public void LogWithSource()
    {
        LogEntry? captured = null;
        Action<LogEntry> handler = e => captured = e;
        FxLog.OnLog += handler;
        var fields = new Dictionary<string, string> { ["source"] = "MyClass.cs:42" };
        FxLog.Log(LogLevel.Error, "error", fields: fields);
        Assert.NotNull(captured);
        Assert.Equal("error", captured!.Message);
        FxLog.OnLog -= handler;
    }

    [Fact] public void MultipleLogEntriesInOrder()
    {
        var entries = new List<LogEntry>();
        Action<LogEntry> handler = e => entries.Add(e);
        FxLog.OnLog += handler;
        FxLog.Info("first");
        FxLog.Warn("second");
        FxLog.Error("third");
        Assert.Equal(3, entries.Count);
        Assert.Equal("first", entries[0].Message);
        Assert.Equal("third", entries[2].Message);
        FxLog.OnLog -= handler;
    }
}
