using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.Api.Data;
using Store.Api.Dtos;
using Store.Api.Models;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly StoreDbContext _db;

    public CartController(StoreDbContext db)
    {
        _db = db;
    }

    // GET /api/cart -> current cart, creating one (and setting the session
    // cookie) if the caller has none yet.
    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart(CancellationToken ct)
    {
        var (cart, isNewSession) = await CartAccessor.GetOrCreateCartAsync(_db, Request, ct);
        if (isNewSession)
        {
            CartSession.SetSessionCookie(Response, cart.SessionId);
        }

        return Ok(ToDto(cart));
    }

    // POST /api/cart/items -> {"productId": 1, "quantity": 1}
    // 400 if the product doesn't exist/is inactive, or if quantity exceeds stock.
    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem([FromBody] AddCartItemRequest request, CancellationToken ct)
    {
        if (request.Quantity <= 0)
        {
            return BadRequest("Quantity must be greater than zero.");
        }

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);
        if (product is null || !product.IsActive)
        {
            return BadRequest("Product not found.");
        }

        var (cart, isNewSession) = await CartAccessor.GetOrCreateCartAsync(_db, Request, ct);
        if (isNewSession)
        {
            CartSession.SetSessionCookie(Response, cart.SessionId);
        }

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        var requestedTotalQuantity = (existingItem?.Quantity ?? 0) + request.Quantity;

        if (requestedTotalQuantity > product.StockQuantity)
        {
            return BadRequest($"Only {product.StockQuantity} unit(s) of '{product.Name}' are in stock.");
        }

        if (existingItem is not null)
        {
            existingItem.Quantity = requestedTotalQuantity;
        }
        else
        {
            _db.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                Quantity = request.Quantity,
            });
        }

        await _db.SaveChangesAsync(ct);

        var updatedCart = await LoadCartAsync(cart.Id, ct);
        return Ok(ToDto(updatedCart!));
    }

    // PUT /api/cart/items/{id} -> {"quantity": 3}
    [HttpPut("items/{id:int}")]
    public async Task<ActionResult<CartDto>> UpdateItem(int id, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
    {
        if (request.Quantity <= 0)
        {
            return BadRequest("Quantity must be greater than zero.");
        }

        var (cart, isNewSession) = await CartAccessor.GetOrCreateCartAsync(_db, Request, ct);
        if (isNewSession)
        {
            CartSession.SetSessionCookie(Response, cart.SessionId);
        }

        var item = cart.Items.FirstOrDefault(i => i.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        if (request.Quantity > item.Product.StockQuantity)
        {
            return BadRequest($"Only {item.Product.StockQuantity} unit(s) of '{item.Product.Name}' are in stock.");
        }

        item.Quantity = request.Quantity;
        await _db.SaveChangesAsync(ct);

        var updatedCart = await LoadCartAsync(cart.Id, ct);
        return Ok(ToDto(updatedCart!));
    }

    // DELETE /api/cart/items/{id} -> updated Cart
    [HttpDelete("items/{id:int}")]
    public async Task<ActionResult<CartDto>> RemoveItem(int id, CancellationToken ct)
    {
        var (cart, isNewSession) = await CartAccessor.GetOrCreateCartAsync(_db, Request, ct);
        if (isNewSession)
        {
            CartSession.SetSessionCookie(Response, cart.SessionId);
        }

        var item = cart.Items.FirstOrDefault(i => i.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync(ct);

        var updatedCart = await LoadCartAsync(cart.Id, ct);
        return Ok(ToDto(updatedCart!));
    }

    private async Task<Cart?> LoadCartAsync(int cartId, CancellationToken ct)
        => await _db.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == cartId, ct);

    private static CartDto ToDto(Cart cart) => new(
        cart.Id,
        cart.Items
            .Select(i => new CartItemDto(
                i.Id,
                i.ProductId,
                i.Quantity,
                new ProductSummaryDto(i.Product.Name, i.Product.PriceCents)))
            .ToList());
}
