// Upstream source: .external/frankentui/crates/ftui-demo-showcase/src/data.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Faithful 1-1 port of the simulated system data generator. All values are
// deterministic for a given tick count — no system time or external randomness.

namespace FrankenTui.Demo.Showcase;

/// <summary>Deterministic pseudo-random helpers. Port of det_hash / det_float / det_range.</summary>
internal static class DetRandom
{
    /// <summary>Splitmix64-style scramble. Port of <c>det_hash</c>.</summary>
    public static ulong Hash(ulong seed)
    {
        ulong z = seed + 0x9e37_79b9_7f4a_7c15;
        z = (z ^ (z >> 30)) * 0xbf58_476d_1ce4_e5b9;
        z = (z ^ (z >> 27)) * 0x94d0_49bb_1331_11eb;
        return z ^ (z >> 31);
    }

    /// <summary>Deterministic double in [0.0, 1.0). Port of <c>det_float</c>.</summary>
    public static double Float(ulong seed) =>
        (Hash(seed) >> 11) / (double)(1UL << 53);

    /// <summary>Deterministic double in [lo, hi). Port of <c>det_range</c>.</summary>
    public static double Range(ulong seed, double lo, double hi) =>
        lo + Float(seed) * (hi - lo);
}

/// <summary>System-monitor style simulated data, updated each tick.
/// Port of <c>SimulatedData</c> in data.rs.</summary>
internal sealed class SimulatedData
{
    const int CpuHistoryCap    = 60;
    const int MemoryHistoryCap = 60;
    const int NetworkHistoryCap = 60;
    const int TargetProcessCount = 20;
    const int MaxAlerts = 50;
    const ulong AlertInterval = 20;

    static readonly string[] DiskCategories = ["System", "Applications", "Documents", "Media", "Cache"];

    static readonly string[] ProcessNames =
    [
        "systemd", "ftui-demo", "sshd", "postgres", "nginx", "redis-server",
        "node", "cargo", "rustc", "python3", "dockerd", "containerd",
        "tmux", "zsh", "htop", "git", "rg", "fd", "bat", "asupersync-rt",
        "journald", "dbus-daemon"
    ];

    static readonly string[] AlertMessages =
    [
        "CPU spike detected on core 3",
        "Memory allocation pool expanded",
        "Network latency increased to 45ms",
        "Disk I/O throughput nominal",
        "Service health check passed",
        "Cache eviction rate elevated",
        "Connection pool at 80% capacity",
        "Garbage collection completed",
        "TLS certificate renewal scheduled",
        "Rate limiter threshold adjusted"
    ];

    private readonly ulong _seed;

    public List<double> CpuHistory     { get; } = new(CpuHistoryCap + 1);
    public List<double> MemoryHistory  { get; } = new(MemoryHistoryCap + 1);
    public List<double> NetworkIn      { get; } = new(NetworkHistoryCap + 1);
    public List<double> NetworkOut     { get; } = new(NetworkHistoryCap + 1);
    public List<(string Name, double Usage)> DiskUsage { get; } = [];
    public List<(uint Pid, string Name, double Cpu, double Mem)> Processes { get; } = [];
    public List<(string Severity, string Message, ulong Timestamp)> Alerts { get; } = [];
    public double EventsPerSecond { get; private set; }

    public SimulatedData(ulong seed = 0)
    {
        _seed = seed;
        DiskUsage.AddRange(DiskCategories.Select(name => (name, 0.0)));
    }

    /// <summary>Advance the simulation by one tick. Port of <c>tick()</c>.</summary>
    public void Tick(ulong tickCount)
    {
        var tick = tickCount + _seed;
        UpdateCpu(tick);
        UpdateMemory(tick);
        UpdateNetwork(tick);
        UpdateDisk(tick);
        UpdateProcesses(tick);
        UpdateAlerts(tick);
        EventsPerSecond = 800.0
            + 400.0 * Math.Sin(tick / 30.0)
            + DetRandom.Range(tick * 7, -50.0, 50.0);
    }

    void UpdateCpu(ulong tick)
    {
        double baseVal = 30.0 + 20.0 * Math.Sin(tick / 50.0);
        double noise = DetRandom.Range(tick * 3, -5.0, 5.0);
        double value = Math.Clamp(baseVal + noise, 0.0, 100.0);
        CpuHistory.Add(value);
        if (CpuHistory.Count > CpuHistoryCap) CpuHistory.RemoveAt(0);
    }

    void UpdateMemory(ulong tick)
    {
        double cycle = (tick % 500) / 500.0;
        double baseVal = 40.0 + 30.0 * cycle;
        double noise = DetRandom.Range(tick * 5, -2.0, 2.0);
        double value = Math.Clamp(baseVal + noise, 0.0, 100.0);
        MemoryHistory.Add(value);
        if (MemoryHistory.Count > MemoryHistoryCap) MemoryHistory.RemoveAt(0);
    }

    void UpdateNetwork(ulong tick)
    {
        double burstIn = DetRandom.Hash(tick * 11) % 10 == 0
            ? DetRandom.Range(tick * 13, 500.0, 2000.0)
            : DetRandom.Range(tick * 13, 10.0, 200.0);
        double burstOut = DetRandom.Hash(tick * 17) % 8 == 0
            ? DetRandom.Range(tick * 19, 300.0, 1500.0)
            : DetRandom.Range(tick * 19, 5.0, 150.0);
        NetworkIn.Add(burstIn);
        NetworkOut.Add(burstOut);
        if (NetworkIn.Count > NetworkHistoryCap) NetworkIn.RemoveAt(0);
        if (NetworkOut.Count > NetworkHistoryCap) NetworkOut.RemoveAt(0);
    }

    void UpdateDisk(ulong tick)
    {
        for (int i = 0; i < DiskUsage.Count; i++)
        {
            double baseVal = i switch { 0 => 65.0, 1 => 45.0, 2 => 30.0, 3 => 55.0, _ => 20.0 };
            double drift = 5.0 * Math.Sin(tick / (100.0 + i * 30.0));
            double noise = DetRandom.Range(tick * 23 + (ulong)i, -1.0, 1.0);
            double value = Math.Clamp(baseVal + drift + noise, 0.0, 100.0);
            DiskUsage[i] = (DiskUsage[i].Name, value);
        }
    }

    void UpdateProcesses(ulong tick)
    {
        Processes.Clear();
        int count = Math.Min(TargetProcessCount, ProcessNames.Length);
        ulong jitter = DetRandom.Hash(tick * 29) % 3;
        int activeCount = DetRandom.Hash(tick * 31) % 2 == 0
            ? Math.Max(count - (int)jitter, TargetProcessCount - 2)
            : Math.Min(count + (int)jitter, ProcessNames.Length);

        for (int i = 0; i < activeCount; i++)
        {
            ulong seedBase = tick * 37 + (ulong)i * 41;
            uint pid = 1000 + (uint)(DetRandom.Hash(seedBase) % 50000);
            double cpu = Math.Round(DetRandom.Range(seedBase + 1, 0.0, 25.0) * 10.0) / 10.0;
            double mem = Math.Round(DetRandom.Range(seedBase + 2, 5.0, 500.0) * 10.0) / 10.0;
            Processes.Add((pid, ProcessNames[i], cpu, mem));
        }
    }

    void UpdateAlerts(ulong tick)
    {
        if (tick % AlertInterval == 0 && tick > 0)
        {
            ulong severitySeed = DetRandom.Hash(tick * 43) % 10;
            string severity = severitySeed switch { 0 or 1 => "Error", 2 or 3 or 4 => "Warning", _ => "Info" };
            int msgIdx = (int)(DetRandom.Hash(tick * 47) % (ulong)AlertMessages.Length);
            Alerts.Add((severity, AlertMessages[msgIdx], tick));
            if (Alerts.Count > MaxAlerts) Alerts.RemoveAt(0);
        }
    }
}
