using Djvrstl.Backend.Application.Booking;

namespace Djvrstl.Backend.Api;

public sealed record ProductResponse(
    string Id,
    string Slug,
    string Name,
    string Description,
    ProductDimensionsResponse Dimensions,
    string[] Tags,
    string[] Colors,
    int Price,
    bool Active,
    string[] Images,
    string? AmazonUrl);

public sealed record ProductDimensionsResponse(decimal Height, decimal Width, decimal Length);

public sealed record ProductImageUploadResponse(string[] Images);

public sealed record ValidateZipRequest(string? ZipCode);

public sealed record BookingPricingConfigResponse(
    string Currency,
    int IncludedHours,
    int ExtraHourFee,
    int MinimumDeposit,
    IReadOnlyDictionary<string, int> PackageBasePrices,
    IReadOnlyDictionary<string, string> PackageNames,
    IReadOnlyDictionary<string, string[]> PackageIncludes,
    IReadOnlyDictionary<string, int> AttendeeRangeFees,
    string EstimateNote);

public sealed record BookingQuoteApiRequest(
    string? PackageId,
    DateOnly Date,
    int DurationHours,
    string? AttendeeRange,
    AddressApiRequest? Address);

public sealed record AddressApiRequest(
    string? Street,
    string? ExteriorNumber,
    string? InteriorNumber,
    string? Neighborhood,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    string? References);

public sealed record CreateBookingHoldApiRequest(
    BookingQuote? Quote,
    CustomerDataApiRequest? Customer);

public sealed record CustomerDataApiRequest(
    string? Name,
    string? Email,
    string? Phone,
    AddressApiRequest? Address);

public sealed record CreateBookingHoldRequest(
    BookingQuote Quote,
    CustomerDataDto Customer);

public sealed record CustomerDataDto(
    string Name,
    string Email,
    string Phone,
    AddressDto Address);

public sealed record CreateBookingHoldResult(
    string HoldId,
    DateTimeOffset ExpiresAt,
    string CheckoutUrl);

public sealed record BookingAvailabilityResponse(
    DateOnly Date,
    bool Available,
    string? Reason);

public sealed record BookingStatusResponse(
    string BookingId,
    string Status,
    DateTimeOffset? ExpiresAt,
    DateOnly EventDate,
    CustomerDataResponse Customer,
    int DepositTotal,
    int RemainingBalance,
    int Total,
    string Currency);

public sealed record CustomerDataResponse(
    string Name,
    string Email,
    string Phone,
    AddressResponse Address);

public sealed record AddressResponse(
    string Street,
    string ExteriorNumber,
    string? InteriorNumber,
    string Neighborhood,
    string City,
    string State,
    string PostalCode,
    string Country,
    string? References);

public sealed record StoreCheckoutItemApiRequest(
    string? ProductId,
    string? Color,
    int Quantity);

public sealed record StoreCheckoutApiRequest(
    CustomerDataApiRequest? Customer,
    string? OrderId,
    string? Status,
    StoreCheckoutItemApiRequest[]? Items,
    OrderSummaryApiRequest? Summary);

public sealed record OrderSummaryApiRequest(
    int Subtotal,
    int ShippingFee,
    int Total,
    string? Currency);

public sealed record StoreCheckoutResponse(
    string OrderId,
    string CheckoutUrl);

public sealed record StoreOrderStatusResponse(
    string OrderId,
    string Status,
    CustomerDataResponse Customer,
    OrderItemResponse[] Items,
    OrderSummaryResponse Summary);

public sealed record OrderItemResponse(
    string ProductId,
    string ProductSlug,
    string ProductName,
    string Color,
    int Quantity,
    int UnitPrice,
    int LineTotal,
    ProductDimensionsResponse Dimensions);

public sealed record OrderSummaryResponse(
    int Subtotal,
    int ShippingFee,
    int Total,
    string Currency);

public sealed record AdminSessionResponse(
    bool Authenticated,
    string? Name,
    string? Role);

public sealed record AdminLoginRequest(
    string? Email,
    string? Password);

public sealed record BookingCalendarEntryResponse(
    string Id,
    DateOnly Date,
    string Label,
    string Status,
    string? EventType,
    int? DurationHours,
    string? AttendeeRange,
    CustomerDataResponse? Customer,
    int? Total,
    int? DepositTotal,
    int? RemainingBalance,
    string? Notes);

public sealed record ManualBookingApiRequest(
    string? Id,
    DateOnly Date,
    string? Label,
    string? Status,
    string? EventType,
    int? DurationHours,
    string? AttendeeRange,
    CustomerDataApiRequest? Customer,
    int? Total,
    int? DepositTotal,
    int? RemainingBalance,
    string? Notes);

public sealed record AdminSaleResponse(
    string Id,
    CustomerDataResponse Customer,
    string ItemName,
    int Total,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record LeadApiRequest(
    string? Name,
    string? Email,
    string? Phone,
    string? Message,
    string? Honeypot);

public sealed record LeadResponse(
    string LeadId,
    string Status);

public sealed record PaymentWebhookResponse(
    string Status);
