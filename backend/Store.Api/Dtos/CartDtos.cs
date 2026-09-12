namespace Store.Api.Dtos;

// Response shapes for GET/POST/PUT/DELETE /api/cart* - see guideline 01's
// "Cart" API contract for the exact JSON shape these serialize to.

public record ProductSummaryDto(string Name, int PriceCents);

public record CartItemDto(int Id, int ProductId, int Quantity, ProductSummaryDto Product);

public record CartDto(int Id, List<CartItemDto> Items);

public record AddCartItemRequest(int ProductId, int Quantity);

public record UpdateCartItemRequest(int Quantity);
