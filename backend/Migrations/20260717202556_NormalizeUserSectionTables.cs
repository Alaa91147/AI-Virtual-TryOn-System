using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtualTryOn.Api.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeUserSectionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserDeliveryAddresses",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Street = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Building = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeliveryAddresses", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserDeliveryAddresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFitProfiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HeightCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    WeightKg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    PreferredSize = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BodyShape = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ShoeSize = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TopSize = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BottomSize = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFitProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserFitProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ProfilePhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTryOnPhotos",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullBodyPhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpperBodyPhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LowerBodyPhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FacePhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTryOnPhotos", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserTryOnPhotos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO [UserProfiles] ([UserId], [FullName], [Gender], [PhoneNumber], [ProfilePhotoUrl], [DateOfBirth])
                SELECT [Id], [FullName], [Gender], [PhoneNumber], [ProfilePhotoUrl], [DateOfBirth]
                FROM [Users];
                """);

            migrationBuilder.Sql("""
                INSERT INTO [UserFitProfiles] ([UserId], [HeightCm], [WeightKg], [PreferredSize], [BodyShape], [ShoeSize], [TopSize], [BottomSize])
                SELECT [Id], [HeightCm], [WeightKg], [PreferredSize], [BodyShape], [ShoeSize], [TopSize], [BottomSize]
                FROM [Users];
                """);

            migrationBuilder.Sql("""
                INSERT INTO [UserTryOnPhotos] ([UserId], [FullBodyPhotoUrl], [UpperBodyPhotoUrl], [LowerBodyPhotoUrl], [FacePhotoUrl])
                SELECT [Id], [FullBodyPhotoUrl], [UpperBodyPhotoUrl], [LowerBodyPhotoUrl], [FacePhotoUrl]
                FROM [Users];
                """);

            migrationBuilder.Sql("""
                INSERT INTO [UserDeliveryAddresses] ([UserId], [Country], [City], [Street], [Building], [PhoneNumber])
                SELECT [Id], [DeliveryCountry], [DeliveryCity], [DeliveryStreet], [DeliveryBuilding], [DeliveryPhoneNumber]
                FROM [Users];
                """);

            migrationBuilder.DropColumn(
                name: "BodyShape",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BottomSize",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Users");

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

            migrationBuilder.DropColumn(
                name: "FacePhotoUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FullBodyPhotoUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LowerBodyPhotoUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PreferredSize",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfilePhotoUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ShoeSize",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TopSize",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpperBodyPhotoUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BodyShape",
                table: "Users",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BottomSize",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "Users",
                type: "date",
                nullable: true);

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
                name: "FullName",
                table: "Users",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "HeightCm",
                table: "Users",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LowerBodyPhotoUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredSize",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShoeSize",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TopSize",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpperBodyPhotoUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKg",
                table: "Users",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE users
                SET
                    users.[FullName] = profiles.[FullName],
                    users.[Gender] = profiles.[Gender],
                    users.[PhoneNumber] = profiles.[PhoneNumber],
                    users.[ProfilePhotoUrl] = profiles.[ProfilePhotoUrl],
                    users.[DateOfBirth] = profiles.[DateOfBirth]
                FROM [Users] AS users
                INNER JOIN [UserProfiles] AS profiles ON profiles.[UserId] = users.[Id];
                """);

            migrationBuilder.Sql("""
                UPDATE users
                SET
                    users.[HeightCm] = fit.[HeightCm],
                    users.[WeightKg] = fit.[WeightKg],
                    users.[PreferredSize] = fit.[PreferredSize],
                    users.[BodyShape] = fit.[BodyShape],
                    users.[ShoeSize] = fit.[ShoeSize],
                    users.[TopSize] = fit.[TopSize],
                    users.[BottomSize] = fit.[BottomSize]
                FROM [Users] AS users
                INNER JOIN [UserFitProfiles] AS fit ON fit.[UserId] = users.[Id];
                """);

            migrationBuilder.Sql("""
                UPDATE users
                SET
                    users.[FullBodyPhotoUrl] = photos.[FullBodyPhotoUrl],
                    users.[UpperBodyPhotoUrl] = photos.[UpperBodyPhotoUrl],
                    users.[LowerBodyPhotoUrl] = photos.[LowerBodyPhotoUrl],
                    users.[FacePhotoUrl] = photos.[FacePhotoUrl]
                FROM [Users] AS users
                INNER JOIN [UserTryOnPhotos] AS photos ON photos.[UserId] = users.[Id];
                """);

            migrationBuilder.Sql("""
                UPDATE users
                SET
                    users.[DeliveryCountry] = address.[Country],
                    users.[DeliveryCity] = address.[City],
                    users.[DeliveryStreet] = address.[Street],
                    users.[DeliveryBuilding] = address.[Building],
                    users.[DeliveryPhoneNumber] = address.[PhoneNumber]
                FROM [Users] AS users
                INNER JOIN [UserDeliveryAddresses] AS address ON address.[UserId] = users.[Id];
                """);

            migrationBuilder.DropTable(
                name: "UserDeliveryAddresses");

            migrationBuilder.DropTable(
                name: "UserFitProfiles");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "UserTryOnPhotos");
        }
    }
}
