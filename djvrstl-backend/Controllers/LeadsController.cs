using Djvrstl.Backend.Api;
using Djvrstl.Backend.Application.Leads;
using Djvrstl.Backend.Application.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Controllers;

[ApiController]
[Route("leads")]
public sealed class LeadsController(
    ILeadService leadService,
    IOptions<ApiMessagesOptions> messagesOptions,
    IOptions<LeadWorkflowOptions> leadOptions) : ControllerBase
{
    private readonly ApiMessagesOptions _messages = messagesOptions.Value;
    private readonly LeadWorkflowOptions _lead = leadOptions.Value;

    [EnableRateLimiting(RateLimitPolicies.LeadCapture)]
    [HttpPost]
    public async Task<ActionResult<LeadResponse>> Create(LeadApiRequest request, CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return BadRequest(ApiErrors.Validation(_messages.ValidationFailed, errors));
        }

        var response = await leadService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    private Dictionary<string, string> Validate(LeadApiRequest request)
    {
        var errors = new Dictionary<string, string>();
        Require(request.Name, "name", _messages.NameRequired, errors);
        Require(request.Email, "email", _messages.EmailRequired, errors);
        Require(request.Phone, "phone", _messages.PhoneRequired, errors);
        Require(request.Message, "message", _lead.Messages.MessageRequired, errors);

        if (!string.IsNullOrWhiteSpace(request.Message) && request.Message.Length > _lead.MaxMessageLength)
        {
            errors["message"] = _lead.Messages.MessageTooLong;
        }

        return errors;
    }

    private static void Require(string? value, string field, string message, Dictionary<string, string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = message;
        }
    }
}
