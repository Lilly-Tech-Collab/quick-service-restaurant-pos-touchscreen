using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNumberResetSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Id", "Key", "Value" },
                values: new object[] { new Guid("0b5c308e-9429-4db3-9a1b-0a2d5e6a0f39"), "OrderNumberResetMode", "Daily" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: new Guid("0b5c308e-9429-4db3-9a1b-0a2d5e6a0f39"));
        }
    }
}
