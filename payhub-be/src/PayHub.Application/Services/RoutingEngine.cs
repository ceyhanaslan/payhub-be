namespace PayHub.Application.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using PayHub.Application.Interfaces;

public class RoutingEngine
{
    private readonly RoutingConfig _config;
    private readonly IEnumerable<IPaymentProvider> _providers;

    public RoutingEngine(IEnumerable<IPaymentProvider> providers, RoutingConfig config)
    {
        _providers = providers;
        _config = config;
    }

    public static RoutingConfig LoadConfig(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<RoutingConfig>(json) ?? new RoutingConfig();
    }

public async Task<IPaymentProvider> SelectProviderAsync(string merchantId, string cardBin, decimal amount)
{
    return await Task.Run(() =>
    {
        var rules = _config.ProviderRules
            .Where(r => r.MerchantIds.Contains(merchantId) || r.BankBins.Contains(cardBin))
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.CommissionRate)
            .ToList();
        foreach (var rule in rules)
        {
            var provider = _providers.FirstOrDefault(p => p.GetType().Name.Contains(rule.Provider));
            if (provider != null)
                return provider;
        }
        throw new Exception("No suitable provider found");
    });
}

    public async Task<bool> ProcessWithFallbackAsync(string merchantId, string cardBin, decimal amount, PaymentRequest request)
    {
        var rules = _config.ProviderRules
            .Where(r => r.MerchantIds.Contains(merchantId) || r.BankBins.Contains(cardBin))
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.CommissionRate)
            .ToList();
        foreach (var rule in rules)
        {
            var provider = _providers.FirstOrDefault(p => p.GetType().Name.Contains(rule.Provider));
            if (provider != null)
            {
                try
                {
                    var task = provider.ProcessPaymentAsync(request);
                    if (await Task.WhenAny(task, Task.Delay(3000)) == task)
                    {
                        return await task;
                    }
                }
                catch
                {
                }
            }
        }
        return false;
    }
}
