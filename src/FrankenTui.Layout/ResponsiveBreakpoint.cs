// Compatibility type predating Breakpoint and Breakpoints in ftui-layout/src/lib.rs.
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: Preserved for the legacy ResponsiveLayout.Select projection.

namespace FrankenTui.Layout;

public readonly record struct ResponsiveBreakpoint(string Name, ushort MinimumWidth);
