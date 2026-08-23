// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-a11y/src/preferences.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c

using System.Globalization;
using System.Text;

namespace FrankenTui.A11y;

/// <summary>Host-agnostic accessibility mode flags.</summary>
public readonly record struct AccessibilityPreferences(
    bool ReducedMotion,
    bool HighContrast,
    bool ForcedColors)
{
    public static AccessibilityPreferences None() => new(false, false, false);

    public static AccessibilityPreferences All() => new(true, true, true);

    public static AccessibilityPreferences FromMediaQueries(
        bool prefersReducedMotion,
        bool prefersContrastMore,
        bool forcedColorsActive) =>
        new(prefersReducedMotion, prefersContrastMore, forcedColorsActive);

    public AccessibilityPreferences WithReducedMotion(bool on) =>
        this with { ReducedMotion = on };

    public AccessibilityPreferences WithHighContrast(bool on) =>
        this with { HighContrast = on };

    public AccessibilityPreferences WithForcedColors(bool on) =>
        this with { ForcedColors = on };

    public bool Any => ReducedMotion || HighContrast || ForcedColors;

    public MotionProfile MotionProfile() =>
        FrankenTui.A11y.MotionProfile.FromPreferences(this);

    public ContrastProfile ContrastProfile() =>
        FrankenTui.A11y.ContrastProfile.FromPreferences(this);

    /// <summary>Returns stable, ordered CSS data-attribute pairs.</summary>
    public IReadOnlyList<(string Name, bool Value)> WebDataset() =>
    [
        ("data-ftui-reduced-motion", ReducedMotion),
        ("data-ftui-high-contrast", HighContrast),
        ("data-ftui-forced-colors", ForcedColors),
    ];

    /// <summary>Serializes the preferences and derived policies deterministically.</summary>
    public string ToJsonl(string correlationId)
    {
        ArgumentNullException.ThrowIfNull(correlationId);
        MotionProfile motion = MotionProfile();
        ContrastProfile contrast = ContrastProfile();
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{{\"event\":\"a11y_preferences\",\"correlation_id\":\"{EscapeJson(correlationId)}\"," +
            $"\"reduced_motion\":{LowerBool(ReducedMotion)},\"high_contrast\":{LowerBool(HighContrast)}," +
            $"\"forced_colors\":{LowerBool(ForcedColors)},\"animation_scale_bps\":{motion.AnimationScaleBps}," +
            $"\"announcement_coalescing\":{LowerBool(motion.AnnouncementCoalescing)}," +
            $"\"suppress_transient\":{LowerBool(motion.SuppressTransient)}," +
            $"\"min_contrast_ratio_x100\":{contrast.MinContrastRatioX100}," +
            $"\"drop_dim\":{LowerBool(contrast.DropDim)}," +
            $"\"system_colors_only\":{LowerBool(contrast.SystemColorsOnly)}}}");
    }

    private static string LowerBool(bool value) => value ? "true" : "false";

    private static string EscapeJson(string input)
    {
        var output = new StringBuilder(input.Length + 2);
        foreach (char character in input)
        {
            switch (character)
            {
                case '"': output.Append("\\\""); break;
                case '\\': output.Append("\\\\"); break;
                case '\n': output.Append("\\n"); break;
                case '\r': output.Append("\\r"); break;
                case '\t': output.Append("\\t"); break;
                default:
                    if (character < 0x20)
                    {
                        output.Append("\\u");
                        output.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        output.Append(character);
                    }
                    break;
            }
        }
        return output.ToString();
    }
}

/// <summary>Deterministic motion policy derived from accessibility preferences.</summary>
public readonly record struct MotionProfile(
    bool ReducedMotion,
    ushort AnimationScaleBps,
    bool AnnouncementCoalescing,
    bool SuppressTransient)
{
    public static MotionProfile Full => new(false, 10_000, false, false);

    public static MotionProfile FromPreferences(AccessibilityPreferences preferences) =>
        preferences.ReducedMotion
            ? new MotionProfile(true, 0, true, true)
            : Full;

    public uint ScaledDurationMs(uint baseMs) =>
        (uint)(((ulong)baseMs * AnimationScaleBps) / 10_000UL);

    /// <summary>Applies deterministic reduced-motion filtering to a batch.</summary>
    public MotionFilteredAnnouncements FilterAnnouncements(ScreenReaderAnnouncements input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!AnnouncementCoalescing)
        {
            return new MotionFilteredAnnouncements(
                Array.AsReadOnly(input.Announcements.ToArray()),
                input.DroppedCount,
                0,
                0);
        }

        var lastIndexForNode = new Dictionary<ulong, int>();
        for (int index = 0; index < input.Announcements.Count; index++)
        {
            ScreenReaderAnnouncement announcement = input.Announcements[index];
            if (IsMotionLike(announcement.Reason) && announcement.NodeId is ulong nodeId)
                lastIndexForNode[nodeId] = index;
        }

        var announcements = new List<ScreenReaderAnnouncement>(input.Announcements.Count);
        int coalescedCount = 0;
        int downgradedCount = 0;
        for (int index = 0; index < input.Announcements.Count; index++)
        {
            ScreenReaderAnnouncement announcement = input.Announcements[index];
            if (!IsMotionLike(announcement.Reason))
            {
                announcements.Add(announcement);
                continue;
            }

            if (announcement.NodeId is ulong nodeId && lastIndexForNode[nodeId] != index)
            {
                coalescedCount++;
                continue;
            }

            ScreenReaderAnnouncement kept = announcement;
            if (SuppressTransient && kept.Urgency == LiveRegion.Assertive)
            {
                kept = kept with { Urgency = LiveRegion.Polite };
                downgradedCount++;
            }
            announcements.Add(kept);
        }

        return new MotionFilteredAnnouncements(
            announcements.AsReadOnly(),
            input.DroppedCount,
            coalescedCount,
            downgradedCount);
    }

    private static bool IsMotionLike(AnnouncementReason reason) => reason is
        AnnouncementReason.LiveContentChanged or AnnouncementReason.LiveRegionAdded;
}

/// <summary>Result of applying a motion profile to an announcement batch.</summary>
public sealed class MotionFilteredAnnouncements : IEquatable<MotionFilteredAnnouncements>
{
    public MotionFilteredAnnouncements(
        IReadOnlyList<ScreenReaderAnnouncement> announcements,
        int droppedCount,
        int coalescedCount,
        int downgradedCount)
    {
        Announcements = announcements;
        DroppedCount = droppedCount;
        CoalescedCount = coalescedCount;
        DowngradedCount = downgradedCount;
    }

    public IReadOnlyList<ScreenReaderAnnouncement> Announcements { get; }
    public int DroppedCount { get; }
    public int CoalescedCount { get; }
    public int DowngradedCount { get; }

    public bool Equals(MotionFilteredAnnouncements? other) =>
        other is not null &&
        DroppedCount == other.DroppedCount &&
        CoalescedCount == other.CoalescedCount &&
        DowngradedCount == other.DowngradedCount &&
        Announcements.SequenceEqual(other.Announcements);

    public override bool Equals(object? obj) => Equals(obj as MotionFilteredAnnouncements);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(DroppedCount);
        hash.Add(CoalescedCount);
        hash.Add(DowngradedCount);
        foreach (ScreenReaderAnnouncement announcement in Announcements)
            hash.Add(announcement);
        return hash.ToHashCode();
    }
}

/// <summary>Deterministic contrast intent derived from accessibility preferences.</summary>
public readonly record struct ContrastProfile(
    bool HighContrast,
    bool ForcedColors,
    ushort MinContrastRatioX100,
    bool DropDim,
    bool SystemColorsOnly)
{
    public const ushort WcagAaX100 = 450;
    public const ushort WcagAaaX100 = 700;

    public static ContrastProfile Default => new(false, false, WcagAaX100, false, false);

    public static ContrastProfile FromPreferences(AccessibilityPreferences preferences)
    {
        bool elevated = preferences.HighContrast || preferences.ForcedColors;
        return new ContrastProfile(
            elevated,
            preferences.ForcedColors,
            elevated ? WcagAaaX100 : WcagAaX100,
            elevated,
            preferences.ForcedColors);
    }
}
