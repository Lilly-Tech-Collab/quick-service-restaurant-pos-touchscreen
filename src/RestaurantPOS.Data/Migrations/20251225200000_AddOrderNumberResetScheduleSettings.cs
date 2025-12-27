using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNumberResetScheduleSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Id", "Key", "Value" },
                values: new object[,]
                {
                    { new Guid("79b1fb3d-7b88-4f69-b6aa-636a4f60da46"), "BusinessDayStartHour", "0" },
                    { new Guid("3e2803f4-2b1f-49be-9c85-79c98c00c010"), "DaypartBreakfastStartHour", "6" },
                    { new Guid("45e88ef3-7fb2-4a62-88f9-e4c2475ba499"), "DaypartLunchStartHour", "11" },
                    { new Guid("5c0b5501-1b20-4ef9-84a5-2f9c8746d0d9"), "DaypartDinnerStartHour", "16" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: new Guid("79b1fb3d-7b88-4f69-b6aa-636a4f60da46"));

            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: new Guid("3e2803f4-2b1f-49be-9c85-79c98c00c010"));

            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: new Guid("45e88ef3-7fb2-4a62-88f9-e4c2475ba499"));

            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: new Guid("5c0b5501-1b20-4ef9-84a5-2f9c8746d0d9"));
        }
    }
}
