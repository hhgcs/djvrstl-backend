namespace Djvrstl.Backend.Application.Payments;

public sealed class FakePaymentOptions
{
    public const string SectionName = "Payments:Fake";

    public string CheckoutBaseUrl { get; set; } = string.Empty;
    public string DefaultPaymentStatus { get; set; } = string.Empty;
}
