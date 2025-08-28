using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PayHub.Application.CQRS
{
    public class LoggingMiddleware<TResponse> : IMiddleware<TResponse>
    {
        private readonly ILogger<LoggingMiddleware<TResponse>> _logger;
        public LoggingMiddleware(ILogger<LoggingMiddleware<TResponse>> logger) => _logger = logger;
        public async Task<TResponse> Handle(object context, Func<Task<TResponse>> next)
        {
            _logger.LogInformation("CQRS pipeline started: {Context}", context);
            var result = await next();
            _logger.LogInformation("CQRS pipeline finished: {Context}", context);
            return result;
        }
    }

    public class ValidationMiddleware<TResponse> : IMiddleware<TResponse>
    {
        public async Task<TResponse> Handle(object context, Func<Task<TResponse>> next)
        {
            // Validation logic (FluentValidation vs entegre edilebilir)
            return await next();
        }
    }

    // public class TransactionMiddleware<TResponse> : IMiddleware<TResponse>
    // {
    //     private readonly IDbContext _dbContext;
    //     public TransactionMiddleware(IDbContext dbContext) => _dbContext = dbContext;
    //     public async Task<TResponse> Handle(object context, Func<Task<TResponse>> next)
    //     {
    //         using var transaction = await _dbContext.BeginTransactionAsync();
    //         try
    //         {
    //             var result = await next();
    //             await transaction.CommitAsync();
    //             return result;
    //         }
    //         catch
    //         {
    //             await transaction.RollbackAsync();
    //             throw;
    //         }
    //     }
    // }
}
