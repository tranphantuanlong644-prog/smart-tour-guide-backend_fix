using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTourGuide.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExcludeResetGuestSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ExcludedFromGuestTotal",
                table: "TourSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcludedFromGuestTotal",
                table: "TourSessions");
        }
    }
}
