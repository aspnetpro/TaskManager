using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using TaskManager.Api.Security;

namespace TaskManager.Api.Hosting;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";

    /// <summary>
    /// O IP é o do cliente, não o do proxy: UseForwardedHeaders roda antes
    /// (ForwardedHeadersExtensions). Sem ele, todo mundo divide o limite do nginx.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RateLimitingOptions>>((limiter, options) =>
            {
                var settings = options.Value;
                limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                limiter.OnRejected = OnRejectedAsync;

                limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        PartitionKey(context),
                        _ => FixedWindow(settings.GlobalPermitLimit, settings.Window)));

                limiter.AddPolicy(AuthPolicy, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        $"ip:{context.Connection.RemoteIpAddress}",
                        _ => FixedWindow(settings.AuthPermitLimit, settings.Window)));
            });

        return services;
    }

    private static string PartitionKey(HttpContext context) =>
        WorkspaceClaims.TryGetWorkspace(context.User, out var userId, out _, out _)
            ? $"user:{userId}"
            : $"ip:{context.Connection.RemoteIpAddress}";

    private static FixedWindowRateLimiterOptions FixedWindow(int permitLimit, TimeSpan window) => new()
    {
        PermitLimit = permitLimit,
        Window = window,
        QueueLimit = 0
    };

    private static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        await Results.Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                detail: "Too many requests. Try again later.",
                extensions: new Dictionary<string, object?> { ["code"] = "RateLimit.Exceeded" })
            .ExecuteAsync(context.HttpContext);
    }
}
