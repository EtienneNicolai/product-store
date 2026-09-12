using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using Store.Api.Data;
using Store.Api.Services;

namespace Store.Api.Tests;

// Real ASP.NET pipeline (routing, model binding, JSON, CORS) against an
// isolated in-memory SQLite database - not the dev store.db, and never a
// real call to Stripe (guideline 07). One factory instance is shared across
// all tests in a class via IClassFixture<StoreApiFactory>; test isolation
// comes from each test using its own session cookie (see TestClient), not
// from a fresh database per test.
public class StoreApiFactory : WebApplicationFactory<Program>
{
    // A fake, syntactically-plausible key/secret - never a real Stripe
    // credential. Only lets CheckoutController's "is Stripe configured at
    // all" guard pass so tests can reach the (mocked) gateway; the webhook
    // secret is real enough to actually sign test payloads against with
    // Stripe.net's own EventUtility, which is genuine HMAC verification, not
    // a stub.
    public const string TestSecretKey = "sk_test_fake_key_for_tests";
    public const string TestWebhookSecret = "whsec_test_fake_secret_for_tests";

    private SqliteConnection? _connection;

    // Exposed so individual tests can configure CreateAsync's return value or
    // verify it was called with the expected arguments (e.g. the
    // server-computed total, not a client-supplied one).
    public Mock<IPaymentIntentGateway> PaymentIntentGatewayMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:SecretKey"] = TestSecretKey,
                ["Stripe:WebhookSecret"] = TestWebhookSecret,
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<StoreDbContext>>();

            // Must stay open for the factory's lifetime - an in-memory SQLite
            // database is destroyed the moment its one connection closes.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            services.AddDbContext<StoreDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<IPaymentIntentGateway>();
            services.AddScoped(_ => PaymentIntentGatewayMock.Object);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        db.Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }
}
