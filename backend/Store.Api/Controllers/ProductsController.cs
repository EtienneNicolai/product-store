using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.Api.Data;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly StoreDbContext _db;

    public ProductsController(StoreDbContext db)
    {
        _db = db;
    }

    // GET /api/products -> list of active products only.
    // Projected to the shape in guideline 01's API Contracts section - list view is
    // intentionally slimmer than the detail view (no IsActive). Description was
    // added to this projection for the "comfortable" UI rework, which shows a
    // short excerpt on each catalog card, not just the detail page.
    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _db.Products
            .Where(p => p.IsActive)
            .Select(p => new
            {
                id = p.Id,
                slug = p.Slug,
                name = p.Name,
                description = p.Description,
                priceCents = p.PriceCents,
                currency = p.Currency,
                imageUrl = p.ImageUrl,
                stockQuantity = p.StockQuantity,
            })
            .ToListAsync();

        return Ok(products);
    }

    // GET /api/products/{slug} -> full product detail.
    // Returns 404 for both a non-existent slug and an inactive product, with an
    // identical response body either way, so a visitor can't distinguish "never
    // existed" from "exists but discontinued" - see guideline 03.
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetProductBySlug(string slug)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }
}
