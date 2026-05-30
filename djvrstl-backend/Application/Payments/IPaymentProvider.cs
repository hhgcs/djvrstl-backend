using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Application.Payments;

public interface IPaymentProvider
{
    Task<CheckoutSession> CreateCheckoutAsync(CreateCheckoutRequest request, CancellationToken cancellationToken);
    Task<ProviderPaymentStatus> GetPaymentStatusAsync(string providerPaymentId, CancellationToken cancellationToken);
}

public sealed class FakePaymentProvider(IOptions<FakePaymentOptions> options) : IPaymentProvider
{
    private readonly FakePaymentOptions _options = options.Value;

    public Task<CheckoutSession> CreateCheckoutAsync(CreateCheckoutRequest request, CancellationToken cancellationToken)
    {
        var preferenceId = $"fake_{Guid.NewGuid():N}";
        var checkoutUrl = $"{_options.CheckoutBaseUrl}?pref_id={preferenceId}&reference={Uri.EscapeDataString(request.ReferenceId)}";

        return Task.FromResult(new CheckoutSession(preferenceId, checkoutUrl));
    }

    public Task<ProviderPaymentStatus> GetPaymentStatusAsync(string providerPaymentId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ProviderPaymentStatus(providerPaymentId, _options.DefaultPaymentStatus));
    }
}

public sealed record CreateCheckoutRequest(
    string ReferenceId,
    string Purpose,
    int Amount,
    string Currency,
    string CustomerEmail,
    Uri SuccessUrl,
    Uri PendingUrl,
    Uri FailureUrl);

public sealed record CheckoutSession(string ProviderPreferenceId, string CheckoutUrl);

public sealed record ProviderPaymentStatus(string ProviderPaymentId, string Status);
