# Adding a database change

Every database provider has its own migrations, because they contain SQL for
that provider. A change to the model needs one migration per provider, and the
migrations of a change carry the same id so that the histories line up.

One command generates all of them:

```sh
dev/add-migration AddUserNickname
```

On Windows, or anywhere the file is not executable:

```sh
dotnet run dev/add-migration -- AddUserNickname
```

Use a name that describes the change. The command needs the .NET 10 SDK and the
`dotnet-ef` tool, which is restored with `dotnet tool restore`. It does not
connect to a database. It generates the SQLite migration first, then the
others with the same id, and prints every file it created or changed. If the
SQLite migration fails, no other provider is run.

For each provider the command builds its project first and then runs
`dotnet ef migrations add` with `--no-build`, because `dotnet ef` hides the
compiler errors of a failed build. It passes only `--project`, `--output-dir`
and `--no-build`. The design-time factories ignore the `--provider` and
`--migration-provider` options, so do not add them.

A provider that already has a migration of that name is skipped and the
command says so. If a run stopped half way, or you generated only some
providers, run the command again with the same name: it generates the missing
ones with the id of the SQLite migration. This works only for the newest
SQLite migration. For an older name, a name that differs from an existing one
only in case, a migration that every provider has, a provider that has the
migration while SQLite does not, or ids that differ, the command stops before it
generates anything and says what to do. If the refused older name is your own
uncommitted migration, for example because a rebase brought in a newer one,
delete its files from every provider, restore their snapshots to the commit
before it, and run the command again with a new name.

The providers and their migrations:

| Provider | Project | Servers | Baseline migration |
| --- | --- | --- | --- |
| SQLite | `Jellyfin.Database.Providers.Sqlite` | SQLite | none, it is the reference whose ids the others reuse |
| PostgreSQL | `Jellyfin.Database.Providers.Postgres` | PostgreSQL | `20200101000000_InitialCreate`, with hand-written parts |
| MySQL and MariaDB | `Jellyfin.Database.Providers.MySql` | MySQL 8.4 and MariaDB 11.4.7 or later | `20200101000000_InitialCreate`, generated |

MySQL and MariaDB share one set of migrations. The command generates it for a
fixed server version (MySQL 8.4) without connecting to a server, so the files
come out the same wherever they are generated. The migrations are applied to
both servers, so test a change on both.

## Review every generated migration

A generated migration is a starting point. Read each one before committing it,
and never port SQL from one provider to another blindly: the same operation can
mean something different or be impossible on another engine.

## What each change needs

| Change | What to do |
| --- | --- |
| A query written in LINQ | Nothing. |
| A new column, table or index | Run the command. Review every generated migration and commit them with the change. |
| A rename or a drop | EF scaffolds a drop plus an add, which loses the data. Edit the migrations by hand (`RenameColumn`, `RenameTable`, `RenameIndex`). |
| A change of column type | Check the generated SQL. PostgreSQL often needs a `USING` clause for the conversion, which EF does not write. |
| Data changes in SQL | Write them by hand in each provider, in the SQL of that provider. Where the text of the SQL differs between providers, record the difference in the allowlist described below. |
| An expression index | EF cannot model it. Add it by hand with `migrationBuilder.Sql` in each provider, in the SQL of that provider. Where the text differs between providers, record the difference in the allowlist described below. |

The `Model_HasNoPendingChanges` test fails when the model has changes that the
migrations of a provider do not contain. Its message names the command to run.
The test `MigrationsAfterTheBaseline_AreThoseOfTheReferenceProviderAfterWhatItCovers`
fails when the providers do not have the same migration ids.

The test `MigrationsThatShareAnId_HaveTheSameOperationsOrAnAllowlistEntry`
compares the migrations of a change across the providers. For each operation in
`Up` and `Down` it compares the kind, table and columns, whether a column is
nullable, whether an index is unique, what a foreign key does on delete, and the
text of SQL. It does not compare annotations, column types and lengths,
defaults, collations, index filters, the SQL of check constraints, the names of
new indexes and constraints, the values of data operations, or the order of the
operations. It fails when a hand edit, such as SQL, is in one provider's
migration only. Make the same change in every provider, in the SQL of that
provider. Some differences are correct: SQL that does what the other providers'
operations do, a step that only one engine needs, or a fix for data that only
old SQLite databases can hold (on SQLite only). Add such a difference to
`tests/Jellyfin.Server.Implementations.Tests/EfMigrations/MigrationContentsAllowlist.json`
with its category and reason; the failure message prints the entry to fill in. A
fix for a bug in current code is never listed there, because it has to reach
every provider. When a migration with entries changes, the test reports the
entries that no longer match.

The PostgreSQL and MySQL baselines share the id `20200101000000_InitialCreate`,
so the test compares them as well. They cover a different number of SQLite
migrations: PostgreSQL through `20261002210711_MakeOwnerIdIndexesPartial`, MySQL
through `20261013120000_AddNormalisedMatchColumns`. What one baseline already
contains and the other provider adds in a later migration is recorded with the
category `BaselineCoverage`, which the test accepts for a baseline id only.

## When a query behaves differently on one provider

Work through these in order and stop at the first that is enough.

1. Write the query so that it means the same on every provider, in plain LINQ.
2. Call a `JellyfinDbFunctions` function and let the provider translate it, or
   let the provider set a model convention in `ConfigureConventions`.
3. Add a member to `IJellyfinDatabaseProvider` with a default implementation,
   named after the operation it performs and not after a provider. Members of
   this interface are a permanent API for providers, so this needs the
   approval of the maintainers of the core.
4. If a difference cannot be closed, add an entry to `KnownGaps` in
   `tests/Jellyfin.Database.Testing`, keyed to the provider, and mark the tests
   with `KnownGap.On`. The marked test is skipped while it fails in the way the
   gap describes and fails once it passes, so the entry has to be deleted when
   the difference is gone.

Use `[ProviderFact(TestDbProvider.Sqlite)]` only for a test of SQLite itself.
A test of behaviour that every provider shares runs on every provider.

## Tests

`dotnet test` runs the tests against SQLite. The same tests run against
PostgreSQL in a container, and that leg runs on every pull request. A change to
the model or to a query is therefore checked on both providers before it is
merged.

## Troubleshooting

If you get `Run "dotnet tool restore" to make the "dotnet-ef" command
available.`, run `dotnet tool restore` in the root of the repository.

If you get `System.UnauthorizedAccessException: Access to the path
'/src/Jellyfin.Database' is denied.`, the checkout or its `obj` and `bin`
folders are owned by another user, for example after a build as root. Fix the
ownership instead of running the tools as root.

If the command says that the providers did not produce the same migration id,
delete the files that run created and restore the snapshots it updated (the
`created` and `updated` lines it printed), then run it again. A warning above
that message means a provider did not find the SQLite migrations, which
happens when it is not run from a checkout of the repository.

If a test says that the migrations of a provider do not match, and the SQLite
migration is correct, delete the migration files of the provider, restore its
`JellyfinDbModelSnapshot.cs` to the commit before the migration (`HEAD` while
the migration is not committed) and run the command again with the name of the
SQLite migration. It generates only the missing providers. Otherwise delete
the migration from every provider, restore their snapshots in the same way,
and run the command with the name you want.

## The baseline migration of PostgreSQL

The baseline migration `20200101000000_InitialCreate` of the PostgreSQL
provider contains hand-written parts (the `jf_uuid_smaller` function and the
`min(uuid)` aggregate) in `Migrations/20200101000000_InitialCreate.HandWritten.cs`.
If the baseline is ever regenerated, restore the call
`CreateHandWrittenObjects(migrationBuilder);` at the end of `Up` and
`DropHandWrittenObjects(migrationBuilder);` at the start of `Down`.
