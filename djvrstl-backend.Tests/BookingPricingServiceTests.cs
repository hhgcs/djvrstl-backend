using Djvrstl.Backend.Application.Booking;
using Microsoft.Extensions.Options;
using Xunit;

namespace Djvrstl.Backend.Tests;

public sealed class BookingPricingServiceTests
{
    [Fact]
    public void CalculateQuote_UsesServerPricingForSignatureExtraHourAndAttendeeFee()
    {
        var service = new BookingPricingService(Options.Create(TestOptions.BookingPricing()));

        var quote = service.CalculateQuote(new BookingQuoteRequest(
            "signature",
            new DateOnly(2026, 6, 21),
            6,
            "100-199",
            Address()));

        Assert.Equal(7500, quote.Subtotal);
        Assert.Equal(3000, quote.AttendeeFee);
        Assert.Equal(1200, quote.ExtraHoursFee);
        Assert.Equal(11700, quote.Total);
        Assert.Equal(1500, quote.DepositTotal);
        Assert.Equal(10200, quote.RemainingBalance);
        Assert.Equal("MXN", quote.Currency);
    }

    [Fact]
    public void CalculateQuote_RejectsUnknownPackage()
    {
        var service = new BookingPricingService(Options.Create(TestOptions.BookingPricing()));

        Assert.Throws<ArgumentException>(() => service.CalculateQuote(new BookingQuoteRequest(
            "unknown",
            new DateOnly(2026, 6, 21),
            5,
            "10-99",
            Address())));
    }

    private static AddressDto Address()
    {
        return new AddressDto(
            "Av. Alvaro Obregon",
            "120",
            "4B",
            "Roma Norte",
            "Ciudad de Mexico",
            "CDMX",
            "06700",
            "MX",
            "Entrada por la calle lateral");
    }
}
