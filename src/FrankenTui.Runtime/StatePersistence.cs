// SPDX-License-Identifier: Apache-2.0
// Port of state_persistence.rs and allocation_budget.rs
// Widget state persistence + allocation leak detection via CUSUM/e-process.

namespace FrankenTui.Runtime;

// ── state_persistence ─────────────────────────────────────────────────────

public interface IStorageBackend
{
    Task<Dictionary<string,string>> LoadAll();
    Task Save(string key,string value);
    Task Remove(string key);
    Task Clear();
}

public sealed class MemoryStorage : IStorageBackend
{
    readonly Dictionary<string,string> _data=new();
    public Task<Dictionary<string,string>> LoadAll()=>Task.FromResult(new Dictionary<string,string>(_data));
    public Task Save(string key,string value){_data[key]=value;return Task.CompletedTask;}
    public Task Remove(string key){_data.Remove(key);return Task.CompletedTask;}
    public Task Clear(){_data.Clear();return Task.CompletedTask;}
}

public sealed class StateRegistry
{
    readonly IStorageBackend _backend;
    readonly Dictionary<string,object> _cache=new();
    readonly object _lk=new();

    public StateRegistry(IStorageBackend backend){_backend=backend;}

    public async Task LoadAsync()
    {
        var data=await _backend.LoadAll();
        lock(_lk){foreach(var(k,v)in data)_cache[k]=v;}
    }

    public async Task SaveAsync(string key,object state)
    {
        lock(_lk)_cache[key]=state;
        await _backend.Save(key,state?.ToString()??"");
    }

    public T? Get<T>(string key) where T:class{lock(_lk)return _cache.TryGetValue(key,out var v)?v as T:null;}

    public async Task ClearAsync(){lock(_lk)_cache.Clear();await _backend.Clear();}

    public IStorageBackend Backend=>_backend;
    public int Count{get{lock(_lk)return _cache.Count;}}
}

// ── allocation_budget ─────────────────────────────────────────────────────

public sealed class AllocationBudgetConfig
{
    public double Mu0=0,AllowanceK=1,ThresholdH=5,Alpha=0.05,Lambda=0.5,Sigma=1.0;
    public int WindowSize=64; public bool EnableLogging;
    public static AllocationBudgetConfig Default=>new();
}

public sealed class AllocationBudget
{
    readonly AllocationBudgetConfig _c;
    double _sp,_sn,_e=1; int _n; Queue<double> _window=new(); double _mean,_m2;

    public AllocationBudget(AllocationBudgetConfig? c=null){_c=c??AllocationBudgetConfig.Default;}

    public (bool Alert,double CusumUp,double CusumDown,double EValue) Observe(double x)
    {
        _n++;
        _window.Enqueue(x);while(_window.Count>_c.WindowSize)_window.Dequeue();
        if(_window.Count>=2){_mean=_window.Average();_m2=0;foreach(var v in _window){double d=v-_mean;_m2+=d*d;}}
        double sigma=Math.Max(Math.Sqrt(_m2/Math.Max(1,_window.Count-1)),1e-9);
        _sp=Math.Max(0,_sp+x-_c.Mu0-_c.AllowanceK);
        _sn=Math.Max(0,_sn+_c.Mu0-_c.AllowanceK-x);
        double z=(x-_mean)/sigma;
        double inc=Math.Exp(_c.Lambda*z-_c.Lambda*_c.Lambda/2);
        _e=Math.Clamp(_e*Math.Max(inc,1e-12),1e-12,1e12);
        bool alert=_sp>=_c.ThresholdH||_sn>=_c.ThresholdH||_e>1.0/_c.Alpha;
        return(alert,_sp,_sn,_e);
    }

    public void Reset(){_sp=_sn=0;_e=1;_n=0;_window.Clear();_mean=_m2=0;}
    public double CusumUp=>_sp; public double CusumDown=>_sn; public double EValue=>_e;
    public int ObservationCount=>_n;
}
