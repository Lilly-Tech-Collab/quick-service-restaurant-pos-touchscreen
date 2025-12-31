using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Data;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Services;

public class OrderService
{
    private readonly AppDbContext _db;
    private readonly SettingsService _settingsService;

    public OrderService(AppDbContext db, SettingsService settingsService)
    {
        _db = db;
        _settingsService = settingsService;
    }

    public async Task<Order> CreateOrderAsync(User user, OrderType orderType = OrderType.DineIn)
    {
        var resetMode = await _settingsService.GetOrderNumberResetModeAsync();
        var nextNumber = resetMode switch
        {
            OrderNumberResetMode.Daily => await GetNextDailyOrderNumberAsync(0),
            OrderNumberResetMode.BusinessDay => await GetNextDailyOrderNumberAsync(await _settingsService.GetBusinessDayStartHourAsync()),
            OrderNumberResetMode.Daypart => await GetNextDaypartOrderNumberAsync(),
            _ => await GetNextGlobalOrderNumberAsync()
        };

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

    private async Task<int> GetNextDailyOrderNumberAsync(int startHour)
    {
        var localNow = DateTime.Now;
        var localStart = localNow.Date.AddHours(startHour);
        if (localNow < localStart)
        {
            localStart = localStart.AddDays(-1);
        }

        var localEnd = localStart.AddDays(1);

        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, TimeZoneInfo.Local);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, TimeZoneInfo.Local);

        var maxNumber = await _db.Orders
            .Where(o => o.CreatedAt >= utcStart && o.CreatedAt < utcEnd)
            .MaxAsync(o => (int?)o.OrderNumber);

        return (maxNumber ?? 0) + 1;
    }

    private async Task<int> GetNextDaypartOrderNumberAsync()
    {
        var (breakfast, lunch, dinner) = await _settingsService.GetDaypartStartHoursAsync();
        var localNow = DateTime.Now;
        var dayStart = localNow.Date;

        var breakfastStart = dayStart.AddHours(breakfast);
        var lunchStart = dayStart.AddHours(lunch);
        var dinnerStart = dayStart.AddHours(dinner);

        DateTime localStart;
        DateTime localEnd;
        if (localNow >= dinnerStart)
        {
            localStart = dinnerStart;
            localEnd = dayStart.AddDays(1).AddHours(breakfast);
        }
        else if (localNow >= lunchStart)
        {
            localStart = lunchStart;
            localEnd = dinnerStart;
        }
        else if (localNow >= breakfastStart)
        {
            localStart = breakfastStart;
            localEnd = lunchStart;
        }
        else
        {
            localStart = dayStart.AddDays(-1).AddHours(dinner);
            localEnd = breakfastStart;
        }

        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, TimeZoneInfo.Local);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, TimeZoneInfo.Local);

        var maxNumber = await _db.Orders
            .Where(o => o.CreatedAt >= utcStart && o.CreatedAt < utcEnd)
            .MaxAsync(o => (int?)o.OrderNumber);

        return (maxNumber ?? 0) + 1;
    }

    private async Task<int> GetNextGlobalOrderNumberAsync()
    {
        var maxNumber = await _db.Orders.MaxAsync(o => (int?)o.OrderNumber);
        return (maxNumber ?? 0) + 1;
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

        var allowed = await IsCustomizationAllowedAsync(item.MenuItemId, customization.Id);
        if (!allowed)
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

    public async Task<Order?> UpdateCustomerNameAsync(Guid orderId, string? customerName)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
        {
            return null;
        }

        var trimmed = string.IsNullOrWhiteSpace(customerName) ? null : customerName.Trim();
        order.CustomerName = trimmed;
        await SaveChangesWithRetryAsync();
        return order;
    }

    public async Task<bool> CancelOrderAsync(Guid orderId)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
        {
            return false;
        }

        order.Status = OrderStatus.Cancelled;
        await SaveChangesWithRetryAsync();
        return true;
    }

    public async Task<List<RecentOrderRow>> GetRecentOrdersAsync(DateTime? dateLocal = null)
    {
        var localDay = (dateLocal ?? DateTime.Now).Date;
        var localStart = localDay;
        var localEnd = localStart.AddDays(1);

        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, TimeZoneInfo.Local);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, TimeZoneInfo.Local);

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= utcStart && o.CreatedAt < utcEnd)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(o => new RecentOrderRow
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status.ToString(),
                CustomerName = string.IsNullOrWhiteSpace(o.CustomerName) ? "-" : o.CustomerName,
                TotalCents = o.TotalCents,
                CreatedAt = o.CreatedAt,
                TotalDisplay = FormatCents(o.TotalCents),
                CreatedAtDisplay = o.CreatedAt.ToLocalTime().ToString("g")
            })
            .ToList();
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

    private static string FormatCents(int cents)
    {
        return string.Format("${0:0.00}", cents / 100.0);
    }

    private async Task<bool> IsCustomizationAllowedAsync(Guid menuItemId, Guid customizationId)
    {
        var itemAssignments = _db.CustomizationAssignments
            .AsNoTracking()
            .Where(a => a.MenuItemId == menuItemId);

        return await itemAssignments.AnyAsync(a => a.CustomizationItemId == customizationId)
            && await _db.CustomizationItems.AnyAsync(c => c.Id == customizationId && c.IsActive);
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

public class RecentOrderRow
{
    public Guid OrderId { get; set; }
    public int OrderNumber { get; set; }
    public string Status { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public int TotalCents { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TotalDisplay { get; set; } = "";
    public string CreatedAtDisplay { get; set; } = "";
}
