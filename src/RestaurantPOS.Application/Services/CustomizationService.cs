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

    public async Task<CustomizationItem> AddCustomizationAsync(string name, int priceCents)
    {
        var item = new CustomizationItem
        {
            Name = name.Trim(),
            PriceCents = priceCents,
            IsActive = true
        };

        _db.CustomizationItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }
}
