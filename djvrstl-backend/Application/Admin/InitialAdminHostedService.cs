using Djvrstl.Backend.Domain;
using Djvrstl.Backend.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Application.Admin;

public sealed class InitialAdminHostedService(
    IServiceProvider serviceProvider,
    IOptions<AdminWorkflowOptions> workflowOptions,
    ILogger<InitialAdminHostedService> logger) : IHostedService
{
    private readonly AdminWorkflowOptions _workflow = workflowOptions.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_workflow.InitialAdmin.Enabled)
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.AdminUsers.AnyAsync(user => user.Email == _workflow.InitialAdmin.Email, cancellationToken);
        if (existing)
        {
            return;
        }

        db.AdminUsers.Add(new AdminUser
        {
            Id = $"{_workflow.AdminUserIdPrefix}{Guid.NewGuid():N}",
            Email = _workflow.InitialAdmin.Email,
            Name = _workflow.InitialAdmin.Name,
            Role = string.IsNullOrWhiteSpace(_workflow.InitialAdmin.Role) ? _workflow.DefaultRole : _workflow.InitialAdmin.Role,
            PasswordHash = AdminAuthService.HashPassword(_workflow.InitialAdmin.Password, _workflow.PasswordHashPrefix),
            Active = true
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Initial admin user configured.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
