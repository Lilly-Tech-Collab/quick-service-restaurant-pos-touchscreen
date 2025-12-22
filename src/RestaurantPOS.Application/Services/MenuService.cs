using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Data;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Services;

public class MenuService
{
    private readonly AppDbContext _db;

    public MenuService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<MenuCategory>> GetCategoriesAsync()
    {
        return await _db.MenuCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<MenuCategory>> GetAllCategoriesAsync()
    {
        return await _db.MenuCategories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<MenuItem>> GetMenuItemsAsync(Guid? categoryId = null)
    {
        var query = _db.MenuItems.AsNoTracking().Where(i => i.IsActive);
        if (categoryId.HasValue)
        {
            query = query.Where(i => i.CategoryId == categoryId.Value);
        }

        return await query.OrderBy(i => i.Name).ToListAsync();
    }

    public async Task<List<MenuItem>> GetAllMenuItemsAsync()
    {
        return await _db.MenuItems
            .AsNoTracking()
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<MenuCategory> AddCategoryAsync(string name, int sortOrder)
    {
        var category = new MenuCategory
        {
            Name = name.Trim(),
            SortOrder = sortOrder,
            IsActive = true
        };

        _db.MenuCategories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<MenuItem> AddMenuItemAsync(Guid categoryId, string name, int priceCents, int taxRateBps)
    {
        var item = new MenuItem
        {
            CategoryId = categoryId,
            Name = name.Trim(),
            PriceCents = priceCents,
            TaxRateBps = taxRateBps,
            IsActive = true
        };

        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }
}
