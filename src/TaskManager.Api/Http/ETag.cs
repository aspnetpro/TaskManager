using System.Globalization;

namespace TaskManager.Api.Http;

/// <summary>
/// Concorrência otimista via HTTP: GET devolve ETag com a Version da tarefa,
/// PATCH exige If-Match com esse valor (US-004).
/// </summary>
public static class ETag
{
    public static string From(uint version) => $"\"{version.ToString(CultureInfo.InvariantCulture)}\"";

    public static bool TryParse(string? header, out uint version)
    {
        version = 0;
        if (string.IsNullOrWhiteSpace(header))
            return false;

        var value = header.Trim();
        if (value.StartsWith("W/", StringComparison.Ordinal))
            value = value[2..];

        return uint.TryParse(value.Trim('"'), NumberStyles.None, CultureInfo.InvariantCulture, out version);
    }

    public static IResult PreconditionRequired() => Results.Problem(
        statusCode: StatusCodes.Status428PreconditionRequired,
        detail: "Send the If-Match header with the ETag returned by GET /api/v1/tasks/{id}.");
}
