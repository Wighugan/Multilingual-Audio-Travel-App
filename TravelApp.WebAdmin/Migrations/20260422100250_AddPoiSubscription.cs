using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelApp.WebAdmin.Migrations
{
    /// <inheritdoc />
    public partial class AddPoiSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionExpiry",
                table: "Pois",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionTier",
                table: "Pois",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubscriptionExpiry",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "SubscriptionTier",
                table: "Pois");
        }
    }
}
