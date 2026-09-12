using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Store.Api.Data;
using Store.Api.Models;

namespace Store.Api.Tests;

// The single most important thing to test in this project (guideline 07):
// an invalid or missing webhook signature must never be accepted, since
// that's the one place a forged request could otherwise mark an unpaid
// order as paid. Verification itself happens inside the real controller via
// Stripe.net's EventUtility.ConstructEvent - genuine HMAC-SHA256
// verification, not a stub. This Stripe.net version (52.4.2) has no
// GenerateTestHeaderStringForPayload test helper, so signing test payloads
// here uses Stripe's own documented, public webhook signature scheme
// directly (SignPayload below) - the same HMAC-SHA256("{timestamp}.{payload}")
// construction Stripe's real servers use, confirmed against the real
// controller during manual testing before this suite existed.
public class WebhookTests
{
    private static string SignPayload(string payload, string secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{payload}";
        var signatureBytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(signedPayload));
        var signature = Convert.ToHexString(signatureBytes).ToLowerInvariant();
        return $"t={timestamp},v1={signature}";
    }


    private static string BuildPaymentIntentSucceededPayload(string paymentIntentId)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return $$"""
            {
              "id": "evt_test_{{Guid.NewGuid():N}}",
              "object": "event",
              "api_version": "2020-08-27",
              "created": {{timestamp}},
              "type": "payment_intent.succeeded",
              "livemode": false,
              "pending_webhooks": 0,
              "request": { "id": null, "idempotency_key": null },
              "data": {
                "object": {
                  "id": "{{paymentIntentId}}",
                  "object": "payment_intent",
                  "amount": 1000,
                  "currency": "aud",
                  "status": "succeeded"
                }
              }
            }
            """;
    }

    private static async Task<Order> SeedPendingOrderAsync(StoreApiFactory factory, string paymentIntentId, string sessionId, int productId, int quantity)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();

        var product = await db.Products.FindAsync(productId);
        var cart = new Cart { SessionId = sessionId, CreatedAt = DateTime.UtcNow };
        cart.Items.Add(new CartItem { ProductId = productId, Quantity = quantity });
        db.Carts.Add(cart);

        var order = new Order
        {
            StripePaymentIntentId = paymentIntentId,
            Status = "pending",
            TotalCents = quantity * product!.PriceCents,
            CustomerEmail = "test@example.com",
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            Items = { new OrderItem { ProductId = productId, ProductName = product.Name, UnitPriceCents = product.PriceCents, Quantity = quantity } },
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return order;
    }

    private static HttpRequestMessage BuildWebhookRequest(string payload, string signatureHeader)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/checkout/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Stripe-Signature", signatureHeader);
        return request;
    }

    [Fact]
    public async Task Webhook_MissingSignatureHeader_Returns400()
    {
        using var factory = new StoreApiFactory();
        var client = factory.CreateClient();
        var payload = BuildPaymentIntentSucceededPayload("pi_does_not_matter");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/checkout/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_GarbageSignature_Returns400()
    {
        using var factory = new StoreApiFactory();
        var client = factory.CreateClient();
        var payload = BuildPaymentIntentSucceededPayload("pi_does_not_matter");

        var response = await client.SendAsync(BuildWebhookRequest(payload, "t=1,v1=not_a_real_signature"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_ValidSignatureButTamperedPayload_Returns400()
    {
        using var factory = new StoreApiFactory();
        var client = factory.CreateClient();

        var originalPayload = BuildPaymentIntentSucceededPayload("pi_original");
        var signature = SignPayload(originalPayload, StoreApiFactory.TestWebhookSecret);

        // Same signature, different body - simulates an attacker who captured
        // one valid signed request and tried to replay it with edited data.
        var tamperedPayload = BuildPaymentIntentSucceededPayload("pi_ATTACKER_CHANGED");

        var response = await client.SendAsync(BuildWebhookRequest(tamperedPayload, signature));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_ValidSignature_MarksOrderPaidDecrementsStockAndClearsCart()
    {
        using var factory = new StoreApiFactory();
        var sessionId = Guid.NewGuid().ToString("N");
        var order = await SeedPendingOrderAsync(factory, "pi_valid_1", sessionId, productId: 1, quantity: 2);

        var payload = BuildPaymentIntentSucceededPayload("pi_valid_1");
        var signature = SignPayload(payload, StoreApiFactory.TestWebhookSecret);

        var client = factory.CreateClient();
        var response = await client.SendAsync(BuildWebhookRequest(payload, signature));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();

        var updatedOrder = await db.Orders.FindAsync(order.Id);
        Assert.Equal("paid", updatedOrder!.Status);

        var product = await db.Products.FindAsync(1);
        Assert.Equal(98, product!.StockQuantity); // seeded at 100, order was for 2

        var cart = await db.Carts.Include(c => c.Items).FirstAsync(c => c.SessionId == sessionId);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public async Task Webhook_UnknownPaymentIntentId_ReturnsOkAndChangesNothing()
    {
        using var factory = new StoreApiFactory();
        var payload = BuildPaymentIntentSucceededPayload("pi_never_created_by_this_app");
        var signature = SignPayload(payload, StoreApiFactory.TestWebhookSecret);

        var client = factory.CreateClient();
        var response = await client.SendAsync(BuildWebhookRequest(payload, signature));

        // Not an error - could be an event for an order from a different
        // environment/run. Nothing to reconcile, so just acknowledge it.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_RedeliveredEventForAlreadyPaidOrder_DoesNotDoubleDecrementStock()
    {
        using var factory = new StoreApiFactory();
        var sessionId = Guid.NewGuid().ToString("N");
        await SeedPendingOrderAsync(factory, "pi_redelivered", sessionId, productId: 2, quantity: 3);

        var payload = BuildPaymentIntentSucceededPayload("pi_redelivered");
        var signature = SignPayload(payload, StoreApiFactory.TestWebhookSecret);
        var client = factory.CreateClient();

        var firstResponse = await client.SendAsync(BuildWebhookRequest(payload, signature));
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Stripe explicitly documents at-least-once delivery - a redelivered
        // event for an order already marked paid must be a no-op.
        var secondSignature = SignPayload(payload, StoreApiFactory.TestWebhookSecret);
        var secondResponse = await client.SendAsync(BuildWebhookRequest(payload, secondSignature));
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var product = await db.Products.FindAsync(2);
        Assert.Equal(57, product!.StockQuantity); // seeded at 60, decremented by 3 exactly once
    }
}
