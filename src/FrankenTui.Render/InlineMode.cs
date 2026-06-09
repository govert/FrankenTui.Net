// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-core/src/inline_mode.rs
// Inline mode strategies for preserving terminal scrollback.

using FrankenTui.Core;

namespace FrankenTui.Render;

public enum InlineStrategy{ScrollRegion,OverlayRedraw,Hybrid}

public sealed class InlineMode
{
    public const string CursorSave="\x1b7",CursorRestore="\x1b8",ResetScrollRegion="\x1b[r",EraseLine="\x1b[2K",SyncBegin="\x1b[?2026h",SyncEnd="\x1b[?2026l";

    public static string CursorPosition(int row,int col)=>$"\x1b[{row};{col}H";
    public static string SetScrollRegion(int top,int bottom)=>$"\x1b[{top};{bottom}r";
    public static string MoveUp(int n)=>$"\x1b[{n}A";
    public static string EraseDisplay(int mode)=>$"\x1b[{mode}J";

    public static InlineStrategy Select(TerminalCapabilities caps)
    {
        if(caps.ScrollRegion&&!caps.InTmux)return InlineStrategy.ScrollRegion;
        return InlineStrategy.OverlayRedraw;
    }

    public static bool SupportsScrollRegion(TerminalCapabilities caps)=>caps.ScrollRegion&&!caps.InTmux;
    public static bool SupportsSyncOutput(TerminalCapabilities caps)=>caps.SyncOutput&&!caps.InTmux;
}
