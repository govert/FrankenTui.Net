// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/input_macro.rs
// Input macro recording and playback. DIVERGENCE: replay methods stubbed (depend on ProgramSimulator).

using FrankenTui.Core;

namespace FrankenTui.Runtime;

public sealed record TimedEvent(TerminalEvent Event,TimeSpan Delay)
{
    public static TimedEvent Immediate(TerminalEvent e)=>new(e,TimeSpan.Zero);
}

public sealed record MacroMetadata(string Name,(ushort,ushort) TerminalSize,TimeSpan TotalDuration);

public sealed class InputMacro
{
    readonly List<TimedEvent> _events; readonly MacroMetadata _meta;
    public InputMacro(List<TimedEvent> events,MacroMetadata meta){_events=events;_meta=meta;}
    public static InputMacro FromEvents(string name,List<TerminalEvent> events)
    {
        var timed=events.Select(TimedEvent.Immediate).ToList();
        return new InputMacro(timed,new MacroMetadata(name,(80,24),TimeSpan.Zero));
    }
    public IReadOnlyList<TimedEvent> Events=>_events; public MacroMetadata Metadata=>_meta;
    public int Count=>_events.Count; public bool IsEmpty=>_events.Count==0;
    public TimeSpan TotalDuration=>_meta.TotalDuration;
    public List<TerminalEvent> BareEvents=>_events.Select(e=>e.Event).ToList();
    // DIVERGENCE: replay_with_timing / replay_with_sleeper depend on ProgramSimulator (not ported)
}

public sealed class MacroRecorder
{
    readonly string _name;(ushort,ushort) _size=(80,24);readonly List<TimedEvent> _events=new();long _lastTicks;TimeSpan _total;
    public MacroRecorder(string name){_name=name;_lastTicks=System.Diagnostics.Stopwatch.GetTimestamp();}
    public MacroRecorder WithTerminalSize(ushort w,ushort h){_size=(w,h);return this;}
    public void RecordEvent(TerminalEvent e)
    {
        long now=System.Diagnostics.Stopwatch.GetTimestamp();
        var delay=TimeSpan.FromSeconds((now-_lastTicks)/(double)System.Diagnostics.Stopwatch.Frequency);
        _events.Add(new TimedEvent(e,delay));_total+=delay;_lastTicks=now;
    }
    public void RecordEventWithDelay(TerminalEvent e,TimeSpan delay){_events.Add(new TimedEvent(e,delay));_total+=delay;_lastTicks=System.Diagnostics.Stopwatch.GetTimestamp();}
    public int EventCount=>_events.Count;
    public InputMacro Finish()=>new(_events,new MacroMetadata(_name,_size,_total));
}

public sealed class MacroPlayer
{
    readonly InputMacro _m;int _pos;TimeSpan _elapsed;
    public MacroPlayer(InputMacro m){_m=m;}
    public int Position=>_pos; public TimeSpan Elapsed=>_elapsed; public bool IsDone=>_pos>=_m.Count; public int Remaining=>_m.Count-_pos;
    public TimedEvent? Next(){if(_pos>=_m.Count)return null;var e=_m.Events[_pos];_pos++;_elapsed+=e.Delay;return e;}
    // DIVERGENCE: step / replay_with_timing / replay_with_sleeper depend on ProgramSimulator
}
