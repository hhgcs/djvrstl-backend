using Djvrstl.Backend.Application.Booking;
using Djvrstl.Backend.Application.Shipping;

namespace Djvrstl.Backend.Tests;

internal static class TestOptions
{
    public static BookingPricingOptions BookingPricing()
    {
        return new BookingPricingOptions
        {
            Currency = "MXN",
            IncludedHours = 5,
            ExtraHourFee = 1200,
            MinimumDeposit = 1500,
            RequiredCountry = "MX",
            PostalCodeLength = 5,
            UnknownPackageMessage = "Unknown booking package.",
            MissingPackageNameMessage = "Missing package display name.",
            UnknownAttendeeRangeMessage = "Unknown attendee range.",
            InvalidDurationMessage = "Duration cannot be shorter than included hours.",
            QuoteNote = "Incluye 5 horas base. El anticipo minimo para reservar es 1500 MXN.",
            EstimateNote = "Incluye 5 horas base. El total final se confirma desde backend.",
            PackageBasePrices = new Dictionary<string, int>
            {
                ["essentials"] = 5500,
                ["signature"] = 7500
            },
            PackageNames = new Dictionary<string, string>
            {
                ["essentials"] = "Esencial",
                ["signature"] = "Premium"
            },
            PackageIncludes = new Dictionary<string, string[]>
            {
                ["essentials"] = ["5hrs", "Bocinas", "Cabina", "Luces"],
                ["signature"] = ["5hrs", "4 Bocinas", "Pirotecnia", "Maquinas de CO2"]
            },
            AttendeeRangeFees = new Dictionary<string, int>
            {
                ["10-99"] = 0,
                ["100-199"] = 3000,
                ["200-299"] = 5500,
                ["300+"] = 7500
            }
        };
    }

    public static ShippingOptions Shipping()
    {
        return new ShippingOptions
        {
            PostalCodeLength = 5,
            InvalidPostalCodeMessage = "Postal code format is invalid.",
            MissingFallbackZoneMessage = "Shipping fallback zone is not configured.",
            Zones =
            [
                new ShippingZoneOptions
                {
                    Zone = 1,
                    Label = "Zona 1",
                    ShippingFee = 0,
                    CheckoutAllowed = true,
                    Message = "Envio gratis confirmado por backend.",
                    PostalCodes = ["04650", "04660", "04630", "04410"]
                },
                new ShippingZoneOptions
                {
                    Zone = 2,
                    Label = "Zona 2",
                    ShippingFee = 200,
                    CheckoutAllowed = true,
                    Message = "Se agrego una tarifa fija de envio confirmada por backend.",
                    PostalCodeRanges = [new PostalCodeRangeOptions { From = 1000, To = 16999 }]
                },
                new ShippingZoneOptions
                {
                    Zone = 3,
                    Label = "Zona 3",
                    ShippingFee = 0,
                    CheckoutAllowed = false,
                    Message = "Esta zona requiere cotizacion de envio por WhatsApp.",
                    Fallback = true
                }
            ]
        };
    }

    public static BookingWorkflowOptions BookingWorkflow()
    {
        return new BookingWorkflowOptions
        {
            HoldDurationMinutes = 15,
            ExpirationPollSeconds = 60,
            ExpirationBatchSize = 50,
            BookingIdPrefix = "booking_",
            HoldIdPrefix = "hold_",
            CheckoutPurpose = "booking_deposit",
            SuccessUrlTemplate = "http://localhost:3000/payments/booking/success?booking={bookingId}",
            PendingUrlTemplate = "http://localhost:3000/payments/booking/pending?booking={bookingId}",
            FailureUrlTemplate = "http://localhost:3000/payments/booking/failure?booking={bookingId}",
            Statuses = new BookingStatusesOptions
            {
                Held = "held",
                PendingPayment = "pending_payment",
                Confirmed = "confirmed",
                Expired = "expired",
                ManualBlock = "manual_block"
            },
            Messages = new BookingWorkflowMessagesOptions
            {
                ConfirmedUnavailableReason = "Fecha bloqueada por evento confirmado.",
                HoldUnavailableReason = "Fecha retenida temporalmente por otro cliente.",
                ManualBlockUnavailableReason = "Fecha bloqueada manualmente.",
                QuoteMismatch = "The submitted quote no longer matches the server-calculated quote.",
                DateUnavailable = "The selected date is unavailable.",
                BookingNotFound = "Booking was not found."
            }
        };
    }
}
