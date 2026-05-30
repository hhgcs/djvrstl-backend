using Djvrstl.Backend.Api;
using Djvrstl.Backend.Application.Booking;
using Djvrstl.Backend.Application.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Controllers;

[ApiController]
[Route("booking")]
public sealed class BookingController(
    IBookingPricingService bookingPricingService,
    IBookingReservationService bookingReservationService,
    IOptions<BookingPricingOptions> pricingOptions,
    IOptions<ApiMessagesOptions> messagesOptions) : ControllerBase
{
    private readonly BookingPricingOptions _pricingOptions = pricingOptions.Value;
    private readonly ApiMessagesOptions _messages = messagesOptions.Value;

    [HttpGet("pricing-config")]
    public ActionResult<BookingPricingConfigResponse> GetPricingConfig()
    {
        return Ok(new BookingPricingConfigResponse(
            _pricingOptions.Currency,
            _pricingOptions.IncludedHours,
            _pricingOptions.ExtraHourFee,
            _pricingOptions.MinimumDeposit,
            _pricingOptions.PackageBasePrices,
            _pricingOptions.PackageNames,
            _pricingOptions.PackageIncludes,
            _pricingOptions.AttendeeRangeFees,
            _pricingOptions.EstimateNote));
    }

    [HttpPost("quote")]
    public ActionResult<BookingQuote> Quote(BookingQuoteApiRequest request)
    {
        var validationErrors = ValidateQuoteRequest(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(ApiErrors.Validation(_messages.ValidationFailed, validationErrors));
        }

        try
        {
            var quote = bookingPricingService.CalculateQuote(new BookingQuoteRequest(
                request.PackageId!,
                request.Date,
                request.DurationHours,
                request.AttendeeRange!,
                new AddressDto(
                    request.Address!.Street!,
                    request.Address.ExteriorNumber!,
                    request.Address.InteriorNumber,
                    request.Address.Neighborhood!,
                    request.Address.City!,
                    request.Address.State!,
                    request.Address.PostalCode!,
                    request.Address.Country!,
                    request.Address.References)));

            return Ok(quote);
        }
        catch (ArgumentException exception)
        {
            return UnprocessableEntity(ApiErrors.BusinessRule(exception.Message));
        }
    }

    [HttpGet("availability")]
    public async Task<ActionResult<BookingAvailabilityResponse>> Availability([FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        if (date is null)
        {
            return BadRequest(ApiErrors.Validation(_messages.ValidationFailed, new Dictionary<string, string>
            {
                ["date"] = _messages.DateRequired
            }));
        }

        return Ok(await bookingReservationService.GetAvailabilityAsync(date.Value, cancellationToken));
    }

    [EnableRateLimiting(RateLimitPolicies.BookingHold)]
    [HttpPost("hold")]
    public async Task<ActionResult<CreateBookingHoldResult>> Hold(CreateBookingHoldApiRequest request, CancellationToken cancellationToken)
    {
        var validationErrors = ValidateHoldRequest(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(ApiErrors.Validation(_messages.ValidationFailed, validationErrors));
        }

        try
        {
            var result = await bookingReservationService.CreateHoldAsync(new CreateBookingHoldRequest(
                request.Quote!,
                new CustomerDataDto(
                    request.Customer!.Name!,
                    request.Customer.Email!,
                    request.Customer.Phone!,
                    ToAddressDto(request.Customer.Address!))),
                cancellationToken);

            return Ok(result);
        }
        catch (BookingQuoteMismatchException exception)
        {
            return UnprocessableEntity(ApiErrors.BusinessRule(exception.Message));
        }
        catch (BookingDateUnavailableException exception)
        {
            return Conflict(ApiErrors.BusinessRule(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return UnprocessableEntity(ApiErrors.BusinessRule(exception.Message));
        }
    }

    [HttpGet("status/{id}")]
    public async Task<ActionResult<BookingStatusResponse>> Status(string id, CancellationToken cancellationToken)
    {
        var response = await bookingReservationService.GetStatusAsync(id, cancellationToken);
        return response is null
            ? NotFound(ApiErrors.BusinessRule(_messages.BookingNotFound))
            : Ok(response);
    }

    private Dictionary<string, string> ValidateQuoteRequest(BookingQuoteApiRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.PackageId))
        {
            errors["packageId"] = _messages.PackageRequired;
        }

        if (string.IsNullOrWhiteSpace(request.AttendeeRange))
        {
            errors["attendeeRange"] = _messages.AttendeeRangeRequired;
        }

        if (request.Date < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            errors["date"] = _messages.PastDate;
        }

        if (request.DurationHours < _pricingOptions.IncludedHours)
        {
            errors["durationHours"] = _messages.InvalidDuration;
        }

        if (request.Address is null)
        {
            errors["address"] = _messages.AddressRequired;
            return errors;
        }

        Require(request.Address.Street, "address.street", _messages.StreetRequired, errors);
        Require(request.Address.ExteriorNumber, "address.exteriorNumber", _messages.ExteriorNumberRequired, errors);
        Require(request.Address.Neighborhood, "address.neighborhood", _messages.NeighborhoodRequired, errors);
        Require(request.Address.City, "address.city", _messages.CityRequired, errors);
        Require(request.Address.State, "address.state", _messages.StateRequired, errors);
        Require(request.Address.PostalCode, "address.postalCode", _messages.PostalCodeRequired, errors);
        Require(request.Address.Country, "address.country", _messages.CountryRequired, errors);

        if (!string.IsNullOrWhiteSpace(request.Address.Country) &&
            !string.Equals(request.Address.Country, _pricingOptions.RequiredCountry, StringComparison.OrdinalIgnoreCase))
        {
            errors["address.country"] = _messages.CountryNotSupported;
        }

        if (!string.IsNullOrWhiteSpace(request.Address.PostalCode) &&
            (request.Address.PostalCode.Length != _pricingOptions.PostalCodeLength ||
             !request.Address.PostalCode.All(char.IsDigit)))
        {
            errors["address.postalCode"] = _messages.InvalidPostalCode;
        }

        return errors;
    }

    private Dictionary<string, string> ValidateHoldRequest(CreateBookingHoldApiRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (request.Quote is null)
        {
            errors["quote"] = _messages.QuoteRequired;
        }

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

        if (request.Quote is not null)
        {
            ValidateQuote(request.Quote, errors);
        }

        return errors;
    }

    private void ValidateQuote(BookingQuote quote, Dictionary<string, string> errors)
    {
        if (string.IsNullOrWhiteSpace(quote.PackageId))
        {
            errors["quote.packageId"] = _messages.PackageRequired;
        }

        if (string.IsNullOrWhiteSpace(quote.AttendeeRange))
        {
            errors["quote.attendeeRange"] = _messages.AttendeeRangeRequired;
        }

        if (quote.Date < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            errors["quote.date"] = _messages.PastDate;
        }

        if (quote.DurationHours < _pricingOptions.IncludedHours)
        {
            errors["quote.durationHours"] = _messages.InvalidDuration;
        }

        ValidateAddress(new AddressApiRequest(
            quote.Address.Street,
            quote.Address.ExteriorNumber,
            quote.Address.InteriorNumber,
            quote.Address.Neighborhood,
            quote.Address.City,
            quote.Address.State,
            quote.Address.PostalCode,
            quote.Address.Country,
            quote.Address.References), "quote.address", errors);
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

        if (!string.IsNullOrWhiteSpace(address.Country) &&
            !string.Equals(address.Country, _pricingOptions.RequiredCountry, StringComparison.OrdinalIgnoreCase))
        {
            errors[$"{prefix}.country"] = _messages.CountryNotSupported;
        }

        if (!string.IsNullOrWhiteSpace(address.PostalCode) &&
            (address.PostalCode.Length != _pricingOptions.PostalCodeLength ||
             !address.PostalCode.All(char.IsDigit)))
        {
            errors[$"{prefix}.postalCode"] = _messages.InvalidPostalCode;
        }
    }

    private static AddressDto ToAddressDto(AddressApiRequest address)
    {
        return new AddressDto(
            address.Street!,
            address.ExteriorNumber!,
            address.InteriorNumber,
            address.Neighborhood!,
            address.City!,
            address.State!,
            address.PostalCode!,
            address.Country!,
            address.References);
    }

    private static void Require(string? value, string field, string message, Dictionary<string, string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = message;
        }
    }
}
