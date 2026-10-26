namespace TaskManager.Api.Security;

/// <summary>
/// O refresh token mora num cookie httpOnly: JavaScript não lê (XSS não rouba),
/// SameSite=Strict não vai em request de outro site (CSRF), e o Path restringe
/// o envio aos endpoints de auth.
/// </summary>
public static class RefreshTokenCookie
{
    public const string Name = "tm_refresh";
    public const string Path = "/api/v1/auth";

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    public static void Append(HttpResponse response, string refreshToken, DateTimeOffset expiresAt) =>
        response.Cookies.Append(Name, refreshToken, Options(expiresAt));

    public static void Delete(HttpResponse response) => response.Cookies.Delete(Name, Options(null));

    // Secure também em dev: navegadores aceitam cookie Secure em http://localhost.
    private static CookieOptions Options(DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expiresAt,
        IsEssential = true
    };
}
