using System.Reflection;
using FluentValidation;
using MediatR;
using MTCA.Application.Common.Constants;
using MTCA.Application.Common.Models;

namespace MTCA.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next();
        }

        if (typeof(IResultResponse).IsAssignableFrom(typeof(TResponse)))
        {
            return CreateFailureResult([Error.Validation(ErrorCodes.Validation)]);
        }

        throw new ValidationException(failures);
    }

    private static TResponse CreateFailureResult(IEnumerable<Error> errors)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(errors);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failure = responseType
                .GetMethod(nameof(Result.Failure), BindingFlags.Public | BindingFlags.Static, new[] { typeof(IEnumerable<Error>) })!
                .Invoke(null, new object[] { errors })!;
            return (TResponse)failure;
        }

        throw new InvalidOperationException(
            $"ValidationBehavior cannot construct a failure {responseType.Name}. Use Result or Result<T>.");
    }
}
