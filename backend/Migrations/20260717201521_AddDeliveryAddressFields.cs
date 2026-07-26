using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtualTryOn.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryAddressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryBuilding",
                table: "Users",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryCity",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryCountry",
                table: "Users",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryPhoneNumber",
                table: "Users",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryStreet",
                table: "Users",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryBuilding",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeliveryCity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeliveryCountry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeliveryPhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeliveryStreet",
                table: "Users");
        }
    }
}
