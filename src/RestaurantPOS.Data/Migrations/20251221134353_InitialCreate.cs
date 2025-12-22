using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RestaurantPOS.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MenuCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderType = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    SubtotalCents = table.Column<int>(type: "INTEGER", nullable: false),
                    TaxCents = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscountCents = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalCents = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    PinHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PriceCents = table.Column<int>(type: "INTEGER", nullable: false),
                    TaxRateBps = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItems_MenuCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "MenuCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentMethod = table.Column<string>(type: "TEXT", nullable: false),
                    AmountCents = table.Column<int>(type: "INTEGER", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NameSnapshot = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UnitPriceCents = table.Column<int>(type: "INTEGER", nullable: false),
                    Qty = table.Column<int>(type: "INTEGER", nullable: false),
                    LineTotalCents = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "MenuCategories",
                columns: new[] { "Id", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("5b2b9638-b8b9-4b62-8c31-39aefbcb8c50"), true, "Burgers", 1 },
                    { new Guid("5d6f9a8a-dfa0-4a1e-8844-0d9d41a76169"), true, "Sides", 2 },
                    { new Guid("f566ceab-7a9a-4fd3-a7b7-9dd3b9e4f90e"), true, "Drinks", 3 }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "DisplayName", "IsActive", "PinHash", "Role" },
                values: new object[] { new Guid("8c43e0f9-5f78-4d0d-8b1c-2f623e5bb343"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Admin", true, "03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4", "Admin" });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "CategoryId", "IsActive", "Name", "PriceCents", "TaxRateBps" },
                values: new object[,]
                {
                    { new Guid("1a80d413-98fd-4aac-a785-7e4a84a1ed9c"), new Guid("f566ceab-7a9a-4fd3-a7b7-9dd3b9e4f90e"), true, "Iced Tea", 209, 0 },
                    { new Guid("7e3a0d89-0a2e-409d-bb6e-7d72af4de2dc"), new Guid("5b2b9638-b8b9-4b62-8c31-39aefbcb8c50"), true, "Veggie Burger", 849, 500 },
                    { new Guid("8b5a657d-c3ef-49d1-b857-09c497ea6341"), new Guid("5b2b9638-b8b9-4b62-8c31-39aefbcb8c50"), true, "Cheese Burger", 899, 500 },
                    { new Guid("93f3c3e7-7640-4d14-9cd5-3b1f8cbe00b2"), new Guid("5d6f9a8a-dfa0-4a1e-8844-0d9d41a76169"), true, "Fries", 299, 500 },
                    { new Guid("9f7d4d8d-1f6d-4c46-9ce0-4b5d7b8f9d1a"), new Guid("f566ceab-7a9a-4fd3-a7b7-9dd3b9e4f90e"), true, "Bottled Water", 149, 0 },
                    { new Guid("a7758e44-fef5-41ff-879d-5045e5b84e85"), new Guid("5d6f9a8a-dfa0-4a1e-8844-0d9d41a76169"), true, "Onion Rings", 349, 500 },
                    { new Guid("bd2c0d0a-5297-46a6-9a89-6de819b8f10f"), new Guid("5b2b9638-b8b9-4b62-8c31-39aefbcb8c50"), true, "Classic Burger", 799, 500 },
                    { new Guid("ea0de778-6d83-4ea7-b0fd-95b88c4a2f1f"), new Guid("f566ceab-7a9a-4fd3-a7b7-9dd3b9e4f90e"), true, "Cola", 199, 0 },
                    { new Guid("f2f09107-0d79-4f51-9df9-c1e5b7af1276"), new Guid("5d6f9a8a-dfa0-4a1e-8844-0d9d41a76169"), true, "Garden Salad", 399, 500 },
                    { new Guid("f439a2a4-0f6e-4f2b-9c5b-7cb1a20d8c7e"), new Guid("f566ceab-7a9a-4fd3-a7b7-9dd3b9e4f90e"), true, "Lemonade", 219, 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CategoryId",
                table: "MenuItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_MenuItemId",
                table: "OrderItems",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                table: "Payments",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "MenuCategories");
        }
    }
}
