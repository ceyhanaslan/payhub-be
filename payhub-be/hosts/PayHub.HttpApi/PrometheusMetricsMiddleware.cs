using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

public class PrometheusMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ProviderHealthService _healthService;

    public PrometheusMetricsMiddleware(RequestDelegate next, ProviderHealthService healthService)
    {
        _next = next;
        _healthService = healthService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/metrics")
        {
            var metrics = _healthService.GetAll();
            var sb = new StringBuilder();
            sb.AppendLine("# HELP provider_success_rate Success rate per provider");
            sb.AppendLine("# TYPE provider_success_rate gauge");
            foreach (var m in metrics)
                sb.AppendLine($"provider_success_rate{{provider=\"{m.Provider}\"}} {m.SuccessRate}");
            sb.AppendLine("# HELP provider_avg_response_time Average response time per provider");
            sb.AppendLine("# TYPE provider_avg_response_time gauge");
            foreach (var m in metrics)
                sb.AppendLine($"provider_avg_response_time{{provider=\"{m.Provider}\"}} {m.AverageResponseTime}");
            sb.AppendLine("# HELP provider_error_count Error count per provider");
            sb.AppendLine("# TYPE provider_error_count counter");
            foreach (var m in metrics)
                sb.AppendLine($"provider_error_count{{provider=\"{m.Provider}\"}} {m.ErrorCount}");
            sb.AppendLine("# HELP provider_transaction_count Transaction count per provider");
            sb.AppendLine("# TYPE provider_transaction_count counter");
            foreach (var m in metrics)
                sb.AppendLine($"provider_transaction_count{{provider=\"{m.Provider}\"}} {m.TransactionCount}");
            context.Response.ContentType = "text/plain; version=0.0.4";
            await context.Response.WriteAsync(sb.ToString());
            return;
        }
        await _next(context);
    }
}
