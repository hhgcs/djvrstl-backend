namespace Djvrstl.Backend.Domain;

public sealed class Product
{
    public string Id { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProductDimensions Dimensions { get; set; } = new();
    public string[] Tags { get; set; } = [];
    public string[] Colors { get; set; } = [];
    public int Price { get; set; }
    public bool Active { get; set; }
    public string[] Images { get; set; } = [];
    public string? AmazonUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ProductDimensions
{
    public decimal Height { get; set; }
    public decimal Width { get; set; }
    public decimal Length { get; set; }
}

public sealed class Customer
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Address Address { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Address
{
    public string Street { get; set; } = string.Empty;
    public string ExteriorNumber { get; set; } = string.Empty;
    public string? InteriorNumber { get; set; }
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? References { get; set; }
}

public sealed class CustomerSnapshot
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Address Address { get; set; } = new();
}

public sealed class Order
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public CustomerSnapshot Customer { get; set; } = new();
    public int Subtotal { get; set; }
    public int ShippingFee { get; set; }
    public int Total { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? ProviderPreferenceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<OrderItem> Items { get; set; } = [];
}

public sealed class OrderItem
{
    public string Id { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int UnitPrice { get; set; }
    public int LineTotal { get; set; }
    public ProductDimensions Dimensions { get; set; } = new();
}

public sealed class Booking
{
    public string Id { get; set; } = string.Empty;
    public string PackageId { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public int DurationHours { get; set; }
    public string AttendeeRange { get; set; } = string.Empty;
    public Address EventAddress { get; set; } = new();
    public CustomerSnapshot Customer { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public int Subtotal { get; set; }
    public int AttendeeFee { get; set; }
    public int ExtraHoursFee { get; set; }
    public int LocationFee { get; set; }
    public int Total { get; set; }
    public int DepositTotal { get; set; }
    public int RemainingBalance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? ProviderPreferenceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class BookingHold
{
    public string Id { get; set; } = string.Empty;
    public string? BookingId { get; set; }
    public DateOnly EventDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public CustomerSnapshot Customer { get; set; } = new();
    public Address QuoteAddress { get; set; } = new();
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class BookingCalendarBlock
{
    public string Id { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? EventType { get; set; }
    public int? DurationHours { get; set; }
    public string? AttendeeRange { get; set; }
    public CustomerSnapshot? Customer { get; set; }
    public int? Total { get; set; }
    public int? DepositTotal { get; set; }
    public int? RemainingBalance { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AdminUser
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AdminSession
{
    public string Id { get; set; } = string.Empty;
    public string AdminUserId { get; set; } = string.Empty;
    public string SessionTokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class PaymentEvent
{
    public string Id { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ProviderEventId { get; set; } = string.Empty;
    public string? ProviderPaymentId { get; set; }
    public string? ProviderPreferenceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? BookingId { get; set; }
    public string? OrderId { get; set; }
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public string RawPayloadJson { get; set; } = string.Empty;
}

public sealed class Lead
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
