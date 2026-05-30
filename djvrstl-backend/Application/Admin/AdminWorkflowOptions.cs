namespace Djvrstl.Backend.Application.Admin;

public sealed class AdminWorkflowOptions
{
    public const string SectionName = "AdminWorkflow";

    public int SessionTtlHours { get; set; }
    public string SessionIdPrefix { get; set; } = string.Empty;
    public string AdminUserIdPrefix { get; set; } = string.Empty;
    public string DefaultRole { get; set; } = string.Empty;
    public string PasswordHashPrefix { get; set; } = string.Empty;
    public string ProductIdPrefix { get; set; } = string.Empty;
    public string ManualBookingIdPrefix { get; set; } = string.Empty;
    public InitialAdminOptions InitialAdmin { get; set; } = new();
    public AdminMessagesOptions Messages { get; set; } = new();
}

public sealed class InitialAdminOptions
{
    public bool Enabled { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class AdminMessagesOptions
{
    public string InvalidCredentials { get; set; } = string.Empty;
    public string ProductRequired { get; set; } = string.Empty;
    public string ProductNameRequired { get; set; } = string.Empty;
    public string ProductSlugRequired { get; set; } = string.Empty;
    public string ProductSlugExists { get; set; } = string.Empty;
    public string InvalidProductPrice { get; set; } = string.Empty;
    public string InvalidProductDimensions { get; set; } = string.Empty;
    public string InvalidImageUrl { get; set; } = string.Empty;
    public string ManualBookingOverlap { get; set; } = string.Empty;
    public string ManualBookingNotFound { get; set; } = string.Empty;
}
