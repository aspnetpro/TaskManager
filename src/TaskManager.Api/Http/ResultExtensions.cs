using TaskManager.Shared.Domain.Results;

namespace TaskManager.Api.Http;

/// <summary>Converte Error em ProblemDetails (RFC 9457) com o status HTTP correto.</summary>
public static class ResultExtensions
{
    public static IResult ToProblem(this Error error) => error switch
    {
        ValidationError validation => Results.ValidationProblem(
            validation.Errors,
            extensions: CodeExtension(error)),

        { Type: ErrorType.Validation } => Results.ValidationProblem(
            new Dictionary<string, string[]> { [error.Code] = [error.Message] },
            extensions: CodeExtension(error)),

        _ => Results.Problem(
            statusCode: StatusCodeFor(error.Type),
            detail: error.Message,
            extensions: CodeExtension(error))
    };

    private static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError
    };

    private static Dictionary<string, object?> CodeExtension(Error error) => new() { ["code"] = error.Code };
}
