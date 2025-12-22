# Simple Restaurant POS (Desktop) — .NET (C#) + SQLite

This document is a **starter blueprint** for building a **simple restaurant POS desktop application** using **C# (.NET)** and **SQLite**.  
It’s written so you can copy/paste sections into your repo as a living spec.

---

## 1) MVP Goals (What we are building first)

**Core flow:** `Login → Order → Payment → Receipt`

### Must-have screens (MVP)
1. **Login / User PIN**
2. **Order / Sales (Main POS)**
3. **Payment**
4. **Receipt / Print**
5. **Kitchen Ticket (KOT) — optional but recommended**
6. **Daily Sales / Basic Reports**
7. **Menu Management (Admin)**

---

## 2) Recommended Tech Stack (Simple + Reliable)

### Desktop UI
- **WPF (.NET)** (recommended for Windows desktop POS)
  - Pros: Great for touch screens, mature, runs offline, printer integrations are straightforward.

> Alternative: WinForms (fast to build, less modern UI).

### Data
- **SQLite** (embedded database file)
- **Entity Framework Core (EF Core)** for ORM + migrations

### Printing
- **ESC/POS thermal printers** (common in restaurants) via:
  - Windows printer driver (easiest)
  - OR raw ESC/POS commands (advanced)

### Architecture pattern
- **MVVM** for WPF (clean separation: View / ViewModel / Model)
- Layering:
  - **UI (WPF)**
  - **Application/Services**
  - **Data (EF Core)**
  - **Domain (Entities)**

---

## 3) Requirements & Assumptions

- Works **offline** (no internet needed).
- SQLite DB file is stored locally (e.g., in `%ProgramData%/YourPOS/pos.db`).
- Single store, single device (MVP).
- Later, you can add sync/cloud multi-terminal support.

---

## 4) Solution Structure (Suggested)

```
RestaurantPOS.sln
  /src
    /RestaurantPOS.App            (WPF UI)
    /RestaurantPOS.Domain         (Entities + enums)
    /RestaurantPOS.Application    (Services + business logic)
    /RestaurantPOS.Data           (EF Core DbContext + migrations)
  /docs
    POS-Desktop-DotNet-SQLite.md  (this file)
```

---

## 5) Database Model (MVP Tables)

### Entities (minimum)
- Users
- MenuCategories
- MenuItems
- Orders
- OrderItems
- Payments
- Receipts (optional; often can be derived)
- AuditLog (optional but recommended)

### ER overview (simple)
- **Order** has many **OrderItems**
- **OrderItem** references **MenuItem**
- **Payment** references **Order**
- **User** creates **Order**

---

## 6) SQLite Schema (SQL Version)

> You can use this even if you use EF Core, as a reference.

```sql
-- Users
CREATE TABLE IF NOT EXISTS Users (
  Id TEXT PRIMARY KEY,
  DisplayName TEXT NOT NULL,
  PinHash TEXT NOT NULL,
  Role TEXT NOT NULL, -- Admin, Manager, Cashier
  IsActive INTEGER NOT NULL DEFAULT 1,
  CreatedAt TEXT NOT NULL
);

-- Menu
CREATE TABLE IF NOT EXISTS MenuCategories (
  Id TEXT PRIMARY KEY,
  Name TEXT NOT NULL,
  SortOrder INTEGER NOT NULL DEFAULT 0,
  IsActive INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS MenuItems (
  Id TEXT PRIMARY KEY,
  CategoryId TEXT NOT NULL,
  Name TEXT NOT NULL,
  PriceCents INTEGER NOT NULL,
  TaxRateBps INTEGER NOT NULL DEFAULT 0, -- basis points: 500 = 5.00%
  IsActive INTEGER NOT NULL DEFAULT 1,
  FOREIGN KEY (CategoryId) REFERENCES MenuCategories(Id)
);

-- Orders
CREATE TABLE IF NOT EXISTS Orders (
  Id TEXT PRIMARY KEY,
  OrderNumber INTEGER NOT NULL,
  OrderType TEXT NOT NULL, -- DineIn, TakeAway, Delivery
  Status TEXT NOT NULL,    -- Open, Paid, Cancelled
  CreatedByUserId TEXT NOT NULL,
  CreatedAt TEXT NOT NULL,
  Notes TEXT,
  SubtotalCents INTEGER NOT NULL DEFAULT 0,
  TaxCents INTEGER NOT NULL DEFAULT 0,
  DiscountCents INTEGER NOT NULL DEFAULT 0,
  TotalCents INTEGER NOT NULL DEFAULT 0,
  FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS OrderItems (
  Id TEXT PRIMARY KEY,
  OrderId TEXT NOT NULL,
  MenuItemId TEXT NOT NULL,
  NameSnapshot TEXT NOT NULL,
  UnitPriceCents INTEGER NOT NULL,
  Qty INTEGER NOT NULL,
  LineTotalCents INTEGER NOT NULL,
  Notes TEXT,
  FOREIGN KEY (OrderId) REFERENCES Orders(Id),
  FOREIGN KEY (MenuItemId) REFERENCES MenuItems(Id)
);

-- Payments
CREATE TABLE IF NOT EXISTS Payments (
  Id TEXT PRIMARY KEY,
  OrderId TEXT NOT NULL,
  PaymentMethod TEXT NOT NULL, -- Cash, Card, UPI, Gift
  AmountCents INTEGER NOT NULL,
  Reference TEXT,
  PaidAt TEXT NOT NULL,
  FOREIGN KEY (OrderId) REFERENCES Orders(Id)
);

-- Optional: audit trail
CREATE TABLE IF NOT EXISTS AuditLog (
  Id TEXT PRIMARY KEY,
  EventType TEXT NOT NULL,
  PayloadJson TEXT,
  CreatedAt TEXT NOT NULL,
  UserId TEXT
);
```

---

## 7) .NET Setup (Recommended: .NET 9)

If your Visual Studio doesn’t support .NET 10 yet, target **.NET 9**.

### Create solution & projects
```bash
mkdir RestaurantPOS && cd RestaurantPOS
dotnet new sln -n RestaurantPOS

dotnet new wpf -n RestaurantPOS.App -f net9.0
dotnet new classlib -n RestaurantPOS.Domain -f net9.0
dotnet new classlib -n RestaurantPOS.Application -f net9.0
dotnet new classlib -n RestaurantPOS.Data -f net9.0

dotnet sln add src/RestaurantPOS.App/RestaurantPOS.App.csproj
dotnet sln add src/RestaurantPOS.Domain/RestaurantPOS.Domain.csproj
dotnet sln add src/RestaurantPOS.Application/RestaurantPOS.Application.csproj
dotnet sln add src/RestaurantPOS.Data/RestaurantPOS.Data.csproj
```

### Add references
```bash
dotnet add src/RestaurantPOS.App reference src/RestaurantPOS.Application
dotnet add src/RestaurantPOS.Application reference src/RestaurantPOS.Domain
dotnet add src/RestaurantPOS.Application reference src/RestaurantPOS.Data
dotnet add src/RestaurantPOS.Data reference src/RestaurantPOS.Domain
```

---

## 8) EF Core + SQLite (Data Project)

### Add packages
```bash
dotnet add src/RestaurantPOS.Data package Microsoft.EntityFrameworkCore
dotnet add src/RestaurantPOS.Data package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/RestaurantPOS.Data package Microsoft.EntityFrameworkCore.Design
```

### Domain Entities (example)
Create `src/RestaurantPOS.Domain/Entities/MenuItem.cs`
```csharp
namespace RestaurantPOS.Domain.Entities;

public class MenuItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = "";
    public int PriceCents { get; set; }
    public int TaxRateBps { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
```

### DbContext
Create `src/RestaurantPOS.Data/AppDbContext.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Data;

public class AppDbContext : DbContext
{
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    // TODO: Add Users, Orders, OrderItems, Payments, Categories

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Example: MenuItem mapping
        modelBuilder.Entity<MenuItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
        });
    }
}
```

### Connection string & DI (in WPF App)
In WPF you can use `Microsoft.Extensions.Hosting` for DI.

Add packages:
```bash
dotnet add src/RestaurantPOS.App package Microsoft.Extensions.Hosting
dotnet add src/RestaurantPOS.App package Microsoft.Extensions.DependencyInjection
dotnet add src/RestaurantPOS.App package Microsoft.Extensions.Configuration.Json
dotnet add src/RestaurantPOS.App package Microsoft.EntityFrameworkCore
dotnet add src/RestaurantPOS.App package Microsoft.EntityFrameworkCore.Sqlite
```

Create `src/RestaurantPOS.App/appsettings.json`
```json
{
  "ConnectionStrings": {
    "PosDb": "Data Source=pos.db"
  }
}
```

Create `src/RestaurantPOS.App/App.xaml.cs`
```csharp
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Data;

namespace RestaurantPOS.App;

public partial class App : System.Windows.Application
{
    public static IHost AppHost { get; private set; } = null!;

    public App()
    {
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.AddJsonFile("appsettings.json", optional: false);
            })
            .ConfigureServices((ctx, services) =>
            {
                var cs = ctx.Configuration.GetConnectionString("PosDb")!;
                services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(cs));

                // TODO: services.AddSingleton<MainWindow>();
                // TODO: services.AddTransient<OrderService>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await AppHost.StartAsync();

        // Ensure DB is created (simple approach for MVP)
        using var scope = AppHost.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        base.OnStartup(e);

        var main = new MainWindow();
        main.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost.StopAsync();
        base.OnExit(e);
    }
}
```

---

## 9) Screen-by-Screen UI Requirements (WPF)

### A) Login Screen
- User list or “Enter PIN”
- Role-based navigation

**ViewModel fields**
- `Pin`
- `SelectedUser`
- `LoginCommand`

---

### B) Order Screen (Main POS)
**Left:** Categories + Items  
**Right:** Current ticket (cart)  
**Bottom:** Actions

Actions:
- Add item
- Increase/decrease qty
- Remove
- Notes
- Dine-in / Takeaway
- “Send to Kitchen” (optional)
- “Pay”

---

### C) Payment Screen
- Total, tax, discount
- Choose payment method
- Split payment (optional)
- Confirm payment → mark order Paid

---

### D) Receipt Screen
- Show receipt preview
- Print
- New order

---

### E) Kitchen Ticket Screen (optional)
- Orders list (Open)
- Status changes (New → Preparing → Ready)

---

### F) Reports Screen (basic)
- Total sales today
- Cash vs Card
- Item-wise sales

---

### G) Menu Management (Admin)
- CRUD Categories + Items
- Enable/disable item
- Set price/tax

---

## 10) Hardware Integration Notes (Practical)

### Printers (Thermal receipt)
Simplest: Install Windows printer driver → print via `PrintDialog`.

Later: add ESC/POS library for precise formatting.

### Cash drawer
Many drawers open via printer “kick” command (ESC/POS) when printing receipt.

### Barcode scanner
Most work as **keyboard input** (no special integration needed).

### Card reader
Usually handled by payment providers (Square/Stripe terminal/etc.).
For MVP, store **payment reference** + method; don’t do direct processing.

---

## 11) Core Business Rules (MVP)

- Order totals computed from OrderItems:
  - `Subtotal = sum(UnitPrice * Qty)`
  - `Tax = sum(lineTax)`
  - `Total = Subtotal + Tax - Discount`
- Order statuses:
  - `Open` → `Paid` OR `Cancelled`
- Payment must equal Total to close (unless partial allowed)

---

## 12) Minimal Services (Application Layer)

Suggested services:
- `AuthService` (validate PIN, roles)
- `MenuService` (get categories/items)
- `OrderService` (create order, add/remove items, compute totals)
- `PaymentService` (record payment, close order)
- `ReportService` (simple daily summaries)
- `PrinterService` (print receipt/KOT)

---

## 13) Basic Receipt Formatting (Example)

Receipt fields:
- Restaurant name
- Date/time
- Order number
- Items (name, qty, price, line total)
- Subtotal, tax, discount, total
- Payment method
- “Thank you”

---

## 14) Release Packaging (Simple)

For distribution:
```bash
dotnet publish src/RestaurantPOS.App -c Release -r win-x64 --self-contained true
```

Copy:
- EXE + runtime files
- SQLite DB file (optional seed)
- appsettings.json

---

## 15) Next Step Suggestions (After MVP)

- Table management (Dine-in tables)
- Customer-facing display
- Discounts/promos
- Multi-terminal sync (cloud)
- Inventory
- Role-based audit logs
- Backups (auto copy DB daily)

---

## 16) “MVP Done” Checklist

- [ ] Login works (PIN)
- [ ] Menu loads from DB
- [ ] Create order + add items
- [ ] Totals accurate (subtotal/tax/total)
- [ ] Payment recorded
- [ ] Receipt prints
- [ ] Daily sales report shows totals

---

## 17) Notes for Using SQLite in a Restaurant PC

✅ No separate server required  
✅ DB is just a file  
✅ Works offline  
✅ Easy to backup (copy file)

---

If you want, I can also generate:
- **WPF wireframe layout (XAML) for all screens**
- **Full EF Core entities + migrations**
- **A running starter repo structure (code skeleton)**
