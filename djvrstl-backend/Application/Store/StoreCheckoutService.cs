using Djvrstl.Backend.Api;
using Djvrstl.Backend.Application.Payments;
using Djvrstl.Backend.Application.Shipping;
using Djvrstl.Backend.Domain;
using Djvrstl.Backend.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Application.Store;

public interface IStoreCheckoutService
{
    Task<StoreCheckoutResponse> CheckoutAsync(StoreCheckoutApiRequest request, CancellationToken cancellationToken);
    Task<StoreOrderStatusResponse?> GetOrderAsync(string id, CancellationToken cancellationToken);
}

public sealed class StoreCheckoutService(
    AppDbContext db,
    IShippingZoneService shippingZoneService,
    IPaymentProvider paymentProvider,
    IOptions<StoreWorkflowOptions> workflowOptions) : IStoreCheckoutService
{
    private readonly StoreWorkflowOptions _workflow = workflowOptions.Value;

    public async Task<StoreCheckoutResponse> CheckoutAsync(StoreCheckoutApiRequest request, CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Length == 0)
        {
            throw new StoreCheckoutException(_workflow.Messages.EmptyCart);
        }

        var shipping = shippingZoneService.ValidateZipCode(request.Customer!.Address!.PostalCode!);
        if (!shipping.CheckoutAllowed)
        {
            throw new StoreCheckoutException(_workflow.Messages.CheckoutBlocked);
        }

        var productIds = request.Items.Select(item => item.ProductId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToArray();
        var products = await db.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        var orderId = CreateId(_workflow.OrderIdPrefix);
        var orderItems = new List<OrderItem>();
        var subtotal = 0;

        foreach (var requestItem in request.Items)
        {
            if (string.IsNullOrWhiteSpace(requestItem.ProductId) || !products.TryGetValue(requestItem.ProductId, out var product))
            {
                throw new StoreCheckoutException(_workflow.Messages.ProductNotFound);
            }

            if (!product.Active)
            {
                throw new StoreCheckoutException(_workflow.Messages.ProductInactive);
            }

            if (requestItem.Quantity <= 0)
            {
                throw new StoreCheckoutException(_workflow.Messages.InvalidQuantity);
            }

            if (string.IsNullOrWhiteSpace(requestItem.Color) ||
                !product.Colors.Contains(requestItem.Color, StringComparer.OrdinalIgnoreCase))
            {
                throw new StoreCheckoutException(_workflow.Messages.InvalidColor);
            }

            var lineTotal = product.Price * requestItem.Quantity;
            subtotal += lineTotal;
            orderItems.Add(new OrderItem
            {
                Id = CreateId(_workflow.OrderItemIdPrefix),
                OrderId = orderId,
                ProductId = product.Id,
                ProductSlug = product.Slug,
                ProductName = product.Name,
                Color = requestItem.Color,
                Quantity = requestItem.Quantity,
                UnitPrice = product.Price,
                LineTotal = lineTotal,
                Dimensions = new ProductDimensions
                {
                    Height = product.Dimensions.Height,
                    Width = product.Dimensions.Width,
                    Length = product.Dimensions.Length
                }
            });
        }

        var order = new Order
        {
            Id = orderId,
            Status = _workflow.Statuses.Pending,
            Customer = ToSnapshot(request.Customer!),
            Subtotal = subtotal,
            ShippingFee = shipping.ShippingFee,
            Total = subtotal + shipping.ShippingFee,
            Currency = _workflow.Currency,
            Items = orderItems
        };

        var checkout = await paymentProvider.CreateCheckoutAsync(new CreateCheckoutRequest(
            order.Id,
            _workflow.CheckoutPurpose,
            order.Total,
            order.Currency,
            order.Customer.Email,
            BuildReturnUri(_workflow.SuccessUrlTemplate, order.Id),
            BuildReturnUri(_workflow.PendingUrlTemplate, order.Id),
            BuildReturnUri(_workflow.FailureUrlTemplate, order.Id)),
            cancellationToken);

        order.ProviderPreferenceId = checkout.ProviderPreferenceId;
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        return new StoreCheckoutResponse(order.Id, checkout.CheckoutUrl);
    }

    public async Task<StoreOrderStatusResponse?> GetOrderAsync(string id, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(candidate => candidate.Items)
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        return order is null ? null : ToResponse(order);
    }

    private static StoreOrderStatusResponse ToResponse(Order order)
    {
        return new StoreOrderStatusResponse(
            order.Id,
            order.Status,
            ToCustomerResponse(order.Customer),
            order.Items
                .OrderBy(item => item.ProductName)
                .Select(item => new OrderItemResponse(
                    item.ProductId,
                    item.ProductSlug,
                    item.ProductName,
                    item.Color,
                    item.Quantity,
                    item.UnitPrice,
                    item.LineTotal,
                    new ProductDimensionsResponse(item.Dimensions.Height, item.Dimensions.Width, item.Dimensions.Length)))
                .ToArray(),
            new OrderSummaryResponse(order.Subtotal, order.ShippingFee, order.Total, order.Currency));
    }

    private static CustomerSnapshot ToSnapshot(CustomerDataApiRequest customer)
    {
        return new CustomerSnapshot
        {
            Name = customer.Name!,
            Email = customer.Email!,
            Phone = customer.Phone!,
            Address = ToAddress(customer.Address!)
        };
    }

    private static Address ToAddress(AddressApiRequest address)
    {
        return new Address
        {
            Street = address.Street!,
            ExteriorNumber = address.ExteriorNumber!,
            InteriorNumber = address.InteriorNumber,
            Neighborhood = address.Neighborhood!,
            City = address.City!,
            State = address.State!,
            PostalCode = address.PostalCode!,
            Country = address.Country!,
            References = address.References
        };
    }

    private static CustomerDataResponse ToCustomerResponse(CustomerSnapshot customer)
    {
        return new CustomerDataResponse(
            customer.Name,
            customer.Email,
            customer.Phone,
            new AddressResponse(
                customer.Address.Street,
                customer.Address.ExteriorNumber,
                customer.Address.InteriorNumber,
                customer.Address.Neighborhood,
                customer.Address.City,
                customer.Address.State,
                customer.Address.PostalCode,
                customer.Address.Country,
                customer.Address.References));
    }

    private static Uri BuildReturnUri(string template, string orderId)
    {
        return new Uri(template.Replace("{orderId}", Uri.EscapeDataString(orderId), StringComparison.Ordinal));
    }

    private static string CreateId(string prefix)
    {
        return $"{prefix}{Guid.NewGuid():N}";
    }
}

public sealed class StoreCheckoutException(string message) : Exception(message);
