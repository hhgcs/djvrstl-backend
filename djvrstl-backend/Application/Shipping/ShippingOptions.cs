namespace Djvrstl.Backend.Application.Shipping;

public sealed class ShippingOptions
{
    public const string SectionName = "Shipping";

    public int PostalCodeLength { get; set; }
    public string InvalidPostalCodeMessage { get; set; } = string.Empty;
    public string MissingFallbackZoneMessage { get; set; } = string.Empty;
    public ShippingZoneOptions[] Zones { get; set; } = [];
}

public sealed class ShippingZoneOptions
{
    public int Zone { get; set; }
    public string Label { get; set; } = string.Empty;
    public int ShippingFee { get; set; }
    public bool CheckoutAllowed { get; set; }
    public string Message { get; set; } = string.Empty;
    public string[] PostalCodes { get; set; } = [];
    public PostalCodeRangeOptions[] PostalCodeRanges { get; set; } = [];
    public bool Fallback { get; set; }
}

public sealed class PostalCodeRangeOptions
{
    public int From { get; set; }
    public int To { get; set; }
}
