using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Data;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Services;

public class PaymentService
{
    private readonly AppDbContext _db;

    public PaymentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Order> RecordPaymentAsync(Guid orderId, PaymentMethod method, string? reference = null)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstAsync(o => o.Id == orderId);

        var payment = new Payment
        {
            OrderId = order.Id,
            PaymentMethod = method,
            AmountCents = order.TotalCents,
            Reference = reference,
            PaidAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);
        order.Status = OrderStatus.Paid;
        await _db.SaveChangesAsync();
        return order;
    }
}
