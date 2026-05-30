using Djvrstl.Backend.Application.Booking;
using Djvrstl.Backend.Application.Admin;
using Djvrstl.Backend.Application.Leads;
using Djvrstl.Backend.Application.Notifications;
using Djvrstl.Backend.Application.Payments;
using Djvrstl.Backend.Application.Security;
using Djvrstl.Backend.Application.Shipping;
using Djvrstl.Backend.Application.Store;
using Djvrstl.Backend.Api;
using Djvrstl.Backend.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<CorsSettings>()
    .Bind(builder.Configuration.GetSection(CorsSettings.SectionName));
builder.Services.AddOptions<BookingPricingOptions>()
    .Bind(builder.Configuration.GetSection(BookingPricingOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddOptions<BookingWorkflowOptions>()
    .Bind(builder.Configuration.GetSection(BookingWorkflowOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddOptions<ShippingOptions>()
    .Bind(builder.Configuration.GetSection(ShippingOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddOptions<FakePaymentOptions>()
    .Bind(builder.Configuration.GetSection(FakePaymentOptions.SectionName));
builder.Services.AddOptions<PaymentWorkflowOptions>()
    .Bind(builder.Configuration.GetSection(PaymentWorkflowOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Provider), "Payments:Provider is required.")
    .ValidateOnStart();
builder.Services.AddOptions<StoreWorkflowOptions>()
    .Bind(builder.Configuration.GetSection(StoreWorkflowOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Currency), "StoreWorkflow:Currency is required.")
    .ValidateOnStart();
builder.Services.AddOptions<AdminAuthOptions>()
    .Bind(builder.Configuration.GetSection(AdminAuthOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.SessionCookieName), "AdminAuth:SessionCookieName is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.CookieSecurePolicy), "AdminAuth:CookieSecurePolicy is required.")
    .ValidateOnStart();
builder.Services.AddOptions<AdminWorkflowOptions>()
    .Bind(builder.Configuration.GetSection(AdminWorkflowOptions.SectionName))
    .Validate(options => options.SessionTtlHours > 0, "AdminWorkflow:SessionTtlHours must be greater than zero.")
    .ValidateOnStart();
builder.Services.AddOptions<ProductImageUploadOptions>()
    .Bind(builder.Configuration.GetSection(ProductImageUploadOptions.SectionName))
    .Validate(options => options.MaxFileSizeBytes > 0, "ProductImageUploads:MaxFileSizeBytes must be greater than zero.")
    .Validate(options => options.MaxFiles > 0, "ProductImageUploads:MaxFiles must be greater than zero.")
    .ValidateOnStart();
builder.Services.AddOptions<LeadWorkflowOptions>()
    .Bind(builder.Configuration.GetSection(LeadWorkflowOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ReceivedStatus), "LeadWorkflow:ReceivedStatus is required.")
    .ValidateOnStart();
builder.Services.AddOptions<NotificationOptions>()
    .Bind(builder.Configuration.GetSection(NotificationOptions.SectionName));
builder.Services.AddOptions<RateLimitSettings>()
    .Bind(builder.Configuration.GetSection(RateLimitSettings.SectionName))
    .Validate(options => options.Login.PermitLimit > 0, "RateLimits:Login:PermitLimit must be greater than zero.")
    .ValidateOnStart();
builder.Services.AddOptions<SeedDataOptions>()
    .Bind(builder.Configuration.GetSection(SeedDataOptions.SectionName));
builder.Services.AddOptions<ApiMessagesOptions>()
    .Bind(builder.Configuration.GetSection(ApiMessagesOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ValidationFailed), "ApiMessages:ValidationFailed is required.")
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<BookingPricingOptions>, BookingPricingOptionsValidator>();
builder.Services.AddSingleton<IValidateOptions<BookingWorkflowOptions>, BookingWorkflowOptionsValidator>();
builder.Services.AddSingleton<IValidateOptions<ShippingOptions>, ShippingOptionsValidator>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddRateLimiter(options =>
{
    var settings = builder.Configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>() ?? new();
    RateLimitPolicyRegistration.AddPolicy(options, RateLimitPolicies.Login, settings.Login);
    RateLimitPolicyRegistration.AddPolicy(options, RateLimitPolicies.LeadCapture, settings.LeadCapture);
    RateLimitPolicyRegistration.AddPolicy(options, RateLimitPolicies.StoreCheckout, settings.StoreCheckout);
    RateLimitPolicyRegistration.AddPolicy(options, RateLimitPolicies.BookingHold, settings.BookingHold);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsSettings.PolicyName, policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection($"{CorsSettings.SectionName}:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        var adminAuth = builder.Configuration.GetSection(AdminAuthOptions.SectionName).Get<AdminAuthOptions>()
            ?? throw new InvalidOperationException("AdminAuth configuration is missing.");
        options.Cookie.Name = adminAuth.SessionCookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = Enum.Parse<CookieSecurePolicy>(adminAuth.CookieSecurePolicy, ignoreCase: true);
        options.SlidingExpiration = true;
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var sessionId = context.Principal?.FindFirst("session_id")?.Value;
                var token = context.Principal?.FindFirst("session_token")?.Value;
                if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(token))
                {
                    context.RejectPrincipal();
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var tokenHash = AdminAuthService.HashToken(token);
                var valid = await db.AdminSessions.AnyAsync(session =>
                    session.Id == sessionId &&
                    session.SessionTokenHash == tokenHash &&
                    session.RevokedAt == null &&
                    session.ExpiresAt > DateTimeOffset.UtcNow);

                if (!valid)
                {
                    context.RejectPrincipal();
                }
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IBookingPricingService, BookingPricingService>();
builder.Services.AddScoped<IBookingReservationService, BookingReservationService>();
builder.Services.AddScoped<IBookingHoldExpirationProcessor, BookingHoldExpirationProcessor>();
builder.Services.AddScoped<IShippingZoneService, ShippingZoneService>();
builder.Services.AddScoped<IStoreCheckoutService, StoreCheckoutService>();
var paymentOptions = builder.Configuration.GetSection(PaymentWorkflowOptions.SectionName).Get<PaymentWorkflowOptions>();
if (paymentOptions is not null &&
    string.Equals(paymentOptions.Provider, paymentOptions.MercadoPago.ProviderKey, StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<IPaymentProvider, MercadoPagoPaymentProvider>();
}
else
{
    builder.Services.AddScoped<IPaymentProvider, FakePaymentProvider>();
}
builder.Services.AddScoped<IPaymentEventService, PaymentEventService>();
builder.Services.AddScoped<IAdminAuthService, AdminAuthService>();
builder.Services.AddScoped<IAdminDataService, AdminDataService>();
builder.Services.AddScoped<IProductImageStorageService, ProductImageStorageService>();
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<INotificationService, LocalNotificationService>();
builder.Services.AddHostedService<SeedDataHostedService>();
builder.Services.AddHostedService<InitialAdminHostedService>();
builder.Services.AddHostedService<BookingHoldExpirationService>();

var app = builder.Build();

var webRootPath = app.Environment.WebRootPath;
if (string.IsNullOrWhiteSpace(webRootPath))
{
    webRootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    app.Environment.WebRootPath = webRootPath;
}

Directory.CreateDirectory(webRootPath);
var productImageUploads = app.Services.GetRequiredService<IOptions<ProductImageUploadOptions>>().Value;
var productImageStoragePath = Path.IsPathRooted(productImageUploads.StoragePath)
    ? productImageUploads.StoragePath
    : Path.Combine(app.Environment.ContentRootPath, productImageUploads.StoragePath);
Directory.CreateDirectory(productImageStoragePath);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(productImageStoragePath),
    RequestPath = productImageUploads.RequestPath
});
app.UseCors(CorsSettings.PolicyName);
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (AppDbContext db, IOptions<CorsSettings> corsOptions, CancellationToken cancellationToken) =>
{
    var databaseHealthy = await db.Database.CanConnectAsync(cancellationToken);

    return databaseHealthy
        ? Results.Ok(new
        {
            status = "healthy",
            database = "reachable",
            corsOrigins = corsOptions.Value.AllowedOrigins
        })
        : Results.Json(new { status = "unhealthy", database = "unreachable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.MapControllers();

app.Run();

public partial class Program;
