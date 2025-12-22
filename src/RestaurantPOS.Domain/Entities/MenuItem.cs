namespace RestaurantPOS.Domain.Entities;

public class MenuItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = "";
    public int PriceCents { get; set; }
    public int TaxRateBps { get; set; }
    public bool IsActive { get; set; } = true;
}
