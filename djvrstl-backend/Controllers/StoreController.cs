using Djvrstl.Backend.Api;
using Djvrstl.Backend.Application.Security;
using Djvrstl.Backend.Application.Store;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Controllers;

[ApiController]
[Route("store")]
public sealed class StoreController(
    IStoreCheckoutService storeCheckoutService,
    IOptions<ApiMessagesOptions> messagesOptions,
    IOptions<StoreWorkflowOptions> storeOptions) : ControllerBase
{
    private readonly ApiMessagesOptions _messages = messagesOptions.Value;
    private readonly StoreWorkflowOptions _store = storeOptions.Value;

    [EnableRateLimiting(RateLimitPolicies.StoreCheckout)]
    [HttpPost("checkout")]
    public async Task<ActionResult<StoreCheckoutResponse>> Checkout(StoreCheckoutApiRequest request, CancellationToken cancellationToken)
    {
        var errors = ValidateCheckout(request);
        if (errors.Count > 0)
        {
            return BadRequest(ApiErrors.Validation(_messages.ValidationFailed, errors));
        }

        try
        {
            return Ok(await storeCheckoutService.CheckoutAsync(request, cancellationToken));
        }
        catch (StoreCheckoutException exception)
        {
            return UnprocessableEntity(ApiErrors.BusinessRule(exception.Message));
        }
    }

    [HttpGet("orders/{id}")]
    public async Task<ActionResult<StoreOrderStatusResponse>> GetOrder(string id, CancellationToken cancellationToken)
    {
        var order = await storeCheckoutService.GetOrderAsync(id, cancellationToken);
        return order is null
            ? NotFound(ApiErrors.BusinessRule(_store.Messages.OrderNotFound))
            : Ok(order);
    }

    private Dictionary<string, string> ValidateCheckout(StoreCheckoutApiRequest request)
    {
        var errors = new Dictionary<string, string>();
        if (request.Customer is null)
        {
            errors["customer"] = _messages.CustomerRequired;
        }
        else
        {
            Require(request.Customer.Name, "customer.name", _messages.NameRequired, errors);
            Require(request.Customer.Email, "customer.email", _messages.EmailRequired, errors);
            Require(request.Customer.Phone, "customer.phone", _messages.PhoneRequired, errors);
            if (request.Customer.Address is null)
            {
                errors["customer.address"] = _messages.AddressRequired;
            }
            else
            {
                ValidateAddress(request.Customer.Address, "customer.address", errors);
            }
        }

        if (request.Items is null || request.Items.Length == 0)
        {
            errors["items"] = _store.Messages.EmptyCart;
        }

        return errors;
    }

    private void ValidateAddress(AddressApiRequest address, string prefix, Dictionary<string, string> errors)
    {
        Require(address.Street, $"{prefix}.street", _messages.StreetRequired, errors);
        Require(address.ExteriorNumber, $"{prefix}.exteriorNumber", _messages.ExteriorNumberRequired, errors);
        Require(address.Neighborhood, $"{prefix}.neighborhood", _messages.NeighborhoodRequired, errors);
        Require(address.City, $"{prefix}.city", _messages.CityRequired, errors);
        Require(address.State, $"{prefix}.state", _messages.StateRequired, errors);
        Require(address.PostalCode, $"{prefix}.postalCode", _messages.PostalCodeRequired, errors);
        Require(address.Country, $"{prefix}.country", _messages.CountryRequired, errors);
    }

    private static void Require(string? value, string field, string message, Dictionary<string, string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = message;
        }
    }
}
