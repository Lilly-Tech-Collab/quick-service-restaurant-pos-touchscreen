using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Data;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Services;

public class OrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Order> CreateOrderAsync(User user, OrderType orderType = OrderType.DineIn)
    {
        var nextNumber = (await _db.Orders.MaxAsync(o => (int?)o.OrderNumber) ?? 0) + 1;

        var order = new Order
        {
            OrderNumber = nextNumber,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            OrderType = orderType,
            Status = OrderStatus.Open
        };

        _db.Orders.Add(order);
        await SaveChangesWithRetryAsync();
        return order;
    }

    public async Task<Order> AddItemAsync(Guid orderId, MenuItem menuItem)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstAsync(o => o.Id == orderId);

        var existing = order.Items.FirstOrDefault(i => i.MenuItemId == menuItem.Id);
        if (existing is null)
        {
            var item = new OrderItem
            {
                OrderId = order.Id,
                MenuItemId = menuItem.Id,
                NameSnapshot = menuItem.Name,
                UnitPriceCents = menuItem.PriceCents,
                Qty = 1,
                LineTotalCents = menuItem.PriceCents
            };
            order.Items.Add(item);
            _db.OrderItems.Add(item);
        }
        else
        {
            existing.Qty += 1;
            existing.LineTotalCents = existing.UnitPriceCents * existing.Qty;
        }

        await RecalculateTotalsAsync(order);
        await SaveChangesWithRetryAsync();
        await _db.Entry(order).Collection(o => o.Items).LoadAsync();
        return order;
    }

    public async Task<Order> RemoveItemAsync(Guid orderId, Guid orderItemId)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstAsync(o => o.Id == orderId);

        var item = order.Items.FirstOrDefault(i => i.Id == orderItemId);
        if (item is null)
        {
            return order;
        }

        order.Items.Remove(item);
        _db.OrderItems.Remove(item);

        await RecalculateTotalsAsync(order);
        await SaveChangesWithRetryAsync();
        await _db.Entry(order).Collection(o => o.Items).LoadAsync();
        return order;
    }

    public async Task<Order?> GetOrderAsync(Guid orderId)
    {
        return await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    private async Task RecalculateTotalsAsync(Order order)
    {
        var menuItemIds = order.Items.Select(i => i.MenuItemId).Distinct().ToList();
        var menuItems = await _db.MenuItems
            .Where(m => menuItemIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id);

        var subtotal = 0;
        var tax = 0;
        foreach (var item in order.Items)
        {
            subtotal += item.LineTotalCents;
            if (menuItems.TryGetValue(item.MenuItemId, out var menu))
            {
                tax += (int)Math.Round(item.LineTotalCents * (menu.TaxRateBps / 10000.0));
            }
        }

        order.SubtotalCents = subtotal;
        order.TaxCents = tax;
        order.TotalCents = subtotal + tax - order.DiscountCents;
    }

    private async Task SaveChangesWithRetryAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries)
            {
                try
                {
                    await entry.ReloadAsync();
                }
                catch (InvalidOperationException)
                {
                    entry.State = EntityState.Detached;
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}
