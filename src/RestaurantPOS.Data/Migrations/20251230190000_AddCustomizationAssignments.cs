using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomizationAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomizationAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomizationItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MenuCategoryId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomizationAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomizationAssignments_CustomizationItems_CustomizationItemId",
                        column: x => x.CustomizationItemId,
                        principalTable: "CustomizationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomizationAssignments_MenuCategories_MenuCategoryId",
                        column: x => x.MenuCategoryId,
                        principalTable: "MenuCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomizationAssignments_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.CheckConstraint(
                        name: "CK_CustomizationAssignments_Target",
                        sql: "MenuItemId IS NOT NULL OR MenuCategoryId IS NOT NULL");
                });

            migrationBuilder.InsertData(
                table: "CustomizationAssignments",
                columns: new[] { "Id", "CustomizationItemId", "MenuCategoryId", "MenuItemId" },
                values: new object[,]
                {
                    { new Guid("c52b4d10-8b4a-4c9a-9bb8-5412fd329f3c"), new Guid("f4a7e147-93a6-47de-9cc9-1706e3f54f90"), new Guid("5b2b9638-b8b9-4b62-8c31-39aefbcb8c50"), null },
                    { new Guid("d55f4b29-4bde-43b5-9d2b-1d35a0d0a257"), new Guid("6f1f0fd3-1224-4bcf-9f7e-93694dc43071"), new Guid("5b2b9638-b8b9-4b62-8c31-39aefbcb8c50"), null },
                    { new Guid("2a52b6d0-7c1b-4e0c-91a4-11e5f8b4f9f1"), new Guid("7b1d6c2a-7d8f-4d6e-8071-71e483f3c4c6"), new Guid("5b2b9638-b8b9-4b62-8c31-39aefbcb8c50"), null },
                    { new Guid("a7f1b0d1-73bb-4f29-a9b0-1d2d9e36d4f7"), new Guid("7b1d6c2a-7d8f-4d6e-8071-71e483f3c4c6"), new Guid("5d6f9a8a-dfa0-4a1e-8844-0d9d41a76169"), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomizationAssignments_CustomizationItemId",
                table: "CustomizationAssignments",
                column: "CustomizationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomizationAssignments_MenuCategoryId",
                table: "CustomizationAssignments",
                column: "MenuCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomizationAssignments_MenuItemId",
                table: "CustomizationAssignments",
                column: "MenuItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomizationAssignments");
        }
    }
}
