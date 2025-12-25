namespace RestaurantPOS.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid MenuItemId { get; set; }
    public string NameSnapshot { get; set; } = "";
    public int UnitPriceCents { get; set; }
    public int Qty { get; set; }
    public int LineTotalCents { get; set; }
    public string? Notes { get; set; }
    public List<OrderItemCustomization> Customizations { get; set; } = new();
}
