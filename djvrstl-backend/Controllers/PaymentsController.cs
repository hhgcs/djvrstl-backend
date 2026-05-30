using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Djvrstl.Backend.Api;
using Djvrstl.Backend.Application.Payments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Controllers;

[ApiController]
[Route("payments")]
public sealed class PaymentsController(
    IPaymentEventService paymentEventService,
    IOptions<PaymentWorkflowOptions> paymentOptions) : ControllerBase
{
    private readonly PaymentWorkflowOptions _payments = paymentOptions.Value;

    [HttpPost("webhooks/mercadopago")]
    public async Task<ActionResult<PaymentWebhookResponse>> MercadoPago(JsonElement payload, CancellationToken cancellationToken)
    {
        if (_payments.MercadoPago.RequireSignature && !HasValidSignature(payload.GetRawText()))
        {
            return Unauthorized();
        }

        var webhookEvent = PaymentEventService.ParseMercadoPagoWebhook(payload, _payments);
        var status = await paymentEventService.HandleAsync(webhookEvent, cancellationToken);
        return Ok(new PaymentWebhookResponse(status));
    }

    private bool HasValidSignature(string rawPayload)
    {
        if (string.IsNullOrWhiteSpace(_payments.MercadoPago.WebhookSigningSecret) ||
            string.IsNullOrWhiteSpace(_payments.MercadoPago.SignatureHeaderName) ||
            !Request.Headers.TryGetValue(_payments.MercadoPago.SignatureHeaderName, out var signature))
        {
            return false;
        }

        var secretBytes = Encoding.UTF8.GetBytes(_payments.MercadoPago.WebhookSigningSecret);
        var payloadBytes = Encoding.UTF8.GetBytes(rawPayload);
        var expected = Convert.ToHexString(HMACSHA256.HashData(secretBytes, payloadBytes));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature.ToString()));
    }
}
