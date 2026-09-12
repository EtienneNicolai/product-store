namespace Store.Api.Models;

public class Cart
{
    public int Id { get; set; }

    // From a session cookie, not tied to a user account - no auth in v1.
    public string SessionId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public List<CartItem> Items { get; set; } = new();
}
