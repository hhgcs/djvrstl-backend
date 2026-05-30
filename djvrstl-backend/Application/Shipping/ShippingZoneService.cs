using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Application.Shipping;

public interface IShippingZoneService
{
    ShippingDecision ValidateZipCode(string zipCode);
}

public sealed class ShippingZoneService(IOptions<ShippingOptions> options) : IShippingZoneService
{
    private readonly ShippingOptions _options = options.Value;

    public ShippingDecision ValidateZipCode(string zipCode)
    {
        var normalizedZip = zipCode.Trim();

        if (normalizedZip.Length != _options.PostalCodeLength || !normalizedZip.All(char.IsDigit))
        {
            throw new ArgumentException(_options.InvalidPostalCodeMessage, nameof(zipCode));
        }

        var numericZip = int.Parse(normalizedZip);
        var zone = _options.Zones.FirstOrDefault(candidate =>
            candidate.PostalCodes.Contains(normalizedZip) ||
            candidate.PostalCodeRanges.Any(range => numericZip >= range.From && numericZip <= range.To));

        zone ??= _options.Zones.FirstOrDefault(candidate => candidate.Fallback);

        if (zone is null)
        {
            throw new InvalidOperationException(_options.MissingFallbackZoneMessage);
        }

        return new ShippingDecision(normalizedZip, zone.Zone, zone.Label, zone.ShippingFee, zone.CheckoutAllowed, zone.Message);
    }
}

public sealed record ShippingDecision(
    string ZipCode,
    int Zone,
    string Label,
    int ShippingFee,
    bool CheckoutAllowed,
    string Message);
