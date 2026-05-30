using System.Text.Json;
using Djvrstl.Backend.Application.Booking;
using Djvrstl.Backend.Application.Notifications;
using Djvrstl.Backend.Application.Store;
using Djvrstl.Backend.Domain;
using Djvrstl.Backend.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Application.Payments;

public interface IPaymentEventService
{
    Task<string> HandleAsync(PaymentWebhookEvent paymentEvent, CancellationToken cancellationToken);
}

public sealed class PaymentEventService(
    AppDbContext db,
    INotificationService notifications,
    IOptions<PaymentWorkflowOptions> paymentOptions,
    IOptions<BookingWorkflowOptions> bookingOptions,
    IOptions<StoreWorkflowOptions> storeOptions) : IPaymentEventService
{
    private readonly PaymentWorkflowOptions _payments = paymentOptions.Value;
    private readonly BookingWorkflowOptions _booking = bookingOptions.Value;
    private readonly StoreWorkflowOptions _store = storeOptions.Value;

    public async Task<string> HandleAsync(PaymentWebhookEvent paymentEvent, CancellationToken cancellationToken)
    {
        var exists = await db.PaymentEvents
            .AnyAsync(candidate => candidate.ProviderEventId == paymentEvent.ProviderEventId, cancellationToken);

        if (exists)
        {
            return _payments.DuplicateWebhookStatus;
        }

        var entity = new PaymentEvent
        {
            Id = CreateId(_payments.EventIdPrefix),
            Provider = paymentEvent.Provider,
            ProviderEventId = paymentEvent.ProviderEventId,
            ProviderPaymentId = paymentEvent.ProviderPaymentId,
            ProviderPreferenceId = paymentEvent.ProviderPreferenceId,
            Status = paymentEvent.Status,
            RawPayloadJson = paymentEvent.RawPayloadJson
        };

        if (!string.IsNullOrWhiteSpace(paymentEvent.ProviderPreferenceId))
        {
            var booking = await db.Bookings
                .FirstOrDefaultAsync(candidate => candidate.ProviderPreferenceId == paymentEvent.ProviderPreferenceId, cancellationToken);
            if (booking is not null)
            {
                entity.BookingId = booking.Id;
                await ApplyBookingStatusAsync(booking, paymentEvent.Status, cancellationToken);
            }

            var order = await db.Orders
                .FirstOrDefaultAsync(candidate => candidate.ProviderPreferenceId == paymentEvent.ProviderPreferenceId, cancellationToken);
            if (order is not null)
            {
                entity.OrderId = order.Id;
                await ApplyOrderStatusAsync(order, paymentEvent.Status, cancellationToken);
            }
        }

        db.PaymentEvents.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return _payments.DefaultWebhookStatus;
    }

    private async Task ApplyBookingStatusAsync(Domain.Booking booking, string providerStatus, CancellationToken cancellationToken)
    {
        if (providerStatus == _payments.StatusMapping.Approved)
        {
            booking.Status = _booking.Statuses.Confirmed;
            booking.UpdatedAt = DateTimeOffset.UtcNow;
            var hold = await db.BookingHolds.FirstOrDefaultAsync(candidate => candidate.BookingId == booking.Id, cancellationToken);
            if (hold is not null)
            {
                hold.Status = _booking.Statuses.Confirmed;
            }

            await notifications.BookingConfirmedAsync(booking.Id, cancellationToken);
        }
        else if (providerStatus == _payments.StatusMapping.Pending || providerStatus == _payments.StatusMapping.InProcess)
        {
            booking.Status = _booking.Statuses.PendingPayment;
            booking.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else if (IsFailure(providerStatus))
        {
            booking.Status = _booking.Statuses.Expired;
            booking.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private async Task ApplyOrderStatusAsync(Order order, string providerStatus, CancellationToken cancellationToken)
    {
        if (providerStatus == _payments.StatusMapping.Approved)
        {
            order.Status = _store.Statuses.Paid;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            await notifications.OrderPaidAsync(order.Id, cancellationToken);
        }
        else if (providerStatus == _payments.StatusMapping.Pending || providerStatus == _payments.StatusMapping.InProcess)
        {
            order.Status = _store.Statuses.Pending;
            order.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else if (IsFailure(providerStatus))
        {
            order.Status = _store.Statuses.Failed;
            order.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private bool IsFailure(string providerStatus)
    {
        return providerStatus == _payments.StatusMapping.Rejected ||
               providerStatus == _payments.StatusMapping.Cancelled ||
               providerStatus == _payments.StatusMapping.Refunded ||
               providerStatus == _payments.StatusMapping.ChargedBack;
    }

    public static PaymentWebhookEvent ParseMercadoPagoWebhook(JsonElement payload, PaymentWorkflowOptions options)
    {
        var providerEventId = ReadString(payload, "id") ?? ReadString(payload, "eventId") ?? CreateId(options.EventIdPrefix);
        var providerPaymentId = ReadString(payload, "paymentId") ?? ReadNestedString(payload, "data", "id");
        var providerPreferenceId = ReadString(payload, "preferenceId") ?? ReadString(payload, "providerPreferenceId");
        var status = ReadString(payload, "status") ?? options.StatusMapping.Pending;

        return new PaymentWebhookEvent(
            options.MercadoPago.WebhookProviderName,
            providerEventId,
            providerPaymentId,
            providerPreferenceId,
            status,
            payload.GetRawText());
    }

    private static string? ReadString(JsonElement payload, string propertyName)
    {
        return payload.ValueKind == JsonValueKind.Object &&
               payload.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string? ReadNestedString(JsonElement payload, string parentName, string propertyName)
    {
        return payload.ValueKind == JsonValueKind.Object &&
               payload.TryGetProperty(parentName, out var parent) &&
               parent.ValueKind == JsonValueKind.Object &&
               parent.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string CreateId(string prefix)
    {
        return $"{prefix}{Guid.NewGuid():N}";
    }
}
