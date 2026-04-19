using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelApp.WebAdmin.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyVisitsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WeeklyVisitsJson",
                table: "Pois",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeeklyVisitsJson",
                table: "Pois");
        }
    }
}
