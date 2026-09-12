namespace Store.Api.Models;

public class Order
{
    public int Id { get; set; }
    public string StripePaymentIntentId { get; set; } = null!;

    // "pending" | "paid" | "failed" - set to "paid" only by the webhook handler
    // once Stripe confirms payment succeeded, never at PaymentIntent creation.
    public string Status { get; set; } = "pending";
    public int TotalCents { get; set; }
    public string CustomerEmail { get; set; } = null!;

    // The session cookie value of the cart this order was created from (see
    // guideline 01, checkout API contract) - GET /api/orders/{id} compares
    // against this so a visitor cannot view someone else's order by guessing
    // an id. Added in Session 3; not part of Session 1's original model set.
    public string SessionId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
