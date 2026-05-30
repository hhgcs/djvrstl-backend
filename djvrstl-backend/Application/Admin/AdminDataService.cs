using Djvrstl.Backend.Api;
using Djvrstl.Backend.Application.Booking;
using Djvrstl.Backend.Application.Store;
using Djvrstl.Backend.Domain;
using Djvrstl.Backend.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Application.Admin;

public interface IAdminDataService
{
    Task<ProductResponse[]> GetProductsAsync(CancellationToken cancellationToken);
    Task<ProductResponse> SaveProductAsync(ProductResponse product, CancellationToken cancellationToken);
    Task<AdminSaleResponse[]> GetSalesAsync(string? productId, string? status, string? search, CancellationToken cancellationToken);
    Task<BookingCalendarEntryResponse[]> GetBookingsAsync(CancellationToken cancellationToken);
    Task<BookingCalendarEntryResponse> SaveManualBookingAsync(ManualBookingApiRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteManualBookingAsync(string id, CancellationToken cancellationToken);
}

public sealed class AdminDataService(
    AppDbContext db,
    IOptions<AdminWorkflowOptions> adminOptions,
    IOptions<BookingWorkflowOptions> bookingOptions,
    IOptions<StoreWorkflowOptions> storeOptions) : IAdminDataService
{
    private readonly AdminWorkflowOptions _admin = adminOptions.Value;
    private readonly BookingWorkflowOptions _booking = bookingOptions.Value;
    private readonly StoreWorkflowOptions _store = storeOptions.Value;

    public async Task<ProductResponse[]> GetProductsAsync(CancellationToken cancellationToken)
    {
        return await db.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .Select(product => ToProductResponse(product))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ProductResponse> SaveProductAsync(ProductResponse product, CancellationToken cancellationToken)
    {
        ValidateProduct(product);
        var id = string.IsNullOrWhiteSpace(product.Id) ? CreateId(_admin.ProductIdPrefix) : product.Id;
        var slug = string.IsNullOrWhiteSpace(product.Slug) ? Slugify(product.Name) : product.Slug;

        var slugExists = await db.Products.AnyAsync(
            candidate => candidate.Slug == slug && candidate.Id != id,
            cancellationToken);
        if (slugExists)
        {
            throw new AdminDataException(_admin.Messages.ProductSlugExists);
        }

        var entity = await db.Products.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (entity is null)
        {
            entity = new Product { Id = id };
            db.Products.Add(entity);
        }

        entity.Slug = slug;
        entity.Name = product.Name;
        entity.Description = product.Description;
        entity.Dimensions = new ProductDimensions
        {
            Height = product.Dimensions.Height,
            Width = product.Dimensions.Width,
            Length = product.Dimensions.Length
        };
        entity.Tags = product.Tags;
        entity.Colors = product.Colors;
        entity.Price = product.Price;
        entity.Active = product.Active;
        entity.Images = product.Images;
        entity.AmazonUrl = product.AmazonUrl;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return ToProductResponse(entity);
    }

    public async Task<AdminSaleResponse[]> GetSalesAsync(string? productId, string? status, string? search, CancellationToken cancellationToken)
    {
        var query = db.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(order => order.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(productId))
        {
            query = query.Where(order => order.Items.Any(item => item.ProductId == productId));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(order =>
                order.Customer.Name.Contains(search) ||
                order.Customer.Email.Contains(search) ||
                order.Customer.Phone.Contains(search) ||
                order.Items.Any(item => item.ProductName.Contains(search)));
        }

        return await query
            .OrderByDescending(order => order.CreatedAt)
            .Select(order => new AdminSaleResponse(
                order.Id,
                ToCustomerResponse(order.Customer),
                order.Items.OrderBy(item => item.ProductName).Select(item => item.ProductName).FirstOrDefault() ?? string.Empty,
                order.Total,
                order.Status,
                order.CreatedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<BookingCalendarEntryResponse[]> GetBookingsAsync(CancellationToken cancellationToken)
    {
        var confirmedBookings = await db.Bookings
            .AsNoTracking()
            .Where(booking => booking.Status == _booking.Statuses.Confirmed || booking.Status == _booking.Statuses.PendingPayment)
            .Select(booking => new BookingCalendarEntryResponse(
                booking.Id,
                booking.EventDate,
                booking.PackageName,
                booking.Status,
                booking.PackageId,
                booking.DurationHours,
                booking.AttendeeRange,
                ToCustomerResponse(booking.Customer),
                booking.Total,
                booking.DepositTotal,
                booking.RemainingBalance,
                null))
            .ToArrayAsync(cancellationToken);

        var manualBlocks = await db.BookingCalendarBlocks
            .AsNoTracking()
            .Select(block => ToBookingResponse(block))
            .ToArrayAsync(cancellationToken);

        return confirmedBookings.Concat(manualBlocks).OrderBy(entry => entry.Date).ToArray();
    }

    public async Task<BookingCalendarEntryResponse> SaveManualBookingAsync(ManualBookingApiRequest request, CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(request.Id) ? CreateId(_admin.ManualBookingIdPrefix) : request.Id;
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? request.Customer is null ? _booking.Statuses.ManualBlock : _booking.Statuses.Confirmed
            : request.Status;

        var overlap = await HasOverlapAsync(id, request.Date, cancellationToken);
        if (overlap)
        {
            throw new AdminDataException(_admin.Messages.ManualBookingOverlap);
        }

        var entity = await db.BookingCalendarBlocks.FirstOrDefaultAsync(block => block.Id == id, cancellationToken);
        if (entity is null)
        {
            entity = new BookingCalendarBlock { Id = id };
            db.BookingCalendarBlocks.Add(entity);
        }

        entity.Date = request.Date;
        entity.Label = string.IsNullOrWhiteSpace(request.Label) ? request.Notes ?? string.Empty : request.Label;
        entity.Status = status;
        entity.EventType = request.EventType;
        entity.DurationHours = request.DurationHours;
        entity.AttendeeRange = request.AttendeeRange;
        entity.Customer = request.Customer is null ? null : ToSnapshot(request.Customer);
        entity.Total = request.Total;
        entity.DepositTotal = request.DepositTotal;
        entity.RemainingBalance = request.RemainingBalance;
        entity.Notes = request.Notes;

        await db.SaveChangesAsync(cancellationToken);
        return ToBookingResponse(entity);
    }

    public async Task<bool> DeleteManualBookingAsync(string id, CancellationToken cancellationToken)
    {
        var entity = await db.BookingCalendarBlocks.FirstOrDefaultAsync(block => block.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        db.BookingCalendarBlocks.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> HasOverlapAsync(string id, DateOnly date, CancellationToken cancellationToken)
    {
        var activeHold = await db.BookingHolds.AnyAsync(
            hold => hold.EventDate == date &&
                    hold.ExpiresAt > DateTimeOffset.UtcNow &&
                    (hold.Status == _booking.Statuses.Held || hold.Status == _booking.Statuses.PendingPayment),
            cancellationToken);
        var confirmedBooking = await db.Bookings.AnyAsync(
            booking => booking.EventDate == date && booking.Status == _booking.Statuses.Confirmed,
            cancellationToken);
        var manualBlock = await db.BookingCalendarBlocks.AnyAsync(
            block => block.Id != id && block.Date == date,
            cancellationToken);

        return activeHold || confirmedBooking || manualBlock;
    }

    private void ValidateProduct(ProductResponse product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            throw new AdminDataException(_admin.Messages.ProductNameRequired);
        }

        if (product.Price <= 0)
        {
            throw new AdminDataException(_admin.Messages.InvalidProductPrice);
        }

        if (product.Dimensions.Height <= 0 || product.Dimensions.Width <= 0 || product.Dimensions.Length <= 0)
        {
            throw new AdminDataException(_admin.Messages.InvalidProductDimensions);
        }

        if (product.Images.Any(image => !IsValidImageReference(image)))
        {
            throw new AdminDataException(_admin.Messages.InvalidImageUrl);
        }
    }

    private static bool IsValidImageReference(string image)
    {
        if (Uri.TryCreate(image, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.Scheme is "http" or "https";
        }

        return image.StartsWith("/uploads/products/", StringComparison.OrdinalIgnoreCase);
    }

    private static ProductResponse ToProductResponse(Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Slug,
            product.Name,
            product.Description,
            new ProductDimensionsResponse(product.Dimensions.Height, product.Dimensions.Width, product.Dimensions.Length),
            product.Tags,
            product.Colors,
            product.Price,
            product.Active,
            product.Images,
            product.AmazonUrl);
    }

    private static BookingCalendarEntryResponse ToBookingResponse(BookingCalendarBlock block)
    {
        return new BookingCalendarEntryResponse(
            block.Id,
            block.Date,
            block.Label,
            block.Status,
            block.EventType,
            block.DurationHours,
            block.AttendeeRange,
            block.Customer is null ? null : ToCustomerResponse(block.Customer),
            block.Total,
            block.DepositTotal,
            block.RemainingBalance,
            block.Notes);
    }

    private static CustomerSnapshot ToSnapshot(CustomerDataApiRequest customer)
    {
        return new CustomerSnapshot
        {
            Name = customer.Name!,
            Email = customer.Email!,
            Phone = customer.Phone!,
            Address = new Address
            {
                Street = customer.Address!.Street!,
                ExteriorNumber = customer.Address.ExteriorNumber!,
                InteriorNumber = customer.Address.InteriorNumber,
                Neighborhood = customer.Address.Neighborhood!,
                City = customer.Address.City!,
                State = customer.Address.State!,
                PostalCode = customer.Address.PostalCode!,
                Country = customer.Address.Country!,
                References = customer.Address.References
            }
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

    private static string Slugify(string value)
    {
        return string.Join('-', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string CreateId(string prefix)
    {
        return $"{prefix}{Guid.NewGuid():N}";
    }
}

public sealed class AdminDataException(string message) : Exception(message);
