using Djvrstl.Backend.Application.Booking;
using Djvrstl.Backend.Application.Shipping;
using Xunit;

namespace Djvrstl.Backend.Tests;

public sealed class OptionsValidatorTests
{
    [Fact]
    public void BookingPricingOptionsValidator_FailsWhenRequiredConfigIsMissing()
    {
        var result = new BookingPricingOptionsValidator().Validate(null, new BookingPricingOptions());

        Assert.True(result.Failed);
    }

    [Fact]
    public void ShippingOptionsValidator_FailsWhenFallbackZoneIsMissing()
    {
        var options = TestOptions.Shipping();
        foreach (var zone in options.Zones)
        {
            zone.Fallback = false;
        }

        var result = new ShippingOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void BookingWorkflowOptionsValidator_FailsWhenRequiredConfigIsMissing()
    {
        var result = new BookingWorkflowOptionsValidator().Validate(null, new BookingWorkflowOptions());

        Assert.True(result.Failed);
    }
}
