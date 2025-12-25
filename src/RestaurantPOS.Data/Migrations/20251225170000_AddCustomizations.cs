using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomizationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PriceCents = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomizationItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderItemCustomizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomizationItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NameSnapshot = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PriceCents = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemCustomizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemCustomizations_CustomizationItems_CustomizationItemId",
                        column: x => x.CustomizationItemId,
                        principalTable: "CustomizationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItemCustomizations_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CustomizationItems",
                columns: new[] { "Id", "IsActive", "Name", "PriceCents" },
                values: new object[,]
                {
                    { new Guid("f4a7e147-93a6-47de-9cc9-1706e3f54f90"), true, "No Cheese", 0 },
                    { new Guid("6f1f0fd3-1224-4bcf-9f7e-93694dc43071"), true, "Extra Cheese", 50 },
                    { new Guid("7b1d6c2a-7d8f-4d6e-8071-71e483f3c4c6"), true, "Extra Sauce", 25 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemCustomizations_CustomizationItemId",
                table: "OrderItemCustomizations",
                column: "CustomizationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemCustomizations_OrderItemId",
                table: "OrderItemCustomizations",
                column: "OrderItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItemCustomizations");

            migrationBuilder.DropTable(
                name: "CustomizationItems");
        }
    }
}
