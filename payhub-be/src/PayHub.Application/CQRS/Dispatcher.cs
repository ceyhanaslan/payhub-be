using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace PayHub.Application.CQRS
{
    public interface ICommandDispatcher
    {
        Task<TResponse> Dispatch<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
    }

    public interface IQueryDispatcher
    {
        Task<TResponse> Dispatch<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
    }

    public class CommandDispatcher : ICommandDispatcher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEnumerable<IMiddleware> _middlewares;
        public CommandDispatcher(IServiceProvider serviceProvider, IEnumerable<IMiddleware> middlewares)
        {
            _serviceProvider = serviceProvider;
            _middlewares = middlewares;
        }
        public async Task<TResponse> Dispatch<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
        {
            var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
            var handler = _serviceProvider.GetRequiredService(handlerType);
            var context = new CommandContext(command, handler, cancellationToken);
            var pipeline = _middlewares.Reverse().Aggregate(
                (Func<Task<TResponse>>)(() => ((dynamic)handler).Handle((dynamic)command, cancellationToken)),
                (next, middleware) => () => ((IMiddleware<TResponse>)middleware).Handle(context, next)
            );
            return await pipeline();
        }
    }

    public class QueryDispatcher : IQueryDispatcher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEnumerable<IMiddleware> _middlewares;
        public QueryDispatcher(IServiceProvider serviceProvider, IEnumerable<IMiddleware> middlewares)
        {
            _serviceProvider = serviceProvider;
            _middlewares = middlewares;
        }
        public async Task<TResponse> Dispatch<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
        {
            var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));
            var handler = _serviceProvider.GetRequiredService(handlerType);
            var context = new QueryContext(query, handler, cancellationToken);
            var pipeline = _middlewares.Reverse().Aggregate(
                (Func<Task<TResponse>>)(() => ((dynamic)handler).Handle((dynamic)query, cancellationToken)),
                (next, middleware) => () => ((IMiddleware<TResponse>)middleware).Handle(context, next)
            );
            return await pipeline();
        }
    }

    public interface IMiddleware { }
    public interface IMiddleware<TResponse> : IMiddleware
    {
        Task<TResponse> Handle(object context, Func<Task<TResponse>> next);
    }

    public class CommandContext
    {
        public object Command { get; }
        public object Handler { get; }
        public CancellationToken CancellationToken { get; }
        public CommandContext(object command, object handler, CancellationToken cancellationToken)
        {
            Command = command;
            Handler = handler;
            CancellationToken = cancellationToken;
        }
    }
    public class QueryContext
    {
        public object Query { get; }
        public object Handler { get; }
        public CancellationToken CancellationToken { get; }
        public QueryContext(object query, object handler, CancellationToken cancellationToken)
        {
            Query = query;
            Handler = handler;
            CancellationToken = cancellationToken;
        }
    }
}
