// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-a11y/src/lib.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: The Rust Accessible trait is named IAccessible under the
// repository's C# interface-naming convention.

using FrankenTui.Core;

namespace FrankenTui.A11y;

/// <summary>Implemented by widgets that provide accessibility metadata.</summary>
/// <remarks>
/// Implementations return at least one node. The first node is the widget's
/// primary node; additional nodes describe internal structure. Node IDs must
/// be unique within a render pass and stable across frames.
/// </remarks>
public interface IAccessible
{
    /// <summary>Returns the accessibility nodes for this widget at <paramref name="area"/>.</summary>
    List<A11yNodeInfo> AccessibilityNodes(Rect area);
}
