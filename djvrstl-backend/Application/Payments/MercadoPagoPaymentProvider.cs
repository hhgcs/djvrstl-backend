using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Application.Payments;

public sealed class MercadoPagoPaymentProvider(
    HttpClient httpClient,
    IOptions<PaymentWorkflowOptions> paymentOptions) : IPaymentProvider
{
    private readonly PaymentWorkflowOptions _options = paymentOptions.Value;

    public async Task<CheckoutSession> CreateCheckoutAsync(CreateCheckoutRequest request, CancellationToken cancellationToken)
    {
        httpClient.BaseAddress = new Uri(_options.MercadoPago.BaseUrl);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.MercadoPago.AccessToken);

        var payload = new
        {
            items = new[]
            {
                new
                {
                    title = request.Purpose,
                    quantity = 1,
                    unit_price = request.Amount,
                    currency_id = request.Currency
                }
            },
            payer = new
            {
                email = request.CustomerEmail
            },
            external_reference = request.ReferenceId,
            back_urls = new
            {
                success = request.SuccessUrl.ToString(),
                pending = request.PendingUrl.ToString(),
                failure = request.FailureUrl.ToString()
            },
            notification_url = string.IsNullOrWhiteSpace(_options.MercadoPago.NotificationUrl)
                ? null
                : _options.MercadoPago.NotificationUrl
        };

        using var response = await httpClient.PostAsJsonAsync(_options.MercadoPago.PreferencesPath, payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        var preferenceId = root.GetProperty("id").GetString() ?? string.Empty;
        var checkoutUrl = root.GetProperty(_options.MercadoPago.CheckoutUrlPropertyName).GetString() ?? string.Empty;

        return new CheckoutSession(preferenceId, checkoutUrl);
    }

    public Task<ProviderPaymentStatus> GetPaymentStatusAsync(string providerPaymentId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ProviderPaymentStatus(providerPaymentId, _options.StatusMapping.Pending));
    }
}
