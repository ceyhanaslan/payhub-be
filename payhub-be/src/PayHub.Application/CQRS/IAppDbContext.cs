using System.Threading.Tasks;
using System;

namespace PayHub.Application.CQRS
{
    public interface IAppDbContext
    {
        Task<ITransaction> BeginTransactionAsync();
    }

    public interface ITransaction : IAsyncDisposable
    {
        Task CommitAsync();
        Task RollbackAsync();
    }
}
