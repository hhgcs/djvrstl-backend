namespace Djvrstl.Backend.Application.Leads;

public sealed class LeadWorkflowOptions
{
    public const string SectionName = "LeadWorkflow";

    public string LeadIdPrefix { get; set; } = string.Empty;
    public string ReceivedStatus { get; set; } = string.Empty;
    public string SpamStatus { get; set; } = string.Empty;
    public string HoneypotFieldName { get; set; } = string.Empty;
    public int MaxMessageLength { get; set; }
    public LeadMessagesOptions Messages { get; set; } = new();
}

public sealed class LeadMessagesOptions
{
    public string MessageRequired { get; set; } = string.Empty;
    public string MessageTooLong { get; set; } = string.Empty;
}
