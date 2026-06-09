// Tests for .external/frankentui/crates/ftui-runtime/src/policy_registry.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c

using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class PolicyRegistryTests
{
    [Fact]
    public void NewHasStandardPolicy()
    {
        var reg = new PolicyRegistry();
        Assert.Equal(PolicyRegistryConstants.StandardPolicy, reg.ActiveName);
        Assert.Equal([PolicyRegistryConstants.StandardPolicy], reg.List());
    }

    [Fact]
    public void RegisterAndSwitch()
    {
        var reg = new PolicyRegistry();
        var custom = PolicyConfig.Default;
        custom.Conformal.Alpha = 0.01;

        reg.Register("custom", custom);
        var evt = reg.SetActive("custom");

        Assert.Equal(PolicyRegistryConstants.StandardPolicy, evt.OldName);
        Assert.Equal("custom", evt.NewName);
        Assert.Equal(0UL, evt.SwitchId);
        Assert.Equal("custom", reg.ActiveName);
        Assert.InRange(reg.ActiveConfig.Conformal.Alpha - 0.01, -1e-15, 1e-15);
    }

    [Fact]
    public void SwitchBackToStandard()
    {
        var reg = new PolicyRegistry();
        reg.Register("custom", PolicyConfig.Default);
        reg.SetActive("custom");

        var evt = reg.SetActive(PolicyRegistryConstants.StandardPolicy);
        Assert.Equal("custom", evt.OldName);
        Assert.Equal(PolicyRegistryConstants.StandardPolicy, evt.NewName);
        Assert.Equal(1UL, evt.SwitchId);
        Assert.Equal(2UL, reg.SwitchCount);
    }

    [Fact]
    public void SwitchToNonexistentFails()
    {
        var reg = new PolicyRegistry();
        Assert.Throws<KeyNotFoundException>(() => reg.SetActive("nonexistent"));
    }

    [Fact]
    public void CannotOverwriteStandard()
    {
        var reg = new PolicyRegistry();
        Assert.Throws<InvalidOperationException>(() =>
            reg.Register(PolicyRegistryConstants.StandardPolicy, PolicyConfig.Default));
    }

    [Fact]
    public void CannotRemoveStandard()
    {
        var reg = new PolicyRegistry();
        Assert.Throws<InvalidOperationException>(() =>
            reg.Remove(PolicyRegistryConstants.StandardPolicy));
    }

    [Fact]
    public void CannotRemoveActive()
    {
        var reg = new PolicyRegistry();
        reg.Register("custom", PolicyConfig.Default);
        reg.SetActive("custom");
        Assert.Throws<InvalidOperationException>(() => reg.Remove("custom"));
    }

    [Fact]
    public void RemoveInactive()
    {
        var reg = new PolicyRegistry();
        reg.Register("custom", PolicyConfig.Default);
        Assert.Equal(2, reg.List().Count);
        reg.Remove("custom");
        Assert.Single(reg.List());
    }

    [Fact]
    public void RegisterValidates()
    {
        var reg = new PolicyRegistry();
        var bad = PolicyConfig.Default;
        bad.Conformal.Alpha = 0.0; // invalid
        var ex = Assert.Throws<ArgumentException>(() => reg.Register("bad", bad));
        Assert.Contains("validation", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void GetExisting()
    {
        var reg = new PolicyRegistry();
        var config = reg.Get(PolicyRegistryConstants.StandardPolicy);
        Assert.NotNull(config);
    }

    [Fact]
    public void GetNonexistent()
    {
        var reg = new PolicyRegistry();
        Assert.Null(reg.Get("nonexistent"));
    }

    [Fact]
    public void SwitchEventJsonl()
    {
        var evt = new PolicySwitchEvent("standard", "aggressive", 42);
        var jsonl = evt.ToJsonl();
        Assert.Contains("policy-switch-v1", jsonl);
        Assert.Contains("\"switch_id\":42", jsonl);
        Assert.Contains("\"old\":\"standard\"", jsonl);
        Assert.Contains("\"new\":\"aggressive\"", jsonl);
    }

    [Fact]
    public void OverwriteRegisteredPolicy()
    {
        var reg = new PolicyRegistry();
        var v1 = PolicyConfig.Default;
        v1.Conformal.Alpha = 0.02;
        reg.Register("custom", v1);

        var v2 = PolicyConfig.Default;
        v2.Conformal.Alpha = 0.03;
        reg.Register("custom", v2);

        var config = reg.Get("custom")!;
        Assert.InRange(config.Conformal.Alpha - 0.03, -1e-15, 1e-15);
    }

    [Fact]
    public void ConcurrentReadsDuringSwitch()
    {
        var reg = new PolicyRegistry();
        var custom = PolicyConfig.Default;
        custom.Conformal.Alpha = 0.02;
        reg.Register("custom", custom);

        var tasks = new List<Task>();
        // Reader threads
        for (int r = 0; r < 4; r++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var _ = reg.ActiveName;
                    var __ = reg.ActiveConfig;
                }
            }));
        }
        // Writer thread
        tasks.Add(Task.Run(() =>
        {
            for (int i = 0; i < 50; i++)
            {
                try
                {
                    if (i % 2 == 0)
                        reg.SetActive("custom");
                    else
                        reg.SetActive(PolicyRegistryConstants.StandardPolicy);
                }
                catch { }
            }
        }));

        Task.WaitAll(tasks.ToArray());
        Assert.True(reg.SwitchCount > 0);
    }
}
