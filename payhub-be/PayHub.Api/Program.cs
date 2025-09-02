
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PayHub.Application.Payments.Commands.ProcessTransactionCommand>());

builder.Services.AddScoped<PayHub.Application.Interfaces.IPaymentProvider, PayHub.Infrastructure.Adapters.ExampleBankPaymentProvider>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
