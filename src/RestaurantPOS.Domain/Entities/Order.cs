using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int OrderNumber { get; set; }
    public OrderType OrderType { get; set; } = OrderType.DineIn;
    public OrderStatus Status { get; set; } = OrderStatus.Open;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public string? CustomerName { get; set; }
    public int SubtotalCents { get; set; }
    public int TaxCents { get; set; }
    public int DiscountCents { get; set; }
    public int TotalCents { get; set; }

    public List<OrderItem> Items { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}
