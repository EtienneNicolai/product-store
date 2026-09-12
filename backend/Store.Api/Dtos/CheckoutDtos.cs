namespace Store.Api.Dtos;

// Response/request shapes for the checkout endpoints - see guideline 01's
// "Checkout" API contract.

public record CreatePaymentIntentRequest(string Email);

public record CreatePaymentIntentResponse(string ClientSecret, int TotalCents, int OrderId);

public record OrderItemDto(int ProductId, string ProductName, int UnitPriceCents, int Quantity);

public record OrderDto(
    int Id,
    string Status,
    int TotalCents,
    string CustomerEmail,
    DateTime CreatedAt,
    List<OrderItemDto> Items);
