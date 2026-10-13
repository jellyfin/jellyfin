using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class PeopleUpdateQueryTests : JellyfinDbTestFixture
{
    private readonly CommandRecorder _recorder;
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly PeopleRepository _people;

    public PeopleUpdateQueryTests()
        : this(new CommandRecorder())
    {
    }

    private PeopleUpdateQueryTests(CommandRecorder recorder)
        : base(recorder)
    {
        _recorder = recorder;
        using var context = CreateDbContext();
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = _itemId,
            Name = "Movie",
            Type = new ItemTypeLookup().BaseItemKindNames[BaseItemKind.Movie]
        });
        context.SaveChanges();
        _people = new PeopleRepository(CreateDbContextFactory(), new ItemTypeLookup(), Mock.Of<IItemQueryHelpers>());
    }

    [Theory]
    [InlineData("Hero")]
    [InlineData("HERO")]
    public void UnchangedCredits_DoNotWriteOrLookUpAllPeople(string role)
    {
        _people.UpdatePeople(_itemId, [new PersonInfo { Name = "Actor", Type = PersonKind.Actor, Role = "Hero" }]);
        _recorder.Commands.Clear();
        _people.UpdatePeople(_itemId, [new PersonInfo { Name = "actor", Type = PersonKind.Actor, Role = role }]);
        Assert.Single(_recorder.Commands);
        Assert.StartsWith("SELECT", _recorder.Commands[0].Sql, StringComparison.Ordinal);
        using var context = CreateDbContext();
        Assert.Equal("Hero", Assert.Single(context.PeopleBaseItemMap).Role);
    }

    [Fact]
    public void SortOrderChange_IsPersisted()
    {
        _people.UpdatePeople(_itemId, [new PersonInfo { Name = "Actor", Type = PersonKind.Actor, SortOrder = 1 }]);
        _people.UpdatePeople(_itemId, [new PersonInfo { Name = "Actor", Type = PersonKind.Actor, SortOrder = 2 }]);
        using var context = CreateDbContext();
        Assert.Equal(2, Assert.Single(context.PeopleBaseItemMap).SortOrder);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void UpdatePeople_GeneratedSqlUsesPeopleNameIndex()
    {
        var query = RecordLookup();
        Assert.Contains(Explain(query), line => line.Contains("SEARCH p USING INDEX IX_Peoples_NameLower", StringComparison.Ordinal));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public void UpdatePeople_GeneratedSqlUsesPeopleNameIndex_OnPostgres()
    {
        var query = RecordLookup();
        Assert.Contains(ExplainOnPostgres(query), line => line.Contains("IX_Peoples_NameLower", StringComparison.Ordinal));
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void GetPeople_NameStartsWith_GeneratedSqlUsesPeopleNameIndex()
    {
        var query = RecordPrefixLookup();
        Assert.Contains(Explain(query), line => line.Contains("INDEX IX_Peoples_NameLower", StringComparison.Ordinal));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public void GetPeople_NameStartsWith_GeneratedSqlUsesPeopleNameIndex_OnPostgres()
    {
        var query = RecordPrefixLookup();
        Assert.Contains(ExplainOnPostgres(query), line => line.Contains("IX_Peoples_NameLower", StringComparison.Ordinal));
    }

    private RecordedCommand RecordPrefixLookup()
    {
        _recorder.Commands.Clear();
        _people.GetPeople(new InternalPeopleQuery { NameStartsWith = "Émi", EnableTotalRecordCount = false });
        var query = _recorder.Commands.Last(IsPeopleFilter);
        Assert.Contains("p.\"NameLower\"", WhereClause(query), StringComparison.Ordinal);
        Assert.DoesNotContain("lower(", query.Sql, StringComparison.OrdinalIgnoreCase);
        return query;
    }

    private RecordedCommand RecordLookup()
    {
        _recorder.Commands.Clear();
        _people.UpdatePeople(_itemId, [
            new PersonInfo { Name = "Actor A", Type = PersonKind.Actor },
            new PersonInfo { Name = "Actor B", Type = PersonKind.Actor }
        ]);
        var query = Assert.Single(_recorder.Commands, IsPeopleFilter);
        Assert.Contains("p.\"NameLower\"", WhereClause(query), StringComparison.Ordinal);
        Assert.DoesNotContain("lower(", query.Sql, StringComparison.OrdinalIgnoreCase);
        return query;
    }

    private static bool IsPeopleFilter(RecordedCommand command) => command.Sql.StartsWith("SELECT", StringComparison.Ordinal)
        && Unquoted(command.Sql).Contains("FROM \"Peoples\" AS p\nWHERE", StringComparison.Ordinal);

    private static string WhereClause(RecordedCommand command)
    {
        var sql = Unquoted(command.Sql);
        return sql[sql.IndexOf("WHERE", StringComparison.Ordinal)..];
    }

    private static string Unquoted(string sql) => sql.Replace("\"p\"", "p", StringComparison.Ordinal);

    private string[] ExplainOnPostgres(RecordedCommand query)
    {
        using var context = CreateDbContext();
        context.Database.OpenConnection();

        // An empty table is always scanned, so the planner is told that a scan is the worse choice.
        context.Database.ExecuteSqlRaw("SET enable_seqscan = off");
        using var command = context.Database.GetDbConnection().CreateCommand();
#pragma warning disable CA2100 // query.Sql is generated by EF Core; query values remain bound parameters.
        command.CommandText = "EXPLAIN " + query.Sql;
#pragma warning restore CA2100
        foreach (var value in query.Parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = value.Name;
            parameter.Value = value.Value;
            command.Parameters.Add(parameter);
        }

        using var reader = command.ExecuteReader();
        var plan = new List<string>();
        while (reader.Read())
        {
            plan.Add(reader.GetString(0));
        }

        return plan.ToArray();
    }

    private string[] Explain(RecordedCommand query)
    {
        using var context = CreateDbContext();
        using var command = context.Database.GetDbConnection().CreateCommand();
#pragma warning disable CA2100 // query.Sql is generated by EF Core; query values remain bound parameters.
        command.CommandText = "EXPLAIN QUERY PLAN " + query.Sql;
#pragma warning restore CA2100
        foreach (var value in query.Parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = value.Name;
            parameter.Value = value.Value;
            command.Parameters.Add(parameter);
        }

        using var reader = command.ExecuteReader();
        var plan = new List<string>();
        while (reader.Read())
        {
            plan.Add(reader.GetString(3));
        }

        return plan.ToArray();
    }

    private sealed record RecordedCommand(string Sql, (string Name, object? Value)[] Parameters);

    private sealed class CommandRecorder : DbCommandInterceptor
    {
        public List<RecordedCommand> Commands { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Record(command);
            return result;
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            Record(command);
            return result;
        }

        private void Record(DbCommand command) => Commands.Add(new RecordedCommand(
            command.CommandText,
            command.Parameters.Cast<DbParameter>().Select(p => (p.ParameterName, p.Value)).ToArray()));
    }
}
