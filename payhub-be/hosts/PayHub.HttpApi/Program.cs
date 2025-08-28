

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();



// Payment Providers (DI registration, örnek adapter)
builder.Services.AddScoped<PayHub.Application.Interfaces.IPaymentProvider, PayHub.Infrastructure.Adapters.ExampleBankPaymentProvider>();

// TransactionService DI
builder.Services.AddSingleton<PayHub.Application.Services.ITransactionService, PayHub.Application.Services.TransactionService>();

// Provider Health Service (singleton, tüm providerlar için)
builder.Services.AddSingleton<ProviderHealthService>();


var app = builder.Build();

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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

// ...existing code...

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
