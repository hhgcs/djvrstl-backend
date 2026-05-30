using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Application.Booking;

public sealed class BookingPricingOptionsValidator : IValidateOptions<BookingPricingOptions>
{
    public ValidateOptionsResult Validate(string? name, BookingPricingOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Currency))
        {
            errors.Add("BookingPricing:Currency is required.");
        }

        if (options.IncludedHours <= 0)
        {
            errors.Add("BookingPricing:IncludedHours must be greater than zero.");
        }

        if (options.ExtraHourFee < 0)
        {
            errors.Add("BookingPricing:ExtraHourFee cannot be negative.");
        }

        if (options.MinimumDeposit <= 0)
        {
            errors.Add("BookingPricing:MinimumDeposit must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.RequiredCountry))
        {
            errors.Add("BookingPricing:RequiredCountry is required.");
        }

        if (options.PostalCodeLength <= 0)
        {
            errors.Add("BookingPricing:PostalCodeLength must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.QuoteNote))
        {
            errors.Add("BookingPricing:QuoteNote is required.");
        }

        if (string.IsNullOrWhiteSpace(options.EstimateNote))
        {
            errors.Add("BookingPricing:EstimateNote is required.");
        }

        if (string.IsNullOrWhiteSpace(options.UnknownPackageMessage))
        {
            errors.Add("BookingPricing:UnknownPackageMessage is required.");
        }

        if (string.IsNullOrWhiteSpace(options.MissingPackageNameMessage))
        {
            errors.Add("BookingPricing:MissingPackageNameMessage is required.");
        }

        if (string.IsNullOrWhiteSpace(options.UnknownAttendeeRangeMessage))
        {
            errors.Add("BookingPricing:UnknownAttendeeRangeMessage is required.");
        }

        if (string.IsNullOrWhiteSpace(options.InvalidDurationMessage))
        {
            errors.Add("BookingPricing:InvalidDurationMessage is required.");
        }

        if (options.PackageBasePrices.Count == 0)
        {
            errors.Add("BookingPricing:PackageBasePrices requires at least one package.");
        }

        foreach (var packageId in options.PackageBasePrices.Keys)
        {
            if (!options.PackageNames.ContainsKey(packageId))
            {
                errors.Add($"BookingPricing:PackageNames is missing '{packageId}'.");
            }

            if (!options.PackageIncludes.ContainsKey(packageId))
            {
                errors.Add($"BookingPricing:PackageIncludes is missing '{packageId}'.");
            }
        }

        if (options.AttendeeRangeFees.Count == 0)
        {
            errors.Add("BookingPricing:AttendeeRangeFees requires at least one range.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
