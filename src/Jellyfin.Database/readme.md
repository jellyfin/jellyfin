# How to run EFCore migrations

This shall provide context on how to work with entity frameworks multi provider migration feature.

Jellyfin will support multiple database providers in the future, namely SQLite as its default and the experimental PostgreSQL.

Each provider has its own set of migrations, as they contain provider specific instructions to migrate the specific changes to their respective systems.

When creating a new migration, you always have to create migrations for all providers. This is supported via the following syntax:

```cmd
dotnet ef migrations add MIGRATION_NAME --project "PATH_TO_PROJECT" -- --provider PROVIDER_KEY
```

with SQLite currently being the only supported provider, you need to run the Entity Framework tool with the correct project to tell EFCore where to store the migrations and the correct provider key to tell Jellyfin to load that provider.

The example is made from the root folder of the project e.g for codespaces `/workspaces/jellyfin`

```cmd
dotnet ef migrations add {MIGRATION_NAME} --project "src/Jellyfin.Database/Jellyfin.Database.Providers.Sqlite" -- --migration-provider Jellyfin-SQLite
```

If you get the error: `Run "dotnet tool restore" to make the "dotnet-ef" command available.` Run `dotnet restore`.

in the event that you get the error: `System.UnauthorizedAccessException: Access to the path '/src/Jellyfin.Database' is denied.` you have to restore as sudo and then run `ef migrations` as sudo too.
