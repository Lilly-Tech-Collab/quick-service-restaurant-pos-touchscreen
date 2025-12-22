using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Data;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Services;

public class ReportSummary
{
    public int TotalOrders { get; set; }
    public int TotalSalesCents { get; set; }
    public int CashCents { get; set; }
    public int CardCents { get; set; }
    public int UpiCents { get; set; }
    public int GiftCents { get; set; }
}

public class ReportOrderRow
{
    public int OrderNumber { get; set; }
    public string PaymentMethod { get; set; } = "";
    public int TotalCents { get; set; }
    public DateTime PaidAt { get; set; }
    public string TotalDisplay { get; set; } = "";
    public string PaidAtDisplay { get; set; } = "";
}

public class ReportItemRow
{
    public string ItemName { get; set; } = "";
    public int Quantity { get; set; }
}

public class ReportCategoryRow
{
    public string CategoryName { get; set; } = "";
    public int Quantity { get; set; }
    public int RevenueCents { get; set; }
    public string RevenueDisplay { get; set; } = "";
}

public class ReportTopItemRow
{
    public string ItemName { get; set; } = "";
    public int Quantity { get; set; }
    public int RevenueCents { get; set; }
    public string RevenueDisplay { get; set; } = "";
}

public class ReportHourlyRow
{
    public string HourLabel { get; set; } = "";
    public int RevenueCents { get; set; }
    public string RevenueDisplay { get; set; } = "";
}

public class ReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ReportSummary> GetDailySummaryAsync(DateTime? dateUtc = null)
    {
        var day = (dateUtc ?? DateTime.UtcNow).Date;
        var end = day.AddDays(1);

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Paid && o.CreatedAt >= day && o.CreatedAt < end)
            .ToListAsync();

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.PaidAt >= day && p.PaidAt < end)
            .ToListAsync();

        var summary = new ReportSummary
        {
            TotalOrders = orders.Count,
            TotalSalesCents = orders.Sum(o => o.TotalCents)
        };

        foreach (var payment in payments)
        {
            switch (payment.PaymentMethod)
            {
                case PaymentMethod.Cash:
                    summary.CashCents += payment.AmountCents;
                    break;
                case PaymentMethod.Card:
                    summary.CardCents += payment.AmountCents;
                    break;
                case PaymentMethod.Upi:
                    summary.UpiCents += payment.AmountCents;
                    break;
                case PaymentMethod.Gift:
                    summary.GiftCents += payment.AmountCents;
                    break;
            }
        }

        return summary;
    }

    public async Task<List<ReportOrderRow>> GetDailyOrdersAsync(DateTime? dateUtc = null)
    {
        var day = (dateUtc ?? DateTime.UtcNow).Date;
        var end = day.AddDays(1);

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.PaidAt >= day && p.PaidAt < end)
            .ToListAsync();

        var orderIds = payments.Select(p => p.OrderId).Distinct().ToList();
        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => orderIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id);

        var rows = new List<ReportOrderRow>();
        foreach (var payment in payments.OrderByDescending(p => p.PaidAt))
        {
            if (!orders.TryGetValue(payment.OrderId, out var order))
            {
                continue;
            }

            rows.Add(new ReportOrderRow
            {
                OrderNumber = order.OrderNumber,
                PaymentMethod = payment.PaymentMethod.ToString(),
                TotalCents = payment.AmountCents,
                PaidAt = payment.PaidAt,
                TotalDisplay = FormatCents(payment.AmountCents),
                PaidAtDisplay = payment.PaidAt.ToLocalTime().ToString("g")
            });
        }

        return rows;
    }

    public async Task<List<ReportItemRow>> GetDailyItemSalesAsync(DateTime? dateUtc = null)
    {
        var day = (dateUtc ?? DateTime.UtcNow).Date;
        var end = day.AddDays(1);

        var items = await _db.OrderItems
            .AsNoTracking()
            .Join(_db.Orders.AsNoTracking(),
                item => item.OrderId,
                order => order.Id,
                (item, order) => new { item, order })
            .Where(x => x.order.Status == OrderStatus.Paid && x.order.CreatedAt >= day && x.order.CreatedAt < end)
            .ToListAsync();

        return items
            .GroupBy(x => x.item.NameSnapshot)
            .Select(g => new ReportItemRow
            {
                ItemName = g.Key,
                Quantity = g.Sum(x => x.item.Qty)
            })
            .OrderByDescending(r => r.Quantity)
            .ThenBy(r => r.ItemName)
            .ToList();
    }

    public async Task<List<ReportCategoryRow>> GetDailyCategorySalesAsync(DateTime? dateUtc = null)
    {
        var day = (dateUtc ?? DateTime.UtcNow).Date;
        var end = day.AddDays(1);

        var items = await _db.OrderItems
            .AsNoTracking()
            .Join(_db.Orders.AsNoTracking(),
                item => item.OrderId,
                order => order.Id,
                (item, order) => new { item, order })
            .Join(_db.MenuItems.AsNoTracking(),
                io => io.item.MenuItemId,
                menu => menu.Id,
                (io, menu) => new { io.item, io.order, menu })
            .Join(_db.MenuCategories.AsNoTracking(),
                iom => iom.menu.CategoryId,
                category => category.Id,
                (iom, category) => new { iom.item, iom.order, category })
            .Where(x => x.order.Status == OrderStatus.Paid && x.order.CreatedAt >= day && x.order.CreatedAt < end)
            .ToListAsync();

        return items
            .GroupBy(x => x.category.Name)
            .Select(g => new ReportCategoryRow
            {
                CategoryName = g.Key,
                Quantity = g.Sum(x => x.item.Qty),
                RevenueCents = g.Sum(x => x.item.LineTotalCents)
            })
            .OrderByDescending(r => r.RevenueCents)
            .ThenBy(r => r.CategoryName)
            .Select(r =>
            {
                r.RevenueDisplay = FormatCents(r.RevenueCents);
                return r;
            })
            .ToList();
    }

    public async Task<List<ReportTopItemRow>> GetTopItemsAsync(DateTime? dateUtc = null, int take = 10)
    {
        var day = (dateUtc ?? DateTime.UtcNow).Date;
        var end = day.AddDays(1);

        var items = await _db.OrderItems
            .AsNoTracking()
            .Join(_db.Orders.AsNoTracking(),
                item => item.OrderId,
                order => order.Id,
                (item, order) => new { item, order })
            .Where(x => x.order.Status == OrderStatus.Paid && x.order.CreatedAt >= day && x.order.CreatedAt < end)
            .ToListAsync();

        return items
            .GroupBy(x => x.item.NameSnapshot)
            .Select(g => new ReportTopItemRow
            {
                ItemName = g.Key,
                Quantity = g.Sum(x => x.item.Qty),
                RevenueCents = g.Sum(x => x.item.LineTotalCents)
            })
            .OrderByDescending(r => r.Quantity)
            .ThenByDescending(r => r.RevenueCents)
            .ThenBy(r => r.ItemName)
            .Take(take)
            .Select(r =>
            {
                r.RevenueDisplay = FormatCents(r.RevenueCents);
                return r;
            })
            .ToList();
    }

    public async Task<List<ReportHourlyRow>> GetHourlySalesAsync(DateTime? dateUtc = null)
    {
        var day = (dateUtc ?? DateTime.UtcNow).Date;
        var end = day.AddDays(1);

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.PaidAt >= day && p.PaidAt < end)
            .ToListAsync();

        return payments
            .GroupBy(p => p.PaidAt.Hour)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var cents = g.Sum(p => p.AmountCents);
                return new ReportHourlyRow
                {
                    HourLabel = $"{g.Key:00}:00",
                    RevenueCents = cents,
                    RevenueDisplay = FormatCents(cents)
                };
            })
            .ToList();
    }

    private static string FormatCents(int cents)
    {
        return string.Format("${0:0.00}", cents / 100.0);
    }
}
