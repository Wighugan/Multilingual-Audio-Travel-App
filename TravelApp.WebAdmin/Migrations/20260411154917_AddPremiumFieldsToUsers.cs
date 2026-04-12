using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelApp.WebAdmin.Migrations
{
    /// <inheritdoc />
    public partial class AddPremiumFieldsToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPremium",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PremiumExpiry",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PremiumToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPremium",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PremiumExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PremiumToken",
                table: "Users");
        }
    }
}
