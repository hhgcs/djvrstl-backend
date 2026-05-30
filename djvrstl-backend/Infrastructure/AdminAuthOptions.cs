namespace Djvrstl.Backend.Infrastructure;

public sealed class AdminAuthOptions
{
    public const string SectionName = "AdminAuth";

    public string SessionCookieName { get; set; } = string.Empty;
    public string CookieSecurePolicy { get; set; } = string.Empty;
}
