namespace Djvrstl.Backend.Application.Notifications;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public string Provider { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string[] AdminRecipients { get; set; } = [];
    public NotificationSubjectsOptions Subjects { get; set; } = new();
}

public sealed class NotificationSubjectsOptions
{
    public string BookingConfirmed { get; set; } = string.Empty;
    public string OrderPaid { get; set; } = string.Empty;
    public string LeadReceived { get; set; } = string.Empty;
}

public interface INotificationService
{
    Task BookingConfirmedAsync(string bookingId, CancellationToken cancellationToken);
    Task OrderPaidAsync(string orderId, CancellationToken cancellationToken);
    Task LeadReceivedAsync(string leadId, CancellationToken cancellationToken);
}

public sealed class LocalNotificationService(ILogger<LocalNotificationService> logger) : INotificationService
{
    public Task BookingConfirmedAsync(string bookingId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Booking confirmation notification queued for {BookingId}.", bookingId);
        return Task.CompletedTask;
    }

    public Task OrderPaidAsync(string orderId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Order paid notification queued for {OrderId}.", orderId);
        return Task.CompletedTask;
    }

    public Task LeadReceivedAsync(string leadId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Lead notification queued for {LeadId}.", leadId);
        return Task.CompletedTask;
    }
}
