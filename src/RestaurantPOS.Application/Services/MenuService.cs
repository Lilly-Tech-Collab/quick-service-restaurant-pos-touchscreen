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

    public async Task<MenuCategory> AddCategoryAsync(string name, int sortOrder, bool isActive)
    {
        var category = new MenuCategory
        {
            Name = name.Trim(),
            SortOrder = sortOrder,
            IsActive = isActive
        };

        _db.MenuCategories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<MenuCategory?> UpdateCategoryAsync(Guid categoryId, string name, int sortOrder, bool isActive)
    {
        var category = await _db.MenuCategories.FirstOrDefaultAsync(c => c.Id == categoryId);
        if (category is null)
        {
            return null;
        }

        category.Name = name.Trim();
        category.SortOrder = sortOrder;
        category.IsActive = isActive;
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<bool> DeleteCategoryAsync(Guid categoryId)
    {
        var category = await _db.MenuCategories.FirstOrDefaultAsync(c => c.Id == categoryId);
        if (category is null)
        {
            return false;
        }

        var hasItems = await _db.MenuItems.AnyAsync(i => i.CategoryId == categoryId);
        if (hasItems)
        {
            return false;
        }

        _db.MenuCategories.Remove(category);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<MenuItem> AddMenuItemAsync(Guid categoryId, string name, int priceCents, int taxRateBps, bool isActive)
    {
        var item = new MenuItem
        {
            CategoryId = categoryId,
            Name = name.Trim(),
            PriceCents = priceCents,
            TaxRateBps = taxRateBps,
            IsActive = isActive
        };

        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<MenuItem?> UpdateMenuItemAsync(Guid menuItemId, Guid categoryId, string name, int priceCents, int taxRateBps, bool isActive)
    {
        var item = await _db.MenuItems.FirstOrDefaultAsync(i => i.Id == menuItemId);
        if (item is null)
        {
            return null;
        }

        item.CategoryId = categoryId;
        item.Name = name.Trim();
        item.PriceCents = priceCents;
        item.TaxRateBps = taxRateBps;
        item.IsActive = isActive;
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> DeleteMenuItemAsync(Guid menuItemId)
    {
        var item = await _db.MenuItems.FirstOrDefaultAsync(i => i.Id == menuItemId);
        if (item is null)
        {
            return false;
        }

        var hasOrders = await _db.OrderItems.AnyAsync(i => i.MenuItemId == menuItemId);
        if (hasOrders)
        {
            return false;
        }

        _db.MenuItems.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }
}
