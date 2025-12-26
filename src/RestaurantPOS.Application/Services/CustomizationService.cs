using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Data;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Services;

public class CustomizationService
{
    private readonly AppDbContext _db;

    public CustomizationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CustomizationItem>> GetCustomizationsAsync()
    {
        return await _db.CustomizationItems
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<CustomizationItem>> GetAllCustomizationsAsync()
    {
        return await _db.CustomizationItems
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<CustomizationItem> AddCustomizationAsync(string name, int priceCents, bool isActive)
    {
        var item = new CustomizationItem
        {
            Name = name.Trim(),
            PriceCents = priceCents,
            IsActive = isActive
        };

        _db.CustomizationItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<CustomizationItem?> UpdateCustomizationAsync(Guid customizationId, string name, int priceCents, bool isActive)
    {
        var item = await _db.CustomizationItems.FirstOrDefaultAsync(c => c.Id == customizationId);
        if (item is null)
        {
            return null;
        }

        item.Name = name.Trim();
        item.PriceCents = priceCents;
        item.IsActive = isActive;
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> DeleteCustomizationAsync(Guid customizationId)
    {
        var item = await _db.CustomizationItems.FirstOrDefaultAsync(c => c.Id == customizationId);
        if (item is null)
        {
            return false;
        }

        var hasOrders = await _db.OrderItemCustomizations.AnyAsync(c => c.CustomizationItemId == customizationId);
        if (hasOrders)
        {
            return false;
        }

        _db.CustomizationItems.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }
}
