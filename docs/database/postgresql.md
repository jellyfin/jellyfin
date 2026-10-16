<!-- Rendered from docs/database/templates/how-to.md by docs/database/render. Edit the template, not this file. -->
# Running Jellyfin on PostgreSQL

This page belongs to the multi-database fork of Jellyfin (`feature/multi-db`). Upstream Jellyfin stores its
data in SQLite only; the fork adds PostgreSQL, MySQL and MariaDB providers. The page covers a new installation.
Moving an existing library from SQLite to another engine is not covered or tested here.

PostgreSQL is a **Supported** engine in this fork ([ADR-0011](../adr/0011-supported-database-targets.md)): its CI
job is a required check.

## Versions

| | |
| --- | --- |
| Oldest supported version | PostgreSQL 16 |
| Tested in CI | `run-tests-db / Postgres` on PostgreSQL 16.15, on every pull request, as a required check; `run-tests-db / Postgres newest` on PostgreSQL 18.6, every night |
| Image of the tests and of the Compose example | `postgres:16.15-trixie` |
| Checked at startup | No. The provider generates SQL for PostgreSQL 16 and does not read the server version, so an older server is not refused, only untested. |


## 1. Create the database and the user

Jellyfin needs an empty database and a user of its own for it. It creates the tables itself on its first start.

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

## 2. Write `database.xml`

`database.xml` in Jellyfin's configuration directory selects the provider. The configuration directory is the one
given by `--configdir` or `JELLYFIN_CONFIG_DIR`; in the Jellyfin Docker image it is `/config/config`. Write the
file before the first start: a server that starts without it writes a SQLite configuration and uses SQLite.

```xml
<?xml version="1.0" encoding="utf-8"?>
<DatabaseConfigurationOptions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <DatabaseType>Jellyfin-PostgreSQL</DatabaseType>
  <LockingBehavior>NoLock</LockingBehavior>
  <CustomProviderOptions>
    <PluginName />
    <PluginAssembly />
    <ConnectionString>Host=db.example.com;Port=5432;Database=jellyfin;Username=jellyfin;Password=CHANGE-ME</ConnectionString>
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
| `DatabaseType` | `Jellyfin-PostgreSQL`, compared without regard to case. An unknown name stops the server with the list of supported names. |
| `LockingBehavior` | `NoLock`, the default. `Pessimistic` and `Optimistic` add locking in Jellyfin itself, which only SQLite needs. |
| `CustomProviderOptions/ConnectionString` | An [Npgsql connection string](https://www.npgsql.org/doc/connection-string-parameters.html). It can instead come from the environment, see below. |
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

Prefer the variable: it keeps the password out of `database.xml`, and so out of Jellyfin's backup archives, which
contain every `.xml` file of the configuration directory.

### What Jellyfin sets on the connection

`Timezone=UTC`, always. `Command Timeout=60` unless the connection string or the `command-timeout` option sets
another.

## 3. First start

On its first start Jellyfin applies every migration to the empty database, beginning with the baseline
`20200101000000_InitialCreate`, then runs as usual. `/health` answers `Healthy` once the server is up.

The log shows which provider runs:

```text
PostgreSQL connection string: Host=db.example.com;Port=5432;Database=jellyfin;Username=jellyfin;Command Timeout=60;Timezone=UTC
```

The password is removed from the connection string in the log.

## 4. Back up and restore

A backup of a Jellyfin server on PostgreSQL has two parts: the database, and Jellyfin's own directories
(configuration, data, metadata and cache). Back up both, at the same time.

### The database: a dump

A dump made with `pg_dump` is the backup of the database, and the only way back from a failed upgrade (see
below).

```sh
pg_dump --host=db.example.com --username=jellyfin --format=custom --file=jellyfin.dump jellyfin
```

To restore, create an empty database as in step 1, then:

```sh
pg_restore --host=db.example.com --username=jellyfin --dbname=jellyfin --no-owner --role=jellyfin jellyfin.dump
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
- **The archive holds `database.xml`.** It contains every `.xml` file of the configuration directory, so a
  connection string with a password in `database.xml` is in the archive. A restore also replaces `database.xml`
  with the copy in the archive; if the archive comes from another server, check `database.xml` before the next
  start.
- **It does not repair a schema.** After a failed upgrade, restore the dump instead.

## 5. Upgrade

### Jellyfin

Take a dump before you upgrade Jellyfin. On SQLite Jellyfin copies the database file before it migrates; on
PostgreSQL there is nothing it can copy. Before it migrates it logs "No backup of the Jellyfin database was taken,
as its provider has nothing to snapshot", and on a database that already has migrations the provider warns:

> Migrations on PostgreSQL are applied one at a time and cannot be rolled back as a whole. Take a pg_dump of the
> database before upgrading, it is the only way to return to the state before a failed upgrade.

Each migration runs in a transaction of its own. A migration that fails is rolled back, but the migrations of the
same upgrade that ran before it stay applied. Jellyfin then logs which migration was the last to apply completely.
To go back, restore the dump and start the previous version of Jellyfin.

The migration `20261016043615_IndexItemPathByHash` rebuilds the index on the paths of the items as a hash index.
It runs at startup, before Jellyfin accepts clients, and holds the items table for as long as the index takes to
build: a few seconds per million items, more when the table is not in memory.

### The database server

Upgrade PostgreSQL with `pg_upgrade`, or by a dump and a restore into the new version. Any version from 16 on is
supported; CI also runs the newest release every night.

## Docker Compose

[`compose/postgresql/compose.yaml`](compose/postgresql/compose.yaml) runs Jellyfin and PostgreSQL (`postgres:16.15-trixie`) side by
side. Upstream Jellyfin images have no PostgreSQL provider, so the Compose file builds the Jellyfin image from this
checkout with [`compose/Dockerfile`](compose/Dockerfile): an official Jellyfin image, for its web client and
FFmpeg, with the server of this fork in place of its own.

```sh
cd docs/database/compose/postgresql
cp env.example .env               # then set JELLYFIN_DB_PASSWORD and JELLYFIN_MEDIA_DIR in .env
docker compose up -d --build
```

It needs Docker Compose 2.23.1 or later. What it sets up:

- **The database.** The image creates the user `jellyfin` and the database `jellyfin`, owned by it, with the
  encoding `UTF8`, from `POSTGRES_USER`, `POSTGRES_PASSWORD` and `POSTGRES_DB`.
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

Check that it runs on PostgreSQL, and make a dump from the database container:

```sh
docker compose logs jellyfin | grep "connection string"
curl -fsS http://localhost:8096/health
docker compose exec -T postgres pg_dump --username=jellyfin --format=custom jellyfin > jellyfin.dump
```

`-T` keeps a terminal out of the way of the dump. `docker compose down` removes the containers and keeps the
volumes; `docker compose down -v` also deletes the volumes, and with them the database.

## Running the tests on PostgreSQL

For developers: the database tests start a `postgres:16.15-trixie` container through Testcontainers when they are run with

```sh
dotnet test tests/Jellyfin.Server.Implementations.Tests --settings tests/postgres.runsettings
```

or with `JELLYFIN_TEST_DB_PROVIDER=Postgres`. CI runs `run-tests-db / Postgres` on PostgreSQL 16.15, on every pull request, as a required check; `run-tests-db / Postgres newest` on PostgreSQL 18.6, every night.

## Where this comes from

- The provider: `src/Jellyfin.Database/Jellyfin.Database.Providers.Postgres/PostgresDatabaseProvider.cs`.
- The selection of the provider and the connection string from the environment:
  `Jellyfin.Server.Implementations/Extensions/ServiceCollectionExtensions.cs`.
- Supported engines, tiers and oldest versions: [ADR-0011](../adr/0011-supported-database-targets.md).
- The CI jobs: [ADR-0010](../adr/0010-ci-gating-for-database-provider-tests.md), `.github/workflows/ci-tests-db.yml`.
