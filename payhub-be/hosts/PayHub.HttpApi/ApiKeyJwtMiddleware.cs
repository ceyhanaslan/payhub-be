using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

public class ApiKeyJwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _apiKeyHeader = "X-API-KEY";
    private readonly string _expectedApiKey = "YOUR_API_KEY_HERE"; // Gerçek ortamda configden alınmalı

    public ApiKeyJwtMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Public endpoints - no authentication required
        var publicEndpoints = new[]
{
            "/openapi", "/scalar", "/swagger", "/metrics", "/health"
        };
        if (publicEndpoints.Any(endpoint => context.Request.Path.StartsWithSegments(endpoint)))
        {
            await _next(context);
            return;
        }

        // API Key kontrolü
        if (!context.Request.Headers.TryGetValue(_apiKeyHeader, out var apiKey) || apiKey != _expectedApiKey)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key missing or invalid");
            return;
        }
        // JWT kontrolü
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader == null || !authHeader.StartsWith("Bearer "))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("JWT missing");
            return;
        }
        var token = authHeader.Substring("Bearer ".Length);
        try
        {
            var handler = new JwtSecurityTokenHandler();
            handler.ReadJwtToken(token);
        }
        catch
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("JWT invalid");
            return;
        }
        await _next(context);
    }
}
