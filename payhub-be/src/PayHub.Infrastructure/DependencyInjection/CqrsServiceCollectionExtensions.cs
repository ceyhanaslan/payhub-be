using Microsoft.Extensions.DependencyInjection;

using PayHub.Application.CQRS;
using PayHub.Application.Interfaces;
using PayHub.Application.Payments.Commands;
using PayHub.Application.Services;
using PayHub.Infrastructure.Adapters;

public static class CqrsServiceCollectionExtensions
{
    public static IServiceCollection AddCqrs(this IServiceCollection services)
    {
        services.AddHttpClient<SipayPaymentProvider>();
        services.AddScoped<IPaymentProvider, SipayPaymentProvider>();

        services.AddScoped<RoutingEngine>();
        services.AddScoped<TransactionService>();
        services.AddScoped<TransactionLogService>();
        services.AddScoped<TokenizationService>();

        services.AddScoped<ICommandHandler<ProcessTransactionCommand, bool>, PayHub.Application.Payments.Handlers.ProcessTransactionHandler>();

        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();
        services.AddScoped(typeof(IMiddleware<>), typeof(LoggingMiddleware<>));
        services.AddScoped(typeof(IMiddleware<>), typeof(ValidationMiddleware<>));
        services.AddScoped(typeof(IMiddleware<>), typeof(TransactionMiddleware<>));
        return services;
    }
}
