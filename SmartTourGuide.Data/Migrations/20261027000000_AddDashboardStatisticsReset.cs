using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTourGuide.Data.Migrations;

public partial class AddDashboardStatisticsReset : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DashboardStatisticsResets",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                ResetAt = table.Column<DateTimeOffset>(
                    type: "datetimeoffset",
                    nullable: false),
                ScanCountAtReset = table.Column<int>(
                    type: "int",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_DashboardStatisticsResets",
                    x => x.Id);
                table.CheckConstraint(
                    "CK_DashboardStatisticsResets_Singleton",
                    "[Id] = 1");
                table.CheckConstraint(
                    "CK_DashboardStatisticsResets_ScanCount",
                    "[ScanCountAtReset] >= 0");
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DashboardStatisticsResets");
    }
}
