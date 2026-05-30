namespace Djvrstl.Backend.Infrastructure;

public sealed class CorsSettings
{
    public const string SectionName = "Cors";
    public const string PolicyName = "FrontendCors";

    public string[] AllowedOrigins { get; set; } = [];
}
