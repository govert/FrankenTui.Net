// SPDX-License-Identifier: Apache-2.0
// Former stub placeholder for the modal/ subsystem.
//
// The canonical port of stack.rs, dialog.rs, animation.rs, and focus_integration.rs
// is now spread across:
//
//   src/FrankenTui.Widgets/modal/ModalStack.cs    — ModalId, ModalResult, IStackModal, ModalStack
//   src/FrankenTui.Widgets/modal/ModalDialog.cs   — ModalDialogButton, ModalDialogKind, ModalDialog
//   src/FrankenTui.Widgets/modal/ModalAnimation.cs — ModalAnimationConfig and related types
//
// This file retains ONLY the types from container.rs (BackdropConfig, ModalSizeConstraints,
// ModalPosition, ModalAction) and stub dialog helpers (DialogButton, DialogKind,
// DialogResult, DialogState) that are required by callers outside the modal/ directory
// until the container.rs port lands as a dedicated file.
//
// DIVERGENCE: The old stub IStackModal interface (with Id, Title, Animation members) has
// been removed to avoid conflict with the canonical IStackModal in modal/ModalStack.cs.

using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Widgets.Modal;

// ── BackdropConfig ─────────────────────────────────────────────────────────
// Port of container.rs BackdropConfig.

/// <summary>
/// Configuration for the modal backdrop overlay.
/// Rust: <c>pub struct BackdropConfig</c> in container.rs.
/// </summary>
public sealed class BackdropConfig
{
    /// <summary>Backdrop tint color.</summary>
    public PackedRgba Color { get; set; }
    /// <summary>Backdrop opacity (0.0–1.0).</summary>
    public float Opacity { get; set; }

    /// <summary>Create a backdrop config. Rust: <c>BackdropConfig { color, opacity }</c>.</summary>
    public BackdropConfig(PackedRgba color, float opacity) => (Color, Opacity) = (color, opacity);

    /// <summary>Default backdrop: semi-transparent black at 50% opacity.</summary>
    public static BackdropConfig Default => new(PackedRgba.Rgb(0, 0, 0), 0.5f);

    /// <summary>Return a copy of this config with the opacity multiplied by <paramref name="factor"/>.</summary>
    public BackdropConfig WithOpacity(float factor) => new(Color, Opacity * factor);
}

// ── ModalSizeConstraints ───────────────────────────────────────────────────
// Port of container.rs ModalSizeConstraints.

/// <summary>
/// Size constraints for a modal dialog.
/// Rust: <c>pub struct ModalSizeConstraints</c> in container.rs.
/// </summary>
public sealed class ModalSizeConstraints
{
    /// <summary>Minimum width in columns.</summary>
    public ushort MinWidth { get; set; }
    /// <summary>Minimum height in rows.</summary>
    public ushort MinHeight { get; set; } = 3;
    /// <summary>Maximum width (null = unlimited).</summary>
    public ushort? MaxWidth { get; set; }
    /// <summary>Maximum height (null = unlimited).</summary>
    public ushort? MaxHeight { get; set; }

    /// <summary>Default size constraints (min width 20).</summary>
    public static ModalSizeConstraints Default => new() { MinWidth = 20 };
}

// ── ModalPosition / ModalAction ────────────────────────────────────────────

/// <summary>Modal position anchor. Rust: <c>enum ModalPosition</c> in container.rs.</summary>
public enum ModalPosition { Center, Top, Bottom, Left, Right, TopLeft, TopRight, BottomLeft, BottomRight }

/// <summary>Modal lifecycle action. Rust: <c>enum ModalAction</c> in container.rs.</summary>
public enum ModalAction { Show, Dismiss, Resize, Move }

// ── Stub dialog helpers (pre-existing API; superseded by ModalDialog.cs) ──

/// <summary>
/// Simple dialog button. Rust equivalents live in dialog.rs.
/// DIVERGENCE: This stub predates the 1-to-1 dialog.rs port.
/// For new code use <see cref="ModalDialogButton"/> in ModalDialog.cs.
/// </summary>
public sealed class DialogButton
{
    public string Label { get; set; } = "";
    public string Id { get; set; } = "";
    public bool IsPrimary { get; set; }
    public bool IsDanger { get; set; }

    public static DialogButton New(string label, string id) => new() { Label = label, Id = id };
    public DialogButton Primary() { IsPrimary = true; return this; }
    public DialogButton Danger() { IsDanger = true; return this; }
}

/// <summary>Dialog kind. DIVERGENCE: stub; see <see cref="ModalDialogKind"/>.</summary>
public enum DialogKind { Info, Warning, Error, Confirm, Input }

/// <summary>Dialog result. DIVERGENCE: stub; see <see cref="DialogOutcome"/>.</summary>
public enum DialogResult { None, Ok, Cancel, Yes, No, Custom }

/// <summary>Dialog state. DIVERGENCE: stub; see <see cref="ModalDialogState"/>.</summary>
public sealed class DialogState
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public DialogKind Kind { get; set; }
    public List<DialogButton> Buttons { get; set; } = new();
    public int FocusedButton { get; set; }
}
