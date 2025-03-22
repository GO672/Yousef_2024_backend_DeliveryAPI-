using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Delivery_API.Migrations
{
    /// <inheritdoc />
    public partial class Dish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "DishOrders",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rating",
                table: "DishOrders");
        }
    }
}

