namespace PayHub.Application.Services;
using System.Collections.Generic;

public class RoutingConfig
{
    public List<ProviderRule> ProviderRules { get; set; } = new();
}

public class ProviderRule
{
    public string Provider { get; set; } = string.Empty;
    public decimal CommissionRate { get; set; }
    public List<string> BankBins { get; set; } = new();
    public List<string> MerchantIds { get; set; } = new();
    public int Priority { get; set; } = 0;
}
