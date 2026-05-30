namespace Djvrstl.Backend.Application.Booking;

public sealed class BookingPricingOptions
{
    public const string SectionName = "BookingPricing";

    public string Currency { get; set; } = string.Empty;
    public int IncludedHours { get; set; }
    public int ExtraHourFee { get; set; }
    public int MinimumDeposit { get; set; }
    public string QuoteNote { get; set; } = string.Empty;
    public string EstimateNote { get; set; } = string.Empty;
    public string RequiredCountry { get; set; } = string.Empty;
    public string UnknownPackageMessage { get; set; } = string.Empty;
    public string MissingPackageNameMessage { get; set; } = string.Empty;
    public string UnknownAttendeeRangeMessage { get; set; } = string.Empty;
    public string InvalidDurationMessage { get; set; } = string.Empty;
    public int PostalCodeLength { get; set; }
    public Dictionary<string, int> PackageBasePrices { get; set; } = [];
    public Dictionary<string, string> PackageNames { get; set; } = [];
    public Dictionary<string, string[]> PackageIncludes { get; set; } = [];
    public Dictionary<string, int> AttendeeRangeFees { get; set; } = [];
}
