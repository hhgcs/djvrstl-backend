namespace Djvrstl.Backend.Application.Security;

using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

public static class RateLimitPolicies
{
    public const string Login = "Login";
    public const string LeadCapture = "LeadCapture";
    public const string StoreCheckout = "StoreCheckout";
    public const string BookingHold = "BookingHold";
}

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimits";

    public RateLimitRuleOptions Login { get; set; } = new();
    public RateLimitRuleOptions LeadCapture { get; set; } = new();
    public RateLimitRuleOptions StoreCheckout { get; set; } = new();
    public RateLimitRuleOptions BookingHold { get; set; } = new();
}

public sealed class RateLimitRuleOptions
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
}

public static class RateLimitPolicyRegistration
{
    public static void AddPolicy(RateLimiterOptions options, string policyName, RateLimitRuleOptions rule)
    {
        options.AddPolicy(policyName, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.TraceIdentifier,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rule.PermitLimit,
                    Window = TimeSpan.FromSeconds(rule.WindowSeconds),
                    QueueLimit = 0
                }));
    }
}
