# How to run EFCore migrations

This shall provide context on how to work with entity frameworks multi provider migration feature.

Jellyfin supports multiple database providers, namely SqLite as its default and the experimental postgresSQL.

Each provider has its own set of migrations, as they contain provider specific instructions to migrate the specific changes to their respective systems.

When creating a new migration, you always have to create migrations for all providers. This is supported via the following syntax:

```cmd
dotnet ef migrations add MIGRATION_NAME --project "PATH_TO_PROJECT" -- --provider PROVIDER_KEY
```

with both sqlite and pgsql currently beeing supported and both need migrations, you need to run the efcore tool with the correct project to tell EfCore where to store the migrations and the correct provider key to tell jellyfin to load that provider.

The example is made from the root folder of the project e.g for codespaces `/workspaces/jellyfin`

```cmd
dotnet ef migrations add {MIGRATION_NAME} --project "Jellyfin.Database/Jellyfin.Database.Providers.SqLite" -- --migration-provider Jellyfin-SqLite
dotnet ef migrations add {MIGRATION_NAME} --project "Jellyfin.Database/Jellyfin.Database.Providers.PgSql" -- --migration-provider Jellyfin-PgSql
```

If you get the error: `Run "dotnet tool restore" to make the "dotnet-ef" command available.` Run `dotnet restore`.

in the event that you get the error: `System.UnauthorizedAccessException: Access to the path '/Jellyfin.Database' is denied.` you have to restore as sudo and then run `ef migrations` as sudo too.
