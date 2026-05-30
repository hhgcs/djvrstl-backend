using Djvrstl.Backend.Api;
using Djvrstl.Backend.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Djvrstl.Backend.Controllers;

[ApiController]
[Route("catalog")]
public sealed class CatalogController(AppDbContext db) : ControllerBase
{
    [HttpGet("products")]
    public async Task<ActionResult<ProductResponse[]>> GetProducts(CancellationToken cancellationToken)
    {
        var products = await db.Products
            .AsNoTracking()
            .Where(product => product.Active)
            .OrderBy(product => product.Name)
            .Select(product => new ProductResponse(
                product.Id,
                product.Slug,
                product.Name,
                product.Description,
                new ProductDimensionsResponse(
                    product.Dimensions.Height,
                    product.Dimensions.Width,
                    product.Dimensions.Length),
                product.Tags,
                product.Colors,
                product.Price,
                product.Active,
                product.Images,
                product.AmazonUrl))
            .ToArrayAsync(cancellationToken);

        return Ok(products);
    }
}
