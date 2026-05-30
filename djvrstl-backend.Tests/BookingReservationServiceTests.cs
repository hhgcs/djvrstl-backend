using Djvrstl.Backend.Api;
using Djvrstl.Backend.Application.Booking;
using Djvrstl.Backend.Application.Payments;
using Djvrstl.Backend.Domain;
using Djvrstl.Backend.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Djvrstl.Backend.Tests;

public sealed class BookingReservationServiceTests
{
    [Fact]
    public async Task Availability_ReturnsUnavailableForConfirmedBooking()
    {
        await using var db = CreateDb();
        var options = TestOptions.BookingWorkflow();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        db.Bookings.Add(new Booking { Id = "booking-existing", EventDate = date, Status = options.Statuses.Confirmed });
        await db.SaveChangesAsync();
        var service = CreateService(db, options);

        var availability = await service.GetAvailabilityAsync(date, CancellationToken.None);

        Assert.False(availability.Available);
        Assert.Equal(options.Messages.ConfirmedUnavailableReason, availability.Reason);
    }

    [Fact]
    public async Task Availability_IgnoresExpiredHold()
    {
        await using var db = CreateDb();
        var options = TestOptions.BookingWorkflow();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        db.BookingHolds.Add(new BookingHold
        {
            Id = "hold-existing",
            EventDate = date,
            Status = options.Statuses.Held,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        });
        await db.SaveChangesAsync();
        var service = CreateService(db, options);

        var availability = await service.GetAvailabilityAsync(date, CancellationToken.None);

        Assert.True(availability.Available);
    }

    [Fact]
    public async Task CreateHold_PersistsBookingAndHoldWithConfiguredDuration()
    {
        await using var db = CreateDb();
        var options = TestOptions.BookingWorkflow();
        var service = CreateService(db, options);
        var request = CreateHoldRequest();
        var before = DateTimeOffset.UtcNow;

        var result = await service.CreateHoldAsync(request, CancellationToken.None);

        Assert.StartsWith(options.HoldIdPrefix, result.HoldId);
        Assert.Contains("pref_id=fake_", result.CheckoutUrl, StringComparison.Ordinal);
        Assert.True(result.ExpiresAt >= before.AddMinutes(options.HoldDurationMinutes - 1));
        Assert.Single(db.Bookings);
        Assert.Single(db.BookingHolds);
        Assert.Equal(options.Statuses.PendingPayment, db.Bookings.Single().Status);
        Assert.Equal(options.Statuses.Held, db.BookingHolds.Single().Status);
    }

    [Fact]
    public async Task CreateHold_RejectsMismatchedQuote()
    {
        await using var db = CreateDb();
        var options = TestOptions.BookingWorkflow();
        var service = CreateService(db, options);
        var request = CreateHoldRequest() with
        {
            Quote = CreateQuote() with { Total = 1 }
        };

        var exception = await Assert.ThrowsAsync<BookingQuoteMismatchException>(() =>
            service.CreateHoldAsync(request, CancellationToken.None));

        Assert.Equal(options.Messages.QuoteMismatch, exception.Message);
    }

    [Fact]
    public async Task CreateHold_RejectsSecondHoldForSameDate()
    {
        await using var db = CreateDb();
        var options = TestOptions.BookingWorkflow();
        var service = CreateService(db, options);
        var request = CreateHoldRequest();
        await service.CreateHoldAsync(request, CancellationToken.None);

        await Assert.ThrowsAsync<BookingDateUnavailableException>(() =>
            service.CreateHoldAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetStatus_ReturnsBookingByHoldId()
    {
        await using var db = CreateDb();
        var service = CreateService(db, TestOptions.BookingWorkflow());
        var hold = await service.CreateHoldAsync(CreateHoldRequest(), CancellationToken.None);

        var status = await service.GetStatusAsync(hold.HoldId, CancellationToken.None);

        Assert.NotNull(status);
        Assert.Equal("pending_payment", status!.Status);
        Assert.Equal(1500, status.DepositTotal);
    }

    [Fact]
    public async Task ExpirationProcessor_MarksExpiredHoldAndPendingBookingExpired()
    {
        await using var db = CreateDb();
        var options = TestOptions.BookingWorkflow();
        db.Bookings.Add(new Booking
        {
            Id = "booking-expired",
            EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Status = options.Statuses.PendingPayment
        });
        db.BookingHolds.Add(new BookingHold
        {
            Id = "hold-expired",
            BookingId = "booking-expired",
            EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Status = options.Statuses.Held,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        });
        await db.SaveChangesAsync();
        var processor = new BookingHoldExpirationProcessor(
            db,
            Options.Create(options),
            NullLogger<BookingHoldExpirationProcessor>.Instance);

        var expiredCount = await processor.ExpireHoldsAsync(CancellationToken.None);

        Assert.Equal(1, expiredCount);
        Assert.Equal(options.Statuses.Expired, db.BookingHolds.Single().Status);
        Assert.Equal(options.Statuses.Expired, db.Bookings.Single().Status);
    }

    private static AppDbContext CreateDb()
    {
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"booking-reservation-{Guid.NewGuid():N}")
            .Options);
    }

    private static BookingReservationService CreateService(AppDbContext db, BookingWorkflowOptions workflowOptions)
    {
        return new BookingReservationService(
            db,
            new BookingPricingService(Options.Create(TestOptions.BookingPricing())),
            new FakePaymentProvider(Options.Create(new FakePaymentOptions { CheckoutBaseUrl = "https://payments.local/checkout" })),
            Options.Create(workflowOptions));
    }

    private static CreateBookingHoldRequest CreateHoldRequest()
    {
        return new CreateBookingHoldRequest(
            CreateQuote(),
            new CustomerDataDto(
                "Mariana Ortiz",
                "mariana@example.com",
                "5512345678",
                Address()));
    }

    private static BookingQuote CreateQuote()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        return new BookingQuote(
            "signature",
            "Premium",
            date,
            6,
            "100-199",
            Address(),
            7500,
            3000,
            1200,
            0,
            11700,
            1500,
            10200,
            "MXN",
            "Incluye 5 horas base. El anticipo minimo para reservar es 1500 MXN.");
    }

    private static AddressDto Address()
    {
        return new AddressDto(
            "Av. Alvaro Obregon",
            "120",
            "4B",
            "Roma Norte",
            "Ciudad de Mexico",
            "CDMX",
            "06700",
            "MX",
            "Entrada por la calle lateral");
    }
}
