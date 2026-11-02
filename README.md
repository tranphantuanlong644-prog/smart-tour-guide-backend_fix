# SmartTourGuide Backend

## Local development

The API targets .NET 10 and uses SQL Server. The checked-in connection string
uses Windows LocalDB; change it with user-secrets if your local database is
different.

The JWT signing key is intentionally not stored in this repository. Set a
private development key before starting the API:

~~~powershell
dotnet user-secrets set "Jwt:Key" "<replace-with-a-random-secret-of-at-least-32-characters>" --project SmartTourGuide.API
dotnet run --project SmartTourGuide.API
~~~

In Development, the API applies migrations and seeds the demo tour. The admin
page is available at http://localhost:5011/admin/.

## Database tables

The application schema keeps five business tables: `TourSessions`,
`TourQrCodes`, `GuestIdentities`, `TourLocations`, and
`TourLocationTranslations`. These tables have no foreign keys to removed
tables; their remaining foreign keys are only between the five retained
tables. The EF-managed `__EFMigrationsHistory` table also remains so EF Core
can track which schema changes have already run. Tour descriptions are stored
with QR records because the separate `Tours` and `TourTranslations` tables are
removed. User accounts are held in memory and are cleared when the API restarts
because there is no `Users` table. The migration removes the seven non-system
tables present in the current migration history that are outside the retained
five-table schema.

## Production configuration

Set JWT__Key and ConnectionStrings__DefaultConnection in the deployment
environment or secret manager. Use a randomly generated signing key with at
least 32 characters. Do not commit production keys or database credentials.
Apply database migrations through the deployment process; automatic migrations
and demo seeding run only in Development.

## Authentication

Registrations are held in memory because the `Users` table is intentionally
removed. Accounts are cleared whenever the API process restarts. The
development dashboard remains available locally; production administrator
accounts need a separate persistent identity provider before using protected
admin-management endpoints.
