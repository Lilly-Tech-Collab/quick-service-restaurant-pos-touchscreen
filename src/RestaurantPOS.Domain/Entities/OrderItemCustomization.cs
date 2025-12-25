namespace RestaurantPOS.Domain.Entities;

public class OrderItemCustomization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderItemId { get; set; }
    public Guid CustomizationItemId { get; set; }
    public string NameSnapshot { get; set; } = "";
    public int PriceCents { get; set; }
}
