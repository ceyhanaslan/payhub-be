
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();


// Payment Providers (DI registration, örnek adapter)
builder.Services.AddScoped<PayHub.Application.Interfaces.IPaymentProvider, PayHub.Infrastructure.Adapters.ExampleBankPaymentProvider>();

// TransactionService DI
builder.Services.AddSingleton<PayHub.Application.Services.ITransactionService, PayHub.Application.Services.TransactionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ...existing code...

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
