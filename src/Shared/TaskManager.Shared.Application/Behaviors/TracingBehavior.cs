using System.Diagnostics;
using MediatR;
using TaskManager.Shared.Domain.Results;

namespace TaskManager.Shared.Application.Behaviors;

/// <summary>
/// Um span por command/query, filho do span HTTP. Erro de negócio não é falha do
/// sistema: vira tag (`result.error_code`), e o status Error fica só para Unexpected.
/// Senão o dashboard de erros enche de 404 e 422, que são respostas corretas.
/// </summary>
public sealed class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResultFactory<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity(typeof(TRequest).Name);

        var response = await next(cancellationToken);

        if (activity is not null && response.IsFailure)
        {
            activity.SetTag("result.error_code", response.Error.Code);
            activity.SetTag("result.error_type", response.Error.Type.ToString());
            if (response.Error.Type == ErrorType.Unexpected)
                activity.SetStatus(ActivityStatusCode.Error, response.Error.Code);
        }

        return response;
    }
}
