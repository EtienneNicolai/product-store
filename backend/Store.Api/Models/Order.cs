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
    public DateTime CreatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
