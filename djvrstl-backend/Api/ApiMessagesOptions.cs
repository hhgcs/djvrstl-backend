namespace Djvrstl.Backend.Api;

public sealed class ApiMessagesOptions
{
    public const string SectionName = "ApiMessages";

    public string ValidationFailed { get; set; } = string.Empty;
    public string ZipCodeRequired { get; set; } = string.Empty;
    public string PackageRequired { get; set; } = string.Empty;
    public string AttendeeRangeRequired { get; set; } = string.Empty;
    public string PastDate { get; set; } = string.Empty;
    public string InvalidDuration { get; set; } = string.Empty;
    public string AddressRequired { get; set; } = string.Empty;
    public string StreetRequired { get; set; } = string.Empty;
    public string ExteriorNumberRequired { get; set; } = string.Empty;
    public string NeighborhoodRequired { get; set; } = string.Empty;
    public string CityRequired { get; set; } = string.Empty;
    public string StateRequired { get; set; } = string.Empty;
    public string PostalCodeRequired { get; set; } = string.Empty;
    public string CountryRequired { get; set; } = string.Empty;
    public string CountryNotSupported { get; set; } = string.Empty;
    public string InvalidPostalCode { get; set; } = string.Empty;
    public string BookingNotFound { get; set; } = string.Empty;
    public string QuoteRequired { get; set; } = string.Empty;
    public string CustomerRequired { get; set; } = string.Empty;
    public string NameRequired { get; set; } = string.Empty;
    public string EmailRequired { get; set; } = string.Empty;
    public string PhoneRequired { get; set; } = string.Empty;
    public string DateRequired { get; set; } = string.Empty;
}
