namespace Store.Api.Models;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }

    // Snapshots at time of purchase - historical orders must survive a product
    // being deleted, deactivated, or repriced later.
    public string ProductName { get; set; } = null!;
    public int UnitPriceCents { get; set; }
    public int Quantity { get; set; }
}
