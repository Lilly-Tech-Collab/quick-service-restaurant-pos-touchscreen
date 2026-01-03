using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantPOS.Data;

#nullable disable

namespace RestaurantPOS.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260103120000_AddOrderItemCreatedAt")]
    partial class AddOrderItemCreatedAt
    {
    }
}
