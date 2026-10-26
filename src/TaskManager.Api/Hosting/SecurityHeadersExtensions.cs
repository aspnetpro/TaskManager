namespace TaskManager.Api.Hosting;

public static class SecurityHeadersExtensions
{
    /// <summary>
    /// Headers para uma API JSON. A CSP é a mais fechada possível porque nenhuma
    /// resposta é HTML — exceto a UI do Scalar em dev, que fica de fora.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";

            if (!context.Request.Path.StartsWithSegments("/scalar"))
                headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

            await next(context);
        });
}
