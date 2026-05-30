using Djvrstl.Backend.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Application.Booking;

public interface IBookingHoldExpirationProcessor
{
    Task<int> ExpireHoldsAsync(CancellationToken cancellationToken);
}

public sealed class BookingHoldExpirationProcessor(
    AppDbContext db,
    IOptions<BookingWorkflowOptions> workflowOptions,
    ILogger<BookingHoldExpirationProcessor> logger) : IBookingHoldExpirationProcessor
{
    private readonly BookingWorkflowOptions _workflowOptions = workflowOptions.Value;

    public async Task<int> ExpireHoldsAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var holds = await db.BookingHolds
            .Where(hold => hold.Status == _workflowOptions.Statuses.Held && hold.ExpiresAt <= now)
            .OrderBy(hold => hold.ExpiresAt)
            .Take(_workflowOptions.ExpirationBatchSize)
            .ToListAsync(cancellationToken);

        foreach (var hold in holds)
        {
            hold.Status = _workflowOptions.Statuses.Expired;

            if (hold.BookingId is null)
            {
                continue;
            }

            var booking = await db.Bookings.FirstOrDefaultAsync(candidate => candidate.Id == hold.BookingId, cancellationToken);
            if (booking is not null && booking.Status == _workflowOptions.Statuses.PendingPayment)
            {
                booking.Status = _workflowOptions.Statuses.Expired;
                booking.UpdatedAt = now;
            }
        }

        if (holds.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Expired {HoldCount} booking holds.", holds.Count);
        }

        return holds.Count;
    }
}

public sealed class BookingHoldExpirationService(
    IServiceProvider serviceProvider,
    IOptions<BookingWorkflowOptions> workflowOptions) : BackgroundService
{
    private readonly BookingWorkflowOptions _workflowOptions = workflowOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_workflowOptions.ExpirationPollSeconds));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ExpireHoldsAsync(stoppingToken);
        }
    }

    private async Task ExpireHoldsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IBookingHoldExpirationProcessor>();
        await processor.ExpireHoldsAsync(cancellationToken);
    }
}
