namespace FrankenTui.Extras;

/// <summary>File size formatting utility. Matches upstream filesize.</summary>
public static class FileSize
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    public static string Format(long bytes)
    {
        if (bytes < 0) return "0 B";
        double size = bytes;
        var unitIndex = 0;
        while (size >= 1024 && unitIndex < Units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }
        return unitIndex == 0 ? $"{bytes} {Units[unitIndex]}" : $"{size:F1} {Units[unitIndex]}";
    }

    public static string Format(ulong bytes) => Format((long)bytes);
}
