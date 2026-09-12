using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Stripe;

namespace Store.Api.Tests;

// Each test builds its own StoreApiFactory (rather than sharing one via
// IClassFixture) so its Mock<IPaymentIntentGateway> setup can't leak into
// another test - checkout tests configure that mock differently per test.
public class CheckoutControllerTests
{
    [Fact]
    public async Task CreatePaymentIntent_ValidCart_ComputesTotalServerSideAndCreatesOrder()
    {
        using var factory = new StoreApiFactory();
        factory.PaymentIntentGatewayMock
            .Setup(g => g.CreateAsync(It.IsAny<PaymentIntentCreateOptions>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentIntent { Id = "pi_test_123", ClientSecret = "pi_test_123_secret_abc" });

        var client = new TestClient(factory.CreateClient());
        // Sticker Pack (id 1) is 500 cents; 2 of them should total 1000,
        // regardless of anything the client might claim.
        await client.PostAsync("/api/cart/items", new { productId = 1, quantity = 2 });

        var response = await client.PostAsync("/api/checkout/create-payment-intent", new { email = "test@example.com" });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreatePaymentIntentApiResponse>(Json.Options);
        Assert.NotNull(result);
        Assert.Equal("pi_test_123_secret_abc", result!.ClientSecret);
        Assert.Equal(1000, result.TotalCents);
        Assert.True(result.OrderId > 0);

        // The single most important thing about this endpoint per guideline
        // 04: the amount actually sent to Stripe is the server-computed
        // total, not something a client could have supplied.
        factory.PaymentIntentGatewayMock.Verify(
            g => g.CreateAsync(
                It.Is<PaymentIntentCreateOptions>(o => o.Amount == 1000),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreatePaymentIntent_EmptyCart_Returns400AndNeverCallsStripe()
    {
        using var factory = new StoreApiFactory();
        var client = new TestClient(factory.CreateClient());

        var response = await client.PostAsync("/api/checkout/create-payment-intent", new { email = "test@example.com" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        factory.PaymentIntentGatewayMock.Verify(
            g => g.CreateAsync(It.IsAny<PaymentIntentCreateOptions>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreatePaymentIntent_InsufficientStock_Returns400AndNeverCallsStripe()
    {
        using var factory = new StoreApiFactory();
        var client = new TestClient(factory.CreateClient());

        // Add at valid quantity first (AddItem itself also rejects
        // over-stock adds), then shrink stock out from under the cart by
        // adding right up to the limit - Coffee Mug (id 3) has 40 in stock.
        await client.PostAsync("/api/cart/items", new { productId = 3, quantity = 40 });

        // Directly reduce stock in the DB to simulate another customer
        // buying the remaining stock between add-to-cart and checkout.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Store.Api.Data.StoreDbContext>();
            var product = await db.Products.FindAsync(3);
            product!.StockQuantity = 10;
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsync("/api/checkout/create-payment-intent", new { email = "test@example.com" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        factory.PaymentIntentGatewayMock.Verify(
            g => g.CreateAsync(It.IsAny<PaymentIntentCreateOptions>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreatePaymentIntent_StripeThrows_Returns502AndDoesNotCreateOrder()
    {
        using var factory = new StoreApiFactory();
        factory.PaymentIntentGatewayMock
            .Setup(g => g.CreateAsync(It.IsAny<PaymentIntentCreateOptions>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StripeException("card declined or similar"));

        var client = new TestClient(factory.CreateClient());
        await client.PostAsync("/api/cart/items", new { productId = 1, quantity = 1 });

        var response = await client.PostAsync("/api/checkout/create-payment-intent", new { email = "test@example.com" });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Store.Api.Data.StoreDbContext>();
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task GetOrder_WrongSession_Returns404()
    {
        using var factory = new StoreApiFactory();
        factory.PaymentIntentGatewayMock
            .Setup(g => g.CreateAsync(It.IsAny<PaymentIntentCreateOptions>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentIntent { Id = "pi_test_456", ClientSecret = "pi_test_456_secret" });

        var owner = new TestClient(factory.CreateClient());
        await owner.PostAsync("/api/cart/items", new { productId = 2, quantity = 1 });
        var createResponse = await owner.PostAsync("/api/checkout/create-payment-intent", new { email = "owner@example.com" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreatePaymentIntentApiResponse>(Json.Options);

        // A different session (a different browser/cookie) must not be able
        // to view this order just by guessing/incrementing the id.
        var stranger = new TestClient(factory.CreateClient());
        await stranger.GetAsync("/api/cart"); // establishes a session cookie for the stranger
        var strangerResponse = await stranger.GetAsync($"/api/orders/{created!.OrderId}");

        Assert.Equal(HttpStatusCode.NotFound, strangerResponse.StatusCode);

        var ownerResponse = await owner.GetAsync($"/api/orders/{created.OrderId}");
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
    }

    [Fact]
    public async Task GetOrder_NoSessionCookieAtAll_Returns404()
    {
        using var factory = new StoreApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/orders/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
