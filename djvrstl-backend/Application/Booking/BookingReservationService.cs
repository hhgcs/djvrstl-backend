using Djvrstl.Backend.Api;
using Djvrstl.Backend.Application.Payments;
using Djvrstl.Backend.Domain;
using Djvrstl.Backend.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Application.Booking;

public interface IBookingReservationService
{
    Task<BookingAvailabilityResponse> GetAvailabilityAsync(DateOnly date, CancellationToken cancellationToken);
    Task<CreateBookingHoldResult> CreateHoldAsync(CreateBookingHoldRequest request, CancellationToken cancellationToken);
    Task<BookingStatusResponse?> GetStatusAsync(string id, CancellationToken cancellationToken);
}

public sealed class BookingReservationService(
    AppDbContext db,
    IBookingPricingService pricingService,
    IPaymentProvider paymentProvider,
    IOptions<BookingWorkflowOptions> workflowOptions) : IBookingReservationService
{
    private readonly BookingWorkflowOptions _workflowOptions = workflowOptions.Value;

    public async Task<BookingAvailabilityResponse> GetAvailabilityAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var reason = await GetUnavailableReasonAsync(date, cancellationToken);

        return reason is null
            ? new BookingAvailabilityResponse(date, true, null)
            : new BookingAvailabilityResponse(date, false, reason);
    }

    public async Task<CreateBookingHoldResult> CreateHoldAsync(CreateBookingHoldRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await BeginSerializableTransactionIfSupportedAsync(cancellationToken);

        var recalculatedQuote = pricingService.CalculateQuote(new BookingQuoteRequest(
            request.Quote.PackageId,
            request.Quote.Date,
            request.Quote.DurationHours,
            request.Quote.AttendeeRange,
            request.Quote.Address));

        if (!QuoteMatches(request.Quote, recalculatedQuote))
        {
            throw new BookingQuoteMismatchException(_workflowOptions.Messages.QuoteMismatch);
        }

        var unavailableReason = await GetUnavailableReasonAsync(request.Quote.Date, cancellationToken);
        if (unavailableReason is not null)
        {
            throw new BookingDateUnavailableException(_workflowOptions.Messages.DateUnavailable);
        }

        var now = DateTimeOffset.UtcNow;
        var bookingId = CreateId(_workflowOptions.BookingIdPrefix);
        var holdId = CreateId(_workflowOptions.HoldIdPrefix);
        var expiresAt = now.AddMinutes(_workflowOptions.HoldDurationMinutes);

        var booking = new Domain.Booking
        {
            Id = bookingId,
            PackageId = recalculatedQuote.PackageId,
            PackageName = recalculatedQuote.PackageName,
            EventDate = recalculatedQuote.Date,
            DurationHours = recalculatedQuote.DurationHours,
            AttendeeRange = recalculatedQuote.AttendeeRange,
            EventAddress = ToDomainAddress(recalculatedQuote.Address),
            Customer = ToCustomerSnapshot(request.Customer),
            Status = _workflowOptions.Statuses.PendingPayment,
            Subtotal = recalculatedQuote.Subtotal,
            AttendeeFee = recalculatedQuote.AttendeeFee,
            ExtraHoursFee = recalculatedQuote.ExtraHoursFee,
            LocationFee = recalculatedQuote.LocationFee,
            Total = recalculatedQuote.Total,
            DepositTotal = recalculatedQuote.DepositTotal,
            RemainingBalance = recalculatedQuote.RemainingBalance,
            Currency = recalculatedQuote.Currency,
            CreatedAt = now,
            UpdatedAt = now
        };

        var hold = new BookingHold
        {
            Id = holdId,
            BookingId = bookingId,
            EventDate = recalculatedQuote.Date,
            Status = _workflowOptions.Statuses.Held,
            Customer = ToCustomerSnapshot(request.Customer),
            QuoteAddress = ToDomainAddress(recalculatedQuote.Address),
            ExpiresAt = expiresAt,
            CreatedAt = now
        };

        var checkout = await paymentProvider.CreateCheckoutAsync(new CreateCheckoutRequest(
            bookingId,
            _workflowOptions.CheckoutPurpose,
            recalculatedQuote.DepositTotal,
            recalculatedQuote.Currency,
            request.Customer.Email,
            BuildReturnUri(_workflowOptions.SuccessUrlTemplate, bookingId),
            BuildReturnUri(_workflowOptions.PendingUrlTemplate, bookingId),
            BuildReturnUri(_workflowOptions.FailureUrlTemplate, bookingId)),
            cancellationToken);

        booking.ProviderPreferenceId = checkout.ProviderPreferenceId;

        db.Bookings.Add(booking);
        db.BookingHolds.Add(hold);
        await db.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new CreateBookingHoldResult(holdId, expiresAt, checkout.CheckoutUrl);
    }

    public async Task<BookingStatusResponse?> GetStatusAsync(string id, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        BookingHold? hold = null;
        if (booking is null)
        {
            hold = await db.BookingHolds
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

            if (hold?.BookingId is not null)
            {
                booking = await db.Bookings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(candidate => candidate.Id == hold.BookingId, cancellationToken);
            }
        }
        else
        {
            hold = await db.BookingHolds
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.BookingId == booking.Id, cancellationToken);
        }

        if (booking is null)
        {
            return null;
        }

        return new BookingStatusResponse(
            booking.Id,
            booking.Status,
            hold?.ExpiresAt,
            booking.EventDate,
            ToCustomerResponse(booking.Customer),
            booking.DepositTotal,
            booking.RemainingBalance,
            booking.Total,
            booking.Currency);
    }

    private async Task<string?> GetUnavailableReasonAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var confirmedBookingExists = await db.Bookings.AnyAsync(
            booking => booking.EventDate == date && booking.Status == _workflowOptions.Statuses.Confirmed,
            cancellationToken);
        if (confirmedBookingExists)
        {
            return _workflowOptions.Messages.ConfirmedUnavailableReason;
        }

        var activeHoldExists = await db.BookingHolds.AnyAsync(
            hold => hold.EventDate == date &&
                    hold.Status == _workflowOptions.Statuses.Held &&
                    hold.ExpiresAt > now,
            cancellationToken);
        if (activeHoldExists)
        {
            return _workflowOptions.Messages.HoldUnavailableReason;
        }

        var manualBlockExists = await db.BookingCalendarBlocks.AnyAsync(
            block => block.Date == date &&
                     (block.Status == _workflowOptions.Statuses.ManualBlock ||
                      block.Status == _workflowOptions.Statuses.Confirmed),
            cancellationToken);
        return manualBlockExists ? _workflowOptions.Messages.ManualBlockUnavailableReason : null;
    }

    private async Task<IDbContextTransaction?> BeginSerializableTransactionIfSupportedAsync(CancellationToken cancellationToken)
    {
        if (!db.Database.IsRelational())
        {
            return null;
        }

        return await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
    }

    private static bool QuoteMatches(BookingQuote submitted, BookingQuote recalculated)
    {
        return submitted.PackageId == recalculated.PackageId &&
               submitted.PackageName == recalculated.PackageName &&
               submitted.Date == recalculated.Date &&
               submitted.DurationHours == recalculated.DurationHours &&
               submitted.AttendeeRange == recalculated.AttendeeRange &&
               submitted.Subtotal == recalculated.Subtotal &&
               submitted.AttendeeFee == recalculated.AttendeeFee &&
               submitted.ExtraHoursFee == recalculated.ExtraHoursFee &&
               submitted.LocationFee == recalculated.LocationFee &&
               submitted.Total == recalculated.Total &&
               submitted.DepositTotal == recalculated.DepositTotal &&
               submitted.RemainingBalance == recalculated.RemainingBalance &&
               submitted.Currency == recalculated.Currency;
    }

    private static string CreateId(string prefix)
    {
        return $"{prefix}{Guid.NewGuid():N}";
    }

    private static Uri BuildReturnUri(string template, string bookingId)
    {
        return new Uri(template.Replace("{bookingId}", Uri.EscapeDataString(bookingId), StringComparison.Ordinal));
    }

    private static Address ToDomainAddress(AddressDto address)
    {
        return new Address
        {
            Street = address.Street,
            ExteriorNumber = address.ExteriorNumber,
            InteriorNumber = address.InteriorNumber,
            Neighborhood = address.Neighborhood,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode,
            Country = address.Country,
            References = address.References
        };
    }

    private static CustomerSnapshot ToCustomerSnapshot(CustomerDataDto customer)
    {
        return new CustomerSnapshot
        {
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone,
            Address = ToDomainAddress(customer.Address)
        };
    }

    private static CustomerDataResponse ToCustomerResponse(CustomerSnapshot customer)
    {
        return new CustomerDataResponse(
            customer.Name,
            customer.Email,
            customer.Phone,
            new AddressResponse(
                customer.Address.Street,
                customer.Address.ExteriorNumber,
                customer.Address.InteriorNumber,
                customer.Address.Neighborhood,
                customer.Address.City,
                customer.Address.State,
                customer.Address.PostalCode,
                customer.Address.Country,
                customer.Address.References));
    }
}

public sealed class BookingQuoteMismatchException(string message) : InvalidOperationException(message);

public sealed class BookingDateUnavailableException(string message) : InvalidOperationException(message);
