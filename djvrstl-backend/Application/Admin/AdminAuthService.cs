using System.Security.Claims;
using System.Security.Cryptography;
using Djvrstl.Backend.Api;
using Djvrstl.Backend.Domain;
using Djvrstl.Backend.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Application.Admin;

public interface IAdminAuthService
{
    Task<AdminSessionResponse> GetSessionAsync(ClaimsPrincipal user, CancellationToken cancellationToken);
    Task<AdminSessionResponse?> LoginAsync(HttpContext httpContext, string email, string password, CancellationToken cancellationToken);
    Task LogoutAsync(HttpContext httpContext, CancellationToken cancellationToken);
}

public sealed class AdminAuthService(
    AppDbContext db,
    IOptions<AdminWorkflowOptions> workflowOptions) : IAdminAuthService
{
    private const int SaltByteLength = 16;
    private const int HashByteLength = 32;
    private const int Iterations = 100_000;
    private readonly AdminWorkflowOptions _workflow = workflowOptions.Value;

    public async Task<AdminSessionResponse> GetSessionAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var session = await GetActiveSessionAsync(user, cancellationToken);
        return session is null
            ? new AdminSessionResponse(false, null, null)
            : new AdminSessionResponse(true, user.FindFirstValue(ClaimTypes.Name), user.FindFirstValue(ClaimTypes.Role));
    }

    public async Task<AdminSessionResponse?> LoginAsync(HttpContext httpContext, string email, string password, CancellationToken cancellationToken)
    {
        var admin = await db.AdminUsers.FirstOrDefaultAsync(
            user => user.Email == email && user.Active,
            cancellationToken);

        if (admin is null || !VerifyPassword(password, admin.PasswordHash, _workflow.PasswordHashPrefix))
        {
            return null;
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(HashByteLength));
        var session = new AdminSession
        {
            Id = CreateId(_workflow.SessionIdPrefix),
            AdminUserId = admin.Id,
            SessionTokenHash = HashToken(token),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(_workflow.SessionTtlHours)
        };

        db.AdminSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, admin.Id),
            new(ClaimTypes.Name, admin.Name),
            new(ClaimTypes.Email, admin.Email),
            new(ClaimTypes.Role, admin.Role),
            new("session_id", session.Id),
            new("session_token", token)
        };

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = session.ExpiresAt
            });

        return new AdminSessionResponse(true, admin.Name, admin.Role);
    }

    public async Task LogoutAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var session = await GetActiveSessionAsync(httpContext.User, cancellationToken);
        if (session is not null)
        {
            session.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private async Task<AdminSession?> GetActiveSessionAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var sessionId = user.FindFirstValue("session_id");
        var token = user.FindFirstValue("session_token");
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var tokenHash = HashToken(token);
        return await db.AdminSessions.FirstOrDefaultAsync(
            session => session.Id == sessionId &&
                       session.SessionTokenHash == tokenHash &&
                       session.RevokedAt == null &&
                       session.ExpiresAt > DateTimeOffset.UtcNow,
            cancellationToken);
    }

    public static string HashPassword(string password, string prefix)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltByteLength);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashByteLength);
        return $"{prefix}{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash, string prefix)
    {
        if (!storedHash.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var parts = storedHash[prefix.Length..].Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashByteLength);
        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }

    public static string HashToken(string token)
    {
        return Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
    }

    private static string CreateId(string prefix)
    {
        return $"{prefix}{Guid.NewGuid():N}";
    }
}
