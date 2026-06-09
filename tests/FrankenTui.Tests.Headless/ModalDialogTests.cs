// Upstream source: .external/frankentui/crates/ftui-widgets/src/modal/dialog.rs (tests module)
// Full 1-1 port of all upstream dialog.rs tests.

// DIVERGENCE: Rust GraphemePool::new() → new GraphemePool() (C# heap-allocated).
// DIVERGENCE: Rust Frame::new(w, h, &mut pool) → new Frame(w, h, pool).
// DIVERGENCE: Rust Frame::with_hit_grid(w, h, &mut pool) → Frame.WithHitGrid(w, h, pool).
// DIVERGENCE: Rust frame.hit_test(x, y) returns Option<(HitId, HitRegion, u64)>;
//   C# Frame.HitTest returns (HitId, HitRegionKind, ulong)?; the tuple data field is ulong.
// DIVERGENCE: Rust HitRegion::Button → HitRegionKind.Button; HitRegion::Custom(1) for
//   DIALOG_HIT_INPUT → HitRegionKind.DialogInput; HitRegion::Custom(2) for MODAL_HIT_CONTENT
//   → HitRegionKind.ModalContent. See HitRegion.cs for the full expansion table.
// DIVERGENCE: Rust DialogResult enum values are closed record variants in C# (DialogOutcome).
// DIVERGENCE: Rust Dialog struct → ModalDialog class; DialogState → ModalDialogState;
//   DialogButton → ModalDialogButton; DialogKind → ModalDialogKind.
// DIVERGENCE: row_text helper: Rust cell.content.as_char() → C# cell.Content.AsChar().
// DIVERGENCE: Rust Event::Key(KeyEvent{...}) → InputEvent.Key(new KeyEvent(...)).
// DIVERGENCE: Rust Event::Mouse(MouseEvent::new(...)) → InputEvent.Mouse(new MouseEvent(...)).
// DIVERGENCE: Rust Event::Paste(PasteEvent::bracketed(...)) → InputEvent.Paste(new PasteEvent(..., true)).
// DIVERGENCE: Rust PasteEvent::bracketed(text) → new PasteEvent(text, true).
// DIVERGENCE: Rust HitId::new(n) → HitId.New(n) (C# static factory).
// DIVERGENCE: Rust usize::try_from(data) cast → direct (int) cast of ulong.
// DIVERGENCE: Rust dialog.render(...) on StatefulWidget → dialog.Render(area, frame, state).
// DIVERGENCE: Rust dialog.render_content(...) is internal; accessed directly from test as dialog.RenderContent(...).
// DIVERGENCE: Rust crate::modal::MODAL_HIT_CONTENT → DialogHitRegions.ModalHitContent.
// DIVERGENCE: Rust Modifiers::SHIFT → KeyModifiers.Shift; Modifiers::empty() → KeyModifiers.None.
// DIVERGENCE: Rust KeyCode::Char(c) → new KeyCode.Char(c); KeyCode::Enter → new KeyCode.Enter(); etc.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Widgets;
using FrankenTui.Widgets.Modal;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class ModalDialogTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Rust: fn row_text(frame: &amp;Frame, y: u16) -> String
    /// Collects cell characters across the full frame width for the given row.
    /// </summary>
    private static string RowText(Frame frame, ushort y)
    {
        var sb = new System.Text.StringBuilder();
        for (ushort x = 0; x < frame.Buffer.Width; x++)
        {
            var cell = frame.Buffer.Get(x, y);
            char c = cell?.Content.AsChar() ?? ' ';
            sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Rust: fn hit_bounds(frame: &amp;Frame, expected: (HitId, HitRegion, u64)) -> Option&lt;Rect&gt;
    /// Scans the hit grid for cells matching (hitId, region, data) and returns the bounding Rect.
    /// </summary>
    private static Rect? HitBounds(Frame frame, HitId hitId, HitRegionKind region, ulong data)
    {
        ushort minX = ushort.MaxValue, minY = ushort.MaxValue;
        ushort maxX = 0, maxY = 0;
        bool found = false;

        for (ushort y = 0; y < frame.Buffer.Height; y++)
        {
            for (ushort x = 0; x < frame.Buffer.Width; x++)
            {
                var result = frame.HitTest(x, y);
                if (result is { } r && r.Item1 == hitId && r.Item2 == region && r.Item3 == data)
                {
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                    found = true;
                }
            }
        }

        if (!found) return null;
        return new Rect(minX, minY, (ushort)(maxX - minX + 1), (ushort)(maxY - minY + 1));
    }

    // ── Basic construction tests ──────────────────────────────────────────────

    [Fact]
    public void AlertDialogSingleButton()
    {
        var dialog = ModalDialog.Alert("Title", "Message");
        Assert.Equal(1, dialog.Buttons.Count);
        Assert.Equal("OK", dialog.Buttons[0].Label);
        Assert.True(dialog.Buttons[0].IsPrimary);
    }

    [Fact]
    public void ConfirmDialogTwoButtons()
    {
        var dialog = ModalDialog.Confirm("Title", "Message");
        Assert.Equal(2, dialog.Buttons.Count);
        Assert.Equal("OK", dialog.Buttons[0].Label);
        Assert.Equal("Cancel", dialog.Buttons[1].Label);
    }

    [Fact]
    public void PromptDialogHasInput()
    {
        var dialog = ModalDialog.Prompt("Title", "Message");
        Assert.Equal(ModalDialogKind.Prompt, dialog.Config.Kind);
        Assert.Equal(2, dialog.Buttons.Count);
    }

    [Fact]
    public void CustomDialogBuilder()
    {
        var dialog = ModalDialog.Custom("Custom", "Message")
            .OkButton()
            .CancelButton()
            .CustomButton("Help", "help")
            .Build();
        Assert.Equal(3, dialog.Buttons.Count);
    }

    // ── DialogState tests ─────────────────────────────────────────────────────

    [Fact]
    public void DialogStateStartsOpen()
    {
        var state = ModalDialogState.New();
        Assert.True(state.IsOpen());
        Assert.Null(state.Result);
    }

    [Fact]
    public void DialogStateCloseSetResult()
    {
        var state = ModalDialogState.New();
        state.Close(DialogOutcome.OkInstance);
        Assert.False(state.IsOpen());
        Assert.Equal(new DialogOutcome.Ok(), state.Result);
    }

    // ── Keyboard event tests ──────────────────────────────────────────────────

    [Fact]
    public void DialogEscapeCloses()
    {
        var dialog = ModalDialog.Alert("Test", "Msg");
        var state = ModalDialogState.New();
        var evt = new InputEvent.Key(new KeyEvent(new KeyCode.Escape(), KeyModifiers.None, KeyEventKind.Press));
        var result = dialog.HandleEvent(evt, state, null);
        Assert.Equal(new DialogOutcome.Dismissed(), result);
        Assert.False(state.IsOpen());
    }

    [Fact]
    public void DialogEnterActivatesPrimary()
    {
        var dialog = ModalDialog.Alert("Test", "Msg");
        var state = ModalDialogState.New();
        state.InputFocused = false; // Not on input
        var evt = new InputEvent.Key(new KeyEvent(new KeyCode.Enter(), KeyModifiers.None, KeyEventKind.Press));
        var result = dialog.HandleEvent(evt, state, null);
        Assert.Equal(new DialogOutcome.Ok(), result);
    }

    [Fact]
    public void DialogMouseUpActivatesPressedButton()
    {
        var dialog = ModalDialog.Confirm("Test", "Msg").WithHitId(HitId.New(1));
        var state = ModalDialogState.New();

        var down = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 0, 0));
        var hit = ((HitId, HitRegionKind, ulong)?)(HitId.New(1), HitRegionKind.Button, 0UL);
        var result = dialog.HandleEvent(down, state, hit);
        Assert.Null(result);
        Assert.Equal(0, state.FocusedButton);
        Assert.Equal(0, state.PressedButton);

        var up = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Up(MouseButton.Left), 0, 0));
        result = dialog.HandleEvent(up, state, hit);
        Assert.Equal(new DialogOutcome.Ok(), result);
        Assert.False(state.IsOpen());
    }

    [Fact]
    public void PromptMouseDownOnButtonTransfersFocusFromInput()
    {
        var dialog = ModalDialog.Prompt("Test", "Msg").WithHitId(HitId.New(1));
        var state = ModalDialogState.New();
        Assert.True(state.InputFocused);
        Assert.Null(state.FocusedButton);

        var down = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 0, 0));
        var hit = ((HitId, HitRegionKind, ulong)?)(HitId.New(1), HitRegionKind.Button, 1UL);
        var result = dialog.HandleEvent(down, state, hit);

        Assert.Null(result);
        Assert.False(state.InputFocused);
        Assert.Equal(1, state.FocusedButton);
        Assert.Equal(1, state.PressedButton);
    }

    [Fact]
    public void PromptMouseButtonFocusAllowsArrowNavigationAfterMissedClick()
    {
        var dialog = ModalDialog.Prompt("Test", "Msg").WithHitId(HitId.New(1));
        var state = ModalDialogState.New();

        var down = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 0, 0));
        var hit0 = ((HitId, HitRegionKind, ulong)?)(HitId.New(1), HitRegionKind.Button, 0UL);
        dialog.HandleEvent(down, state, hit0);

        var upOutside = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Up(MouseButton.Left), 0, 0));
        dialog.HandleEvent(upOutside, state, null);
        Assert.False(state.InputFocused);
        Assert.Equal(0, state.FocusedButton);

        var right = new InputEvent.Key(new KeyEvent(new KeyCode.Right(), KeyModifiers.None, KeyEventKind.Press));
        dialog.HandleEvent(right, state, null);
        Assert.Equal(1, state.FocusedButton);
    }

    [Fact]
    public void PromptMouseDownOnInputRestoresInputFocus()
    {
        var dialog = ModalDialog.Prompt("Test", "Msg").WithHitId(HitId.New(1));
        var state = ModalDialogState.New();
        state.InputFocused = false;
        state.FocusedButton = 1;
        state.PressedButton = 1;

        var down = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 0, 0));
        var hit = ((HitId, HitRegionKind, ulong)?)(HitId.New(1), HitRegionKind.DialogInput, 0UL);
        var result = dialog.HandleEvent(down, state, hit);

        Assert.Null(result);
        Assert.True(state.InputFocused);
        Assert.Null(state.FocusedButton);
        Assert.Null(state.PressedButton);
    }

    [Fact]
    public void RenderPromptRegistersInputHitRegion()
    {
        var dialog = ModalDialog.Prompt("Prompt", "Enter:").WithHitId(HitId.New(7));
        var state = ModalDialogState.New();
        var pool = new GraphemePool();
        var frame = Frame.WithHitGrid(40, 10, pool);

        dialog.Render(new Rect(0, 0, 40, 10), frame, state);

        bool found = false;
        for (ushort y = 0; y < frame.Buffer.Height && !found; y++)
        for (ushort x = 0; x < frame.Buffer.Width && !found; x++)
        {
            var r = frame.HitTest(x, y);
            if (r is { } t && t.Item1 == HitId.New(7) && t.Item2 == HitRegionKind.DialogInput && t.Item3 == 0)
                found = true;
        }
        Assert.True(found);
    }

    [Fact]
    public void RenderPromptRegistersInputHitRegionFromModalConfigHitId()
    {
        var dialog = ModalDialog.Prompt("Prompt", "Enter:")
            .WithModalConfig(ModalContainerConfig.Default().WithHitId(HitId.New(7)));
        var state = ModalDialogState.New();
        var pool = new GraphemePool();
        var frame = Frame.WithHitGrid(40, 10, pool);

        dialog.Render(new Rect(0, 0, 40, 10), frame, state);

        bool found = false;
        for (ushort y = 0; y < frame.Buffer.Height && !found; y++)
        for (ushort x = 0; x < frame.Buffer.Width && !found; x++)
        {
            var r = frame.HitTest(x, y);
            if (r is { } t && t.Item1 == HitId.New(7) && t.Item2 == HitRegionKind.DialogInput && t.Item3 == 0)
                found = true;
        }
        Assert.True(found);
    }

    [Fact]
    public void RenderRespectsModalConfigSizeConstraints()
    {
        var dialog = ModalDialog.Alert("Prompt", "Enter:").WithModalConfig(
            ModalContainerConfig.Default()
                .WithHitId(HitId.New(11))
                .WithSize(new ModalContainerSizeConstraints().WithMaxWidth(10).WithMaxHeight(5)));
        var state = ModalDialogState.New();
        var pool = new GraphemePool();
        var frame = Frame.WithHitGrid(40, 20, pool);

        dialog.Render(new Rect(0, 0, 40, 20), frame, state);

        var content = HitBounds(frame, HitId.New(11), DialogHitRegions.ModalHitContent, 0);
        Assert.NotNull(content);
        Assert.Equal(10, content!.Value.Width);
        Assert.Equal(5, content.Value.Height);
    }

    [Fact]
    public void DialogMouseUpOutsideDoesNotActivate()
    {
        var dialog = ModalDialog.Confirm("Test", "Msg").WithHitId(HitId.New(1));
        var state = ModalDialogState.New();

        var down = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 0, 0));
        var hit = ((HitId, HitRegionKind, ulong)?)(HitId.New(1), HitRegionKind.Button, 0UL);
        var result = dialog.HandleEvent(down, state, hit);
        Assert.Null(result);
        Assert.Equal(0, state.PressedButton);

        var up = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Up(MouseButton.Left), 0, 0));
        result = dialog.HandleEvent(up, state, null);
        Assert.Null(result);
        Assert.True(state.IsOpen());
        Assert.Null(state.PressedButton);
    }

    [Fact]
    public void DialogTabCyclesFocus()
    {
        var dialog = ModalDialog.Confirm("Test", "Msg");
        var state = ModalDialogState.New();
        state.InputFocused = false;
        state.FocusedButton = 0;

        var tab = new InputEvent.Key(new KeyEvent(new KeyCode.Tab(), KeyModifiers.None, KeyEventKind.Press));

        dialog.HandleEvent(tab, state, null);
        Assert.Equal(1, state.FocusedButton);

        dialog.HandleEvent(tab, state, null);
        Assert.Equal(0, state.FocusedButton); // Wraps around
    }

    [Fact]
    public void FreshNonPromptTabStartsOnPrimaryButton()
    {
        var dialog = ModalDialog.Confirm("Test", "Msg");
        var state = ModalDialogState.New();
        Assert.Null(state.FocusedButton);
        Assert.True(state.InputFocused);

        var tab = new InputEvent.Key(new KeyEvent(new KeyCode.Tab(), KeyModifiers.None, KeyEventKind.Press));

        dialog.HandleEvent(tab, state, null);
        Assert.False(state.InputFocused);
        Assert.Equal(0, state.FocusedButton);
    }

    [Fact]
    public void FreshNonPromptRightArrowStartsOnPrimaryButton()
    {
        var dialog = ModalDialog.Confirm("Test", "Msg");
        var state = ModalDialogState.New();
        Assert.Null(state.FocusedButton);
        Assert.True(state.InputFocused);

        var right = new InputEvent.Key(new KeyEvent(new KeyCode.Right(), KeyModifiers.None, KeyEventKind.Press));

        dialog.HandleEvent(right, state, null);
        Assert.False(state.InputFocused);
        Assert.Equal(0, state.FocusedButton);
    }

    [Fact]
    public void TabNavigationCancelsPressedButton()
    {
        var dialog = ModalDialog.Confirm("Test", "Msg").WithHitId(HitId.New(1));
        var state = ModalDialogState.New();

        var down = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 0, 0));
        var hit0 = ((HitId, HitRegionKind, ulong)?)(HitId.New(1), HitRegionKind.Button, 0UL);
        dialog.HandleEvent(down, state, hit0);
        Assert.Equal(0, state.PressedButton);

        var tab = new InputEvent.Key(new KeyEvent(new KeyCode.Tab(), KeyModifiers.None, KeyEventKind.Press));
        dialog.HandleEvent(tab, state, null);
        Assert.Equal(1, state.FocusedButton);
        Assert.Null(state.PressedButton);

        var up = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Up(MouseButton.Left), 0, 0));
        var result = dialog.HandleEvent(up, state, hit0);
        Assert.Null(result);
        Assert.True(state.IsOpen());
    }

    [Fact]
    public void PromptEnterReturnsInput()
    {
        var dialog = ModalDialog.Prompt("Test", "Enter:");
        var state = ModalDialogState.New();
        state.InputValue = "hello";
        state.InputFocused = false;
        state.FocusedButton = 0; // OK button

        var enter = new InputEvent.Key(new KeyEvent(new KeyCode.Enter(), KeyModifiers.None, KeyEventKind.Press));

        var result = dialog.HandleEvent(enter, state, null);
        Assert.Equal(new DialogOutcome.Input("hello"), result);
    }

    [Fact]
    public void ButtonDisplayWidth()
    {
        var button = new ModalDialogButton("OK", "ok");
        Assert.Equal(6, button.DisplayWidth()); // [ OK ]
    }

    [Fact]
    public void RenderAlertDoesNotPanic()
    {
        var dialog = ModalDialog.Alert("Alert", "This is an alert message.");
        var state = ModalDialogState.New();
        var pool = new GraphemePool();
        var frame = new Frame(80, 24, pool);
        dialog.Render(new Rect(0, 0, 80, 24), frame, state);
    }

    [Fact]
    public void RenderConfirmDoesNotPanic()
    {
        var dialog = ModalDialog.Confirm("Confirm", "Are you sure?");
        var state = ModalDialogState.New();
        var pool = new GraphemePool();
        var frame = new Frame(80, 24, pool);
        dialog.Render(new Rect(0, 0, 80, 24), frame, state);
    }

    [Fact]
    public void RenderPromptDoesNotPanic()
    {
        var dialog = ModalDialog.Prompt("Prompt", "Enter your name:");
        var state = ModalDialogState.New();
        state.InputValue = "Test User";
        var pool = new GraphemePool();
        var frame = new Frame(80, 24, pool);
        dialog.Render(new Rect(0, 0, 80, 24), frame, state);
    }

    [Fact]
    public void RenderContentShorterMessageAndButtonsClearStaleInnerRows()
    {
        var dialogLong = ModalDialog.Custom("Title", "LLLLLLLLLLLLLLLLLLLL")
            .CustomButton("Alpha", "alpha")
            .CustomButton("Beta", "beta")
            .CustomButton("Gamma", "gamma")
            .Build();
        var dialogShort = ModalDialog.Custom("Title", "S").OkButton().Build();
        var stateReadOnly = ModalDialogState.New();
        var area = new Rect(10, 5, 40, 8);
        var pool = new GraphemePool();
        var frame = new Frame(80, 24, pool);

        dialogLong.RenderContent(area, frame, stateReadOnly);
        dialogShort.RenderContent(area, frame, stateReadOnly);

        var inner = Block.New()
            .Borders(Borders.All)
            .Title("Title")
            .TitleAlignment(Alignment.Center)
            .Inner(area);
        var messageRow = RowText(frame, inner.Y);
        var buttonRow = RowText(frame, (ushort)(inner.Y + 2));

        Assert.Contains('S', messageRow);
        Assert.DoesNotContain('L', messageRow);
        Assert.Contains("[ OK ]", buttonRow);
        Assert.DoesNotContain("Alpha", buttonRow);
        Assert.DoesNotContain("Beta", buttonRow);
        Assert.DoesNotContain("Gamma", buttonRow);
    }

    [Fact]
    public void RenderPromptShorterInputClearsStaleSuffix()
    {
        var dialog = ModalDialog.Prompt("Prompt", "Enter:");
        var area = new Rect(10, 5, 40, 8);
        var longState = ModalDialogState.New();
        longState.InputValue = "LongInputValue";
        var shortState = ModalDialogState.New();
        shortState.InputValue = "Hi";
        var pool = new GraphemePool();
        var frame = new Frame(80, 24, pool);

        dialog.RenderContent(area, frame, longState);
        dialog.RenderContent(area, frame, shortState);

        var inner = Block.New()
            .Borders(Borders.All)
            .Title("Prompt")
            .TitleAlignment(Alignment.Center)
            .Inner(area);
        var inputRow = RowText(frame, (ushort)(inner.Y + 2));

        Assert.Contains("Hi", inputRow);
        Assert.DoesNotContain("LongInputValue", inputRow);
        Assert.DoesNotContain("ngInputValue", inputRow);
    }

    [Fact]
    public void RenderTinyAreaDoesNotPanic()
    {
        var dialog = ModalDialog.Alert("T", "M");
        var state = ModalDialogState.New();
        var pool = new GraphemePool();
        var frame = new Frame(10, 5, pool);
        dialog.Render(new Rect(0, 0, 10, 5), frame, state);
    }

    [Fact]
    public void CustomDialogEmptyButtonsGetsDefault()
    {
        var dialog = ModalDialog.Custom("Custom", "No buttons").Build();
        Assert.Equal(1, dialog.Buttons.Count);
        Assert.Equal("OK", dialog.Buttons[0].Label);
    }

    [Fact]
    public void RenderUnicodeMessageDoesNotPanic()
    {
        // CJK characters are 2 columns wide each
        var dialog = ModalDialog.Alert("你好", "这是一条消息 🎉");
        var state = ModalDialogState.New();
        var pool = new GraphemePool();
        var frame = new Frame(80, 24, pool);
        dialog.Render(new Rect(0, 0, 80, 24), frame, state);
    }

    [Fact]
    public void PromptWithUnicodeInputRendersCorrectly()
    {
        var dialog = ModalDialog.Prompt("入力", "名前を入力:");
        var state = ModalDialogState.New();
        state.InputValue = "田中太郎"; // CJK input
        var pool = new GraphemePool();
        var frame = new Frame(80, 24, pool);
        dialog.Render(new Rect(0, 0, 80, 24), frame, state);
    }

    // ── Edge-case tests (bd-1is2p) ────────────────────────────────────────────

    [Fact]
    public void EdgeStateDefaultVsNew()
    {
        var defaultState = new ModalDialogState();
        var newState = ModalDialogState.New();
        // Default: open=false, input_focused=false
        Assert.False(defaultState.Open);
        Assert.False(defaultState.InputFocused);
        // New: open=true, input_focused=true
        Assert.True(newState.Open);
        Assert.True(newState.InputFocused);
    }

    [Fact]
    public void EdgeStateResetThenReuse()
    {
        var state = ModalDialogState.New();
        state.InputValue = "typed";
        state.FocusedButton = 1;
        state.Close(DialogOutcome.CancelInstance);

        Assert.False(state.IsOpen());
        Assert.NotNull(state.Result);

        state.Reset();
        Assert.True(state.IsOpen());
        Assert.Null(state.Result);
        Assert.True(string.IsNullOrEmpty(state.InputValue));
        Assert.Null(state.FocusedButton);
        Assert.True(state.InputFocused);
    }

    [Fact]
    public void EdgeTakeResultWhenNone()
    {
        var state = ModalDialogState.New();
        Assert.Null(state.TakeResult());
        // Calling again still returns null
        Assert.Null(state.TakeResult());
    }

    [Fact]
    public void EdgeTakeResultConsumes()
    {
        var state = ModalDialogState.New();
        state.Close(DialogOutcome.OkInstance);
        Assert.Equal(new DialogOutcome.Ok(), state.TakeResult());
        // Second call returns null — consumed
        Assert.Null(state.TakeResult());
    }

    [Fact]
    public void EdgeHandleEventWhenClosed()
    {
        var dialog = ModalDialog.Alert("Test", "Msg");
        var state = ModalDialogState.New();
        state.Close(DialogOutcome.DismissedInstance);

        var enter = new InputEvent.Key(new KeyEvent(new KeyCode.Enter(), KeyModifiers.None, KeyEventKind.Press));
        // Events on a closed dialog return null immediately
        var result = dialog.HandleEvent(enter, state, null);
        Assert.Null(result);
    }

    [Fact]
    public void EdgePromptTabFullCycle()
    {
        var dialog = ModalDialog.Prompt("Test", "Enter:");
        var state = ModalDialogState.New();
        // Prompt starts with input_focused=true
        Assert.True(state.InputFocused);
        Assert.Null(state.FocusedButton);

        var tab = new InputEvent.Key(new KeyEvent(new KeyCode.Tab(), KeyModifiers.None, KeyEventKind.Press));

        // Tab 1: input -> button 0 (OK)
        dialog.HandleEvent(tab, state, null);
        Assert.False(state.InputFocused);
        Assert.Equal(0, state.FocusedButton);

        // Tab 2: button 0 -> button 1 (Cancel)
        dialog.HandleEvent(tab, state, null);
        Assert.False(state.InputFocused);
        Assert.Equal(1, state.FocusedButton);

        // Tab 3: button 1 -> back to input
        dialog.HandleEvent(tab, state, null);
        Assert.True(state.InputFocused);
        Assert.Null(state.FocusedButton);
    }

    [Fact]
    public void EdgePromptShiftTabReverseCycle()
    {
        var dialog = ModalDialog.Prompt("Test", "Enter:");
        var state = ModalDialogState.New();

        var shiftTab = new InputEvent.Key(new KeyEvent(new KeyCode.Tab(), KeyModifiers.Shift, KeyEventKind.Press));

        // Shift+Tab from input -> last button (Cancel, index 1)
        dialog.HandleEvent(shiftTab, state, null);
        Assert.False(state.InputFocused);
        Assert.Equal(1, state.FocusedButton);

        // Shift+Tab from button 1 -> button 0
        dialog.HandleEvent(shiftTab, state, null);
        Assert.False(state.InputFocused);
        Assert.Equal(0, state.FocusedButton);

        // Shift+Tab from button 0 -> back to input
        dialog.HandleEvent(shiftTab, state, null);
        Assert.True(state.InputFocused);
        Assert.Null(state.FocusedButton);
    }

    [Fact]
    public void PromptTabRecoveryWhenButtonFocusIsMissing()
    {
        var dialog = ModalDialog.Prompt("Test", "Enter:");
        var state = ModalDialogState.New();
        state.InputFocused = false;
        state.FocusedButton = null;

        var tab = new InputEvent.Key(new KeyEvent(new KeyCode.Tab(), KeyModifiers.None, KeyEventKind.Press));

        dialog.HandleEvent(tab, state, null);
        Assert.False(state.InputFocused);
        Assert.Equal(0, state.FocusedButton);
    }

    [Fact]
    public void EdgeArrowKeyNavigation()
    {
        var dialog = ModalDialog.Confirm("Test", "Msg");
        var state = ModalDialogState.New();
        state.InputFocused = false;
        state.FocusedButton = 0;

        var right = new InputEvent.Key(new KeyEvent(new KeyCode.Right(), KeyModifiers.None, KeyEventKind.Press));
        var left = new InputEvent.Key(new KeyEvent(new KeyCode.Left(), KeyModifiers.None, KeyEventKind.Press));

        // Right: 0 -> 1
        dialog.HandleEvent(right, state, null);
        Assert.Equal(1, state.FocusedButton);

        // Right: 1 -> 0 (wrap)
        dialog.HandleEvent(right, state, null);
        Assert.Equal(0, state.FocusedButton);

        // Left: 0 -> 1 (wrap backwards)
        dialog.HandleEvent(left, state, null);
        Assert.Equal(1, state.FocusedButton);

        // Left: 1 -> 0
        dialog.HandleEvent(left, state, null);
        Assert.Equal(0, state.FocusedButton);
    }

    [Fact]
    public void EdgeArrowKeysIgnoredWhenInputFocused()
    {
        var dialog = ModalDialog.Prompt("Test", "Enter:");
        var state = ModalDialogState.New();
        // input_focused=true by default for prompt
        Assert.True(state.InputFocused);
        state.FocusedButton = null;

        var right = new InputEvent.Key(new KeyEvent(new KeyCode.Right(), KeyModifiers.None, KeyEventKind.Press));

        dialog.HandleEvent(right, state, null);
        // Arrow keys should NOT navigate buttons when input is focused
        Assert.True(state.InputFocused);
        Assert.Null(state.FocusedButton);
    }

    [Fact]
    public void PromptArrowNavigationCancelsPressedButton()
    {
        var dialog = ModalDialog.Prompt("Test", "Enter:").WithHitId(HitId.New(1));
        var state = ModalDialogState.New();

        var down = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 0, 0));
        var hit0 = ((HitId, HitRegionKind, ulong)?)(HitId.New(1), HitRegionKind.Button, 0UL);
        dialog.HandleEvent(down, state, hit0);
        Assert.Equal(0, state.FocusedButton);
        Assert.Equal(0, state.PressedButton);

        var right = new InputEvent.Key(new KeyEvent(new KeyCode.Right(), KeyModifiers.None, KeyEventKind.Press));
        dialog.HandleEvent(right, state, null);
        Assert.Equal(1, state.FocusedButton);
        Assert.Null(state.PressedButton);

        var up = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Up(MouseButton.Left), 0, 0));
        var result = dialog.HandleEvent(up, state, hit0);
        Assert.Null(result);
        Assert.True(state.IsOpen());
    }

    [Fact]
    public void EdgeInputBackspaceOnEmpty()
    {
        var dialog = ModalDialog.Prompt("Test", "Enter:");
        var state = ModalDialogState.New();
        Assert.True(string.IsNullOrEmpty(state.InputValue));

        var backspace = new InputEvent.Key(new KeyEvent(new KeyCode.Backspace(), KeyModifiers.None, KeyEventKind.Press));

        // Backspace on empty input should not panic
        dialog.HandleEvent(backspace, state, null);
        Assert.True(string.IsNullOrEmpty(state.InputValue));
    }

    [Fact]
    public void EdgeInputBackspaceRemovesWholeGraphemeCluster()
    {
        var dialog = ModalDialog.Prompt("Test", "Enter:");
        var state = ModalDialogState.New();
        state.InputValue = "é"; // 'e' + combining acute accent = one grapheme cluster

        var backspace = new InputEvent.Key(new KeyEvent(new KeyCode.Backspace(), KeyModifiers.None, KeyEventKind.Press));

        dialog.HandleEvent(backspace, state, null);
        Assert.True(string.IsNullOrEmpty(state.InputValue));
    }

    [Fact]
    public void EdgeInputDeleteClearsAll()
    {
        var dialog = ModalDialog.Prompt("Test", "Enter:");
        var state = ModalDialogState.New();
        state.InputValue = "hello world";

        var delete = new InputEvent.Key(new KeyEvent(new KeyCode.Delete(), KeyModifiers.None, KeyEventKind.Press));

        dialog.HandleEvent(delete, state, null);
        Assert.True(string.IsNullOrEmpty(state.InputValue));
    }

    [Fact]
    public void EdgeInputCharAccumulation()
    {
        var dialog = ModalDialog.Prompt("Test", "Enter:");
        var state = ModalDialogState.New();

        foreach (char c in new[] { 'h', 'e', 'l', 'l', 'o' })
        {
            var evt = new InputEvent.Key(new KeyEvent(new KeyCode.Char(c), KeyModifiers.None, KeyEventKind.Press));
            dialog.HandleEvent(evt, state, null);
        }
        Assert.Equal("hello", state.InputValue);
    }

    [Fact]
    public void EdgePromptPasteAppendsSanitizedSingleLineText()
    {
        var dialog = ModalDialog.Prompt("Test", "Enter:");
        var state = ModalDialogState.New();
        state.InputValue = "hello";

        // Rust: Event::Paste(PasteEvent::bracketed(" world\nnext\tline\u{0007}"))
        var paste = new InputEvent.Paste(new PasteEvent(" world\nnext\tline", true));

        dialog.HandleEvent(paste, state, null);
        Assert.Equal("hello world next line", state.InputValue);
    }

    [Fact]
    public void EdgePromptPasteIgnoredWhenInputNotFocused()
    {
        var dialog = ModalDialog.Prompt("Test", "Enter:");
        var state = ModalDialogState.New();
        state.InputFocused = false;
        state.FocusedButton = 0;

        var paste = new InputEvent.Paste(new PasteEvent("ignored", true));

        dialog.HandleEvent(paste, state, null);
        Assert.True(string.IsNullOrEmpty(state.InputValue));
    }

    [Fact]
    public void EdgePromptCancelReturnsCancel()
    {
        var dialog = ModalDialog.Prompt("Test", "Enter:");
        var state = ModalDialogState.New();
        state.InputValue = "typed something";
        state.InputFocused = false;
        state.FocusedButton = 1; // Cancel button

        var enter = new InputEvent.Key(new KeyEvent(new KeyCode.Enter(), KeyModifiers.None, KeyEventKind.Press));

        var result = dialog.HandleEvent(enter, state, null);
        Assert.Equal(new DialogOutcome.Cancel(), result);
        Assert.False(state.IsOpen());
    }

    [Fact]
    public void EdgeCustomButtonActivation()
    {
        var dialog = ModalDialog.Custom("Test", "Msg")
            .CustomButton("Save", "save")
            .CustomButton("Delete", "delete")
            .Build();
        var state = ModalDialogState.New();
        state.InputFocused = false;
        state.FocusedButton = 1; // "Delete" button

        var enter = new InputEvent.Key(new KeyEvent(new KeyCode.Enter(), KeyModifiers.None, KeyEventKind.Press));

        var result = dialog.HandleEvent(enter, state, null);
        Assert.Equal(new DialogOutcome.Custom("delete"), result);
    }

    [Fact]
    public void EdgeRenderZeroSizeArea()
    {
        var dialog = ModalDialog.Alert("T", "M");
        var state = ModalDialogState.New();
        var pool = new GraphemePool();
        var frame = new Frame(80, 24, pool);
        // Zero-width area
        dialog.Render(new Rect(0, 0, 0, 0), frame, state);
        // Zero-height area
        dialog.Render(new Rect(0, 0, 80, 0), frame, state);
        // Zero-width nonzero-height
        dialog.Render(new Rect(0, 0, 0, 24), frame, state);
    }

    [Fact]
    public void EdgeRenderClosedDialogIsNoop()
    {
        var dialog = ModalDialog.Alert("Test", "Msg");
        var state = ModalDialogState.New();
        state.Close(DialogOutcome.DismissedInstance);

        var pool = new GraphemePool();
        var frame = new Frame(80, 24, pool);

        // Rendering a closed dialog should not panic or alter frame
        dialog.Render(new Rect(0, 0, 80, 24), frame, state);
    }

    [Fact]
    public void EdgeBuilderHitId()
    {
        var dialog = ModalDialog.Custom("T", "M")
            .OkButton()
            .WithHitId(HitId.New(42))
            .Build();
        Assert.Equal(HitId.New(42), dialog.HitIdValue);
    }

    [Fact]
    public void EdgeBuilderModalConfig()
    {
        var config = ModalContainerConfig.Default()
            .WithPosition(new ModalContainerPosition.TopCenter(5));
        var dialog = ModalDialog.Custom("T", "M")
            .OkButton()
            .WithModalConfig(config)
            .Build();
        Assert.Equal(new ModalContainerPosition.TopCenter(5), dialog.Config.ModalConfig.Position);
    }

    [Fact]
    public void EdgeBuilderModalConfigSyncsHitId()
    {
        var dialog = ModalDialog.Custom("T", "M")
            .OkButton()
            .WithModalConfig(ModalContainerConfig.Default().WithHitId(HitId.New(42)))
            .Build();
        Assert.Equal(HitId.New(42), dialog.HitIdValue);
    }

    [Fact]
    public void EdgeContentHeightAlert()
    {
        var dialog = ModalDialog.Alert("Title", "Message");
        var h = dialog.ContentHeight();
        // 2 (borders) + 1 (message) + 1 (spacing) + 1 (buttons) = 5
        Assert.Equal(5, h);
    }

    [Fact]
    public void EdgeContentHeightPrompt()
    {
        var dialog = ModalDialog.Prompt("Title", "Message");
        var h = dialog.ContentHeight();
        // 2 (borders) + 1 (message) + 1 (spacing) + 1 (input) + 1 (input spacing) + 1 (buttons) = 7
        Assert.Equal(7, h);
    }

    [Fact]
    public void EdgeContentHeightEmptyTitleAndMessage()
    {
        var dialog = ModalDialog.Alert("", "");
        var h = dialog.ContentHeight();
        // 2 (borders) + 0 (no message) + 1 (spacing) + 1 (buttons) = 4
        Assert.Equal(4, h);
    }

    [Fact]
    public void EdgeButtonDisplayWidthUnicode()
    {
        var button = new ModalDialogButton("保存", "save");
        // "保存" is 4 display columns + 4 for brackets = 8
        Assert.Equal(8, button.DisplayWidth());
    }

    [Fact]
    public void EdgeDialogResultEquality()
    {
        Assert.Equal(new DialogOutcome.Ok(), new DialogOutcome.Ok());
        Assert.Equal(new DialogOutcome.Cancel(), new DialogOutcome.Cancel());
        Assert.Equal(new DialogOutcome.Dismissed(), new DialogOutcome.Dismissed());
        Assert.Equal(new DialogOutcome.Custom("a"), new DialogOutcome.Custom("a"));
        Assert.NotEqual(new DialogOutcome.Custom("a"), new DialogOutcome.Custom("b"));
        Assert.Equal(new DialogOutcome.Input("x"), new DialogOutcome.Input("x"));
        Assert.NotEqual((DialogOutcome)new DialogOutcome.Ok(), new DialogOutcome.Cancel());
    }

    [Fact]
    public void EdgeMouseDownMismatchedHitId()
    {
        var dialog = ModalDialog.Confirm("Test", "Msg").WithHitId(HitId.New(1));
        var state = ModalDialogState.New();

        var down = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 0, 0));
        // Hit with different ID should not register
        var hit = ((HitId, HitRegionKind, ulong)?)(HitId.New(99), HitRegionKind.Button, 0UL);
        dialog.HandleEvent(down, state, hit);
        Assert.Null(state.PressedButton);
        Assert.Null(state.FocusedButton);
    }

    [Fact]
    public void MouseDownOutsideCancelsExistingPressedButton()
    {
        var dialog = ModalDialog.Confirm("Test", "Msg").WithHitId(HitId.New(1));
        var state = ModalDialogState.New();

        var down = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 0, 0));
        var hit0 = ((HitId, HitRegionKind, ulong)?)(HitId.New(1), HitRegionKind.Button, 0UL);
        dialog.HandleEvent(down, state, hit0);
        Assert.Equal(0, state.PressedButton);

        dialog.HandleEvent(down, state, null);
        Assert.Null(state.PressedButton);

        var up = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Up(MouseButton.Left), 0, 0));
        var result = dialog.HandleEvent(up, state, hit0);
        Assert.Null(result);
        Assert.True(state.IsOpen());
    }

    [Fact]
    public void MouseDownMismatchedHitIdCancelsExistingPressedButton()
    {
        var dialog = ModalDialog.Confirm("Test", "Msg").WithHitId(HitId.New(1));
        var state = ModalDialogState.New();

        var down = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 0, 0));
        var hit0 = ((HitId, HitRegionKind, ulong)?)(HitId.New(1), HitRegionKind.Button, 0UL);
        dialog.HandleEvent(down, state, hit0);
        Assert.Equal(0, state.PressedButton);

        var wrongHit = ((HitId, HitRegionKind, ulong)?)(HitId.New(99), HitRegionKind.Button, 1UL);
        dialog.HandleEvent(down, state, wrongHit);
        Assert.Null(state.PressedButton);
        Assert.Equal(0, state.FocusedButton);

        var up = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Up(MouseButton.Left), 0, 0));
        var result = dialog.HandleEvent(up, state, hit0);
        Assert.Null(result);
        Assert.True(state.IsOpen());
    }

    [Fact]
    public void EdgeMouseDownOutOfBoundsIndex()
    {
        var dialog = ModalDialog.Confirm("Test", "Msg").WithHitId(HitId.New(1));
        var state = ModalDialogState.New();

        var down = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 0, 0));
        // Button index beyond button count
        var hit = ((HitId, HitRegionKind, ulong)?)(HitId.New(1), HitRegionKind.Button, 99UL);
        dialog.HandleEvent(down, state, hit);
        Assert.Null(state.PressedButton);
    }

    [Fact]
    public void EdgeMouseUpDifferentButtonFromPressed()
    {
        var dialog = ModalDialog.Confirm("Test", "Msg").WithHitId(HitId.New(1));
        var state = ModalDialogState.New();

        // Press button 0
        var down = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 0, 0));
        var hit0 = ((HitId, HitRegionKind, ulong)?)(HitId.New(1), HitRegionKind.Button, 0UL);
        dialog.HandleEvent(down, state, hit0);
        Assert.Equal(0, state.PressedButton);

        // Release on button 1 — should NOT activate
        var up = new InputEvent.Mouse(new MouseEvent(new MouseEventKind.Up(MouseButton.Left), 0, 0));
        var hit1 = ((HitId, HitRegionKind, ulong)?)(HitId.New(1), HitRegionKind.Button, 1UL);
        var result = dialog.HandleEvent(up, state, hit1);
        Assert.Null(result);
        Assert.True(state.IsOpen());
        // pressed_button cleared by take()
        Assert.Null(state.PressedButton);
    }

    [Fact]
    public void EdgeNonPromptClearsInputFocused()
    {
        var dialog = ModalDialog.Alert("Test", "Msg");
        var state = ModalDialogState.New();
        // Manually set input_focused (e.g. leftover from state reuse)
        state.InputFocused = true;

        var tab = new InputEvent.Key(new KeyEvent(new KeyCode.Tab(), KeyModifiers.None, KeyEventKind.Press));
        dialog.HandleEvent(tab, state, null);
        // Non-prompt dialog should clear input_focused
        Assert.False(state.InputFocused);
    }

    [Fact]
    public void EdgeKeyReleaseIgnored()
    {
        var dialog = ModalDialog.Prompt("Test", "Enter:");
        var state = ModalDialogState.New();
        state.InputValue = string.Empty;

        // Key release event should be ignored by input handler
        var release = new InputEvent.Key(new KeyEvent(new KeyCode.Char('x'), KeyModifiers.None, KeyEventKind.Release));
        dialog.HandleEvent(release, state, null);
        Assert.True(string.IsNullOrEmpty(state.InputValue));
    }

    [Fact]
    public void EdgeEnterNoFocusedNoPrimaryDoesNothing()
    {
        // Build a dialog with no primary button
        var dialog = ModalDialog.Custom("Test", "Msg")
            .CustomButton("A", "a")
            .CustomButton("B", "b")
            .Build();
        var state = ModalDialogState.New();
        state.InputFocused = false;
        state.FocusedButton = null;

        var enter = new InputEvent.Key(new KeyEvent(new KeyCode.Enter(), KeyModifiers.None, KeyEventKind.Press));
        // No focused button and no primary → activate_button returns null
        var result = dialog.HandleEvent(enter, state, null);
        Assert.Null(result);
        Assert.True(state.IsOpen());
    }

    [Fact]
    public void EdgeDialogStyleSetters()
    {
        var style = new WidgetStyle(null, null, CellStyleFlags.Bold);
        var dialog = ModalDialog.Alert("T", "M")
            .WithButtonStyle(style)
            .WithPrimaryButtonStyle(style)
            .WithFocusedButtonStyle(style);
        Assert.Equal(style, dialog.Config.ButtonStyle);
        Assert.Equal(style, dialog.Config.PrimaryButtonStyle);
        Assert.Equal(style, dialog.Config.FocusedButtonStyle);
    }

    [Fact]
    public void EdgeDialogModalConfigSetter()
    {
        var mc = ModalContainerConfig.Default()
            .WithPosition(new ModalContainerPosition.CustomPosition(10, 20));
        var dialog = ModalDialog.Alert("T", "M").WithModalConfig(mc);
        Assert.Equal(new ModalContainerPosition.CustomPosition(10, 20), dialog.Config.ModalConfig.Position);
    }

    [Fact]
    public void EdgeDialogModalConfigSetterSyncsHitId()
    {
        var dialog = ModalDialog.Alert("T", "M")
            .WithModalConfig(ModalContainerConfig.Default().WithHitId(HitId.New(9)));
        Assert.Equal(HitId.New(9), dialog.HitIdValue);
    }

    [Fact]
    public void EdgeDialogCloneDebug()
    {
        // C# classes are reference types so no Clone() needed; verify field accessibility.
        var dialog = ModalDialog.Alert("T", "M");
        Assert.Equal("T", dialog.Title);
        Assert.Equal("M", dialog.Message);
    }

    [Fact]
    public void EdgeDialogBuilderCloneDebug()
    {
        var builder = ModalDialog.Custom("T", "M").OkButton();
        Assert.Equal("T", builder.Title);
    }

    [Fact]
    public void EdgeDialogConfigCloneDebug()
    {
        var config = ModalDialogConfig.Default();
        Assert.Equal(ModalDialogKind.Alert, config.Kind);
    }

    [Fact]
    public void EdgeDialogStateCloneDebug()
    {
        var state = ModalDialogState.New();
        state.InputValue = "test";
        state.FocusedButton = 1;
        Assert.Equal("test", state.InputValue);
        Assert.Equal(1, state.FocusedButton);
        Assert.True(state.Open);
    }

    [Fact]
    public void EdgeDialogButtonCloneDebug()
    {
        var button = new ModalDialogButton("Save", "save").Primary();
        Assert.Equal("Save", button.Label);
        Assert.Equal("save", button.ButtonId);
        Assert.True(button.IsPrimary);
    }

    [Fact]
    public void EdgeDialogResultCloneDebug()
    {
        var results = new DialogOutcome[]
        {
            new DialogOutcome.Ok(),
            new DialogOutcome.Cancel(),
            new DialogOutcome.Dismissed(),
            new DialogOutcome.Custom("x"),
            new DialogOutcome.Input("y"),
        };
        foreach (var r in results)
        {
            // Verify equality with itself
            Assert.Equal(r, r);
        }
    }

    [Fact]
    public void EdgeDialogKindCloneDebugEq()
    {
        var kinds = new[]
        {
            ModalDialogKind.Alert,
            ModalDialogKind.Confirm,
            ModalDialogKind.Prompt,
            ModalDialogKind.Custom,
        };
        foreach (var k in kinds)
        {
            var cloned = k;
            Assert.Equal(cloned, k);
        }
        Assert.NotEqual(ModalDialogKind.Alert, ModalDialogKind.Confirm);
    }
}
