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
        var result = await AddItemWithResultAsync(orderId, menuItem);
        return result.Order;
    }

    public async Task<(Order Order, OrderItem Item)> AddItemWithResultAsync(Guid orderId, MenuItem menuItem)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Customizations)
            .FirstAsync(o => o.Id == orderId);

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

        await RecalculateTotalsAsync(order);
        await SaveChangesWithRetryAsync();
        await _db.Entry(order).Collection(o => o.Items).Query().Include(i => i.Customizations).LoadAsync();
        return (order, item);
    }

    public async Task<Order> RemoveItemAsync(Guid orderId, Guid orderItemId)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Customizations)
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
        await _db.Entry(order).Collection(o => o.Items).Query().Include(i => i.Customizations).LoadAsync();
        return order;
    }

    public async Task<Order> AddCustomizationAsync(Guid orderId, Guid orderItemId, CustomizationItem customization)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Customizations)
            .FirstAsync(o => o.Id == orderId);

        var item = order.Items.FirstOrDefault(i => i.Id == orderItemId);
        if (item is null)
        {
            return order;
        }

        var orderCustomization = new OrderItemCustomization
        {
            OrderItemId = item.Id,
            CustomizationItemId = customization.Id,
            NameSnapshot = customization.Name,
            PriceCents = customization.PriceCents
        };

        item.Customizations.Add(orderCustomization);
        _db.OrderItemCustomizations.Add(orderCustomization);
        UpdateOrderItemNotes(item);

        await RecalculateTotalsAsync(order);
        await SaveChangesWithRetryAsync();
        await _db.Entry(order).Collection(o => o.Items).Query().Include(i => i.Customizations).LoadAsync();
        return order;
    }

    public async Task<Order> RemoveCustomizationAsync(Guid orderId, Guid orderItemId, Guid customizationId)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Customizations)
            .FirstAsync(o => o.Id == orderId);

        var item = order.Items.FirstOrDefault(i => i.Id == orderItemId);
        if (item is null)
        {
            return order;
        }

        var customization = item.Customizations.FirstOrDefault(c => c.Id == customizationId);
        if (customization is null)
        {
            return order;
        }

        item.Customizations.Remove(customization);
        _db.OrderItemCustomizations.Remove(customization);
        UpdateOrderItemNotes(item);

        await RecalculateTotalsAsync(order);
        await SaveChangesWithRetryAsync();
        await _db.Entry(order).Collection(o => o.Items).Query().Include(i => i.Customizations).LoadAsync();
        return order;
    }

    public async Task<Order?> GetOrderAsync(Guid orderId)
    {
        return await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(i => i.Customizations)
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
            var customizationTotal = item.Customizations.Sum(c => c.PriceCents);
            item.LineTotalCents = (item.UnitPriceCents + customizationTotal) * item.Qty;
            UpdateOrderItemNotes(item);
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

    private static void UpdateOrderItemNotes(OrderItem item)
    {
        if (item.Customizations.Count == 0)
        {
            item.Notes = null;
            return;
        }

        item.Notes = string.Join(", ", item.Customizations.Select(c => c.NameSnapshot));
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
