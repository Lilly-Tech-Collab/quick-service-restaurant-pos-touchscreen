using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Data;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Services;

public class UserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        return await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.DisplayName)
            .ToListAsync();
    }

    public async Task<User> CreateUserAsync(string displayName, string pin, UserRole role, bool isActive)
    {
        var user = new User
        {
            DisplayName = displayName.Trim(),
            PinHash = ComputePinHash(pin),
            Role = role,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<User?> UpdateUserAsync(Guid userId, string displayName, string? pin, UserRole role, bool isActive)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return null;
        }

        user.DisplayName = displayName.Trim();
        if (!string.IsNullOrWhiteSpace(pin))
        {
            user.PinHash = ComputePinHash(pin);
        }
        user.Role = role;
        user.IsActive = isActive;
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return false;
        }

        var hasOrders = await _db.Orders.AnyAsync(o => o.CreatedByUserId == userId);
        if (hasOrders)
        {
            return false;
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return true;
    }

    private static string ComputePinHash(string pin)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(pin));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
