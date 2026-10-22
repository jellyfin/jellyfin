# Running Jellyfin on {{engine}}

This page belongs to the multi-database fork of Jellyfin (`feature/multi-db`). Upstream Jellyfin stores its
data in SQLite only; the fork adds PostgreSQL, MySQL, MariaDB and SQL Server providers. The page covers a new installation.
Moving an existing library from SQLite to another engine is not covered or tested here.

{{#if mysql-family}}
> [!WARNING]
> **The MySQL and MariaDB provider is experimental.** It is built on a small third-party EF Core package
> (`Microting.EntityFrameworkCore.MySql`), it has known gaps, its CI jobs are not required checks, and it may be
> withdrawn ([ADR-0011](../adr/0011-supported-database-targets.md)). Jellyfin logs this at every start:
> "The MySQL and MariaDB database provider is experimental. Back up the data before using it with a library you
> cannot rebuild." Use it only with a library you can rebuild, and keep dumps of the database.

One provider serves both MySQL and MariaDB. This page is about {{engine}}; the [{{other-engine}} page]({{other-id}}.md)
differs only where the servers differ.
{{/if}}
{{#if postgresql}}
PostgreSQL is a **Supported** engine in this fork ([ADR-0011](../adr/0011-supported-database-targets.md)): its CI
job is a required check.
{{/if}}
{{#if sqlserver}}
> [!WARNING]
> **The SQL Server database provider is experimental.** Its CI jobs are not required checks, and it may be
> withdrawn ([ADR-0011](../adr/0011-supported-database-targets.md)). Jellyfin logs this at every start:
> "The SQL Server database provider is experimental. Back up the data before using it with a library you cannot rebuild."
> Use it only with a library you can rebuild, and keep backups of the database.
{{/if}}

## Versions

| | |
| --- | --- |
| Oldest supported version | {{engine}} {{floor}} |
| Tested in CI | {{ci-jobs}} |
| Image of the tests and of the Compose example | `{{image}}` |
{{#if postgresql}}
| Checked at startup | No. The provider generates SQL for PostgreSQL 16 and does not read the server version, so an older server is not refused, only untested. |
{{/if}}
{{#if mysql}}
| Checked at startup | Yes. Jellyfin refuses to start on an older server: "MySQL 8.4 or later is required, but the server is version ...". |
{{/if}}
{{#if mariadb}}
| Checked at startup | Yes. Jellyfin refuses to start on an older server: "MariaDB 11.4.7 or later is required, but the server is version ...". |
{{/if}}
{{#if sqlserver}}
| Checked at startup | Yes. When a connection opens, Jellyfin refuses a server below major version 16 ("SQL Server ... is not supported. Jellyfin needs SQL Server 2022 (16.0) or later.") and refuses Azure SQL Database, Azure SQL Managed Instance and Azure Synapse ("Azure SQL Database, Azure SQL Managed Instance and Azure Synapse are not supported: Jellyfin is not tested on them. Use SQL Server 2022 (16.0) or later."). |
{{/if}}

{{#if mysql}}
MySQL 8.4 is the oldest long-term release that is still maintained. MySQL 9.7, the newest long-term release, runs as
an advisory CI job. Innovation releases, such as 26.7, are not tested.
{{/if}}
{{#if mariadb}}
MariaDB 11.4.7 is the first 11.4 release with the `utf8mb4_0900_bin` collation that every Jellyfin text column
uses, as a collation that compares without padding: 11.4.4 does not have it, and in 11.4.5 it is an alias of the
padding `utf8mb4_bin`, under which `Rock` and `Rock ` collide ([ADR-0006](../adr/0006-string-collation-and-null-ordering.md) C4).
Because an older server would give wrong results without an error, the provider refuses it.
{{/if}}
{{#if sqlserver}}
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
{{/if}}

## 1. Create the database and the user

Jellyfin needs an empty database and a user of its own for it. It creates the tables itself on its first start.

{{#if postgresql}}
```sql
CREATE ROLE jellyfin LOGIN PASSWORD 'CHANGE-ME';
CREATE DATABASE jellyfin OWNER jellyfin ENCODING 'UTF8' TEMPLATE template0;
```

- **Owner.** The first migration creates the tables, and a `min(uuid)` aggregate with the function it is built
  on, in the schema it migrates (normally `public`). Since PostgreSQL 15 only the owner of the database may create
  objects in `public` by default. The aggregate needs neither a superuser nor an extension, and a dump of the
  database carries it.
- **Encoding.** Use `UTF8`, so that any text can be stored. Jellyfin does not check the encoding.
- **Collation.** Nothing is required of the database. The provider creates every text column as `text` with the
  collation `"C"`, which orders by byte, whatever the default collation of the database is.
- **Time zone.** Nothing is required of the server. Jellyfin stores dates as UTC and sets `Timezone=UTC` on every
  connection.
- **JIT.** On every start Jellyfin turns JIT compilation off for its role in its database
  (`ALTER ROLE ... IN DATABASE ... SET jit = off`), because it slows down Jellyfin's short queries, and logs
  that it did. If you set `jit` for the role yourself, Jellyfin leaves it as it is.
{{/if}}
{{#if mysql-family}}
```sql
CREATE DATABASE jellyfin CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_bin;
CREATE USER 'jellyfin'@'%' IDENTIFIED BY 'CHANGE-ME';
GRANT ALL PRIVILEGES ON jellyfin.* TO 'jellyfin'@'%';
```

- **Character set and collation.** Create the database with `utf8mb4` and `utf8mb4_0900_bin`, as the tests do.
  What decides how Jellyfin's text compares is the collation of the columns, and the provider gives every column
  that the server stores as text `utf8mb4_0900_bin` explicitly, whatever the default of the database is. That
  collation compares code points without padding and exists on both MySQL and MariaDB
  ([ADR-0006](../adr/0006-string-collation-and-null-ordering.md) C4 and C8). Jellyfin's first migration also makes
  `utf8mb4_0900_bin` the default collation of the database (`ALTER DATABASE COLLATE utf8mb4_0900_bin`), and every
  table, EF Core's history table `__EFMigrationsHistory` included, names it as its default. So a database created
  with the server's default for `utf8mb4`, {{server-default-collation}},
  ends up binary as well, and a database that Jellyfin creates itself, because it does not exist yet, is created
  with `utf8mb4_0900_bin`.
- **Fixed: one model column, the history table and the defaults were not binary.** Before step 27a of the
  [plan](../adr/pr-plan.md):
  - one column of the model lacked `utf8mb4_0900_bin`, `KeyframeData.KeyframeTicks`. The provider's collation
    convention covered string properties only, and `KeyframeTicks` is a list of numbers that EF Core stores as
    text, so it took the default collation of `utf8mb4`. The convention now chooses a column by its type in the
    database, so primitive collections and value-converted strings get the collation too;
  - EF Core's history table `__EFMigrationsHistory`, and the default collation of the database and of every table,
    were the server's default for `utf8mb4`. The first migration named only the character set of the database and
    of each table, and that resets even a database created with `utf8mb4_0900_bin` to the server's default.

  None of this affected how Jellyfin compares text, as every other text column named `utf8mb4_0900_bin` itself:
  `KeyframeTicks` is read back whole, and `MigrationId` is only compared as an exact ASCII id. The MySQL baseline
  was regenerated with the fix, as the provider has not been released. In local runs on MySQL 8.4.11 and MariaDB
  11.4.13, a test migrates a new database that Jellyfin creates, one created with the server's defaults, and one
  created as above, and reads from `information_schema` that the database, every table and every text column, the
  history table included, use `utf8mb4_0900_bin`. A database created by an earlier build of the fork keeps the old
  collations of those columns and defaults, as its first migration is recorded as applied and does not run again.
  The fork is an unreleased development branch, so such a database is not upgraded: recreate it as in step 1 and let
  Jellyfin create its tables again.
- **Connection character set.** Jellyfin always connects with the character set `utf8mb4`, which the collation of
  the columns needs; a different character set in the connection string is replaced, with a warning in the log.
- **Privileges.** The migrations create, alter and drop tables and indexes, and the scheduled maintenance runs
  `ANALYZE TABLE`, so the user needs all privileges on the database, and none outside it.
- **Session settings.** Nothing is required of the server configuration. On every connection Jellyfin sets
  `READ-COMMITTED` isolation, the SQL mode `STRICT_ALL_TABLES,NO_ENGINE_SUBSTITUTION,ERROR_FOR_DIVISION_BY_ZERO`
  and the time zone `+00:00`, and it starts every transaction at `READ COMMITTED`.
- **Filtered indexes.** Neither MySQL nor MariaDB can create a partial index, so the four filtered indexes of
  `BaseItems` (on `OwnerId`; `ExtraType` and `OwnerId`; `PrimaryVersionId`; and `TopParentId`, `Type` and
  `IsVirtualItem`) are full indexes on {{engine}}. None of them is unique, so this changes their size and the query
  plans that use them, not the results of a query.
{{/if}}
{{#if mysql}}
- **Packet size.** Jellyfin sends a list of ids as one parameter per id, about 26 bytes each, and the largest lists
  (whole-library operations) must fit into `max_allowed_packet`. MySQL 8.4's default of 64M takes about a million
  ids.
{{/if}}
{{#if mariadb}}
- **Packet size.** Set `max_allowed_packet` to at least `64M`. Jellyfin sends a list of ids as one parameter per
  id, about 26 bytes each, and the largest lists (whole-library operations) must fit into one packet. MariaDB's
  default of 16M takes about 600,000 ids; 64M takes about a million.
- **Fixed: error 1020 on MariaDB 11.8 and later.** From 11.8 on, including the 12.3 long-term release,
  `innodb_snapshot_isolation` is on by default, and a write at REPEATABLE READ fails with error 1020, "Record has
  changed since last read in table ...; try restarting transaction", when another connection changed the row after
  the transaction read it. Before step 27a of the [plan](../adr/pr-plan.md), a transaction that Jellyfin started
  without an isolation level ran at REPEATABLE READ, and one integration test failed that way now and then on 11.8.9
  and 12.3.3. Every transaction now runs at READ COMMITTED, where 1020 does not occur, and the provider also retries
  1020 as a write conflict ([ADR-0008](../adr/0008-concurrency-locking-and-retry.md) S5 and S6). In local runs of
  the affected integration tests on MariaDB 11.8.9 and 12.3.3, error 1020 no longer occurs: no failure in 10 runs on
  each, against 5 and 7 in 10 before the fix. MariaDB 11.4 is still the tested baseline, and 12.3 is still tested
  only by the advisory job. That advisory job, on MariaDB 12.3.3, passed at commit `399af59770`, which has the fix
  (a CI result relayed to this page, not rerun for it).
{{/if}}
{{#if sqlserver}}
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
{{/if}}

## 2. Write `database.xml`

`database.xml` in Jellyfin's configuration directory selects the provider. The configuration directory is the one
given by `--configdir` or `JELLYFIN_CONFIG_DIR`; in the Jellyfin Docker image it is `/config/config`. Write the
file before the first start: a server that starts without it writes a SQLite configuration and uses SQLite.

```xml
<?xml version="1.0" encoding="utf-8"?>
<DatabaseConfigurationOptions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <DatabaseType>{{provider-key}}</DatabaseType>
  <LockingBehavior>NoLock</LockingBehavior>
  <CustomProviderOptions>
    <PluginName />
    <PluginAssembly />
    <ConnectionString>{{connection-string}}</ConnectionString>
    <Options>
      <CustomDatabaseOption>
        <Key>command-timeout</Key>
        <Value>60</Value>
      </CustomDatabaseOption>
{{#if mysql-family}}
      <CustomDatabaseOption>
        <Key>server-version</Key>
        <Value>{{server-version-example}}</Value>
      </CustomDatabaseOption>
{{/if}}
    </Options>
  </CustomProviderOptions>
</DatabaseConfigurationOptions>
```

| Element | Value |
| --- | --- |
| `DatabaseType` | `{{provider-key}}`, compared without regard to case. An unknown name stops the server with the list of supported names. |
| `LockingBehavior` | `NoLock`, the default, or `Optimistic`, which retries in Jellyfin itself and only SQLite needs. `Pessimistic` stops the server at startup: only SQLite supports it. |
| `CustomProviderOptions/ConnectionString` | {{connection-string-kind}} It can instead come from the environment, see below. |
| `CustomProviderOptions/PluginName`, `PluginAssembly` | Empty. They are only read for `PLUGIN_PROVIDER`, a provider loaded from a plugin. |
| `CustomProviderOptions/Options` | Optional settings, each a `CustomDatabaseOption` with a `Key` and a `Value`. Keys are compared without regard to case. |

The options:

| Key | Value |
| --- | --- |
| `command-timeout` | The time a command may take, in seconds. Without it a command may take 60 seconds, unless the connection string sets a command timeout. |
| `max-pool-size` | The largest number of pooled connections. Without it the driver's default applies. |
{{#if mysql-family}}
| `server-version` | The kind and version of the server, for example `8.4-mysql` or `11.4.13-mariadb`: a version, a hyphen, and `mysql` or `mariadb`. Without it Jellyfin connects once at startup, without selecting the database, and reads the version the server reports. With it Jellyfin trusts the option, which is what a proxy that reports a version of its own needs; a version the server does report is still checked against the oldest supported one on every connection. A version without a patch level, such as `11.4-mariadb`, passes until a connection reports the exact version. |
{{/if}}
| `EnableSensitiveDataLogging` | `true` writes the values of query parameters to the log. For debugging only: the log then holds the data of your users. |

### The connection string from the environment

The environment variable `JELLYFIN_DATABASE__CONNECTIONSTRING` (the configuration key `database:ConnectionString`)
replaces `CustomProviderOptions/ConnectionString` when it is set and not blank. `DatabaseType` and the options still
come from `database.xml`, and the variable is never written to it. Jellyfin logs "The database connection string is
taken from the configuration key database:ConnectionString instead of database.xml", and shows the variable as
`[redacted]` where it logs its environment.

Prefer the variable: it keeps the password out of `database.xml` and off disk.

### What Jellyfin sets on the connection

{{#if postgresql}}
`Timezone=UTC`, always. `Command Timeout=60` unless the connection string or the `command-timeout` option sets
another.
{{/if}}
{{#if mysql-family}}
`Character Set=utf8mb4`, `GUID Format=Binary16` (ids are stored as 16 bytes) and `Allow User Variables=False`,
always. `Default Command Timeout=60` unless the connection string or the `command-timeout` option sets another.
{{/if}}
{{#if sqlserver}}
`Encrypt=True` (Mandatory) when the connection string does not name `Encrypt`; any `Encrypt` value you write
(`False`/`Optional`, `True`/`Mandatory`, `Strict`) and `TrustServerCertificate` are kept as written
([ADR-0011](../adr/0011-supported-database-targets.md) F10). For a container or a server with a self-signed
certificate, add `TrustServerCertificate=True` (or `Encrypt=False` on the same machine). `Command Timeout=60`
unless the connection string or the `command-timeout` option sets another.
{{/if}}

## 3. First start

On its first start Jellyfin applies every migration to the empty database, beginning with the baseline
`20200101000000_InitialCreate`, then runs as usual. `/health` answers `Healthy` once the server is up.

The log shows which provider runs:

{{#if postgresql}}
```text
PostgreSQL connection string: Host=db.example.com;Port=5432;Database=jellyfin;Username=jellyfin;Command Timeout=60;Timezone=UTC
```
{{/if}}
{{#if mysql}}
```text
The MySQL and MariaDB database provider is experimental. Back up the data before using it with a library you cannot rebuild.
MySQL 8.4.11 connection string: Server=db.example.com;Port=3306;User ID=jellyfin;Password=;Database=jellyfin;Allow User Variables=False;Character Set=utf8mb4;Default Command Timeout=60;GUID Format=Binary16
```
{{/if}}
{{#if mariadb}}
```text
The MySQL and MariaDB database provider is experimental. Back up the data before using it with a library you cannot rebuild.
MariaDB 11.4.13 connection string: Server=db.example.com;Port=3306;User ID=jellyfin;Password=;Database=jellyfin;Allow User Variables=False;Character Set=utf8mb4;Default Command Timeout=60;GUID Format=Binary16
```
{{/if}}
{{#if sqlserver}}
```text
The SQL Server database provider is experimental. Back up the data before using it with a library you cannot rebuild.
SQL Server connection string: Data Source=db.example.com,1433;Initial Catalog=jellyfin;User ID=jellyfin;Password=;Command Timeout=60;Encrypt=True
```
{{/if}}

The password is removed from the connection string in the log.
{{#if mysql-family}}
The version in that line is the one the server reported, or the `server-version` option when it is set.
{{/if}}

## 4. Back up and restore

A backup of a Jellyfin server on {{engine}} has two parts: the database, and Jellyfin's own directories
(configuration, data, metadata and cache). Back up both, at the same time.

### The database: a dump

A dump made with `{{dump-tool}}` is the backup of the database, and the only way back from a failed upgrade (see
below).

{{#if postgresql}}
```sh
pg_dump --host=db.example.com --username=jellyfin --format=custom --file=jellyfin.dump jellyfin
```

To restore, create an empty database as in step 1, then:

```sh
pg_restore --host=db.example.com --username=jellyfin --dbname=jellyfin --no-owner --role=jellyfin jellyfin.dump
```
{{/if}}
{{#if mysql-family}}
```sh
{{dump-tool}} --host=db.example.com --user=jellyfin --password --single-transaction --hex-blob \
  --no-tablespaces --default-character-set=utf8mb4 jellyfin > jellyfin.sql
```

`--single-transaction` gives a consistent dump without locking the tables, `--hex-blob` writes the 16-byte ids
as hexadecimal, and `--no-tablespaces` lets a user without the `PROCESS` privilege make the dump. To restore,
create an empty database as in step 1, then:

```sh
{{client}} --host=db.example.com --user=jellyfin --password --default-character-set=utf8mb4 jellyfin < jellyfin.sql
```
{{/if}}
{{#if sqlserver}}
```sh
sqlcmd -S db.example.com,1433 -U jellyfin -C -b -Q \
  "BACKUP DATABASE [jellyfin] TO DISK = N'/var/opt/mssql/data/jellyfin.bak' WITH FORMAT, INIT"
```

To restore (with Jellyfin stopped, using a login that may restore databases such as `sa`):

```sh
sqlcmd -S db.example.com,1433 -U sa -C -b -Q \
  "ALTER DATABASE [jellyfin] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [jellyfin] FROM DISK = N'/var/opt/mssql/data/jellyfin.bak' WITH REPLACE; ALTER DATABASE [jellyfin] SET MULTI_USER;"
```
{{/if}}

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
{{#if mysql-family}}
- **The database must be migrated first.** A restore refuses a database that has migrations still to apply (a new
  installation, or a server that was updated and not started since), because {{engine}} cannot undo a failed
  migration. Run Jellyfin once with `--mode MigrateSystem`, which migrates the database and exits, then restore.
{{/if}}
{{#if sqlserver}}
- **A restore rolls back as a whole if a row exceeds SQL Server's limits.** Restoring an archive runs in one
  transaction. If the archive contains a row that SQL Server refuses (such as an item from another engine whose
  `Path` exceeds 850 characters, error 1946), the restore fails and rolls back completely, leaving the database
  with the rows it held before ([ADR-0012](../adr/0012-sqlserver-text-index-keys.md) T7).
{{/if}}
- **It does not repair a schema.** After a failed upgrade, restore the dump instead.

## 5. Upgrade

### Jellyfin

Take a dump before you upgrade Jellyfin. On SQLite Jellyfin copies the database file before it migrates; on
{{engine}} there is nothing it can copy. Before it migrates it logs "No backup of the Jellyfin database was taken,
as its provider has nothing to snapshot", and on a database that already has migrations the provider warns:

{{#if postgresql}}
> Migrations on PostgreSQL are applied one at a time and cannot be rolled back as a whole. Take a pg_dump of the
> database before upgrading, it is the only way to return to the state before a failed upgrade.

Each migration runs in a transaction of its own. A migration that fails is rolled back, but the migrations of the
same upgrade that ran before it stay applied. Jellyfin then logs which migration was the last to apply completely.
To go back, restore the dump and start the previous version of Jellyfin.

The migration `20261016043615_IndexItemPathByHash` rebuilds the index on the paths of the items as a hash index.
It runs at startup, before Jellyfin accepts clients, and holds the items table for as long as the index takes to
build: a few seconds per million items, more when the table is not in memory.
{{/if}}
{{#if mysql-family}}
> Migrations on MySQL and MariaDB are applied one statement at a time and cannot be rolled back as a whole. Take a
> dump of the database with mysqldump or mariadb-dump before upgrading, it is the only way to return to the state
> before a failed upgrade.

{{engine}} commits each change of the schema as soon as it runs, so a migration that fails part way leaves the
database partly migrated. Before each migration Jellyfin writes `migration-in-progress.json` into its data directory
(`/config/data` in the Docker image), and deletes it once the migration is recorded. If the file is left behind,
every later start refuses to migrate and names the migration. To continue, restore the dump taken before the
upgrade, delete the file, and start Jellyfin again. Restoring a Jellyfin backup archive does not repair the
database.
{{/if}}
{{#if sqlserver}}
> No backup of the SQL Server database is taken before migrating. A migration that fails is rolled back with its
> transaction, but back the database up with BACKUP DATABASE before upgrading, it is the only way to return to the
> previous version.

Each migration runs in a transaction of its own (DDL is transactional on SQL Server); a migration that fails is
rolled back with its transaction while earlier migrations of the same upgrade stay applied, and Jellyfin logs the
last migration that applied completely.
{{/if}}

### The database server

{{#if postgresql}}
Upgrade PostgreSQL with `pg_upgrade`, or by a dump and a restore into the new version. Any version from 16 on is
supported; CI also runs the newest release every night.
{{/if}}
{{#if mysql-family}}
Any version from {{floor}} on is supported. The provider builds its model for the server it finds, or for the
`server-version` option if you set it: update the option when you upgrade the server.
{{/if}}
{{#if mysql}}
MySQL upgrades its own system tables when a newer server starts on the data directory of an older one.
{{/if}}
{{#if mariadb}}
CI runs MariaDB 12.3, the newest long-term release, as an advisory job. The MariaDB Docker image upgrades its
system tables after a server upgrade only with `MARIADB_AUTO_UPGRADE=1`; otherwise run `mariadb-upgrade`.
{{/if}}
{{#if sqlserver}}
Any version from SQL Server 2022 (`16.0`) on is supported; upgrade in place or with `BACKUP DATABASE` and
`RESTORE DATABASE`. CI also runs SQL Server 2025 (`17.0`) as an advisory job.
{{/if}}

## Docker Compose

[`compose/{{id}}/compose.yaml`](compose/{{id}}/compose.yaml) runs Jellyfin and {{engine}} (`{{image}}`) side by
side. Upstream Jellyfin images have no {{engine}} provider, so the Compose file builds the Jellyfin image from this
checkout with [`compose/Dockerfile`](compose/Dockerfile): an official Jellyfin image, for its web client and
FFmpeg, with the server of this fork in place of its own.

```sh
cd docs/database/compose/{{id}}
cp env.example .env               # then set JELLYFIN_DB_PASSWORD and JELLYFIN_MEDIA_DIR in .env
docker compose up -d --build
```

It needs Docker Compose 2.23.1 or later. What it sets up:

{{#if postgresql}}
- **The database.** The image creates the user `jellyfin` and the database `jellyfin`, owned by it, with the
  encoding `UTF8`, from `POSTGRES_USER`, `POSTGRES_PASSWORD` and `POSTGRES_DB`.
{{/if}}
{{#if mysql}}
- **The database.** The image creates the database `jellyfin` and the user `jellyfin` with all privileges on it,
  from `MYSQL_DATABASE`, `MYSQL_USER` and `MYSQL_PASSWORD`, and a random root password it does not show. The server
  starts with `--character-set-server=utf8mb4 --collation-server=utf8mb4_0900_bin`, so the database gets the
  character set and collation of step 1.
{{/if}}
{{#if mariadb}}
- **The database.** The image creates the database `jellyfin` and the user `jellyfin` with all privileges on it,
  from `MARIADB_DATABASE`, `MARIADB_USER` and `MARIADB_PASSWORD`, and a random root password it does not show. The
  server starts with `--character-set-server=utf8mb4 --collation-server=utf8mb4_0900_bin --max-allowed-packet=64M`,
  so the database gets the character set and collation of step 1, and long lists of ids fit into a packet.
  `MARIADB_AUTO_UPGRADE=1` upgrades the system tables after a server upgrade.
{{/if}}
{{#if sqlserver}}
- **The database.** The SQL Server image starts `sqlservr` with `ACCEPT_EULA=Y`, `MSSQL_PID` (`Developer` by default)
  and `MSSQL_SA_PASSWORD` set from `JELLYFIN_DB_PASSWORD`. Unlike the PostgreSQL, MySQL and MariaDB images, the SQL
  Server image does not create an application database or user by itself: the container's health check connects as
  `sa` once `sqlservr` is up and, if the `jellyfin` login does not exist yet, creates the `jellyfin` database with
  `COLLATE Latin1_General_100_BIN2` and `READ_COMMITTED_SNAPSHOT ON` and the `jellyfin` login and `db_owner` user
  with `JELLYFIN_DB_PASSWORD`, before reporting healthy.
{{/if}}
- **`database.xml`.** It is written inline in the Compose file and mounted at `/run/jellyfin/database.xml`. On the
  first start the image copies it to `/config/config/database.xml`, where it stays an ordinary file, as a restore of
  a backup archive rewrites it. To change it later, edit that copy in the `config` volume.
{{#if mysql-family}}
  Set `JELLYFIN_DB_SERVER_VERSION` in `.env` before the first start to fill in the `server-version` option; empty
  asks the server.
{{/if}}
- **The connection string.** `JELLYFIN_DATABASE__CONNECTIONSTRING` carries it, with the password from `.env`, so
  the password is in neither `database.xml` nor a backup archive.
- **Health checks.** Jellyfin starts once the database answers over TCP; the image's own setup server does not.
  Jellyfin is healthy once `/health` answers `Healthy`: while it starts it answers `Degraded`, and once it runs the
  answer includes a check of the database.
- **Volumes.** `database` holds the database, `config` and `cache` hold Jellyfin's directories (`/config` and
  `/cache`); the media folder is mounted read-only at `/media`.

Check that it runs on {{engine}}, and make a dump from the database container:

```sh
docker compose logs jellyfin | grep "connection string"
curl -fsS http://localhost:8096/health
{{#if postgresql}}
docker compose exec -T postgres pg_dump --username=jellyfin --format=custom jellyfin > jellyfin.dump
{{/if}}
{{#if mysql-family}}
docker compose exec -T {{service}} sh -c '{{dump-tool}} --user=jellyfin --password="${{password-variable}}" --single-transaction \
  --hex-blob --no-tablespaces --default-character-set=utf8mb4 jellyfin' > jellyfin.sql
{{/if}}
{{#if sqlserver}}
docker compose exec -T sqlserver sh -c 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S 127.0.0.1 -U jellyfin -C -b -Q \
  "BACKUP DATABASE [jellyfin] TO DISK = N'\''/var/opt/mssql/data/jellyfin.bak'\'' WITH FORMAT, INIT"'
{{/if}}
```

`-T` keeps a terminal out of the way of the dump. `docker compose down` removes the containers and keeps the
volumes; `docker compose down -v` also deletes the volumes, and with them the database.

## Running the tests on {{engine}}

For developers: the database tests start a `{{image}}` container through Testcontainers when they are run with

```sh
dotnet test tests/Jellyfin.Server.Implementations.Tests --settings tests/{{run-settings}}
```

or with `JELLYFIN_TEST_DB_PROVIDER={{test-provider}}`. CI runs {{ci-jobs}}.

## Where this comes from

- The provider: `{{provider-source}}`.
- The selection of the provider and the connection string from the environment:
  `Jellyfin.Server.Implementations/Extensions/ServiceCollectionExtensions.cs`.
- Supported engines, tiers and oldest versions: [ADR-0011](../adr/0011-supported-database-targets.md).
- The CI jobs: [ADR-0010](../adr/0010-ci-gating-for-database-provider-tests.md), `.github/workflows/ci-tests-db.yml`.
