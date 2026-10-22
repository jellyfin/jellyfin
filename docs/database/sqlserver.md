<!-- Rendered from docs/database/templates/how-to.md by docs/database/render. Edit the template, not this file. -->
# Running Jellyfin on SQL Server

This page belongs to the multi-database fork of Jellyfin (`feature/multi-db`). Upstream Jellyfin stores its
data in SQLite only; the fork adds PostgreSQL, MySQL, MariaDB and SQL Server providers. The page covers a new installation.
Moving an existing library from SQLite to another engine is not covered or tested here.

> [!WARNING]
> **The SQL Server database provider is experimental.** Its CI jobs are not required checks, and it may be
> withdrawn ([ADR-0011](../adr/0011-supported-database-targets.md)). Jellyfin logs this at every start:
> "The SQL Server database provider is experimental. Back up the data before using it with a library you cannot rebuild."
> Use it only with a library you can rebuild, and keep backups of the database.

## Versions

| | |
| --- | --- |
| Oldest supported version | SQL Server 2022 (16.0) |
| Tested in CI | `run-tests-db / SqlServer` on SQL Server 2022 CU27 GDR1, not required, and `run-tests-db / SqlServer newest` on SQL Server 2025 CU9 GDR1, advisory; both on every pull request, every night and on demand, where the nightly and manual runs start once the workflow is on the default branch |
| Image of the tests and of the Compose example | `mcr.microsoft.com/mssql/server:2022-CU27-GDR1-ubuntu-22.04` |
| Checked at startup | Yes. When a connection opens, Jellyfin refuses a server below major version 16 ("SQL Server ... is not supported. Jellyfin needs SQL Server 2022 (16.0) or later.") and refuses Azure SQL Database, Azure SQL Managed Instance and Azure Synapse ("Azure SQL Database, Azure SQL Managed Instance and Azure Synapse are not supported: Jellyfin is not tested on them. Use SQL Server 2022 (16.0) or later."). |

SQL Server 2022 (`16.0`) is the oldest supported release (EF Core generates SQL for compatibility level 160); SQL
Server 2025 (`17.0`), GA with mainstream support to 2031, runs as an advisory CI job.

Azure SQL Database, Azure SQL Managed Instance and Azure Synapse are refused by engine edition
(`SERVERPROPERTY('EngineEdition')` editions 5, 6, 8 and 11): none is a target of
[ADR-0011](../adr/0011-supported-database-targets.md) or tested in CI, and version numbers alone would mislead
because Azure SQL Database reports version 12 regardless of what it runs and Managed Instance's reported version
depends on how it is updated ([ADR-0011](../adr/0011-supported-database-targets.md) F9).

The container image runs Developer edition (`MSSQL_PID=Developer`) by default, which is free for development and
testing only; production deployments need Express (`MSSQL_PID=Express`, capped at 10 GB per database on 2022 or
50 GB on 2025) or a licensed edition ([ADR-0011](../adr/0011-supported-database-targets.md)).

## 1. Create the database and the user

Jellyfin needs an empty database and a user of its own for it. It creates the tables itself on its first start.

```sql
CREATE DATABASE jellyfin COLLATE Latin1_General_100_BIN2;
ALTER DATABASE jellyfin SET READ_COMMITTED_SNAPSHOT ON;
CREATE LOGIN jellyfin WITH PASSWORD = 'CHANGE-ME';
USE jellyfin;
CREATE USER jellyfin FOR LOGIN jellyfin;
ALTER ROLE db_owner ADD MEMBER jellyfin;
```

- **Collation (`Latin1_General_100_BIN2`).** Every text column and `__EFMigrationsHistory`
  (`BinaryCollationHistoryRepository`) are given `Latin1_General_100_BIN2` explicitly. Creating the database with
  `COLLATE Latin1_General_100_BIN2` also sets the database default; if the database has another default collation
  (such as `SQL_Latin1_General_CP1_CI_AS`), Jellyfin logs a warning at startup and continues.
- **Row versioning (`READ_COMMITTED_SNAPSHOT ON`).** Without it, `READ COMMITTED` reads take shared locks and
  wait behind the writes of a library scan. If `READ_COMMITTED_SNAPSHOT` is `OFF`, Jellyfin logs a warning at
  startup naming `ALTER DATABASE [jellyfin] SET READ_COMMITTED_SNAPSHOT ON` (which needs exclusive access while
  Jellyfin is stopped) and continues.
- **Privileges (`db_owner`).** Migrations create, alter and drop tables and indexes, scheduled maintenance runs
  `UPDATE STATISTICS`, and backup restore switches foreign keys off and on, enables `IDENTITY_INSERT`, and runs
  `DBCC CHECKIDENT (..., RESEED, 0)` (which requires `db_owner` in `jellyfin` and nothing outside it).
- **Session settings.** On every connection open Jellyfin runs `SET TRANSACTION ISOLATION LEVEL READ COMMITTED`,
  so statements outside a transaction do not inherit a stricter isolation level left on a pooled connection;
  SqlClient starts every unnamed transaction at `READ COMMITTED`
  ([ADR-0008](../adr/0008-concurrency-locking-and-retry.md) S7).
- **Index key limit (1700 bytes) and item paths over about 850 characters ([ADR-0012](../adr/0012-sqlserver-text-index-keys.md) decision 7).**
  Text columns that participate in an index are `nvarchar(4000)`, and 18 nonclustered indexes in the baseline
  migration have declared key widths above SQL Server's 1700-byte nonclustered index key limit (SQL Server emits
  warning Msg 1945 when creating them, which is expected). At runtime, SQL Server refuses any row whose actual
  index key exceeds 1700 bytes with error 1946 (`ADR-0009/G2`). Because `nvarchar` uses UTF-16 (2 bytes per code
  unit, 4 bytes per supplementary character outside the Basic Multilingual Plane such as emoji), a single-column
  index like `IX_BaseItems_Path` holds at most **850 characters** (or **425 supplementary characters such as emoji**),
  and composite indexes over `SortName`, `PresentationUniqueKey`, `SeriesPresentationUniqueKey`, `CleanName`,
  `CleanValue`, `ProviderValue` or `Language` leave 581 to 848 code units for the text column(s). **An item whose
  path is longer than about 850 characters (or 425 characters outside the Basic Multilingual Plane) is not saved
  on SQL Server** (and a path over 4000 characters is refused at the `nvarchar(4000)` column with error 2628).
  Without per-item fallback (step X11), a folder scan saves all new children in one transaction, so one over-long
  item rolls back the whole batch, stops the scan before the folder's subfolders are validated, and keeps the
  folder from gaining items on later scans; with step X11 (`SaveItemsSkippingFailures`), Jellyfin retries the batch
  one unit at a time, logs and skips only the over-long item, and saves the rest of the folder so its subfolders are
  still scanned—except when a **folder's own path** exceeds the limit (or the first 3 per-item saves fail alike), in
  which case that folder's subtree is lost while sibling folders are unaffected.
- **Trailing spaces and UTF-16 order ([ADR-0006](../adr/0006-string-collation-and-null-ordering.md) G1 and G2).**
  SQL Server ignores trailing spaces in `=` and `UNIQUE` index checks even under `Latin1_General_100_BIN2`
  (`'Rock'` and `'Rock '` collide, `ADR-0006/G1`), and orders strings by 16-bit UTF-16 code units rather than
  Unicode code points so supplementary characters (`U+10000` and above) sort before BMP characters at
  `U+E000..U+FFFF` (`ADR-0006/G2`).
- **Filtered indexes and cascade paths ([ADR-0001](../adr/0001-fork-owned-multi-provider-architecture.md) M3, M5, M6).**
  `IX_BaseItems_PrimaryVersionId` is unfiltered because SQL Server rejects `OR` in a filtered index;
  `IX_ImageInfos_UserId` is filtered to `[UserId] IS NOT NULL` so multiple rows may have a `NULL` user id; and the
  self-referencing foreign keys `FK_BaseItems_BaseItems_ParentId` and `FK_AncestorIds_BaseItems_ParentItemId` use
  `ON DELETE NO ACTION` to avoid error 1785 (multiple cascade paths), with Jellyfin clearing or deleting those
  rows before deleting a folder.

## 2. Write `database.xml`

`database.xml` in Jellyfin's configuration directory selects the provider. The configuration directory is the one
given by `--configdir` or `JELLYFIN_CONFIG_DIR`; in the Jellyfin Docker image it is `/config/config`. Write the
file before the first start: a server that starts without it writes a SQLite configuration and uses SQLite.

```xml
<?xml version="1.0" encoding="utf-8"?>
<DatabaseConfigurationOptions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <DatabaseType>Jellyfin-SqlServer</DatabaseType>
  <LockingBehavior>NoLock</LockingBehavior>
  <CustomProviderOptions>
    <PluginName />
    <PluginAssembly />
    <ConnectionString>Server=db.example.com,1433;Database=jellyfin;User ID=jellyfin;Password=CHANGE-ME;Encrypt=True</ConnectionString>
    <Options>
      <CustomDatabaseOption>
        <Key>command-timeout</Key>
        <Value>60</Value>
      </CustomDatabaseOption>
    </Options>
  </CustomProviderOptions>
</DatabaseConfigurationOptions>
```

| Element | Value |
| --- | --- |
| `DatabaseType` | `Jellyfin-SqlServer`, compared without regard to case. An unknown name stops the server with the list of supported names. |
| `LockingBehavior` | `NoLock`, the default, or `Optimistic`, which retries in Jellyfin itself and only SQLite needs. `Pessimistic` stops the server at startup: only SQLite supports it. |
| `CustomProviderOptions/ConnectionString` | A [Microsoft.Data.SqlClient connection string](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlconnection.connectionstring). It can instead come from the environment, see below. |
| `CustomProviderOptions/PluginName`, `PluginAssembly` | Empty. They are only read for `PLUGIN_PROVIDER`, a provider loaded from a plugin. |
| `CustomProviderOptions/Options` | Optional settings, each a `CustomDatabaseOption` with a `Key` and a `Value`. Keys are compared without regard to case. |

The options:

| Key | Value |
| --- | --- |
| `command-timeout` | The time a command may take, in seconds. Without it a command may take 60 seconds, unless the connection string sets a command timeout. |
| `max-pool-size` | The largest number of pooled connections. Without it the driver's default applies. |
| `EnableSensitiveDataLogging` | `true` writes the values of query parameters to the log. For debugging only: the log then holds the data of your users. |

### The connection string from the environment

The environment variable `JELLYFIN_DATABASE__CONNECTIONSTRING` (the configuration key `database:ConnectionString`)
replaces `CustomProviderOptions/ConnectionString` when it is set and not blank. `DatabaseType` and the options still
come from `database.xml`, and the variable is never written to it. Jellyfin logs "The database connection string is
taken from the configuration key database:ConnectionString instead of database.xml", and shows the variable as
`[redacted]` where it logs its environment.

Prefer the variable: it keeps the password out of `database.xml` and off disk.

### What Jellyfin sets on the connection

`Encrypt=True` (Mandatory) when the connection string does not name `Encrypt`; any `Encrypt` value you write
(`False`/`Optional`, `True`/`Mandatory`, `Strict`) and `TrustServerCertificate` are kept as written
([ADR-0011](../adr/0011-supported-database-targets.md) F10). For a container or a server with a self-signed
certificate, add `TrustServerCertificate=True` (or `Encrypt=False` on the same machine). `Command Timeout=60`
unless the connection string or the `command-timeout` option sets another.

## 3. First start

On its first start Jellyfin applies every migration to the empty database, beginning with the baseline
`20200101000000_InitialCreate`, then runs as usual. `/health` answers `Healthy` once the server is up.

The log shows which provider runs:

```text
The SQL Server database provider is experimental. Back up the data before using it with a library you cannot rebuild.
SQL Server connection string: Data Source=db.example.com,1433;Initial Catalog=jellyfin;User ID=jellyfin;Password=;Command Timeout=60;Encrypt=True
```

The password is removed from the connection string in the log.

## 4. Back up and restore

A backup of a Jellyfin server on SQL Server has two parts: the database, and Jellyfin's own directories
(configuration, data, metadata and cache). Back up both, at the same time.

### The database: a dump

A dump made with `BACKUP DATABASE` is the backup of the database, and the only way back from a failed upgrade (see
below).

```sh
sqlcmd -S db.example.com,1433 -U jellyfin -C -b -Q \
  "BACKUP DATABASE [jellyfin] TO DISK = N'/var/opt/mssql/data/jellyfin.bak' WITH FORMAT, INIT"
```

To restore (with Jellyfin stopped, using a login that may restore databases such as `sa`):

```sh
sqlcmd -S db.example.com,1433 -U sa -C -b -Q \
  "ALTER DATABASE [jellyfin] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [jellyfin] FROM DISK = N'/var/opt/mssql/data/jellyfin.bak' WITH REPLACE; ALTER DATABASE [jellyfin] SET MULTI_USER;"
```

Stop Jellyfin before you restore, and start the version of Jellyfin that wrote the dump.

### Jellyfin's backup archive

Jellyfin's own backup (Dashboard, Backups) writes an archive with the rows of every table, read through Entity
Framework, plus the configuration and, as chosen, metadata, trickplay images and subtitles. It works on every
engine. Know its limits:

- **Home screen sections.** The archive carries the home screen sections of the users (the `HomeSection` rows),
  and a restore puts them back, on every engine, from item X10 of the [plan](../adr/pr-plan.md) on. Earlier builds
  left them out of the archive, so their restores did not give the sections back. An archive made by an earlier
  build has no sections, and a restore of it gives none back. The fork's CI at that change (`939887782c`), as
  relayed, passed every job, including the ABI difference check, which is consistent with the new public
  `DbSet<HomeSection>` being an addition only; PostgreSQL is CI's only database job there.
- **The archive leaves `database.xml` alone.** The archive no longer contains `database.xml`, and a restore never
  changes it. To move a server, configure the target first, then restore. Archives made before this change still
  hold `database.xml`, so treat them as secrets.
- **A restore rolls back as a whole if a row exceeds SQL Server's limits.** Restoring an archive runs in one
  transaction. If the archive contains a row that SQL Server refuses (such as an item from another engine whose
  `Path` exceeds 850 characters, error 1946), the restore fails and rolls back completely, leaving the database
  with the rows it held before ([ADR-0012](../adr/0012-sqlserver-text-index-keys.md) T7).
- **It does not repair a schema.** After a failed upgrade, restore the dump instead.

## 5. Upgrade

### Jellyfin

Take a dump before you upgrade Jellyfin. On SQLite Jellyfin copies the database file before it migrates; on
SQL Server there is nothing it can copy. Before it migrates it logs "No backup of the Jellyfin database was taken,
as its provider has nothing to snapshot", and on a database that already has migrations the provider warns:

> No backup of the SQL Server database is taken before migrating. A migration that fails is rolled back with its
> transaction, but back the database up with BACKUP DATABASE before upgrading, it is the only way to return to the
> previous version.

Each migration runs in a transaction of its own (DDL is transactional on SQL Server); a migration that fails is
rolled back with its transaction while earlier migrations of the same upgrade stay applied, and Jellyfin logs the
last migration that applied completely.

### The database server

Any version from SQL Server 2022 (`16.0`) on is supported; upgrade in place or with `BACKUP DATABASE` and
`RESTORE DATABASE`. CI also runs SQL Server 2025 (`17.0`) as an advisory job.

## Docker Compose

[`compose/sqlserver/compose.yaml`](compose/sqlserver/compose.yaml) runs Jellyfin and SQL Server (`mcr.microsoft.com/mssql/server:2022-CU27-GDR1-ubuntu-22.04`) side by
side. Upstream Jellyfin images have no SQL Server provider, so the Compose file builds the Jellyfin image from this
checkout with [`compose/Dockerfile`](compose/Dockerfile): an official Jellyfin image, for its web client and
FFmpeg, with the server of this fork in place of its own.

```sh
cd docs/database/compose/sqlserver
cp env.example .env               # then set JELLYFIN_DB_PASSWORD and JELLYFIN_MEDIA_DIR in .env
docker compose up -d --build
```

It needs Docker Compose 2.23.1 or later. What it sets up:

- **The database.** The SQL Server image starts `sqlservr` with `ACCEPT_EULA=Y`, `MSSQL_PID` (`Developer` by default)
  and `MSSQL_SA_PASSWORD` set from `JELLYFIN_DB_PASSWORD`. Unlike the PostgreSQL, MySQL and MariaDB images, the SQL
  Server image does not create an application database or user by itself: the container's health check connects as
  `sa` once `sqlservr` is up and, if the `jellyfin` login does not exist yet, creates the `jellyfin` database with
  `COLLATE Latin1_General_100_BIN2` and `READ_COMMITTED_SNAPSHOT ON` and the `jellyfin` login and `db_owner` user
  with `JELLYFIN_DB_PASSWORD`, before reporting healthy.
- **`database.xml`.** It is written inline in the Compose file and mounted at `/run/jellyfin/database.xml`. On the
  first start the image copies it to `/config/config/database.xml`, where it stays an ordinary file, as a restore of
  a backup archive rewrites it. To change it later, edit that copy in the `config` volume.
- **The connection string.** `JELLYFIN_DATABASE__CONNECTIONSTRING` carries it, with the password from `.env`, so
  the password is in neither `database.xml` nor a backup archive.
- **Health checks.** Jellyfin starts once the database answers over TCP; the image's own setup server does not.
  Jellyfin is healthy once `/health` answers `Healthy`: while it starts it answers `Degraded`, and once it runs the
  answer includes a check of the database.
- **Volumes.** `database` holds the database, `config` and `cache` hold Jellyfin's directories (`/config` and
  `/cache`); the media folder is mounted read-only at `/media`.

Check that it runs on SQL Server, and make a dump from the database container:

```sh
docker compose logs jellyfin | grep "connection string"
curl -fsS http://localhost:8096/health
docker compose exec -T sqlserver sh -c 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S 127.0.0.1 -U jellyfin -C -b -Q \
  "BACKUP DATABASE [jellyfin] TO DISK = N'\''/var/opt/mssql/data/jellyfin.bak'\'' WITH FORMAT, INIT"'
```

`-T` keeps a terminal out of the way of the dump. `docker compose down` removes the containers and keeps the
volumes; `docker compose down -v` also deletes the volumes, and with them the database.

## Running the tests on SQL Server

For developers: the database tests start a `mcr.microsoft.com/mssql/server:2022-CU27-GDR1-ubuntu-22.04` container through Testcontainers when they are run with

```sh
dotnet test tests/Jellyfin.Server.Implementations.Tests --settings tests/sqlserver.runsettings
```

or with `JELLYFIN_TEST_DB_PROVIDER=SqlServer`. CI runs `run-tests-db / SqlServer` on SQL Server 2022 CU27 GDR1, not required, and `run-tests-db / SqlServer newest` on SQL Server 2025 CU9 GDR1, advisory; both on every pull request, every night and on demand, where the nightly and manual runs start once the workflow is on the default branch.

## Where this comes from

- The provider: `src/Jellyfin.Database/Jellyfin.Database.Providers.SqlServer/SqlServerDatabaseProvider.cs`.
- The selection of the provider and the connection string from the environment:
  `Jellyfin.Server.Implementations/Extensions/ServiceCollectionExtensions.cs`.
- Supported engines, tiers and oldest versions: [ADR-0011](../adr/0011-supported-database-targets.md).
- The CI jobs: [ADR-0010](../adr/0010-ci-gating-for-database-provider-tests.md), `.github/workflows/ci-tests-db.yml`.
