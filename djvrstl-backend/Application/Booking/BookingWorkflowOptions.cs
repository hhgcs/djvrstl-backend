using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Application.Booking;

public sealed class BookingWorkflowOptions
{
    public const string SectionName = "BookingWorkflow";

    public int HoldDurationMinutes { get; set; }
    public int ExpirationPollSeconds { get; set; }
    public int ExpirationBatchSize { get; set; }
    public string BookingIdPrefix { get; set; } = string.Empty;
    public string HoldIdPrefix { get; set; } = string.Empty;
    public string CheckoutPurpose { get; set; } = string.Empty;
    public string SuccessUrlTemplate { get; set; } = string.Empty;
    public string PendingUrlTemplate { get; set; } = string.Empty;
    public string FailureUrlTemplate { get; set; } = string.Empty;
    public BookingStatusesOptions Statuses { get; set; } = new();
    public BookingWorkflowMessagesOptions Messages { get; set; } = new();
}

public sealed class BookingStatusesOptions
{
    public string Held { get; set; } = string.Empty;
    public string PendingPayment { get; set; } = string.Empty;
    public string Confirmed { get; set; } = string.Empty;
    public string Expired { get; set; } = string.Empty;
    public string ManualBlock { get; set; } = string.Empty;
}

public sealed class BookingWorkflowMessagesOptions
{
    public string ConfirmedUnavailableReason { get; set; } = string.Empty;
    public string HoldUnavailableReason { get; set; } = string.Empty;
    public string ManualBlockUnavailableReason { get; set; } = string.Empty;
    public string QuoteMismatch { get; set; } = string.Empty;
    public string DateUnavailable { get; set; } = string.Empty;
    public string BookingNotFound { get; set; } = string.Empty;
}

public sealed class BookingWorkflowOptionsValidator : IValidateOptions<BookingWorkflowOptions>
{
    public ValidateOptionsResult Validate(string? name, BookingWorkflowOptions options)
    {
        var errors = new List<string>();

        if (options.HoldDurationMinutes <= 0)
        {
            errors.Add("BookingWorkflow:HoldDurationMinutes must be greater than zero.");
        }

        if (options.ExpirationPollSeconds <= 0)
        {
            errors.Add("BookingWorkflow:ExpirationPollSeconds must be greater than zero.");
        }

        if (options.ExpirationBatchSize <= 0)
        {
            errors.Add("BookingWorkflow:ExpirationBatchSize must be greater than zero.");
        }

        Require(options.BookingIdPrefix, "BookingWorkflow:BookingIdPrefix", errors);
        Require(options.HoldIdPrefix, "BookingWorkflow:HoldIdPrefix", errors);
        Require(options.CheckoutPurpose, "BookingWorkflow:CheckoutPurpose", errors);
        Require(options.SuccessUrlTemplate, "BookingWorkflow:SuccessUrlTemplate", errors);
        Require(options.PendingUrlTemplate, "BookingWorkflow:PendingUrlTemplate", errors);
        Require(options.FailureUrlTemplate, "BookingWorkflow:FailureUrlTemplate", errors);
        Require(options.Statuses.Held, "BookingWorkflow:Statuses:Held", errors);
        Require(options.Statuses.PendingPayment, "BookingWorkflow:Statuses:PendingPayment", errors);
        Require(options.Statuses.Confirmed, "BookingWorkflow:Statuses:Confirmed", errors);
        Require(options.Statuses.Expired, "BookingWorkflow:Statuses:Expired", errors);
        Require(options.Statuses.ManualBlock, "BookingWorkflow:Statuses:ManualBlock", errors);
        Require(options.Messages.ConfirmedUnavailableReason, "BookingWorkflow:Messages:ConfirmedUnavailableReason", errors);
        Require(options.Messages.HoldUnavailableReason, "BookingWorkflow:Messages:HoldUnavailableReason", errors);
        Require(options.Messages.ManualBlockUnavailableReason, "BookingWorkflow:Messages:ManualBlockUnavailableReason", errors);
        Require(options.Messages.QuoteMismatch, "BookingWorkflow:Messages:QuoteMismatch", errors);
        Require(options.Messages.DateUnavailable, "BookingWorkflow:Messages:DateUnavailable", errors);
        Require(options.Messages.BookingNotFound, "BookingWorkflow:Messages:BookingNotFound", errors);

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    private static void Require(string value, string path, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{path} is required.");
        }
    }
}
