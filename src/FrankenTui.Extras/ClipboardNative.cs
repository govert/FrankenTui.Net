// Native clipboard backend using platform APIs.
// Windows: Win32 clipboard (OpenClipboard/SetClipboardData/GetClipboardData)
// macOS: pbcopy/pbpaste (uses NSPasteboard internally, no subprocess advantage to P/Invoke)
// Linux: xclip/xsel/wl-copy/wl-paste (tool-based, already the standard way)
//
// Implements the equivalent of Rust's arboard crate for native clipboard access
// without subprocess overhead on Windows.

using System.Runtime.InteropServices;
using System.Text;

namespace FrankenTui.Extras;

/// <summary>Platform-native clipboard access. Avoids subprocess overhead on Windows.</summary>
internal static class ClipboardNative
{
    // ============================================================
    // Windows Win32 clipboard API
    // ============================================================

    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 2;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsClipboardFormatAvailable(uint uFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern UIntPtr GlobalSize(IntPtr hMem);

    // ============================================================
    // Public API
    // ============================================================

    /// <summary>Whether a native backend is available on this platform.</summary>
    public static bool IsAvailable => OperatingSystem.IsWindows()
        || (OperatingSystem.IsMacOS() && CmdExists("pbcopy"))
        || OperatingSystem.IsLinux();

    /// <summary>Probe whether a command is runnable on the PATH (used for tool-based backends).</summary>
    private static bool CmdExists(string command)
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "which",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });
            if (process is null)
                return false;
            process.WaitForExit(1000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Write text to the system clipboard using native APIs.</summary>
    public static bool SetText(string text)
    {
        if (OperatingSystem.IsWindows())
            return SetTextWindows(text);
        // macOS/Linux: the tool-based approach already uses native tools
        return false; // fall through to tool-based
    }

    /// <summary>Read text from the system clipboard using native APIs.</summary>
    public static string? GetText()
    {
        if (OperatingSystem.IsWindows())
            return GetTextWindows();
        return null; // fall through to tool-based
    }

    /// <summary>Clear the system clipboard on Windows.</summary>
    public static void ClearClipboard()
    {
        if (OperatingSystem.IsWindows())
            ClearClipboardWindows();
    }

    // ============================================================
    // Windows implementation
    // ============================================================

    private static bool SetTextWindows(string text)
    {
        if (!OpenClipboard(IntPtr.Zero))
            return false;
        try
        {
            if (!EmptyClipboard())
                return false;

            var byteCount = Encoding.Unicode.GetByteCount(text) + 2; // + null terminator
            var hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)byteCount);
            if (hGlobal == IntPtr.Zero)
                return false;

            var ptr = GlobalLock(hGlobal);
            if (ptr == IntPtr.Zero)
            {
                GlobalFree(hGlobal);
                return false;
            }

            try
            {
                var bytes = Encoding.Unicode.GetBytes(text + '\0');
                Marshal.Copy(bytes, 0, ptr, bytes.Length);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }

            var result = SetClipboardData(CF_UNICODETEXT, hGlobal);
            if (result == IntPtr.Zero)
                GlobalFree(hGlobal);

            return result != IntPtr.Zero;
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static string? GetTextWindows()
    {
        if (!OpenClipboard(IntPtr.Zero))
            return null;
        try
        {
            if (!IsClipboardFormatAvailable(CF_UNICODETEXT))
                return null;

            var hData = GetClipboardData(CF_UNICODETEXT);
            if (hData == IntPtr.Zero)
                return null;

            var ptr = GlobalLock(hData);
            if (ptr == IntPtr.Zero)
                return null;

            try
            {
                var size = GlobalSize(hData).ToUInt64();
                if (size == 0) return null;

                // Find null terminator within the global memory block
                int charCount = 0;
                while (charCount + 1 < (int)size)
                {
                    var w = Marshal.ReadInt16(ptr, charCount * 2);
                    if (w == 0) break;
                    charCount++;
                }

                if (charCount == 0) return string.Empty;
                var chars = new char[charCount];
                var bytes = new byte[charCount * 2];
                Marshal.Copy(ptr, bytes, 0, bytes.Length);
                Encoding.Unicode.GetChars(bytes, 0, bytes.Length, chars, 0);
                return new string(chars);
            }
            finally
            {
                GlobalUnlock(hData);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static void ClearClipboardWindows()
    {
        if (!OpenClipboard(IntPtr.Zero))
            return;
        try
        {
            EmptyClipboard();
        }
        finally
        {
            CloseClipboard();
        }
    }
}
