namespace PayHub.Application.CQRS;
using System;
using System.Threading.Tasks;

public interface IAppDbContext
{
    Task<ITransaction> BeginTransactionAsync();
}

public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}
