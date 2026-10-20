using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTourGuide.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuestIdentities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PreferredLanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "vi"),
                    DeviceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastAccessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestIdentities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DefaultName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CenterLatitude = table.Column<double>(type: "float(9)", precision: 9, scale: 6, nullable: true),
                    CenterLongitude = table.Column<double>(type: "float(9)", precision: 9, scale: 6, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tours", x => x.Id);
                    table.CheckConstraint("CK_Tours_CenterLatitude", "[CenterLatitude] IS NULL OR ([CenterLatitude] >= -90 AND [CenterLatitude] <= 90)");
                    table.CheckConstraint("CK_Tours_CenterLongitude", "[CenterLongitude] IS NULL OR ([CenterLongitude] >= -180 AND [CenterLongitude] <= 180)");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PreferredLanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "vi"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TourLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DefaultName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Latitude = table.Column<double>(type: "float(9)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<double>(type: "float(9)", precision: 9, scale: 6, nullable: false),
                    TriggerRadiusMeters = table.Column<double>(type: "float(8)", precision: 8, scale: 2, nullable: false, defaultValue: 20.0),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourLocations", x => x.Id);
                    table.CheckConstraint("CK_TourLocations_DisplayOrder", "[DisplayOrder] > 0");
                    table.CheckConstraint("CK_TourLocations_Latitude", "[Latitude] >= -90 AND [Latitude] <= 90");
                    table.CheckConstraint("CK_TourLocations_Longitude", "[Longitude] >= -180 AND [Longitude] <= 180");
                    table.CheckConstraint("CK_TourLocations_TriggerRadius", "[TriggerRadiusMeters] > 0 AND [TriggerRadiusMeters] <= 500");
                    table.ForeignKey(
                        name: "FK_TourLocations_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourQrCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccessCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MaximumUsageCount = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeactivatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourQrCodes", x => x.Id);
                    table.CheckConstraint("CK_TourQrCodes_Expiration", "[ExpiresAt] > [CreatedAt]");
                    table.CheckConstraint("CK_TourQrCodes_MaximumUsageCount", "[MaximumUsageCount] IS NULL OR [MaximumUsageCount] > 0");
                    table.CheckConstraint("CK_TourQrCodes_SingleUse", "[Type] <> 2 OR [MaximumUsageCount] IS NULL OR [MaximumUsageCount] = 1");
                    table.CheckConstraint("CK_TourQrCodes_UsageCount", "[UsageCount] >= 0");
                    table.CheckConstraint("CK_TourQrCodes_UsageLimit", "[MaximumUsageCount] IS NULL OR [UsageCount] <= [MaximumUsageCount]");
                    table.ForeignKey(
                        name: "FK_TourQrCodes_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    WelcomeMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourTranslations_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourLocationTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    NarrationText = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    AudioUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DurationSeconds = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: false),
                    NarratorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourLocationTranslations", x => x.Id);
                    table.CheckConstraint("CK_TourLocationTranslations_DurationSeconds", "[DurationSeconds] > 0");
                    table.ForeignKey(
                        name: "FK_TourLocationTranslations_TourLocations_TourLocationId",
                        column: x => x.TourLocationId,
                        principalTable: "TourLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentityType = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GuestIdentityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceQrCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "vi"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourSessions", x => x.Id);
                    table.CheckConstraint("CK_TourSessions_EndTime", "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]");
                    table.CheckConstraint("CK_TourSessions_Expiration", "[ExpiresAt] > [StartedAt]");
                    table.CheckConstraint("CK_TourSessions_Identity", "([IdentityType] = 1 AND [UserId] IS NOT NULL AND [GuestIdentityId] IS NULL) OR ([IdentityType] = 2 AND [UserId] IS NULL AND [GuestIdentityId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_TourSessions_GuestIdentities_GuestIdentityId",
                        column: x => x.GuestIdentityId,
                        principalTable: "GuestIdentities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourSessions_TourQrCodes_SourceQrCodeId",
                        column: x => x.SourceQrCodeId,
                        principalTable: "TourQrCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourSessions_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AudioPlaybackProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourLocationTranslationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "vi"),
                    PositionSeconds = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: false, defaultValue: 0.0),
                    DurationSeconds = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: false),
                    CompletionPercentage = table.Column<double>(type: "float(5)", precision: 5, scale: 2, nullable: false, defaultValue: 0.0),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastListenedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioPlaybackProgresses", x => x.Id);
                    table.CheckConstraint("CK_AudioPlaybackProgresses_CompletedAt", "([IsCompleted] = 0 AND [CompletedAt] IS NULL) OR ([IsCompleted] = 1 AND [CompletedAt] IS NOT NULL)");
                    table.CheckConstraint("CK_AudioPlaybackProgresses_CompletionPercentage", "[CompletionPercentage] >= 0 AND [CompletionPercentage] <= 100");
                    table.CheckConstraint("CK_AudioPlaybackProgresses_DurationSeconds", "[DurationSeconds] > 0");
                    table.CheckConstraint("CK_AudioPlaybackProgresses_PositionSeconds", "[PositionSeconds] >= 0 AND [PositionSeconds] <= [DurationSeconds]");
                    table.ForeignKey(
                        name: "FK_AudioPlaybackProgresses_TourLocationTranslations_TourLocationTranslationId",
                        column: x => x.TourLocationTranslationId,
                        principalTable: "TourLocationTranslations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AudioPlaybackProgresses_TourLocations_TourLocationId",
                        column: x => x.TourLocationId,
                        principalTable: "TourLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AudioPlaybackProgresses_TourSessions_TourSessionId",
                        column: x => x.TourSessionId,
                        principalTable: "TourSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocationDetectionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetectedTourLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Latitude = table.Column<double>(type: "float(9)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<double>(type: "float(9)", precision: 9, scale: 6, nullable: false),
                    AccuracyMeters = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: false),
                    DeviceRecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LocationDetected = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DistanceMeters = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: true),
                    WithinTriggerRadius = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AudioTriggerRequested = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationDetectionLogs", x => x.Id);
                    table.CheckConstraint("CK_LocationDetectionLogs_AccuracyMeters", "[AccuracyMeters] >= 0");
                    table.CheckConstraint("CK_LocationDetectionLogs_AudioTrigger", "[AudioTriggerRequested] = 0 OR ([LocationDetected] = 1 AND [WithinTriggerRadius] = 1)");
                    table.CheckConstraint("CK_LocationDetectionLogs_DetectedLocation", "([LocationDetected] = 0 AND [DetectedTourLocationId] IS NULL) OR ([LocationDetected] = 1 AND [DetectedTourLocationId] IS NOT NULL)");
                    table.CheckConstraint("CK_LocationDetectionLogs_DeviceRecordedAt", "[DeviceRecordedAt] <= DATEADD(MINUTE, 5, [ReceivedAt])");
                    table.CheckConstraint("CK_LocationDetectionLogs_DistanceMeters", "[DistanceMeters] IS NULL OR [DistanceMeters] >= 0");
                    table.CheckConstraint("CK_LocationDetectionLogs_Latitude", "[Latitude] >= -90 AND [Latitude] <= 90");
                    table.CheckConstraint("CK_LocationDetectionLogs_Longitude", "[Longitude] >= -180 AND [Longitude] <= 180");
                    table.CheckConstraint("CK_LocationDetectionLogs_TriggerRadius", "[WithinTriggerRadius] = 0 OR [LocationDetected] = 1");
                    table.ForeignKey(
                        name: "FK_LocationDetectionLogs_TourLocations_DetectedTourLocationId",
                        column: x => x.DetectedTourLocationId,
                        principalTable: "TourLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LocationDetectionLogs_TourSessions_TourSessionId",
                        column: x => x.TourSessionId,
                        principalTable: "TourSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourVisits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetectionMethod = table.Column<int>(type: "int", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "vi"),
                    VisitedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DistanceMeters = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: true),
                    AccuracyMeters = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: true),
                    DetectedLatitude = table.Column<double>(type: "float(9)", precision: 9, scale: 6, nullable: true),
                    DetectedLongitude = table.Column<double>(type: "float(9)", precision: 9, scale: 6, nullable: true),
                    AudioTriggered = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AudioTriggeredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourVisits", x => x.Id);
                    table.CheckConstraint("CK_TourVisits_AccuracyMeters", "[AccuracyMeters] IS NULL OR [AccuracyMeters] >= 0");
                    table.CheckConstraint("CK_TourVisits_AudioTriggeredAt", "([AudioTriggered] = 0 AND [AudioTriggeredAt] IS NULL) OR ([AudioTriggered] = 1 AND [AudioTriggeredAt] IS NOT NULL)");
                    table.CheckConstraint("CK_TourVisits_DetectedLatitude", "[DetectedLatitude] IS NULL OR ([DetectedLatitude] >= -90 AND [DetectedLatitude] <= 90)");
                    table.CheckConstraint("CK_TourVisits_DetectedLongitude", "[DetectedLongitude] IS NULL OR ([DetectedLongitude] >= -180 AND [DetectedLongitude] <= 180)");
                    table.CheckConstraint("CK_TourVisits_DistanceMeters", "[DistanceMeters] IS NULL OR [DistanceMeters] >= 0");
                    table.ForeignKey(
                        name: "FK_TourVisits_TourLocations_TourLocationId",
                        column: x => x.TourLocationId,
                        principalTable: "TourLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourVisits_TourSessions_TourSessionId",
                        column: x => x.TourSessionId,
                        principalTable: "TourSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AudioPlaybackProgresses_Location_Language",
                table: "AudioPlaybackProgresses",
                columns: new[] { "TourLocationId", "LanguageCode" });

            migrationBuilder.CreateIndex(
                name: "IX_AudioPlaybackProgresses_Session_LastListenedAt",
                table: "AudioPlaybackProgresses",
                columns: new[] { "TourSessionId", "LastListenedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AudioPlaybackProgresses_TourLocationTranslationId",
                table: "AudioPlaybackProgresses",
                column: "TourLocationTranslationId");

            migrationBuilder.CreateIndex(
                name: "UX_AudioPlaybackProgresses_Session_Translation",
                table: "AudioPlaybackProgresses",
                columns: new[] { "TourSessionId", "TourLocationTranslationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestIdentities_DeviceId",
                table: "GuestIdentities",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestIdentities_ExpiresAt",
                table: "GuestIdentities",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_LocationDetectionLogs_Location_ReceivedAt",
                table: "LocationDetectionLogs",
                columns: new[] { "DetectedTourLocationId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LocationDetectionLogs_Session_Detected_ReceivedAt",
                table: "LocationDetectionLogs",
                columns: new[] { "TourSessionId", "LocationDetected", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LocationDetectionLogs_Session_ReceivedAt",
                table: "LocationDetectionLogs",
                columns: new[] { "TourSessionId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TourLocations_IsActive",
                table: "TourLocations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TourLocations_Latitude_Longitude",
                table: "TourLocations",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "UX_TourLocations_TourId_Code",
                table: "TourLocations",
                columns: new[] { "TourId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_TourLocations_TourId_DisplayOrder",
                table: "TourLocations",
                columns: new[] { "TourId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourLocationTranslations_LanguageCode",
                table: "TourLocationTranslations",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "UX_TourLocationTranslations_TourLocationId_LanguageCode",
                table: "TourLocationTranslations",
                columns: new[] { "TourLocationId", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourQrCodes_ExpiresAt",
                table: "TourQrCodes",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TourQrCodes_TourId_IsActive",
                table: "TourQrCodes",
                columns: new[] { "TourId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UX_TourQrCodes_AccessCode",
                table: "TourQrCodes",
                column: "AccessCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tours_IsActive",
                table: "Tours",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "UX_Tours_Code",
                table: "Tours",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourSessions_ExpiresAt",
                table: "TourSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TourSessions_GuestIdentityId_Status",
                table: "TourSessions",
                columns: new[] { "GuestIdentityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TourSessions_SourceQrCodeId",
                table: "TourSessions",
                column: "SourceQrCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_TourSessions_TourId_Status",
                table: "TourSessions",
                columns: new[] { "TourId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TourSessions_UserId_Status",
                table: "TourSessions",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TourTranslations_LanguageCode",
                table: "TourTranslations",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "UX_TourTranslations_TourId_LanguageCode",
                table: "TourTranslations",
                columns: new[] { "TourId", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourVisits_Session_Location_VisitedAt",
                table: "TourVisits",
                columns: new[] { "TourSessionId", "TourLocationId", "VisitedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TourVisits_TourLocationId_VisitedAt",
                table: "TourVisits",
                columns: new[] { "TourLocationId", "VisitedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TourVisits_TourSessionId_VisitedAt",
                table: "TourVisits",
                columns: new[] { "TourSessionId", "VisitedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AudioPlaybackProgresses");

            migrationBuilder.DropTable(
                name: "LocationDetectionLogs");

            migrationBuilder.DropTable(
                name: "TourTranslations");

            migrationBuilder.DropTable(
                name: "TourVisits");

            migrationBuilder.DropTable(
                name: "TourLocationTranslations");

            migrationBuilder.DropTable(
                name: "TourSessions");

            migrationBuilder.DropTable(
                name: "TourLocations");

            migrationBuilder.DropTable(
                name: "GuestIdentities");

            migrationBuilder.DropTable(
                name: "TourQrCodes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Tours");
        }
    }
}
