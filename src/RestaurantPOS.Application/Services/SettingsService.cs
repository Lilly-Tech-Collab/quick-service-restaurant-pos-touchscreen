using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Data;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Services;

public class SettingsService
{
    private const string RestaurantNameKey = "RestaurantName";
    private const string OrderNumberResetModeKey = "OrderNumberResetMode";
    private const string BusinessDayStartHourKey = "BusinessDayStartHour";
    private const string DaypartBreakfastStartHourKey = "DaypartBreakfastStartHour";
    private const string DaypartLunchStartHourKey = "DaypartLunchStartHour";
    private const string DaypartDinnerStartHourKey = "DaypartDinnerStartHour";
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

    public async Task<OrderNumberResetMode> GetOrderNumberResetModeAsync()
    {
        var setting = await _db.AppSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == OrderNumberResetModeKey);

        if (setting is not null && Enum.TryParse<OrderNumberResetMode>(setting.Value, out var mode))
        {
            return mode;
        }

        return OrderNumberResetMode.Daily;
    }

    public async Task SetOrderNumberResetModeAsync(OrderNumberResetMode mode)
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == OrderNumberResetModeKey);
        if (setting is null)
        {
            setting = new AppSetting
            {
                Key = OrderNumberResetModeKey,
                Value = mode.ToString()
            };
            _db.AppSettings.Add(setting);
        }
        else
        {
            setting.Value = mode.ToString();
        }

        await _db.SaveChangesAsync();
    }

    public async Task<int> GetBusinessDayStartHourAsync()
    {
        return await GetIntSettingAsync(BusinessDayStartHourKey, 0);
    }

    public async Task SetBusinessDayStartHourAsync(int hour)
    {
        await SetIntSettingAsync(BusinessDayStartHourKey, hour);
    }

    public async Task<(int Breakfast, int Lunch, int Dinner)> GetDaypartStartHoursAsync()
    {
        var breakfast = await GetIntSettingAsync(DaypartBreakfastStartHourKey, 6);
        var lunch = await GetIntSettingAsync(DaypartLunchStartHourKey, 11);
        var dinner = await GetIntSettingAsync(DaypartDinnerStartHourKey, 16);
        return (breakfast, lunch, dinner);
    }

    public async Task SetDaypartStartHoursAsync(int breakfast, int lunch, int dinner)
    {
        await SetIntSettingAsync(DaypartBreakfastStartHourKey, breakfast);
        await SetIntSettingAsync(DaypartLunchStartHourKey, lunch);
        await SetIntSettingAsync(DaypartDinnerStartHourKey, dinner);
    }

    private async Task<int> GetIntSettingAsync(string key, int fallback)
    {
        var setting = await _db.AppSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting is not null && int.TryParse(setting.Value, out var value))
        {
            return value;
        }

        return fallback;
    }

    private async Task SetIntSettingAsync(string key, int value)
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting is null)
        {
            setting = new AppSetting
            {
                Key = key,
                Value = value.ToString()
            };
            _db.AppSettings.Add(setting);
        }
        else
        {
            setting.Value = value.ToString();
        }

        await _db.SaveChangesAsync();
    }
}
