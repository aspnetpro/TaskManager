using FluentValidation;
using MediatR;
using TaskManager.Shared.Domain.Results;

namespace TaskManager.Shared.Application.Behaviors;

/// <summary>
/// Executa os validators do request antes do handler. Se houver erros,
/// devolve uma falha do tipo de resposta concreto (Result ou Result&lt;T&gt;)
/// via IResultFactory — sem cast de Result&lt;object&gt; para TResponse.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResultFactory<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).Distinct().ToArray());

        if (errors.Count > 0)
            return TResponse.Failure(new ValidationError(errors));

        return await next(cancellationToken);
    }
}
