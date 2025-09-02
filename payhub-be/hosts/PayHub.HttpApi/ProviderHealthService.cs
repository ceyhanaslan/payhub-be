using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using PayHub.Application.Interfaces;

public class ProviderHealthService
{
    private readonly IEnumerable<IPaymentProvider> _providers;
    private readonly ConcurrentDictionary<string, ProviderHealth> _health = new();

    public ProviderHealthService(IEnumerable<IPaymentProvider> providers)
    {
        _providers = providers;
        foreach (var p in providers)
            _health[p.GetType().Name] = new ProviderHealth { Provider = p.GetType().Name };
    }

    public void ReportSuccess(string provider, long responseTimeMs)
    {
        var h = _health[provider];
        h.TransactionCount++;
        h.SuccessCount++;
        h.TotalResponseTime += responseTimeMs;
    }
    public void ReportError(string provider)
    {
        var h = _health[provider];
        h.TransactionCount++;
        h.ErrorCount++;
    }
    public IEnumerable<ProviderHealth> GetAll() => _health.Values;
    public ProviderHealth? Get(string provider) => _health.TryGetValue(provider, out var h) ? h : null;
}

public class ProviderHealth
{
    public string Provider { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public long TotalResponseTime { get; set; }
    public double SuccessRate => TransactionCount == 0 ? 0 : (double)SuccessCount / TransactionCount;
    public double AverageResponseTime => TransactionCount == 0 ? 0 : (double)TotalResponseTime / TransactionCount;
}
