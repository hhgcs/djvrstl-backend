using System.Text.RegularExpressions;
using Djvrstl.Backend.Api;
using Djvrstl.Backend.Application.Notifications;
using Djvrstl.Backend.Domain;
using Djvrstl.Backend.Infrastructure;
using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Application.Leads;

public interface ILeadService
{
    Task<LeadResponse> CreateAsync(LeadApiRequest request, CancellationToken cancellationToken);
}

public sealed partial class LeadService(
    AppDbContext db,
    INotificationService notifications,
    IOptions<LeadWorkflowOptions> leadOptions) : ILeadService
{
    private readonly LeadWorkflowOptions _options = leadOptions.Value;

    public async Task<LeadResponse> CreateAsync(LeadApiRequest request, CancellationToken cancellationToken)
    {
        var status = string.IsNullOrWhiteSpace(request.Honeypot)
            ? _options.ReceivedStatus
            : _options.SpamStatus;

        var lead = new Lead
        {
            Id = $"{_options.LeadIdPrefix}{Guid.NewGuid():N}",
            Name = Sanitize(request.Name!),
            Email = Sanitize(request.Email!),
            Phone = Sanitize(request.Phone!),
            Message = Sanitize(request.Message!),
            Status = status
        };

        db.Leads.Add(lead);
        await db.SaveChangesAsync(cancellationToken);

        if (status == _options.ReceivedStatus)
        {
            await notifications.LeadReceivedAsync(lead.Id, cancellationToken);
        }

        return new LeadResponse(lead.Id, lead.Status);
    }

    private static string Sanitize(string value)
    {
        return HtmlTagRegex().Replace(value.Trim(), string.Empty);
    }

    [GeneratedRegex("<.*?>")]
    private static partial Regex HtmlTagRegex();
}
