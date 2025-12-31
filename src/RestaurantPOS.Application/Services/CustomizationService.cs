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

    public async Task<List<CustomizationItem>> GetCustomizationsForMenuItemAsync(Guid menuItemId)
    {
        var itemAssignmentIds = await _db.CustomizationAssignments
            .AsNoTracking()
            .Where(a => a.MenuItemId == menuItemId)
            .Select(a => a.CustomizationItemId)
            .ToListAsync();

        if (itemAssignmentIds.Count == 0)
        {
            return new List<CustomizationItem>();
        }

        return await _db.CustomizationItems
            .AsNoTracking()
            .Where(c => c.IsActive && itemAssignmentIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<MenuItem>> GetAssignedMenuItemsAsync(Guid customizationId)
    {
        return await _db.CustomizationAssignments
            .AsNoTracking()
            .Where(a => a.CustomizationItemId == customizationId && a.MenuItemId.HasValue)
            .Join(_db.MenuItems.AsNoTracking(),
                assignment => assignment.MenuItemId!.Value,
                menuItem => menuItem.Id,
                (_, menuItem) => menuItem)
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<bool> AssignCustomizationToMenuItemAsync(Guid customizationId, Guid menuItemId)
    {
        var exists = await _db.CustomizationAssignments
            .AnyAsync(a => a.CustomizationItemId == customizationId && a.MenuItemId == menuItemId);
        if (exists)
        {
            return false;
        }

        var assignment = new CustomizationAssignment
        {
            CustomizationItemId = customizationId,
            MenuItemId = menuItemId
        };

        _db.CustomizationAssignments.Add(assignment);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveCustomizationFromMenuItemAsync(Guid customizationId, Guid menuItemId)
    {
        var assignment = await _db.CustomizationAssignments
            .FirstOrDefaultAsync(a => a.CustomizationItemId == customizationId && a.MenuItemId == menuItemId);
        if (assignment is null)
        {
            return false;
        }

        _db.CustomizationAssignments.Remove(assignment);
        await _db.SaveChangesAsync();
        return true;
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

        var hasAssignments = await _db.CustomizationAssignments.AnyAsync(a => a.CustomizationItemId == customizationId);
        if (hasAssignments)
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
