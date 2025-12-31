namespace RestaurantPOS.Domain.Entities;

public class CustomizationAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomizationItemId { get; set; }
    public Guid? MenuItemId { get; set; }
    public Guid? MenuCategoryId { get; set; }
}
