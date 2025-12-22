using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Data;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Services;

public class SettingsService
{
    private const string RestaurantNameKey = "RestaurantName";
    private readonly AppDbContext _db;

    public SettingsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string> GetRestaurantNameAsync()
    {
        var setting = await _db.AppSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == RestaurantNameKey);

        return string.IsNullOrWhiteSpace(setting?.Value) ? "Restaurant POS" : setting.Value;
    }

    public async Task SetRestaurantNameAsync(string name)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == RestaurantNameKey);
        if (setting is null)
        {
            setting = new AppSetting
            {
                Key = RestaurantNameKey,
                Value = trimmed
            };
            _db.AppSettings.Add(setting);
        }
        else
        {
            setting.Value = trimmed;
        }

        await _db.SaveChangesAsync();
    }
}
