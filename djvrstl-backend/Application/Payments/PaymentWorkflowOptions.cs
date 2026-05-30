namespace Djvrstl.Backend.Application.Payments;

public sealed class PaymentWorkflowOptions
{
    public const string SectionName = "Payments";

    public string Provider { get; set; } = string.Empty;
    public string EventIdPrefix { get; set; } = string.Empty;
    public string DefaultWebhookStatus { get; set; } = string.Empty;
    public string DuplicateWebhookStatus { get; set; } = string.Empty;
    public MercadoPagoOptions MercadoPago { get; set; } = new();
    public PaymentStatusMappingOptions StatusMapping { get; set; } = new();
}

public sealed class MercadoPagoOptions
{
    public string ProviderKey { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string PreferencesPath { get; set; } = string.Empty;
    public string CheckoutUrlPropertyName { get; set; } = string.Empty;
    public string WebhookSigningSecret { get; set; } = string.Empty;
    public string WebhookProviderName { get; set; } = string.Empty;
    public string SignatureHeaderName { get; set; } = string.Empty;
    public string NotificationUrl { get; set; } = string.Empty;
    public bool RequireSignature { get; set; }
}

public sealed class PaymentStatusMappingOptions
{
    public string Approved { get; set; } = string.Empty;
    public string Pending { get; set; } = string.Empty;
    public string InProcess { get; set; } = string.Empty;
    public string Rejected { get; set; } = string.Empty;
    public string Cancelled { get; set; } = string.Empty;
    public string Refunded { get; set; } = string.Empty;
    public string ChargedBack { get; set; } = string.Empty;
}

public sealed record PaymentWebhookEvent(
    string Provider,
    string ProviderEventId,
    string? ProviderPaymentId,
    string? ProviderPreferenceId,
    string Status,
    string RawPayloadJson);
