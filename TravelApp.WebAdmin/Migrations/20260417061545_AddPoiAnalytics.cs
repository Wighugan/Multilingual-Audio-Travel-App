using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelApp.WebAdmin.Migrations
{
    /// <inheritdoc />
    public partial class AddPoiAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ListenCount",
                table: "Pois",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VisitCount",
                table: "Pois",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ListenCount",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "VisitCount",
                table: "Pois");
        }
    }
}
