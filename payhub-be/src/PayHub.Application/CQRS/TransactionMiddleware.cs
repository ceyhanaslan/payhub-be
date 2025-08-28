namespace PayHub.Application.CQRS;
using System;
using System.Threading.Tasks;

public class TransactionMiddleware<TResponse> : IMiddleware<TResponse>
{
    private readonly IAppDbContext _dbContext;
    public TransactionMiddleware(IAppDbContext dbContext) => _dbContext = dbContext;
    public async Task<TResponse> Handle(object context, Func<Task<TResponse>> next)
    {
        await using var transaction = await _dbContext.BeginTransactionAsync();
        try
        {
            var result = await next();
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
