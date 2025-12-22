using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Data;

public class AppDbContext : DbContext
{
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<User> Users => Set<User>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Id);
            e.Property(x => x.DisplayName).IsRequired().HasMaxLength(120);
            e.Property(x => x.PinHash).IsRequired().HasMaxLength(128);
            e.Property(x => x.Role).HasConversion<string>().IsRequired();
            e.Property(x => x.IsActive).IsRequired();
            e.Property(x => x.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<AppSetting>(e =>
        {
            e.ToTable("AppSettings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).IsRequired().HasMaxLength(120);
            e.Property(x => x.Value).IsRequired().HasMaxLength(500);
            e.HasIndex(x => x.Key).IsUnique();
        });

        modelBuilder.Entity<MenuCategory>(e =>
        {
            e.ToTable("MenuCategories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(120);
            e.Property(x => x.SortOrder).IsRequired();
            e.Property(x => x.IsActive).IsRequired();
        });

        modelBuilder.Entity<MenuItem>(e =>
        {
            e.ToTable("MenuItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.PriceCents).IsRequired();
            e.Property(x => x.TaxRateBps).IsRequired();
            e.Property(x => x.IsActive).IsRequired();
            e.HasOne<MenuCategory>()
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("Orders");
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderNumber).IsRequired();
            e.Property(x => x.OrderType).HasConversion<string>().IsRequired();
            e.Property(x => x.Status).HasConversion<string>().IsRequired();
            e.Property(x => x.CreatedAt).IsRequired();
            e.Property(x => x.SubtotalCents).IsRequired();
            e.Property(x => x.TaxCents).IsRequired();
            e.Property(x => x.DiscountCents).IsRequired();
            e.Property(x => x.TotalCents).IsRequired();
            e.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Payments)
                .WithOne()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.ToTable("OrderItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.NameSnapshot).IsRequired().HasMaxLength(200);
            e.Property(x => x.UnitPriceCents).IsRequired();
            e.Property(x => x.Qty).IsRequired();
            e.Property(x => x.LineTotalCents).IsRequired();
            e.HasOne<MenuItem>()
                .WithMany()
                .HasForeignKey(x => x.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("Payments");
            e.HasKey(x => x.Id);
            e.Property(x => x.PaymentMethod).HasConversion<string>().IsRequired();
            e.Property(x => x.AmountCents).IsRequired();
            e.Property(x => x.PaidAt).IsRequired();
        });

        modelBuilder.Entity<User>().HasData(DataSeeder.Users);
        modelBuilder.Entity<AppSetting>().HasData(DataSeeder.AppSettings);
        modelBuilder.Entity<MenuCategory>().HasData(DataSeeder.Categories);
        modelBuilder.Entity<MenuItem>().HasData(DataSeeder.MenuItems);
    }
}
