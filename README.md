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

## Production configuration

Set JWT__Key and ConnectionStrings__DefaultConnection in the deployment
environment or secret manager. Use a randomly generated signing key with at
least 32 characters. Do not commit production keys or database credentials.
Apply database migrations through the deployment process; automatic migrations
and demo seeding run only in Development.

## Administrator access

New registrations receive the Visitor role. To grant administration, first
register a dedicated account, then use a privileged SQL Server account to
promote that account:

~~~sql
UPDATE [dbo].[Users]
SET [Role] = 2
WHERE [NormalizedEmail] = UPPER('admin@example.com');
~~~

2 is the Administrator role. The /admin/ page signs in through the normal
login API, and every /api/admin endpoint requires a valid JWT with that role.
