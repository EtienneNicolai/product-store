using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.Api.Data;
using Store.Api.Dtos;
using Store.Api.Models;
using Store.Api.Services;
using Stripe;

namespace Store.Api.Controllers;

[ApiController]
public class CheckoutController : ControllerBase
{
    private readonly StoreDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IPaymentIntentGateway _paymentIntentGateway;

    // IPaymentIntentGateway (our own thin wrapper, see Services/) is injected
    // rather than constructing `new StripeClient(secretKey)` inline - that
    // made this controller impossible to unit test without a real network
    // call to Stripe. The secret key is still read per-request from
    // configuration (Key Trap 5), just passed as an argument instead of
    // baked into a client at construction time. See guideline 07's
    // completion notes.
    public CheckoutController(StoreDbContext db, IConfiguration configuration, IPaymentIntentGateway paymentIntentGateway)
    {
        _db = db;
        _configuration = configuration;
        _paymentIntentGateway = paymentIntentGateway;
    }

    // POST /api/checkout/create-payment-intent -> {"email": "customer@example.com"}
    // Computes the total server-side from the cart's current items/prices,
    // creates a Stripe PaymentIntent for that amount, and a matching "pending"
    // Order row storing the PaymentIntent id (see CLAUDE.md Key Trap 1 and 6).
    [HttpPost("api/checkout/create-payment-intent")]
    public async Task<ActionResult<CreatePaymentIntentResponse>> CreatePaymentIntent(
        [FromBody] CreatePaymentIntentRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Email is required.");
        }

        var (cart, isNewSession) = await CartAccessor.GetOrCreateCartAsync(_db, Request, ct);
        if (isNewSession)
        {
            CartSession.SetSessionCookie(Response, cart.SessionId);
        }

        if (cart.Items.Count == 0)
        {
            return BadRequest("Cart is empty.");
        }

        var insufficientStock = cart.Items.Where(i => i.Quantity > i.Product.StockQuantity).ToList();
        if (insufficientStock.Count > 0)
        {
            var names = string.Join(", ", insufficientStock.Select(i => i.Product.Name));
            return BadRequest($"Insufficient stock for: {names}.");
        }

        // Server-computed total from current DB prices - never trust a
        // client-sent amount here (guideline 04).
        var totalCents = cart.Items.Sum(i => i.Quantity * i.Product.PriceCents);
        var currency = cart.Items.First().Product.Currency;

        var secretKey = _configuration["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            // Expected in this environment - no real Stripe account/keys are
            // configured yet. See CLAUDE.md Key Trap 5: the secret key is read
            // here, server-side only, never sent to the frontend.
            return Problem(
                detail: "Stripe is not configured: Stripe:SecretKey is missing. Set it via `dotnet user-secrets set Stripe:SecretKey ...`.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Stripe not configured");
        }

        PaymentIntent intent;
        try
        {
            intent = await _paymentIntentGateway.CreateAsync(
                new PaymentIntentCreateOptions
                {
                    Amount = totalCents,
                    Currency = currency,
                    ReceiptEmail = request.Email,
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
                    Metadata = new Dictionary<string, string>
                    {
                        ["cartId"] = cart.Id.ToString(),
                        ["sessionId"] = cart.SessionId,
                    },
                },
                secretKey,
                ct);
        }
        catch (StripeException ex)
        {
            // Expected to fail in this environment without a real test-mode
            // secret key - this is a genuine Stripe auth error, not a bug.
            return Problem(
                detail: $"Stripe payment intent creation failed: {ex.Message}",
                statusCode: StatusCodes.Status502BadGateway,
                title: "Stripe request failed");
        }

        // Only created once Stripe has actually handed back a PaymentIntent -
        // an Order should never exist without a real StripePaymentIntentId.
        var order = new Order
        {
            StripePaymentIntentId = intent.Id,
            Status = "pending",
            TotalCents = totalCents,
            CustomerEmail = request.Email,
            SessionId = cart.SessionId,
            CreatedAt = DateTime.UtcNow,
            Items = cart.Items.Select(ci => new OrderItem
            {
                ProductId = ci.ProductId,
                ProductName = ci.Product.Name,
                UnitPriceCents = ci.Product.PriceCents,
                Quantity = ci.Quantity,
            }).ToList(),
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        // OrderId lets the frontend build a Stripe return_url pointing at
        // /order-confirmation/{orderId} and fetch it via GET /api/orders/{id}
        // afterward - added for Session 5, not part of guideline 01's original
        // contract (see guideline 06's completion notes).
        return Ok(new CreatePaymentIntentResponse(intent.ClientSecret, totalCents, order.Id));
    }

    // POST /api/checkout/webhook -> raw Stripe event payload.
    // Must read the raw body directly (no [FromBody] DTO - see CLAUDE.md Key
    // Trap 2) so Stripe.EventUtility can verify the signature against the
    // exact bytes Stripe signed, before any of it is trusted.
    [HttpPost("api/checkout/webhook")]
    public async Task<IActionResult> HandleWebhook(CancellationToken ct)
    {
        string json;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            json = await reader.ReadToEndAsync(ct);
        }

        var webhookSecret = _configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            return Problem(
                detail: "Stripe is not configured: Stripe:WebhookSecret is missing.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Stripe not configured");
        }

        var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

        Event stripeEvent;
        try
        {
            // throwOnApiVersionMismatch: false - a real Stripe account's
            // configured API version can drift from whatever this Stripe.net
            // build was pinned against; that's a compatibility concern worth
            // logging, not a reason to reject an otherwise validly-signed
            // webhook. The signature check above it is what actually matters
            // for trust.
            stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, webhookSecret, throwOnApiVersionMismatch: false);
        }
        catch (Exception ex)
        {
            // Missing/invalid signature, tampered payload, or malformed
            // event - reject outright, never process an unverified payload.
            return BadRequest($"Webhook signature verification failed: {ex.Message}");
        }

        if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded
            && stripeEvent.Data.Object is PaymentIntent paymentIntent)
        {
            await MarkOrderPaidAsync(paymentIntent.Id, ct);
        }

        return Ok();
    }

    // GET /api/orders/{id} -> Order detail, 404 if it doesn't belong to the
    // requesting session (compared via the session cookie, not the id alone).
    [HttpGet("api/orders/{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id, CancellationToken ct)
    {
        var sessionId = CartSession.ReadSessionId(Request);
        if (sessionId is null)
        {
            return NotFound();
        }

        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null || order.SessionId != sessionId)
        {
            return NotFound();
        }

        return Ok(ToDto(order));
    }

    // Runs on a verified payment_intent.succeeded event: marks the Order
    // paid, decrements stock for each ordered Product, and clears the cart -
    // all atomically (guideline 04).
    private async Task MarkOrderPaidAsync(string paymentIntentId, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.StripePaymentIntentId == paymentIntentId, ct);

        if (order is null)
        {
            // Unknown PaymentIntent - nothing to reconcile. Could be an event
            // for an order created by a different environment/run.
            return;
        }

        if (order.Status == "paid")
        {
            // Stripe may redeliver events - already processed, no-op.
            return;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        order.Status = "paid";

        foreach (var orderItem in order.Items)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == orderItem.ProductId, ct);
            if (product is not null)
            {
                product.StockQuantity = Math.Max(0, product.StockQuantity - orderItem.Quantity);
            }
        }

        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.SessionId == order.SessionId, ct);

        if (cart is not null && cart.Items.Count > 0)
        {
            _db.CartItems.RemoveRange(cart.Items);
        }

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private static OrderDto ToDto(Order order) => new(
        order.Id,
        order.Status,
        order.TotalCents,
        order.CustomerEmail,
        order.CreatedAt,
        order.Items
            .Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.UnitPriceCents, i.Quantity))
            .ToList());
}
