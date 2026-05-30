namespace Djvrstl.Backend.Application.Store;

public sealed class StoreWorkflowOptions
{
    public const string SectionName = "StoreWorkflow";

    public string Currency { get; set; } = string.Empty;
    public string OrderIdPrefix { get; set; } = string.Empty;
    public string OrderItemIdPrefix { get; set; } = string.Empty;
    public string CheckoutPurpose { get; set; } = string.Empty;
    public string SuccessUrlTemplate { get; set; } = string.Empty;
    public string PendingUrlTemplate { get; set; } = string.Empty;
    public string FailureUrlTemplate { get; set; } = string.Empty;
    public StoreStatusesOptions Statuses { get; set; } = new();
    public StoreMessagesOptions Messages { get; set; } = new();
}

public sealed class StoreStatusesOptions
{
    public string Draft { get; set; } = string.Empty;
    public string Pending { get; set; } = string.Empty;
    public string Paid { get; set; } = string.Empty;
    public string Failed { get; set; } = string.Empty;
    public string Quoted { get; set; } = string.Empty;
}

public sealed class StoreMessagesOptions
{
    public string OrderNotFound { get; set; } = string.Empty;
    public string EmptyCart { get; set; } = string.Empty;
    public string ProductNotFound { get; set; } = string.Empty;
    public string ProductInactive { get; set; } = string.Empty;
    public string InvalidColor { get; set; } = string.Empty;
    public string InvalidQuantity { get; set; } = string.Empty;
    public string CheckoutBlocked { get; set; } = string.Empty;
}
