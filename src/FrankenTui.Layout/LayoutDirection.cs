// Compatibility type predating the canonical Direction in ftui-layout/src/lib.rs.
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: Preserved to avoid breaking existing LayoutPlan/LayoutSolver callers.

namespace FrankenTui.Layout;

public enum LayoutDirection
{
    Horizontal,
    Vertical
}
