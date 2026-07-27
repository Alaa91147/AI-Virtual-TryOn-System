using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtualTryOn.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCartItemColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItems_UserId_ProductSizeId",
                table: "CartItems");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductColorId",
                table: "CartItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductColorId",
                table: "CartItems",
                column: "ProductColorId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_UserId_ProductSizeId_ProductColorId",
                table: "CartItems",
                columns: new[] { "UserId", "ProductSizeId", "ProductColorId" },
                unique: true,
                filter: "[ProductColorId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_ProductColors_ProductColorId",
                table: "CartItems",
                column: "ProductColorId",
                principalTable: "ProductColors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_ProductColors_ProductColorId",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_ProductColorId",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_UserId_ProductSizeId_ProductColorId",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "ProductColorId",
                table: "CartItems");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_UserId_ProductSizeId",
                table: "CartItems",
                columns: new[] { "UserId", "ProductSizeId" },
                unique: true);
        }
    }
}
