using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace TaskManager.Api.Hosting;

public static class OpenApiExtensions
{
    /// <summary>OpenAPI nativo do .NET 10; a UI é o Scalar (Program.cs, só em Development).</summary>
    public static IServiceCollection AddApiOpenApi(this IServiceCollection services) =>
        services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

    /// <summary>Declara o esquema Bearer para o Scalar oferecer o campo de token.</summary>
    private sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
    {
        private const string SchemeName = "Bearer";

        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Access token from POST /api/v1/auth/login."
            };

            document.Security ??= [];
            document.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SchemeName, document)] = []
            });

            return Task.CompletedTask;
        }
    }
}
