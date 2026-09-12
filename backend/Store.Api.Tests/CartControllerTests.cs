using System.Net;
using System.Net.Http.Json;

namespace Store.Api.Tests;

public class CartControllerTests : IClassFixture<StoreApiFactory>
{
    private readonly StoreApiFactory _factory;

    public CartControllerTests(StoreApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetCart_NoSessionYet_CreatesEmptyCartAndSetsCookie()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/cart");
        response.EnsureSuccessStatusCode();

        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.StartsWith("session_id="));

        var cart = await response.Content.ReadFromJsonAsync<CartResponse>(Json.Options);
        Assert.NotNull(cart);
        Assert.Empty(cart!.Items);
    }

    [Fact]
    public async Task AddItem_ValidQuantity_AddsToCart()
    {
        var client = new TestClient(_factory.CreateClient());

        var response = await client.PostAsync("/api/cart/items", new { productId = 1, quantity = 2 });
        response.EnsureSuccessStatusCode();

        var cart = await response.Content.ReadFromJsonAsync<CartResponse>(Json.Options);
        Assert.NotNull(cart);
        var item = Assert.Single(cart!.Items);
        Assert.Equal(1, item.ProductId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal("Sticker Pack", item.Product.Name);
    }

    [Fact]
    public async Task AddItem_QuantityExceedsStock_Returns400()
    {
        var client = new TestClient(_factory.CreateClient());

        // Enamel Pin (id 2) has 60 in stock.
        var response = await client.PostAsync("/api/cart/items", new { productId = 2, quantity = 999 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddItem_SameProductTwice_AccumulatesQuantityAndEnforcesStock()
    {
        var client = new TestClient(_factory.CreateClient());

        // Coffee Mug (id 3) has 40 in stock.
        await client.PostAsync("/api/cart/items", new { productId = 3, quantity = 30 });
        var secondResponse = await client.PostAsync("/api/cart/items", new { productId = 3, quantity = 20 });

        // 30 + 20 = 50, exceeds the 40 in stock.
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);

        var cartResponse = await client.GetAsync("/api/cart");
        var cart = await cartResponse.Content.ReadFromJsonAsync<CartResponse>(Json.Options);
        var item = Assert.Single(cart!.Items);
        Assert.Equal(30, item.Quantity); // the failed second add must not have partially applied
    }

    [Fact]
    public async Task UpdateItem_ValidQuantity_UpdatesCart()
    {
        var client = new TestClient(_factory.CreateClient());

        var addResponse = await client.PostAsync("/api/cart/items", new { productId = 4, quantity = 1 });
        var addedCart = await addResponse.Content.ReadFromJsonAsync<CartResponse>(Json.Options);
        var itemId = addedCart!.Items[0].Id;

        var updateResponse = await client.PutAsync($"/api/cart/items/{itemId}", new { quantity = 3 });
        updateResponse.EnsureSuccessStatusCode();

        var updatedCart = await updateResponse.Content.ReadFromJsonAsync<CartResponse>(Json.Options);
        Assert.Equal(3, updatedCart!.Items[0].Quantity);
    }

    [Fact]
    public async Task RemoveItem_RemovesFromCart()
    {
        var client = new TestClient(_factory.CreateClient());

        var addResponse = await client.PostAsync("/api/cart/items", new { productId = 5, quantity = 1 });
        var addedCart = await addResponse.Content.ReadFromJsonAsync<CartResponse>(Json.Options);
        var itemId = addedCart!.Items[0].Id;

        var removeResponse = await client.DeleteAsync($"/api/cart/items/{itemId}");
        removeResponse.EnsureSuccessStatusCode();

        var cart = await removeResponse.Content.ReadFromJsonAsync<CartResponse>(Json.Options);
        Assert.Empty(cart!.Items);
    }

    [Fact]
    public async Task DifferentSessions_HaveIndependentCarts()
    {
        var clientA = new TestClient(_factory.CreateClient());
        var clientB = new TestClient(_factory.CreateClient());

        await clientA.PostAsync("/api/cart/items", new { productId = 1, quantity = 1 });

        var cartBResponse = await clientB.GetAsync("/api/cart");
        var cartB = await cartBResponse.Content.ReadFromJsonAsync<CartResponse>(Json.Options);

        Assert.Empty(cartB!.Items);
    }
}
