
using PayHub.Infrastructure.Adapters;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add CQRS and PayHub services
builder.Services.AddCqrs();

// TransactionService DI
builder.Services.AddSingleton<PayHub.Application.Services.ITransactionService, PayHub.Application.Services.TransactionService>();

// Provider Health Service (scoped because it depends on scoped IPaymentProvider)
builder.Services.AddScoped<ProviderHealthService>();


var app = builder.Build();

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}



app.Use(async (context, next) =>
{
    await new ApiKeyJwtMiddleware(next).InvokeAsync(context);
});
app.Use(async (context, next) =>
{
    var healthService = context.RequestServices.GetRequiredService<ProviderHealthService>();
    await new PrometheusMetricsMiddleware(next, healthService).InvokeAsync(context);
});
app.UseHttpsRedirection();

app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
