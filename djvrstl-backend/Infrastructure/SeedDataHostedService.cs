using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Infrastructure;

public sealed class SeedDataHostedService(
    IServiceProvider serviceProvider,
    IOptions<SeedDataOptions> options,
    ILogger<SeedDataHostedService> logger) : IHostedService
{
    private readonly SeedDataOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync(cancellationToken);

        foreach (var product in _options.Products)
        {
            var exists = await db.Products.AnyAsync(existing => existing.Id == product.Id, cancellationToken);
            if (!exists)
            {
                db.Products.Add(product);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded configured product data.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
