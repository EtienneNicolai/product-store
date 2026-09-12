using System.Text.Json;

namespace Store.Api.Tests;

// Mirrors the real API's JSON shapes (see Store.Api/Dtos and the Product
// entity's default serialization) for deserializing responses in tests.
// Named with a "Response" suffix to avoid colliding with the real
// Store.Api.Models types of the same short name (Cart, Order, ...), which
// WebhookTests.cs needs directly for DB setup/assertions.
internal record ProductSummaryResponse(int Id, string Slug, string Name, int PriceCents, string Currency, string ImageUrl, int StockQuantity);

internal record ProductDetailResponse(int Id, string Slug, string Name, string Description, int PriceCents, string Currency, string ImageUrl, int StockQuantity, bool IsActive);

internal record CartItemProductResponse(string Name, int PriceCents);

internal record CartItemResponse(int Id, int ProductId, int Quantity, CartItemProductResponse Product);

internal record CartResponse(int Id, List<CartItemResponse> Items);

internal record CreatePaymentIntentApiResponse(string ClientSecret, int TotalCents, int OrderId);

internal record OrderItemResponse(int ProductId, string ProductName, int UnitPriceCents, int Quantity);

internal record OrderResponse(int Id, string Status, int TotalCents, string CustomerEmail, DateTime CreatedAt, List<OrderItemResponse> Items);

internal static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
