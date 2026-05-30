using Djvrstl.Backend.Application.Shipping;
using Microsoft.Extensions.Options;
using Xunit;

namespace Djvrstl.Backend.Tests;

public sealed class ShippingZoneServiceTests
{
    [Theory]
    [InlineData("04650", 1, 0, true)]
    [InlineData("06700", 2, 200, true)]
    [InlineData("64000", 3, 0, false)]
    public void ValidateZipCode_ReturnsExpectedShippingDecision(
        string zipCode,
        int expectedZone,
        int expectedFee,
        bool expectedCheckoutAllowed)
    {
        var service = new ShippingZoneService(Options.Create(TestOptions.Shipping()));

        var decision = service.ValidateZipCode(zipCode);

        Assert.Equal(expectedZone, decision.Zone);
        Assert.Equal(expectedFee, decision.ShippingFee);
        Assert.Equal(expectedCheckoutAllowed, decision.CheckoutAllowed);
    }

    [Fact]
    public void ValidateZipCode_RejectsInvalidPostalCode()
    {
        var service = new ShippingZoneService(Options.Create(TestOptions.Shipping()));

        Assert.Throws<ArgumentException>(() => service.ValidateZipCode("abc"));
    }
}
