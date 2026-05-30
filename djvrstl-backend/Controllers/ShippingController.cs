using Djvrstl.Backend.Api;
using Djvrstl.Backend.Application.Shipping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Controllers;

[ApiController]
[Route("shipping")]
public sealed class ShippingController(
    IShippingZoneService shippingZoneService,
    IOptions<ApiMessagesOptions> messagesOptions) : ControllerBase
{
    private readonly ApiMessagesOptions _messages = messagesOptions.Value;

    [HttpPost("validate-zip")]
    public ActionResult<ShippingDecision> ValidateZip(ValidateZipRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ZipCode))
        {
            return BadRequest(ApiErrors.Validation(_messages.ValidationFailed, new Dictionary<string, string>
            {
                ["zipCode"] = _messages.ZipCodeRequired
            }));
        }

        try
        {
            return Ok(shippingZoneService.ValidateZipCode(request.ZipCode));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiErrors.Validation(_messages.ValidationFailed, new Dictionary<string, string>
            {
                ["zipCode"] = exception.Message
            }));
        }
    }
}
