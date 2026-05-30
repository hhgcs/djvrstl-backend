using Djvrstl.Backend.Api;
using Djvrstl.Backend.Application.Admin;
using Djvrstl.Backend.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Controllers;

[ApiController]
[Route("admin")]
public sealed class AdminController(
    IAdminAuthService authService,
    IAdminDataService adminDataService,
    IProductImageStorageService productImageStorageService,
    IOptions<ApiMessagesOptions> messagesOptions,
    IOptions<AdminWorkflowOptions> adminOptions) : ControllerBase
{
    private readonly ApiMessagesOptions _messages = messagesOptions.Value;
    private readonly AdminWorkflowOptions _admin = adminOptions.Value;

    [HttpGet("session")]
    public async Task<ActionResult<AdminSessionResponse>> Session(CancellationToken cancellationToken)
    {
        return Ok(await authService.GetSessionAsync(User, cancellationToken));
    }

    [EnableRateLimiting(RateLimitPolicies.Login)]
    [HttpPost("login")]
    public async Task<ActionResult<AdminSessionResponse>> Login(AdminLoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(ApiErrors.Validation(_messages.ValidationFailed, new Dictionary<string, string>
            {
                ["email"] = _messages.EmailRequired,
                ["password"] = _admin.Messages.InvalidCredentials
            }));
        }

        var response = await authService.LoginAsync(HttpContext, request.Email, request.Password, cancellationToken);
        return response is null
            ? Unauthorized(ApiErrors.BusinessRule(_admin.Messages.InvalidCredentials))
            : Ok(response);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(HttpContext, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("products")]
    public async Task<ActionResult<ProductResponse[]>> Products(CancellationToken cancellationToken)
    {
        return Ok(await adminDataService.GetProductsAsync(cancellationToken));
    }

    [Authorize]
    [HttpPost("products")]
    public async Task<ActionResult<ProductResponse>> SaveProduct(ProductResponse request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await adminDataService.SaveProductAsync(request, cancellationToken));
        }
        catch (AdminDataException exception)
        {
            return UnprocessableEntity(ApiErrors.BusinessRule(exception.Message));
        }
    }

    [Authorize]
    [HttpPatch("products/{id}")]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(
        string id,
        ProductResponse request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await adminDataService.SaveProductAsync(request with { Id = id }, cancellationToken));
        }
        catch (AdminDataException exception)
        {
            return UnprocessableEntity(ApiErrors.BusinessRule(exception.Message));
        }
    }

    [Authorize]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [HttpPost("products/images")]
    public async Task<ActionResult<ProductImageUploadResponse>> UploadProductImages(CancellationToken cancellationToken)
    {
        if (!Request.HasFormContentType)
        {
            return BadRequest(ApiErrors.BusinessRule("Product images must be uploaded as multipart/form-data."));
        }

        try
        {
            var form = await Request.ReadFormAsync(cancellationToken);
            return Ok(await productImageStorageService.SaveAsync(Request, form.Files, cancellationToken));
        }
        catch (ProductImageUploadException exception)
        {
            return UnprocessableEntity(ApiErrors.BusinessRule(exception.Message));
        }
    }

    [Authorize]
    [HttpGet("sales")]
    public async Task<ActionResult<AdminSaleResponse[]>> Sales(
        [FromQuery] string? productId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        return Ok(await adminDataService.GetSalesAsync(productId, status, search, cancellationToken));
    }

    [Authorize]
    [HttpGet("bookings")]
    public async Task<ActionResult<BookingCalendarEntryResponse[]>> Bookings(CancellationToken cancellationToken)
    {
        return Ok(await adminDataService.GetBookingsAsync(cancellationToken));
    }

    [Authorize]
    [HttpPost("bookings/manual")]
    public async Task<ActionResult<BookingCalendarEntryResponse>> SaveManualBooking(
        ManualBookingApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await adminDataService.SaveManualBookingAsync(request, cancellationToken));
        }
        catch (AdminDataException exception)
        {
            return Conflict(ApiErrors.BusinessRule(exception.Message));
        }
    }

    [Authorize]
    [HttpDelete("bookings/{id}")]
    public async Task<IActionResult> DeleteManualBooking(string id, CancellationToken cancellationToken)
    {
        var deleted = await adminDataService.DeleteManualBookingAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound(ApiErrors.BusinessRule(_admin.Messages.ManualBookingNotFound));
    }
}
