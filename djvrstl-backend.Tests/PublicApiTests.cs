using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Djvrstl.Backend.Domain;
using Djvrstl.Backend.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Djvrstl.Backend.Tests;

public sealed class PublicApiTests
{
    [Fact]
    public async Task CatalogProducts_ReturnsOnlyActiveProducts()
    {
        await using var factory = new TestAppFactory();
        await factory.SeedProductsAsync();
        var client = factory.CreateClient();

        var products = await client.GetFromJsonAsync<JsonArray>("/catalog/products");

        Assert.NotNull(products);
        Assert.Equal(2, products!.Count);
        Assert.DoesNotContain(products, product => product?["id"]?.GetValue<string>() == "inactive-product");
    }

    [Fact]
    public async Task ShippingValidateZip_ReturnsConfiguredZoneTwoFee()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/shipping/validate-zip", new { zipCode = "06700" });
        var body = await response.Content.ReadFromJsonAsync<JsonObject>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, body!["zone"]!.GetValue<int>());
        Assert.Equal(200, body["shippingFee"]!.GetValue<int>());
        Assert.True(body["checkoutAllowed"]!.GetValue<bool>());
    }

    [Fact]
    public async Task BookingPricingConfig_ReturnsConfiguredPackageIncludes()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var config = await client.GetFromJsonAsync<JsonObject>("/booking/pricing-config");

        Assert.NotNull(config);
        Assert.Equal(1500, config!["minimumDeposit"]!.GetValue<int>());
        Assert.NotNull(config["packageIncludes"]!["signature"]);
    }

    [Fact]
    public async Task BookingQuote_ReturnsServerCalculatedTotal()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)).ToString("yyyy-MM-dd");

        var response = await client.PostAsJsonAsync("/booking/quote", new
        {
            packageId = "signature",
            date,
            durationHours = 6,
            attendeeRange = "100-199",
            address = ValidAddress()
        });
        var quote = await response.Content.ReadFromJsonAsync<JsonObject>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(11700, quote!["total"]!.GetValue<int>());
        Assert.Equal(1500, quote["depositTotal"]!.GetValue<int>());
        Assert.Equal(10200, quote["remainingBalance"]!.GetValue<int>());
    }

    [Fact]
    public async Task BookingQuote_ReturnsErrorEnvelopeForInvalidAddress()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)).ToString("yyyy-MM-dd");

        var response = await client.PostAsJsonAsync("/booking/quote", new
        {
            packageId = "signature",
            date,
            durationHours = 6,
            attendeeRange = "100-199",
            address = ValidAddress() with { postalCode = "abc" }
        });
        var body = await response.Content.ReadFromJsonAsync<JsonObject>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", body!["error"]!["code"]!.GetValue<string>());
        Assert.NotNull(body["error"]!["fields"]!["address.postalCode"]);
    }

    [Fact]
    public async Task BookingAvailability_ReturnsAvailableForOpenDate()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45)).ToString("yyyy-MM-dd");

        var availability = await client.GetFromJsonAsync<JsonObject>($"/booking/availability?date={date}");

        Assert.NotNull(availability);
        Assert.True(availability!["available"]!.GetValue<bool>());
    }

    [Fact]
    public async Task BookingHold_CreatesHoldAndStatusCanBeRead()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();
        var holdRequest = CreateHoldPayload();

        var holdResponse = await client.PostAsJsonAsync("/booking/hold", holdRequest);
        var hold = await holdResponse.Content.ReadFromJsonAsync<JsonObject>();

        Assert.Equal(HttpStatusCode.OK, holdResponse.StatusCode);
        Assert.StartsWith("hold_", hold!["holdId"]!.GetValue<string>());
        Assert.Contains("pref_id=fake_", hold["checkoutUrl"]!.GetValue<string>(), StringComparison.Ordinal);

        var status = await client.GetFromJsonAsync<JsonObject>($"/booking/status/{hold["holdId"]!.GetValue<string>()}");

        Assert.NotNull(status);
        Assert.Equal("pending_payment", status!["status"]!.GetValue<string>());
        Assert.Equal(1500, status["depositTotal"]!.GetValue<int>());
    }

    [Fact]
    public async Task BookingHold_ReturnsConflictForSameDate()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();
        var holdRequest = CreateHoldPayload();
        var first = await client.PostAsJsonAsync("/booking/hold", holdRequest);

        var second = await client.PostAsJsonAsync("/booking/hold", holdRequest);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task StoreCheckout_CreatesOrderWithServerCalculatedSnapshot()
    {
        await using var factory = new TestAppFactory();
        await factory.SeedProductsAsync();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/store/checkout", new
        {
            customer = ValidCustomer(),
            items = new[] { new { productId = "active-one", color = "Negro", quantity = 2 } },
            summary = new { subtotal = 1, shippingFee = 1, total = 2, currency = "MXN" }
        });
        var body = await response.Content.ReadFromJsonAsync<JsonObject>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("order_", body!["orderId"]!.GetValue<string>());
        Assert.Contains("pref_id=fake_", body["checkoutUrl"]!.GetValue<string>(), StringComparison.Ordinal);

        var order = await client.GetFromJsonAsync<JsonObject>($"/store/orders/{body["orderId"]!.GetValue<string>()}");
        Assert.Equal(200, order!["summary"]!["subtotal"]!.GetValue<int>());
        Assert.Equal(200, order["summary"]!["shippingFee"]!.GetValue<int>());
        Assert.Equal(400, order["summary"]!["total"]!.GetValue<int>());
    }

    [Fact]
    public async Task StoreCheckout_RejectsInactiveProduct()
    {
        await using var factory = new TestAppFactory();
        await factory.SeedProductsAsync();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/store/checkout", new
        {
            customer = ValidCustomer(),
            items = new[] { new { productId = "inactive-product", color = "Negro", quantity = 1 } }
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Leads_CreateSanitizesAndPersistsLead()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/leads", new
        {
            name = "<b>Mariana</b>",
            email = "mariana@example.com",
            phone = "5512345678",
            message = "<script>alert(1)</script>Boda"
        });
        var body = await response.Content.ReadFromJsonAsync<JsonObject>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.StartsWith("lead_", body!["leadId"]!.GetValue<string>());
        Assert.Equal("received", body["status"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var lead = await db.Leads.SingleAsync();
        Assert.DoesNotContain("<", lead.Name, StringComparison.Ordinal);
        Assert.DoesNotContain("<", lead.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminEndpoints_RequireLoginAndAllowProductManagement()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var unauthorized = await client.GetAsync("/admin/products");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        var login = await client.PostAsJsonAsync("/admin/login", new
        {
            email = "admin@example.com",
            password = "dev-admin-password"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var save = await client.PostAsJsonAsync("/admin/products", new
        {
            id = "",
            slug = "",
            name = "Cabina Test",
            description = "Prueba",
            dimensions = new { height = 100, width = 50, length = 70 },
            tags = new[] { "test" },
            colors = new[] { "Negro" },
            price = 1500,
            active = true,
            images = new[] { "https://cdn.example.com/cabina-test.jpg" },
            amazonUrl = (string?)null
        });
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);

        var saved = await save.Content.ReadFromJsonAsync<JsonObject>();
        Assert.StartsWith("product_", saved!["id"]!.GetValue<string>());
    }

    [Fact]
    public async Task AdminProductImageUpload_RequiresLoginAndReturnsImageUrl()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var unauthorized = await client.PostAsync("/admin/products/images", ImageUploadContent());
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        var login = await client.PostAsJsonAsync("/admin/login", new
        {
            email = "admin@example.com",
            password = "dev-admin-password"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var upload = await client.PostAsync("/admin/products/images", ImageUploadContent());
        var body = await upload.Content.ReadFromJsonAsync<JsonObject>();

        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var image = body!["images"]![0]!.GetValue<string>();
        Assert.Contains("/uploads/products/", image, StringComparison.Ordinal);
        Assert.EndsWith(".png", image, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PaymentWebhook_UpdatesOrderStatusIdempotently()
    {
        await using var factory = new TestAppFactory();
        await factory.SeedProductsAsync();
        var client = factory.CreateClient();
        var checkout = await client.PostAsJsonAsync("/store/checkout", new
        {
            customer = ValidCustomer(),
            items = new[] { new { productId = "active-one", color = "Negro", quantity = 1 } }
        });
        var checkoutBody = await checkout.Content.ReadFromJsonAsync<JsonObject>();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await db.Orders.SingleAsync();

        var webhook = new
        {
            id = "evt_test",
            preferenceId = order.ProviderPreferenceId,
            status = "approved"
        };

        var first = await client.PostAsJsonAsync("/payments/webhooks/mercadopago", webhook);
        var second = await client.PostAsJsonAsync("/payments/webhooks/mercadopago", webhook);
        var status = await client.GetFromJsonAsync<JsonObject>($"/store/orders/{checkoutBody!["orderId"]!.GetValue<string>()}");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal("paid", status!["status"]!.GetValue<string>());
        Assert.Equal(1, await db.PaymentEvents.CountAsync());
    }

    private static QuoteAddress ValidAddress()
    {
        return new QuoteAddress(
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

    private static object CreateHoldPayload()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45)).ToString("yyyy-MM-dd");

        return new
        {
            quote = new
            {
                packageId = "signature",
                packageName = "Premium",
                date,
                durationHours = 6,
                attendeeRange = "100-199",
                address = ValidAddress(),
                subtotal = 7500,
                attendeeFee = 3000,
                extraHoursFee = 1200,
                locationFee = 0,
                total = 11700,
                depositTotal = 1500,
                remainingBalance = 10200,
                currency = "MXN",
                note = "Incluye 5 horas base. El anticipo minimo para reservar es 1500 MXN."
            },
            customer = new
            {
                name = "Mariana Ortiz",
                email = "mariana@example.com",
                phone = "5512345678",
                address = ValidAddress()
            }
        };
    }

    private static object ValidCustomer()
    {
        return new
        {
            name = "Mariana Ortiz",
            email = "mariana@example.com",
            phone = "5512345678",
            address = ValidAddress()
        };
    }

    private static MultipartFormDataContent ImageUploadContent()
    {
        var content = new MultipartFormDataContent();
        var image = new ByteArrayContent([137, 80, 78, 71, 13, 10, 26, 10]);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(image, "files", "cabina.png");
        return content;
    }

    private sealed record QuoteAddress(
        string street,
        string exteriorNumber,
        string? interiorNumber,
        string neighborhood,
        string city,
        string state,
        string postalCode,
        string country,
        string? references);

    private sealed class TestAppFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"djvrstl-tests-{Guid.NewGuid():N}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AdminWorkflow:InitialAdmin:Enabled"] = "true",
                    ["AdminWorkflow:InitialAdmin:Email"] = "admin@example.com",
                    ["AdminWorkflow:InitialAdmin:Password"] = "dev-admin-password",
                    ["AdminWorkflow:InitialAdmin:Name"] = "Demo Admin",
                    ["AdminWorkflow:InitialAdmin:Role"] = "manager",
                    ["AdminAuth:CookieSecurePolicy"] = "SameAsRequest",
                    ["ProductImageUploads:StoragePath"] = Path.Combine(
                        Path.GetTempPath(),
                        $"djvrstl-test-uploads-{Guid.NewGuid():N}")
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });
            });
        }

        public async Task SeedProductsAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Products.AddRange(
                Product("active-one", true),
                Product("active-two", true),
                Product("inactive-product", false));

            await db.SaveChangesAsync();
        }

        private static Product Product(string id, bool active)
        {
            return new Product
            {
                Id = id,
                Slug = id,
                Name = id,
                Description = id,
                Dimensions = new ProductDimensions
                {
                    Height = 180,
                    Width = 70,
                    Length = 110
                },
                Tags = ["test"],
                Colors = ["Negro"],
                Price = 100,
                Active = active,
                Images = ["https://cdn.example.com/test.jpg"]
            };
        }
    }
}
