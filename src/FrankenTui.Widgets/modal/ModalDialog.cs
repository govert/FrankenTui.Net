// Port of .external/frankentui/crates/ftui-widgets/src/modal/dialog.rs
// Dialog presets built on the Modal container: Alert, Confirm, Prompt, and Custom builder.

// DIVERGENCE: Rust HitRegion::Button maps to HitRegionKind.Button; HitRegion::Custom(1)
//   for DIALOG_HIT_INPUT maps to the new HitRegionKind.DialogInput enum value.
//   MODAL_HIT_CONTENT (HitRegion::Custom(2) from container.rs) maps to HitRegionKind.ModalContent.
//   See HitRegion.cs for the full Custom(n) expansion table.
// DIVERGENCE: Rust `Event` type maps to `InputEvent` (FrankenTui.Widgets.Input.cs).
// DIVERGENCE: Rust `display_width` maps to `TerminalTextWidth.DisplayWidth` (FrankenTui.Core).
// DIVERGENCE: Rust `grapheme_indices(true).next_back()` for last grapheme removal is
//   implemented via StringInfo.GetTextElementEnumerator enumeration — semantically identical.
// DIVERGENCE: Rust frame.cursor_position / frame.cursor_visible are set via
//   Frame.SetCursor / Frame.SetCursorVisible in C#.
// DIVERGENCE: Modal<C> from container.rs is not yet ported to a standalone C# file;
//   its render logic is inlined as ModalContainerHelper in this file so dialog.rs can
//   be ported faithfully. When container.rs is ported this helper must be removed.
// DIVERGENCE: Existing Modal.cs defines a stub Dialog/DialogState/DialogKind/DialogResult in
//   namespace FrankenTui.Widgets with different field shapes; this file uses namespace
//   FrankenTui.Widgets.Modal to avoid collision.

using System.Globalization;
using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Widgets.Modal;

// ── Hit region constants ───────────────────────────────────────────────────

/// <summary>Hit region for dialog buttons.</summary>
/// <remarks>Rust: <c>pub const DIALOG_HIT_BUTTON: HitRegion = HitRegion::Button;</c></remarks>
public static class DialogHitRegions
{
    /// <summary>Hit region for dialog buttons. Rust: <c>HitRegion::Button</c>.</summary>
    public const HitRegionKind DialogHitButton = HitRegionKind.Button;

    /// <summary>Hit region for prompt input. Rust: <c>HitRegion::Custom(1)</c>.</summary>
    public const HitRegionKind DialogHitInput = HitRegionKind.DialogInput;

    // DIVERGENCE: The upstream modal/mod.rs re-exports MODAL_HIT_BACKDROP and
    // MODAL_HIT_CONTENT from container.rs. They are mirrored here until container.rs
    // is ported. The canonical home is FrankenTui.Widgets.Modal.ModalContainer.
    /// <summary>Hit region for modal backdrop. Rust: <c>MODAL_HIT_BACKDROP = HitRegion::Custom(1)</c>.</summary>
    public const HitRegionKind ModalHitBackdrop = HitRegionKind.ModalBackdrop;

    /// <summary>Hit region for modal content area. Rust: <c>MODAL_HIT_CONTENT = HitRegion::Custom(2)</c>.</summary>
    public const HitRegionKind ModalHitContent = HitRegionKind.ModalContent;
}

// ── DialogResult ───────────────────────────────────────────────────────────

/// <summary>Result from a dialog interaction.</summary>
/// <remarks>
/// Rust: <c>pub enum DialogResult { Dismissed, Ok, Cancel, Custom(String), Input(String) }</c>
/// DIVERGENCE: Rust enum with associated data → closed abstract record hierarchy per AGENTS.md.
/// </remarks>
public abstract record DialogOutcome
{
    private DialogOutcome() { }

    /// <summary>Dialog was dismissed without action.</summary>
    public sealed record Dismissed : DialogOutcome;

    /// <summary>OK / primary button pressed.</summary>
    public sealed record Ok : DialogOutcome;

    /// <summary>Cancel / secondary button pressed.</summary>
    public sealed record Cancel : DialogOutcome;

    /// <summary>Custom button pressed with its ID.</summary>
    public sealed record Custom(string Id) : DialogOutcome;

    /// <summary>Prompt dialog submitted with input value.</summary>
    public sealed record Input(string Value) : DialogOutcome;

    // ── Static factory singletons / helpers ────────────────────────────────
    public static readonly DialogOutcome DismissedInstance = new Dismissed();
    public static readonly DialogOutcome OkInstance = new Ok();
    public static readonly DialogOutcome CancelInstance = new Cancel();
    public static DialogOutcome CustomResult(string id) => new Custom(id);
    public static DialogOutcome InputResult(string value) => new Input(value);
}

// ── DialogButton ───────────────────────────────────────────────────────────

/// <summary>A button in a dialog.</summary>
public sealed class ModalDialogButton
{
    /// <summary>Display label.</summary>
    public string Label { get; set; }

    /// <summary>Unique identifier.</summary>
    public string ButtonId { get; set; }

    /// <summary>Whether this is the primary/default button.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Create a new dialog button.</summary>
    public ModalDialogButton(string label, string id)
    {
        Label = label;
        ButtonId = id;
        IsPrimary = false;
    }

    /// <summary>Mark as primary button.</summary>
    public ModalDialogButton Primary()
    {
        IsPrimary = true;
        return this;
    }

    /// <summary>Display width including brackets: <c>[ label ]</c> = display_width(label) + 4.</summary>
    public int DisplayWidth()
    {
        return TerminalTextWidth.DisplayWidth(Label) + 4;
    }
}

// ── DialogKind ─────────────────────────────────────────────────────────────

/// <summary>Dialog type variants.</summary>
public enum ModalDialogKind
{
    /// <summary>Alert: single OK button.</summary>
    Alert,
    /// <summary>Confirm: OK + Cancel buttons.</summary>
    Confirm,
    /// <summary>Prompt: input field + OK + Cancel.</summary>
    Prompt,
    /// <summary>Custom dialog.</summary>
    Custom,
}

// ── DialogState ────────────────────────────────────────────────────────────

/// <summary>Dialog state for handling input and button focus.</summary>
public sealed class ModalDialogState
{
    /// <summary>Currently focused button index.</summary>
    public int? FocusedButton { get; set; }

    /// <summary>Button index currently pressed by mouse (Down without matching Up yet).</summary>
    public int? PressedButton { get; set; }

    /// <summary>Input field value (for Prompt dialogs).</summary>
    public string InputValue { get; set; } = string.Empty;

    /// <summary>Whether the input field is focused.</summary>
    public bool InputFocused { get; set; }

    /// <summary>Whether the dialog is open.</summary>
    public bool Open { get; set; }

    /// <summary>Result after interaction.</summary>
    public DialogOutcome? Result { get; set; }

    /// <summary>Create a new open dialog state (default: closed, no input focus).</summary>
    public ModalDialogState() { }

    /// <summary>Create a new open dialog state. Rust: <c>DialogState::new()</c>.</summary>
    public static ModalDialogState New()
    {
        return new ModalDialogState
        {
            Open = true,
            InputFocused = true, // Start with input focused for prompts
        };
    }

    /// <summary>Check if dialog is open.</summary>
    public bool IsOpen() => Open;

    /// <summary>Close the dialog with a result.</summary>
    public void Close(DialogOutcome result)
    {
        Open = false;
        PressedButton = null;
        Result = result;
    }

    /// <summary>Reset the dialog state to open.</summary>
    public void Reset()
    {
        Open = true;
        Result = null;
        InputValue = string.Empty;
        FocusedButton = null;
        PressedButton = null;
        InputFocused = true;
    }

    /// <summary>Get the result if closed (consumes it).</summary>
    public DialogOutcome? TakeResult()
    {
        var r = Result;
        Result = null;
        return r;
    }
}

// ── DialogConfig ───────────────────────────────────────────────────────────

/// <summary>Dialog configuration.</summary>
public sealed class ModalDialogConfig
{
    /// <summary>Modal configuration.</summary>
    public ModalContainerConfig ModalConfig { get; set; } = ModalContainerConfig.Default();

    /// <summary>Dialog kind.</summary>
    public ModalDialogKind Kind { get; set; } = ModalDialogKind.Alert;

    /// <summary>Button style.</summary>
    public WidgetStyle ButtonStyle { get; set; } = WidgetStyle.Default;

    /// <summary>Primary button style.</summary>
    public WidgetStyle PrimaryButtonStyle { get; set; } = new WidgetStyle(null, null, CellStyleFlags.Bold);

    /// <summary>Focused button style.</summary>
    public WidgetStyle FocusedButtonStyle { get; set; } = new WidgetStyle(null, null, CellStyleFlags.Reverse);

    /// <summary>Title style.</summary>
    public WidgetStyle TitleStyle { get; set; } = new WidgetStyle(null, null, CellStyleFlags.Bold);

    /// <summary>Message style.</summary>
    public WidgetStyle MessageStyle { get; set; } = WidgetStyle.Default;

    /// <summary>Input style (for Prompt).</summary>
    public WidgetStyle InputStyle { get; set; } = WidgetStyle.Default;

    public static ModalDialogConfig Default() => new();
}

// ── ModalContainerConfig (subset of container.rs ModalConfig) ──────────────

/// <summary>
/// Modal configuration: position, backdrop, size, escape/backdrop close, hit id.
/// </summary>
/// <remarks>
/// DIVERGENCE: Ported inline from <c>modal/container.rs ModalConfig</c> until
/// container.rs is fully ported. When ModalContainer.cs is added this type
/// must be replaced by the canonical one.
/// </remarks>
public sealed class ModalContainerConfig
{
    /// <summary>Modal position.</summary>
    public ModalContainerPosition Position { get; set; } = ModalContainerPosition.Center.Instance;

    /// <summary>Modal size constraints.</summary>
    public ModalContainerSizeConstraints Size { get; set; } = new ModalContainerSizeConstraints();

    /// <summary>Whether to close when the backdrop is clicked.</summary>
    public bool CloseOnBackdrop { get; set; } = true;

    /// <summary>Whether to close when Escape is pressed.</summary>
    public bool CloseOnEscape { get; set; } = true;

    /// <summary>Hit ID for backdrop/content hit registration.</summary>
    public HitId? HitId { get; set; }

    /// <summary>Backdrop opacity (0.0–1.0).</summary>
    public float BackdropOpacity { get; set; } = 0.6f;

    public static ModalContainerConfig Default() => new();

    public ModalContainerConfig WithPosition(ModalContainerPosition p) { Position = p; return this; }
    public ModalContainerConfig WithSize(ModalContainerSizeConstraints s) { Size = s; return this; }
    public ModalContainerConfig WithHitId(HitId id) { HitId = id; return this; }
    public ModalContainerConfig WithCloseOnEscape(bool v) { CloseOnEscape = v; return this; }
    public ModalContainerConfig WithCloseOnBackdrop(bool v) { CloseOnBackdrop = v; return this; }
}

// ── ModalContainerPosition ─────────────────────────────────────────────────

/// <summary>
/// Modal positioning options.
/// </summary>
/// <remarks>
/// DIVERGENCE: Ported inline from <c>modal/container.rs ModalPosition</c>.
/// Rust enum with associated data → closed record hierarchy per AGENTS.md.
/// </remarks>
public abstract record ModalContainerPosition
{
    private ModalContainerPosition() { }

    /// <summary>Centered in the available area (default).</summary>
    public sealed record Center : ModalContainerPosition
    {
        public static readonly Center Instance = new();
    }

    /// <summary>Centered with an offset.</summary>
    public sealed record CenterOffset(short X, short Y) : ModalContainerPosition;

    /// <summary>Top-center with a margin from the top edge.</summary>
    public sealed record TopCenter(ushort Margin) : ModalContainerPosition;

    /// <summary>Absolute position.</summary>
    public sealed record CustomPosition(ushort X, ushort Y) : ModalContainerPosition;

    // Convenience static factory
    public static ModalContainerPosition CenteredOffset(short x, short y) => new CenterOffset(x, y);
    public static ModalContainerPosition TopCentered(ushort margin) => new TopCenter(margin);
    public static ModalContainerPosition At(ushort x, ushort y) => new CustomPosition(x, y);

    /// <summary>Resolve position to a Rect given available area and desired size.</summary>
    internal Rect Resolve(Rect area, ushort width, ushort height)
    {
        int baseX = area.X;
        int baseY = area.Y;
        int maxX = baseX + (area.Width - width);
        int maxY = baseY + (area.Height - height);

        int x, y;
        switch (this)
        {
            case Center:
                x = baseX + (area.Width - width) / 2;
                y = baseY + (area.Height - height) / 2;
                break;
            case CenterOffset co:
                x = baseX + (area.Width - width) / 2 + co.X;
                y = baseY + (area.Height - height) / 2 + co.Y;
                break;
            case TopCenter tc:
                x = baseX + (area.Width - width) / 2;
                y = baseY + tc.Margin;
                break;
            case CustomPosition cp:
                x = cp.X;
                y = cp.Y;
                break;
            default:
                x = baseX + (area.Width - width) / 2;
                y = baseY + (area.Height - height) / 2;
                break;
        }

        x = Math.Clamp(x, baseX, Math.Max(baseX, maxX));
        y = Math.Clamp(y, baseY, Math.Max(baseY, maxY));
        return new Rect((ushort)x, (ushort)y, width, height);
    }
}

// ── ModalContainerSizeConstraints ─────────────────────────────────────────

/// <summary>
/// Modal size constraints (min/max width/height).
/// </summary>
/// <remarks>
/// DIVERGENCE: Ported inline from <c>modal/container.rs ModalSizeConstraints</c>.
/// </remarks>
public sealed class ModalContainerSizeConstraints
{
    public ushort? MinWidth { get; set; }
    public ushort? MaxWidth { get; set; }
    public ushort? MinHeight { get; set; }
    public ushort? MaxHeight { get; set; }

    public static ModalContainerSizeConstraints New() => new();

    public ModalContainerSizeConstraints WithMinWidth(ushort v) { MinWidth = v; return this; }
    public ModalContainerSizeConstraints WithMaxWidth(ushort v) { MaxWidth = v; return this; }
    public ModalContainerSizeConstraints WithMinHeight(ushort v) { MinHeight = v; return this; }
    public ModalContainerSizeConstraints WithMaxHeight(ushort v) { MaxHeight = v; return this; }

    /// <summary>Clamp size to constraints, never exceeding available.</summary>
    public (ushort width, ushort height) Clamp(ushort availWidth, ushort availHeight)
    {
        ushort w = availWidth;
        ushort h = availHeight;
        if (MaxWidth.HasValue) w = Math.Min(w, MaxWidth.Value);
        if (MaxHeight.HasValue) h = Math.Min(h, MaxHeight.Value);
        // Never exceed available; when the minimum exceeds the available area
        // (tiny terminal), use the available area rather than throwing.
        if (MinWidth.HasValue) w = (ushort)Math.Min(Math.Max(w, MinWidth.Value), availWidth);
        if (MinHeight.HasValue) h = (ushort)Math.Min(Math.Max(h, MinHeight.Value), availHeight);
        return (w, h);
    }
}

// ── Internal ModalContainerHelper ──────────────────────────────────────────

/// <summary>
/// Inline port of <c>Modal&lt;C&gt;</c> from <c>modal/container.rs</c>, used by
/// <see cref="ModalDialog"/> until container.rs is ported to its own file.
/// </summary>
internal static class ModalContainerHelper
{
    /// <summary>
    /// Compute the content rectangle for the given area and configuration.
    /// Rust: <c>Modal::content_rect(&amp;self, area: Rect) -> Rect</c>
    /// </summary>
    internal static Rect ContentRect(ModalContainerConfig config, Rect area)
    {
        var (w, h) = config.Size.Clamp(area.Width, area.Height);
        if (w == 0 || h == 0) return new Rect(area.X, area.Y, 0, 0);
        return config.Position.Resolve(area, w, h);
    }

    /// <summary>
    /// Render the modal: apply backdrop tint, register hit regions, then call content render.
    /// Rust: <c>impl Widget for Modal&lt;C&gt; { fn render(&amp;self, ...) }</c>
    /// </summary>
    internal static void Render(ModalContainerConfig config, Rect area, Frame frame, Action<Rect, Frame> renderContent)
    {
        if (area.IsEmpty) return;

        // Backdrop tint — preserves existing glyphs, only tints background.
        float opacity = Math.Clamp(config.BackdropOpacity, 0.0f, 1.0f);
        if (opacity > 0.0f)
        {
            var bg = PackedRgba.Rgb(0, 0, 0).WithOpacity(opacity);
            var style = new WidgetStyle(null, bg, null);
            WidgetDrawing.SetStyleArea(frame.Buffer, area, style);
        }

        // Compute content rect.
        var contentArea = ContentRect(config, area);

        // Register hit regions BEFORE content renders so the dialog can overlay
        // more specific hits (buttons) on top.
        if (config.HitId is { } hitId)
        {
            frame.RegisterHit(area, hitId, HitRegionKind.ModalBackdrop, 0);
            if (!contentArea.IsEmpty)
                frame.RegisterHit(contentArea, hitId, HitRegionKind.ModalContent, 0);
        }

        if (!contentArea.IsEmpty)
            renderContent(contentArea, frame);
    }
}

// ── Dialog ─────────────────────────────────────────────────────────────────

/// <summary>
/// A dialog widget built on Modal.
/// </summary>
/// <remarks>
/// Invariants:
/// <list type="bullet">
///   <item>At least one button is always present.</item>
///   <item>Button focus wraps around (modular arithmetic).</item>
///   <item>For Prompt dialogs, Tab cycles: input → buttons → input.</item>
/// </list>
/// Failure modes:
/// <list type="bullet">
///   <item>If area is too small, content may be truncated but dialog never panics.</item>
///   <item>Empty title/message is allowed (renders nothing for that row).</item>
/// </list>
/// </remarks>
public sealed class ModalDialog
{
    private string _title;
    private string _message;
    private List<ModalDialogButton> _buttons;
    private ModalDialogConfig _config;
    private HitId? _hitId;

    internal ModalDialog(
        string title,
        string message,
        List<ModalDialogButton> buttons,
        ModalDialogConfig config,
        HitId? hitId)
    {
        _title = title;
        _message = message;
        _buttons = buttons;
        _config = config;
        _hitId = hitId;
    }

    // ── Constructors ───────────────────────────────────────────────────────

    /// <summary>Create an alert dialog (message + OK).</summary>
    public static ModalDialog Alert(string title, string message)
    {
        return new ModalDialog(
            title,
            message,
            new List<ModalDialogButton> { new ModalDialogButton("OK", "ok").Primary() },
            new ModalDialogConfig { Kind = ModalDialogKind.Alert },
            null);
    }

    /// <summary>Create a confirm dialog (message + OK/Cancel).</summary>
    public static ModalDialog Confirm(string title, string message)
    {
        return new ModalDialog(
            title,
            message,
            new List<ModalDialogButton>
            {
                new ModalDialogButton("OK", "ok").Primary(),
                new ModalDialogButton("Cancel", "cancel"),
            },
            new ModalDialogConfig { Kind = ModalDialogKind.Confirm },
            null);
    }

    /// <summary>Create a prompt dialog (message + input + OK/Cancel).</summary>
    public static ModalDialog Prompt(string title, string message)
    {
        return new ModalDialog(
            title,
            message,
            new List<ModalDialogButton>
            {
                new ModalDialogButton("OK", "ok").Primary(),
                new ModalDialogButton("Cancel", "cancel"),
            },
            new ModalDialogConfig { Kind = ModalDialogKind.Prompt },
            null);
    }

    /// <summary>Create a custom dialog with a builder.</summary>
    public static ModalDialogBuilder Custom(string title, string message)
    {
        return new ModalDialogBuilder(
            title,
            message,
            new List<ModalDialogButton>(),
            new ModalDialogConfig { Kind = ModalDialogKind.Custom },
            null);
    }

    // ── Builder methods ────────────────────────────────────────────────────

    /// <summary>Set the hit ID for mouse interaction.</summary>
    public ModalDialog WithHitId(HitId id)
    {
        _hitId = id;
        _config.ModalConfig.HitId = id;
        return this;
    }

    /// <summary>Set the modal configuration.</summary>
    public ModalDialog WithModalConfig(ModalContainerConfig config)
    {
        _hitId = config.HitId;
        _config.ModalConfig = config;
        return this;
    }

    /// <summary>Set button style.</summary>
    public ModalDialog WithButtonStyle(WidgetStyle style)
    {
        _config.ButtonStyle = style;
        return this;
    }

    /// <summary>Set primary button style.</summary>
    public ModalDialog WithPrimaryButtonStyle(WidgetStyle style)
    {
        _config.PrimaryButtonStyle = style;
        return this;
    }

    /// <summary>Set focused button style.</summary>
    public ModalDialog WithFocusedButtonStyle(WidgetStyle style)
    {
        _config.FocusedButtonStyle = style;
        return this;
    }

    // ── Internal accessors (test visibility) ──────────────────────────────

    internal string Title => _title;
    internal string Message => _message;
    internal IReadOnlyList<ModalDialogButton> Buttons => _buttons;
    internal ModalDialogConfig Config => _config;
    internal HitId? HitIdValue => _hitId;

    // ── StatefulWidget.Render ──────────────────────────────────────────────

    /// <summary>Render the dialog to the given frame area, driven by state.</summary>
    public void Render(Rect area, Frame frame, ModalDialogState state)
    {
        if (!state.Open || area.IsEmpty) return;

        var contentHeight = ContentHeight();
        var modalConfig = CloneModalConfigWithSize(contentHeight);

        ModalContainerHelper.Render(modalConfig, area, frame, (contentArea, f) =>
        {
            RenderContent(contentArea, f, state);
        });
    }

    // ── Event handling ─────────────────────────────────────────────────────

    /// <summary>
    /// Handle an event and potentially update state.
    /// Returns the dialog result if the dialog was closed, else null.
    /// </summary>
    public DialogOutcome? HandleEvent(
        InputEvent @event,
        ModalDialogState state,
        (HitId hitId, HitRegionKind region, ulong data)? hit)
    {
        if (!state.Open) return null;

        // Non-prompt dialogs cannot have input focus.
        if (_config.Kind != ModalDialogKind.Prompt && state.InputFocused)
            state.InputFocused = false;

        switch (@event)
        {
            // Escape closes with Dismissed
            case InputEvent.Key k
                when k.KeyEvent.Code is KeyCode.Escape
                  && k.KeyEvent.Kind == KeyEventKind.Press
                  && _config.ModalConfig.CloseOnEscape:
                state.Close(DialogOutcome.DismissedInstance);
                return DialogOutcome.DismissedInstance;

            // Tab cycles focus
            case InputEvent.Key k2
                when k2.KeyEvent.Code is KeyCode.Tab
                  && k2.KeyEvent.Kind == KeyEventKind.Press:
            {
                bool shift = (k2.KeyEvent.Modifiers & KeyModifiers.Shift) != 0;
                CycleFocus(state, shift);
                break;
            }

            // Enter activates focused button
            case InputEvent.Key k3
                when k3.KeyEvent.Code is KeyCode.Enter
                  && k3.KeyEvent.Kind == KeyEventKind.Press:
                return ActivateButton(state);

            // Arrow keys navigate buttons (only when input is not focused)
            case InputEvent.Key k4
                when (k4.KeyEvent.Code is KeyCode.Left || k4.KeyEvent.Code is KeyCode.Right)
                  && k4.KeyEvent.Kind == KeyEventKind.Press
                  && !state.InputFocused:
            {
                bool forward = k4.KeyEvent.Code is KeyCode.Right;
                NavigateButtons(state, forward);
                break;
            }

            // Mouse down on button: press only, activate on mouse up.
            case InputEvent.Mouse m when m.MouseEvent.Kind is MouseEventKind.Down d && d.Button == MouseButton.Left:
                state.PressedButton = null;
                if (_config.Kind == ModalDialogKind.Prompt
                    && hit is { } inputHit
                    && _hitId is { } inputExpected
                    && inputHit.hitId == inputExpected
                    && inputHit.region == DialogHitRegions.DialogHitInput)
                {
                    state.InputFocused = true;
                    state.FocusedButton = null;
                    state.PressedButton = null;
                }
                else if (hit is { } btnHit
                      && _hitId is { } btnExpected
                      && btnHit.hitId == btnExpected
                      && btnHit.region == DialogHitRegions.DialogHitButton)
                {
                    int idx = (int)btnHit.data;
                    if (idx < _buttons.Count)
                    {
                        state.InputFocused = false;
                        state.FocusedButton = idx;
                        state.PressedButton = idx;
                    }
                }
                break;

            // Mouse up on button: activate if it matches the pressed target.
            case InputEvent.Mouse m2 when m2.MouseEvent.Kind is MouseEventKind.Up u && u.Button == MouseButton.Left:
            {
                int? pressed = state.PressedButton;
                state.PressedButton = null;
                if (pressed is { } p
                    && hit is { } upHit
                    && _hitId is { } upExpected
                    && upHit.hitId == upExpected
                    && upHit.region == DialogHitRegions.DialogHitButton
                    && (int)upHit.data == p)
                {
                    state.InputFocused = false;
                    state.FocusedButton = p;
                    return ActivateButton(state);
                }
                break;
            }

            // Paste for prompt dialogs when input is focused
            case InputEvent.Paste paste
                when _config.Kind == ModalDialogKind.Prompt && state.InputFocused:
                HandleInputPaste(state, paste.PasteEvent.Text);
                break;

            // Key input for prompt dialogs when input is focused
            case InputEvent.Key k5
                when _config.Kind == ModalDialogKind.Prompt && state.InputFocused:
                HandleInputKey(state, k5.KeyEvent);
                break;
        }

        return null;
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private void CycleFocus(ModalDialogState state, bool reverse)
    {
        bool hasInput = _config.Kind == ModalDialogKind.Prompt;
        int buttonCount = _buttons.Count;
        state.PressedButton = null;

        if (hasInput)
        {
            // Cycle: input → button 0 → button 1 → … → input
            if (state.InputFocused)
            {
                state.InputFocused = false;
                state.FocusedButton = reverse
                    ? (buttonCount > 0 ? buttonCount - 1 : 0)
                    : 0;
            }
            else if (state.FocusedButton is { } idx)
            {
                if (reverse)
                {
                    if (idx == 0)
                    {
                        state.InputFocused = true;
                        state.FocusedButton = null;
                    }
                    else
                    {
                        state.FocusedButton = idx - 1;
                    }
                }
                else
                {
                    if (idx + 1 >= buttonCount)
                    {
                        state.InputFocused = true;
                        state.FocusedButton = null;
                    }
                    else
                    {
                        state.FocusedButton = idx + 1;
                    }
                }
            }
            else
            {
                state.FocusedButton = reverse
                    ? (buttonCount > 0 ? buttonCount - 1 : 0)
                    : 0;
            }
        }
        else
        {
            // Just cycle buttons
            if (reverse)
            {
                state.FocusedButton = state.FocusedButton switch
                {
                    0 => buttonCount - 1,
                    { } current => current - 1,
                    null => buttonCount - 1,
                };
            }
            else
            {
                state.FocusedButton = state.FocusedButton switch
                {
                    { } current => (current + 1) % buttonCount,
                    null => 0,
                };
            }
        }
    }

    private void NavigateButtons(ModalDialogState state, bool forward)
    {
        int count = _buttons.Count;
        if (count == 0) return;
        state.PressedButton = null;
        if (forward)
        {
            state.FocusedButton = state.FocusedButton switch
            {
                { } current => (current + 1) % count,
                null => 0,
            };
        }
        else
        {
            state.FocusedButton = state.FocusedButton switch
            {
                0 => count - 1,
                { } current => current - 1,
                null => count - 1,
            };
        }
    }

    private DialogOutcome? ActivateButton(ModalDialogState state)
    {
        int? idx = state.FocusedButton;
        if (!idx.HasValue)
        {
            // Default to primary button
            for (int i = 0; i < _buttons.Count; i++)
            {
                if (_buttons[i].IsPrimary) { idx = i; break; }
            }
        }

        if (!idx.HasValue) return null;
        if (idx.Value >= _buttons.Count) return null;

        var button = _buttons[idx.Value];
        DialogOutcome result = button.ButtonId switch
        {
            "ok" => _config.Kind == ModalDialogKind.Prompt
                ? DialogOutcome.InputResult(state.InputValue)
                : DialogOutcome.OkInstance,
            "cancel" => DialogOutcome.CancelInstance,
            var id => DialogOutcome.CustomResult(id),
        };

        state.Close(result);
        return result;
    }

    private void HandleInputKey(ModalDialogState state, KeyEvent key)
    {
        if (key.Kind != KeyEventKind.Press) return;

        switch (key.Code)
        {
            case KeyCode.Char c:
                state.InputValue += c.Character;
                break;
            case KeyCode.Backspace:
                state.InputValue = RemoveLastGrapheme(state.InputValue);
                break;
            case KeyCode.Delete:
                state.InputValue = string.Empty;
                break;
        }
    }

    private void HandleInputPaste(ModalDialogState state, string text)
    {
        var sanitized = new System.Text.StringBuilder(text.Length);
        foreach (char c in text)
        {
            char mapped = c switch
            {
                '\n' or '\r' or '\t' => ' ',
                _ => c,
            };
            if (!char.IsControl(mapped))
                sanitized.Append(mapped);
        }
        if (sanitized.Length > 0)
            state.InputValue += sanitized.ToString();
    }

    /// <summary>
    /// Remove the last Unicode grapheme cluster from the string.
    /// Rust: <c>grapheme_indices(true).next_back()</c> via unicode-segmentation.
    /// </summary>
    private static string RemoveLastGrapheme(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;

        // Walk grapheme clusters; find start of last one.
        var te = StringInfo.GetTextElementEnumerator(s);
        int lastStart = 0;
        int currentStart = 0;
        while (te.MoveNext())
        {
            lastStart = currentStart;
            currentStart += te.GetTextElement().Length;
        }
        return s[..lastStart];
    }

    /// <summary>Calculate content height.</summary>
    internal ushort ContentHeight()
    {
        ushort height = 2; // top and bottom border

        // Message row(s) — simplified: 1 row
        if (!string.IsNullOrEmpty(_message))
            height += 1;

        // Spacing
        height += 1;

        // Input row (for Prompt)
        if (_config.Kind == ModalDialogKind.Prompt)
        {
            height += 1;
            height += 1; // spacing
        }

        // Button row
        height += 1;

        return height;
    }

    private ModalContainerConfig CloneModalConfigWithSize(ushort contentHeight)
    {
        var size = _config.ModalConfig.Size;
        var newSize = new ModalContainerSizeConstraints
        {
            MinWidth = size.MinWidth,
            MaxWidth = size.MaxWidth,
            MinHeight = size.MinHeight,
            MaxHeight = size.MaxHeight,
        };

        // Apply default width constraints when none are set
        if (!size.MinWidth.HasValue && !size.MaxWidth.HasValue)
        {
            newSize.MinWidth = 30;
            newSize.MaxWidth = 60;
        }
        // Apply default height constraints when none are set
        if (!size.MinHeight.HasValue && !size.MaxHeight.HasValue)
        {
            newSize.MinHeight = contentHeight;
            newSize.MaxHeight = (ushort)(contentHeight + 4);
        }

        return new ModalContainerConfig
        {
            Position = _config.ModalConfig.Position,
            Size = newSize,
            CloseOnBackdrop = _config.ModalConfig.CloseOnBackdrop,
            CloseOnEscape = _config.ModalConfig.CloseOnEscape,
            HitId = _config.ModalConfig.HitId,
            BackdropOpacity = _config.ModalConfig.BackdropOpacity,
        };
    }

    /// <summary>Render the dialog content into the given area.</summary>
    internal void RenderContent(Rect area, Frame frame, ModalDialogState state)
    {
        if (area.IsEmpty) return;

        // Draw border with centered title.
        var block = Block.New()
            .Borders(Borders.All)
            .Title(_title)
            .TitleAlignment(Alignment.Center);
        block.Render(area, frame);

        var inner = block.Inner(area);
        if (inner.IsEmpty) return;

        // Clear the inner pane (preserve backdrop-tinted background).
        for (ushort y = inner.Y; y < inner.Bottom; y++)
        {
            for (ushort x = inner.X; x < inner.Right; x++)
            {
                var cell = frame.Buffer.Get(x, y);
                if (cell is { } c)
                {
                    var bg = c.Background;
                    var cleared = Cell.FromChar(' ').WithBackground(bg);
                    frame.Buffer.Set(x, y, cleared);
                }
            }
        }

        ushort iy = inner.Y;

        // Message
        if (!string.IsNullOrEmpty(_message) && iy < inner.Bottom)
        {
            DrawCenteredText(frame, inner.X, iy, inner.Width, _message, _config.MessageStyle);
            iy += 1;
        }

        // Spacing
        iy += 1;

        // Input field (for Prompt)
        if (_config.Kind == ModalDialogKind.Prompt && iy < inner.Bottom)
        {
            RenderInput(frame, inner.X, iy, inner.Width, state);
            iy += 2; // input + spacing
        }

        // Buttons
        if (iy < inner.Bottom)
            RenderButtons(frame, inner.X, iy, inner.Width, state);
    }

    private static void DrawCenteredText(Frame frame, ushort x, ushort y, ushort width, string text, WidgetStyle style)
    {
        int textWidth = Math.Min(TerminalTextWidth.DisplayWidth(text), width);
        int offset = (width - textWidth) / 2;
        ushort startX = (ushort)Math.Min(x + offset, ushort.MaxValue);
        ushort maxX = (ushort)Math.Min(x + width, ushort.MaxValue);
        WidgetDrawing.DrawTextSpan(frame, startX, y, text, style, maxX);
    }

    private void RenderInput(Frame frame, ushort x, ushort y, ushort width, ModalDialogState state)
    {
        // Draw input background (inset by 1 on each side).
        var inputArea = new Rect(
            (ushort)(x + 1),
            y,
            (ushort)Math.Max(0, width - 2),
            1);
        var inputStyle = _config.InputStyle;
        WidgetDrawing.SetStyleArea(frame.Buffer, inputArea, inputStyle);

        if (_hitId is { } hitId && !inputArea.IsEmpty)
            frame.RegisterHit(inputArea, hitId, DialogHitRegions.DialogHitInput, 0);

        // Draw input value or placeholder space.
        string displayText = string.IsNullOrEmpty(state.InputValue) ? " " : state.InputValue;
        WidgetDrawing.DrawTextSpan(frame, inputArea.X, y, displayText, inputStyle, inputArea.Right);

        // Draw cursor if focused.
        if (state.InputFocused)
        {
            int inputWidth = TerminalTextWidth.DisplayWidth(state.InputValue);
            ushort cursorX = (ushort)(inputArea.X + Math.Min(inputWidth, inputArea.Width));
            if (cursorX < inputArea.Right)
            {
                frame.SetCursor((cursorX, y));
                frame.SetCursorVisible(true);
            }
        }
    }

    private void RenderButtons(Frame frame, ushort x, ushort y, ushort width, ModalDialogState state)
    {
        if (_buttons.Count == 0) return;

        // Calculate total button width.
        int totalWidth = 0;
        foreach (var b in _buttons) totalWidth += b.DisplayWidth();
        totalWidth += Math.Max(0, _buttons.Count - 1) * 2; // spacing between buttons

        // Center the buttons.
        int startXOffset = (width - Math.Min(totalWidth, width)) / 2;
        ushort bx = (ushort)(x + startXOffset);

        for (int i = 0; i < _buttons.Count; i++)
        {
            var button = _buttons[i];
            bool isFocused = state.FocusedButton == i;

            var style = isFocused
                ? _config.FocusedButtonStyle
                : button.IsPrimary
                    ? _config.PrimaryButtonStyle
                    : _config.ButtonStyle;

            // Ensure focused button always has Reverse set.
            if (isFocused && !style.HasReverse())
                style = style.WithReverse();

            string btnText = $"[ {button.Label} ]";
            int btnWidth = TerminalTextWidth.DisplayWidth(btnText);
            ushort maxX = (ushort)(x + width);
            WidgetDrawing.DrawTextSpan(frame, bx, y, btnText, style, maxX);

            // Register hit region for the button.
            if (_hitId is { } hitId)
            {
                ushort maxBtnWidth = (ushort)Math.Max(0, width - (bx - x));
                ushort btnAreaWidth = (ushort)Math.Min(btnWidth, maxBtnWidth);
                if (btnAreaWidth > 0)
                {
                    var btnArea = new Rect(bx, y, btnAreaWidth, 1);
                    frame.RegisterHit(btnArea, hitId, DialogHitRegions.DialogHitButton, (ulong)i);
                }
            }

            bx = (ushort)Math.Min(bx + btnWidth + 2, ushort.MaxValue); // button + spacing
        }
    }
}

// ── WidgetStyle extensions for dialog ────────────────────────────────────

internal static class ModalDialogWidgetStyleExtensions
{
    /// <summary>Check whether a style has the Reverse attribute set.</summary>
    internal static bool HasReverse(this WidgetStyle style)
        => style.Attrs.HasValue && (style.Attrs.Value & CellStyleFlags.Reverse) != 0;

    /// <summary>Return a copy of the style with Reverse added.</summary>
    internal static WidgetStyle WithReverse(this WidgetStyle style)
        => new WidgetStyle(style.Fg, style.Bg, (style.Attrs ?? CellStyleFlags.None) | CellStyleFlags.Reverse);
}

// ── DialogBuilder ──────────────────────────────────────────────────────────

/// <summary>Builder for custom dialogs.</summary>
public sealed class ModalDialogBuilder
{
    private string _title;
    private string _message;
    private List<ModalDialogButton> _buttons;
    private ModalDialogConfig _config;
    private HitId? _hitId;

    internal ModalDialogBuilder(
        string title,
        string message,
        List<ModalDialogButton> buttons,
        ModalDialogConfig config,
        HitId? hitId)
    {
        _title = title;
        _message = message;
        _buttons = buttons;
        _config = config;
        _hitId = hitId;
    }

    /// <summary>Add a button.</summary>
    public ModalDialogBuilder Button(ModalDialogButton button)
    {
        _buttons.Add(button);
        return this;
    }

    /// <summary>Add an OK button.</summary>
    public ModalDialogBuilder OkButton()
        => Button(new ModalDialogButton("OK", "ok").Primary());

    /// <summary>Add a Cancel button.</summary>
    public ModalDialogBuilder CancelButton()
        => Button(new ModalDialogButton("Cancel", "cancel"));

    /// <summary>Add a custom button.</summary>
    public ModalDialogBuilder CustomButton(string label, string id)
        => Button(new ModalDialogButton(label, id));

    /// <summary>Set modal configuration.</summary>
    public ModalDialogBuilder WithModalConfig(ModalContainerConfig config)
    {
        _hitId = config.HitId;
        _config.ModalConfig = config;
        return this;
    }

    /// <summary>Set hit ID for mouse interaction.</summary>
    public ModalDialogBuilder WithHitId(HitId id)
    {
        _hitId = id;
        _config.ModalConfig.HitId = id;
        return this;
    }

    internal string Title => _title;
    internal string Message => _message;
    internal IReadOnlyList<ModalDialogButton> Buttons => _buttons;
    internal ModalDialogConfig Config => _config;
    internal HitId? HitIdValue => _hitId;

    /// <summary>Build the dialog. Ensures at least one button is present.</summary>
    public ModalDialog Build()
    {
        var buttons = new List<ModalDialogButton>(_buttons);
        if (buttons.Count == 0)
            buttons.Add(new ModalDialogButton("OK", "ok").Primary());

        return new ModalDialog(
            _title,
            _message,
            buttons,
            _config,
            _hitId);
    }
}
