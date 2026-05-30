using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Application.Booking;

public interface IBookingPricingService
{
    BookingQuote CalculateQuote(BookingQuoteRequest request);
}

public sealed class BookingPricingService(IOptions<BookingPricingOptions> options) : IBookingPricingService
{
    private readonly BookingPricingOptions _options = options.Value;

    public BookingQuote CalculateQuote(BookingQuoteRequest request)
    {
        if (!_options.PackageBasePrices.TryGetValue(request.PackageId, out var basePrice))
        {
            throw new ArgumentException(_options.UnknownPackageMessage, nameof(request.PackageId));
        }

        if (!_options.PackageNames.TryGetValue(request.PackageId, out var packageName))
        {
            throw new ArgumentException(_options.MissingPackageNameMessage, nameof(request.PackageId));
        }

        if (!_options.AttendeeRangeFees.TryGetValue(request.AttendeeRange, out var attendeeFee))
        {
            throw new ArgumentException(_options.UnknownAttendeeRangeMessage, nameof(request.AttendeeRange));
        }

        if (request.DurationHours < _options.IncludedHours)
        {
            throw new ArgumentOutOfRangeException(nameof(request.DurationHours), _options.InvalidDurationMessage);
        }

        var extraHoursFee = (request.DurationHours - _options.IncludedHours) * _options.ExtraHourFee;
        var locationFee = 0;
        var total = basePrice + attendeeFee + extraHoursFee + locationFee;
        var depositTotal = Math.Min(_options.MinimumDeposit, total);

        return new BookingQuote(
            request.PackageId,
            packageName,
            request.Date,
            request.DurationHours,
            request.AttendeeRange,
            request.Address,
            basePrice,
            attendeeFee,
            extraHoursFee,
            locationFee,
            total,
            depositTotal,
            total - depositTotal,
            _options.Currency,
            _options.QuoteNote);
    }
}

public sealed record BookingQuoteRequest(
    string PackageId,
    DateOnly Date,
    int DurationHours,
    string AttendeeRange,
    AddressDto Address);

public sealed record BookingQuote(
    string PackageId,
    string PackageName,
    DateOnly Date,
    int DurationHours,
    string AttendeeRange,
    AddressDto Address,
    int Subtotal,
    int AttendeeFee,
    int ExtraHoursFee,
    int LocationFee,
    int Total,
    int DepositTotal,
    int RemainingBalance,
    string Currency,
    string Note);

public sealed record AddressDto(
    string Street,
    string ExteriorNumber,
    string? InteriorNumber,
    string Neighborhood,
    string City,
    string State,
    string PostalCode,
    string Country,
    string? References);
