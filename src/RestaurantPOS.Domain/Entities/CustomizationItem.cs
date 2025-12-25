namespace RestaurantPOS.Domain.Entities;

public class CustomizationItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public int PriceCents { get; set; }
    public bool IsActive { get; set; } = true;
}
