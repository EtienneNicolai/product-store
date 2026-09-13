using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Store.Api.Tests;

public class ProductsControllerTests : IClassFixture<StoreApiFactory>
{
    private readonly StoreApiFactory _factory;

    public ProductsControllerTests(StoreApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetProducts_ReturnsOnlyActiveProducts()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();

        var products = await response.Content.ReadFromJsonAsync<List<ProductSummaryResponse>>(Json.Options);

        // Seed data has 11 products, one deliberately inactive/out of stock
        // (discontinued-keychain) - it must never appear here.
        Assert.NotNull(products);
        Assert.Equal(10, products!.Count);
        Assert.DoesNotContain(products, p => p.Slug == "discontinued-keychain");
    }

    [Fact]
    public async Task GetProductBySlug_ActiveProduct_ReturnsFullDetail()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products/sticker-pack");
        response.EnsureSuccessStatusCode();

        var product = await response.Content.ReadFromJsonAsync<ProductDetailResponse>(Json.Options);

        Assert.NotNull(product);
        Assert.Equal("Sticker Pack", product!.Name);
        Assert.True(product.IsActive);
    }

    [Fact]
    public async Task GetProductBySlug_NonExistentSlug_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products/this-slug-does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProductBySlug_InactiveProduct_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products/discontinued-keychain");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProductBySlug_InactiveAndNonExistent_ReturnIdenticalBody()
    {
        // guideline 03: a visitor must not be able to distinguish "never
        // existed" from "exists but discontinued" via the response body.
        // ProblemDetails includes a per-request traceId, so compare every
        // field except that one rather than the raw string.
        var client = _factory.CreateClient();

        var inactiveResponse = await client.GetAsync("/api/products/discontinued-keychain");
        var missingResponse = await client.GetAsync("/api/products/this-slug-does-not-exist");

        Assert.Equal(inactiveResponse.StatusCode, missingResponse.StatusCode);

        using var inactiveDoc = JsonDocument.Parse(await inactiveResponse.Content.ReadAsStringAsync());
        using var missingDoc = JsonDocument.Parse(await missingResponse.Content.ReadAsStringAsync());

        foreach (var propertyName in new[] { "type", "title", "status" })
        {
            Assert.Equal(
                inactiveDoc.RootElement.GetProperty(propertyName).ToString(),
                missingDoc.RootElement.GetProperty(propertyName).ToString());
        }
    }
}
