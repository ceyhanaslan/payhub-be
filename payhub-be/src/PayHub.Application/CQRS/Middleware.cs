namespace PayHub.Application.CQRS;
using System;
using System.Threading.Tasks;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.Extensions.Logging;

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
    private readonly IServiceProvider _serviceProvider;
    public ValidationMiddleware(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Handle(object context, Func<Task<TResponse>> next)
    {
        var validatorType = typeof(FluentValidation.IValidator<>).MakeGenericType(context.GetType());
        var validator = _serviceProvider.GetService(validatorType);
        if (validator != null)
        {
            var validateMethod = validatorType.GetMethod("Validate", new[] { context.GetType() });
            if (validateMethod != null)
            {
                var result = (FluentValidation.Results.ValidationResult?)validateMethod.Invoke(validator, new[] { context });
                if (result != null && !result.IsValid)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
                    throw new FluentValidation.ValidationException(errors);
                }
            }
        }
        return await next();
    }
}
