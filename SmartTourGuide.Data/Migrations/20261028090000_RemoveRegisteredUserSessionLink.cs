using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartTourGuide.Data.Context;

namespace SmartTourGuide.Data.Migrations;

[DbContext(typeof(SmartTourGuideDbContext))]
[Migration("20261028090000_RemoveRegisteredUserSessionLink")]
public sealed partial class RemoveRegisteredUserSessionLink : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_TourSessions_Identity",
            table: "TourSessions");

        migrationBuilder.DropColumn(
            name: "UserId",
            table: "TourSessions");

        migrationBuilder.AddCheckConstraint(
            name: "CK_TourSessions_Identity",
            table: "TourSessions",
            sql: "([IdentityType] = 1 AND [GuestIdentityId] IS NULL) OR ([IdentityType] = 2 AND [GuestIdentityId] IS NOT NULL)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "THROW 51000, 'This migration removes the deleted Users link and cannot restore UserId data. Restore a database backup instead.', 1;");
    }
}
