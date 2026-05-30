using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Application.Shipping;

public sealed class ShippingOptionsValidator : IValidateOptions<ShippingOptions>
{
    public ValidateOptionsResult Validate(string? name, ShippingOptions options)
    {
        var errors = new List<string>();

        if (options.PostalCodeLength <= 0)
        {
            errors.Add("Shipping:PostalCodeLength must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.InvalidPostalCodeMessage))
        {
            errors.Add("Shipping:InvalidPostalCodeMessage is required.");
        }

        if (string.IsNullOrWhiteSpace(options.MissingFallbackZoneMessage))
        {
            errors.Add("Shipping:MissingFallbackZoneMessage is required.");
        }

        if (options.Zones.Length == 0)
        {
            errors.Add("Shipping:Zones requires at least one zone.");
        }

        if (options.Zones.Count(zone => zone.Fallback) != 1)
        {
            errors.Add("Shipping:Zones must contain exactly one fallback zone.");
        }

        foreach (var zone in options.Zones)
        {
            if (zone.Zone <= 0)
            {
                errors.Add("Shipping zone number must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(zone.Label))
            {
                errors.Add($"Shipping zone {zone.Zone} label is required.");
            }

            if (zone.ShippingFee < 0)
            {
                errors.Add($"Shipping zone {zone.Zone} fee cannot be negative.");
            }

            if (string.IsNullOrWhiteSpace(zone.Message))
            {
                errors.Add($"Shipping zone {zone.Zone} message is required.");
            }
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
