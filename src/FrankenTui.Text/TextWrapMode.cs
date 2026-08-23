// SPDX-License-Identifier: Apache-2.0
// Port of WrapMode in .external/frankentui/crates/ftui-text/src/wrap.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

namespace FrankenTui.Text;

public enum TextWrapMode
{
    None = 0,
    Word = 1,

    /// <summary>Wrap at Unicode grapheme-cluster boundaries.</summary>
    Char = 2,

    /// <summary>
    /// Compatibility name retained for the original managed API.
    /// </summary>
    Character = Char,

    // DIVERGENCE: Optimal retains its pre-existing managed numeric value.
    // Rust enum discriminants are not part of the serialized API.
    Optimal = 3,

    /// <summary>Word wrap with grapheme fallback for overlong words.</summary>
    WordChar = 4,
}
