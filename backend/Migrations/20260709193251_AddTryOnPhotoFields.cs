using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtualTryOn.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTryOnPhotoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacePhotoUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullBodyPhotoUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LowerBodyPhotoUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpperBodyPhotoUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FacePhotoUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FullBodyPhotoUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LowerBodyPhotoUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpperBodyPhotoUrl",
                table: "Users");
        }
    }
}
