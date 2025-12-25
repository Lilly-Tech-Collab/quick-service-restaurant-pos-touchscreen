using System.Security.Cryptography;
using System.Text;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Data;

public static class DataSeeder
{
    private const string SettingRestaurantNameKey = "RestaurantName";
    private static readonly Guid RestaurantNameSettingId = Guid.Parse("55d918c9-4f86-4cf4-9166-b6d4c9a7c1fb");
    private static readonly Guid CategoryBurgersId = Guid.Parse("5b2b9638-b8b9-4b62-8c31-39aefbcb8c50");
    private static readonly Guid CategorySidesId = Guid.Parse("5d6f9a8a-dfa0-4a1e-8844-0d9d41a76169");
    private static readonly Guid CategoryDrinksId = Guid.Parse("f566ceab-7a9a-4fd3-a7b7-9dd3b9e4f90e");

    private static readonly Guid ItemClassicBurgerId = Guid.Parse("bd2c0d0a-5297-46a6-9a89-6de819b8f10f");
    private static readonly Guid ItemCheeseBurgerId = Guid.Parse("8b5a657d-c3ef-49d1-b857-09c497ea6341");
    private static readonly Guid ItemVeggieBurgerId = Guid.Parse("7e3a0d89-0a2e-409d-bb6e-7d72af4de2dc");
    private static readonly Guid ItemFriesId = Guid.Parse("93f3c3e7-7640-4d14-9cd5-3b1f8cbe00b2");
    private static readonly Guid ItemOnionRingsId = Guid.Parse("a7758e44-fef5-41ff-879d-5045e5b84e85");
    private static readonly Guid ItemGardenSaladId = Guid.Parse("f2f09107-0d79-4f51-9df9-c1e5b7af1276");
    private static readonly Guid ItemColaId = Guid.Parse("ea0de778-6d83-4ea7-b0fd-95b88c4a2f1f");
    private static readonly Guid ItemLemonadeId = Guid.Parse("f439a2a4-0f6e-4f2b-9c5b-7cb1a20d8c7e");
    private static readonly Guid ItemIcedTeaId = Guid.Parse("1a80d413-98fd-4aac-a785-7e4a84a1ed9c");
    private static readonly Guid ItemWaterId = Guid.Parse("9f7d4d8d-1f6d-4c46-9ce0-4b5d7b8f9d1a");
    private static readonly Guid CustomizationNoCheeseId = Guid.Parse("f4a7e147-93a6-47de-9cc9-1706e3f54f90");
    private static readonly Guid CustomizationExtraCheeseId = Guid.Parse("6f1f0fd3-1224-4bcf-9f7e-93694dc43071");
    private static readonly Guid CustomizationExtraSauceId = Guid.Parse("7b1d6c2a-7d8f-4d6e-8071-71e483f3c4c6");

    public static readonly User[] Users =
    {
        new()
        {
            Id = Guid.Parse("8c43e0f9-5f78-4d0d-8b1c-2f623e5bb343"),
            DisplayName = "Admin",
            PinHash = ComputePinHash("1234"),
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        }
    };

    public static readonly AppSetting[] AppSettings =
    {
        new()
        {
            Id = RestaurantNameSettingId,
            Key = SettingRestaurantNameKey,
            Value = "Restaurant POS"
        }
    };

    public static readonly MenuCategory[] Categories =
    {
        new()
        {
            Id = CategoryBurgersId,
            Name = "Burgers",
            SortOrder = 1,
            IsActive = true
        },
        new()
        {
            Id = CategorySidesId,
            Name = "Sides",
            SortOrder = 2,
            IsActive = true
        },
        new()
        {
            Id = CategoryDrinksId,
            Name = "Drinks",
            SortOrder = 3,
            IsActive = true
        }
    };

    public static readonly MenuItem[] MenuItems =
    {
        new()
        {
            Id = ItemClassicBurgerId,
            CategoryId = CategoryBurgersId,
            Name = "Classic Burger",
            PriceCents = 799,
            TaxRateBps = 500,
            IsActive = true
        },
        new()
        {
            Id = ItemCheeseBurgerId,
            CategoryId = CategoryBurgersId,
            Name = "Cheese Burger",
            PriceCents = 899,
            TaxRateBps = 500,
            IsActive = true
        },
        new()
        {
            Id = ItemVeggieBurgerId,
            CategoryId = CategoryBurgersId,
            Name = "Veggie Burger",
            PriceCents = 849,
            TaxRateBps = 500,
            IsActive = true
        },
        new()
        {
            Id = ItemFriesId,
            CategoryId = CategorySidesId,
            Name = "Fries",
            PriceCents = 299,
            TaxRateBps = 500,
            IsActive = true
        },
        new()
        {
            Id = ItemOnionRingsId,
            CategoryId = CategorySidesId,
            Name = "Onion Rings",
            PriceCents = 349,
            TaxRateBps = 500,
            IsActive = true
        },
        new()
        {
            Id = ItemGardenSaladId,
            CategoryId = CategorySidesId,
            Name = "Garden Salad",
            PriceCents = 399,
            TaxRateBps = 500,
            IsActive = true
        },
        new()
        {
            Id = ItemColaId,
            CategoryId = CategoryDrinksId,
            Name = "Cola",
            PriceCents = 199,
            TaxRateBps = 0,
            IsActive = true
        },
        new()
        {
            Id = ItemLemonadeId,
            CategoryId = CategoryDrinksId,
            Name = "Lemonade",
            PriceCents = 219,
            TaxRateBps = 0,
            IsActive = true
        },
        new()
        {
            Id = ItemIcedTeaId,
            CategoryId = CategoryDrinksId,
            Name = "Iced Tea",
            PriceCents = 209,
            TaxRateBps = 0,
            IsActive = true
        },
        new()
        {
            Id = ItemWaterId,
            CategoryId = CategoryDrinksId,
            Name = "Bottled Water",
            PriceCents = 149,
            TaxRateBps = 0,
            IsActive = true
        }
    };

    public static readonly CustomizationItem[] CustomizationItems =
    {
        new()
        {
            Id = CustomizationNoCheeseId,
            Name = "No Cheese",
            PriceCents = 0,
            IsActive = true
        },
        new()
        {
            Id = CustomizationExtraCheeseId,
            Name = "Extra Cheese",
            PriceCents = 50,
            IsActive = true
        },
        new()
        {
            Id = CustomizationExtraSauceId,
            Name = "Extra Sauce",
            PriceCents = 25,
            IsActive = true
        }
    };

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
