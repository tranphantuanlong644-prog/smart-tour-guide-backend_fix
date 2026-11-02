using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartTourGuide.Data.Context;

namespace SmartTourGuide.Data.Migrations;

[DbContext(typeof(SmartTourGuideDbContext))]
[Migration("20261028000000_KeepOnlyFiveBusinessTables")]
public sealed partial class KeepOnlyFiveBusinessTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "TourCode", table: "TourQrCodes", type: "nvarchar(50)",
            maxLength: 50, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(
            name: "TourName", table: "TourQrCodes", type: "nvarchar(200)",
            maxLength: 200, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(
            name: "TourDescription", table: "TourQrCodes", type: "nvarchar(4000)",
            maxLength: 4000, nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "WelcomeMessage", table: "TourQrCodes", type: "nvarchar(2000)",
            maxLength: 2000, nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "CoverImageUrl", table: "TourQrCodes", type: "nvarchar(2000)",
            maxLength: 2000, nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "Address", table: "TourQrCodes", type: "nvarchar(500)",
            maxLength: 500, nullable: true);

        migrationBuilder.Sql("""
            EXEC sys.sp_executesql N'
            UPDATE qr
            SET qr.TourCode = tour.Code,
                qr.TourName = tour.DefaultName,
                qr.TourDescription = translation.Description,
                qr.WelcomeMessage = translation.WelcomeMessage,
                qr.CoverImageUrl = tour.CoverImageUrl,
                qr.Address = tour.Address
            FROM TourQrCodes AS qr
            INNER JOIN Tours AS tour ON tour.Id = qr.TourId
            OUTER APPLY (
                SELECT TOP (1) t.Description, t.WelcomeMessage
                FROM TourTranslations AS t
                WHERE t.TourId = tour.Id
                ORDER BY CASE WHEN t.LanguageCode = N''vi'' THEN 0 ELSE 1 END
            ) AS translation;
            ';
            """);

        migrationBuilder.DropTable(name: "AudioPlaybackProgresses");
        migrationBuilder.DropTable(name: "LocationDetectionLogs");
        migrationBuilder.DropTable(name: "TourVisits");
        migrationBuilder.DropTable(name: "DashboardStatisticsResets");

        migrationBuilder.DropForeignKey(
            name: "FK_TourLocations_Tours_TourId", table: "TourLocations");
        migrationBuilder.DropForeignKey(
            name: "FK_TourQrCodes_Tours_TourId", table: "TourQrCodes");
        migrationBuilder.DropForeignKey(
            name: "FK_TourSessions_Tours_TourId", table: "TourSessions");
        migrationBuilder.DropForeignKey(
            name: "FK_TourSessions_Users_UserId", table: "TourSessions");

        migrationBuilder.DropTable(name: "TourTranslations");
        migrationBuilder.DropTable(name: "Users");
        migrationBuilder.DropTable(name: "Tours");

        migrationBuilder.DropIndex(
            name: "IX_TourSessions_UserId_Status", table: "TourSessions");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("THROW 51000, 'This migration deletes business tables and cannot restore their data. Restore a database backup instead.', 1;");
    }
}
