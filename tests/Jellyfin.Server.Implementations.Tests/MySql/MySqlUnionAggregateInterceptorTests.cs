using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.Implementations.Tests.Postgres;
using MediaBrowser.Controller.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

public class MySqlUnionAggregateInterceptorTests
{
    public static TheoryData<int?[], int?[]> Sequences => new()
    {
        { [], [] },
        { [], [3] },
        { [3], [] },
        { [5], [3] },
        { [3], [5] },
        { [4, 4], [4] },
        { [1, 9, 4], [7, 2] },
        { [null], [null] },
        { [null], [3] },
        { [3], [null] },
        { [null, 2], [null] },
        { [null, null], [8, null] }
    };

    [Theory]
    [MemberData(nameof(Sequences))]
    public void Max_OfQueryables_GivesTheValueOfTheUnion(int?[] first, int?[] second)
    {
        Expression<Func<int?>> original = () => first.AsQueryable().Concat(second.AsQueryable()).Max();

        AssertSameValue(original, expectedRewrite: true);
    }

    [Theory]
    [MemberData(nameof(Sequences))]
    public void Min_OfQueryables_GivesTheValueOfTheUnion(int?[] first, int?[] second)
    {
        Expression<Func<int?>> original = () => first.AsQueryable().Concat(second.AsQueryable()).Min();

        AssertSameValue(original, expectedRewrite: true);
    }

    [Theory]
    [MemberData(nameof(Sequences))]
    public void Max_OfEnumerables_GivesTheValueOfTheUnion(int?[] first, int?[] second)
    {
        Expression<Func<int?>> original = () => first.Concat(second).Max();

        AssertSameValue(original, expectedRewrite: true);
    }

    [Theory]
    [MemberData(nameof(Sequences))]
    public void Min_OfEnumerables_GivesTheValueOfTheUnion(int?[] first, int?[] second)
    {
        Expression<Func<int?>> original = () => first.Concat(second).Min();

        AssertSameValue(original, expectedRewrite: true);
    }

    [Theory]
    [MemberData(nameof(Sequences))]
    public void MaxWithSelector_OfQueryables_GivesTheValueOfTheUnion(int?[] first, int?[] second)
    {
        var firstRows = first.Select(e => new Row(e)).AsQueryable();
        var secondRows = second.Select(e => new Row(e)).AsQueryable();
        Expression<Func<int?>> original = () => firstRows.Concat(secondRows).Max(r => r.Number);

        AssertSameValue(original, expectedRewrite: true);
    }

    [Theory]
    [MemberData(nameof(Sequences))]
    public void MinWithSelector_OfEnumerables_GivesTheValueOfTheUnion(int?[] first, int?[] second)
    {
        var firstRows = first.Select(e => new Row(e)).ToList();
        var secondRows = second.Select(e => new Row(e)).ToList();
        Expression<Func<int?>> original = () => firstRows.Concat(secondRows).Min(r => r.Number);

        AssertSameValue(original, expectedRewrite: true);
    }

    [Fact]
    public void Max_OfDates_GivesTheNewestAndNullWhenBothAreEmpty()
    {
        var older = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newer = older.AddDays(3);
        var first = new List<Row> { new(null, older) }.AsQueryable();
        var second = new List<Row> { new(null, newer), new(null, null) }.AsQueryable();
        var none = new List<Row>().AsQueryable();

        Assert.Equal(newer, Evaluate(() => first.Concat(second).Max(r => r.Date)));
        Assert.Equal(newer, Evaluate(() => second.Concat(first).Max(r => r.Date)));
        Assert.Equal(older, Evaluate(() => first.Concat(none).Max(r => r.Date)));
        Assert.Equal(older, Evaluate(() => none.Concat(first).Min(r => r.Date)));
        Assert.Equal(older, Evaluate(() => first.Concat(second).Min(r => r.Date)));
        Assert.Null(Evaluate(() => none.Concat(none).Max(r => r.Date)));
        Assert.Null(Evaluate(() => none.Concat(new List<Row> { new(null, null) }.AsQueryable()).Max(r => r.Date)));
    }

    [Fact]
    public void Max_OfDecimals_GivesTheValueOfTheUnion()
    {
        var first = new decimal?[] { 1.5m, null };
        var second = new decimal?[] { 2.25m };
        var none = Array.Empty<decimal?>();

        Assert.Equal(2.25m, Evaluate(() => first.Concat(second).Max()));
        Assert.Equal(1.5m, Evaluate(() => first.Concat(none).Max()));
        Assert.Equal(1.5m, Evaluate(() => first.Concat(second).Min()));
        Assert.Null(Evaluate(() => none.Concat(none).Min()));
    }

    [Fact]
    public void MaxOfThreeSequences_GivesTheValueOfTheUnion()
    {
        int?[] first = [2];
        int?[] second = [null];
        int?[] third = [9, 1];
        Expression<Func<int?>> original = () => first.Concat(second).Concat(third).Max();

        AssertSameValue(original, expectedRewrite: true);
        Assert.DoesNotContain("Concat", CallNames(Rewrite(original)));
        Assert.Equal(9, Evaluate(() => first.Concat(second).Concat(third).Max()));
        Assert.Equal(1, Evaluate(() => first.Concat(second).Concat(third).Min()));
    }

    [Fact]
    public void Rewrite_ReplacesTheUnionByAggregatesOfItsParts()
    {
        int?[] first = [1];
        int?[] second = [2];
        Expression<Func<int?>> original = () => first.Concat(second).Max();

        var rewritten = Rewrite(original);

        var calls = CallNames(rewritten);
        Assert.DoesNotContain("Concat", calls);
        Assert.Equal(6, calls.Count(name => name == "Max"));
    }

    [Fact]
    public void Rewrite_TestsTheSecondSequenceFirst()
    {
        int?[] first = [1];
        int?[] second = [2];
        Expression<Func<int?>> original = () => first.Concat(second).Max();

        var condition = Assert.IsAssignableFrom<ConditionalExpression>(Rewrite(original).Body);

        var test = Assert.IsAssignableFrom<BinaryExpression>(condition.Test);
        Assert.Equal(ExpressionType.Equal, test.NodeType);
        Assert.Contains("second", test.Left.ToString(), StringComparison.Ordinal);
        Assert.Contains("first", condition.IfTrue.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Rewrite_GivesEveryCopyOfALambdaItsOwnParameter()
    {
        var first = new List<Row>().AsQueryable();
        var second = new List<Row>().AsQueryable();
        Expression<Func<int?>> original = () => first.Where(r => r.Number > 1).Concat(second.Where(r => r.Number > 2)).Max(r => r.Number);

        var parameters = new LambdaCollector();
        parameters.Visit(Rewrite(original));

        Assert.Equal(parameters.Parameters.Count, parameters.Parameters.Distinct().Count());
    }

    [Fact]
    public void Rewrite_KeepsTheColumnsOfTheOuterRow()
    {
        var rows = new List<Row> { new(1), new(2), new(3) }.AsQueryable();
        var others = new List<Row> { new(2), new(5) }.AsQueryable();
        Expression<Func<IQueryable<Row>, IQueryable<int?>>> original = outer => outer.Select(o =>
            rows.Where(r => r.Number == o.Number).Select(r => r.Number)
                .Concat(others.Where(r => r.Number == o.Number).Select(r => r.Number))
                .Max());

        var rewritten = Rewrite(original);

        Assert.DoesNotContain("Concat", CallNames(rewritten));
        var expected = original.Compile()(rows).ToList();
        var actual = ((Expression<Func<IQueryable<Row>, IQueryable<int?>>>)rewritten).Compile()(rows).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NonNullableResult_IsLeftAlone()
    {
        int[] first = [1];
        int[] second = [2];
        Expression<Func<int>> original = () => first.Concat(second).Max();

        Assert.Same(original, Rewrite(original));
    }

    [Fact]
    public void ResultOfATypeThatCannotBeCompared_IsLeftAlone()
    {
        Guid?[] first = [Guid.NewGuid()];
        Guid?[] second = [Guid.NewGuid()];
        string[] names = ["a"];
        Expression<Func<Guid?>> guids = () => first.Concat(second).Max();
        Expression<Func<string?>> strings = () => names.Concat(names).Max();

        Assert.Same(guids, Rewrite(guids));
        Assert.Same(strings, Rewrite(strings));
    }

    [Fact]
    public void AggregateWithoutAUnion_IsLeftAlone()
    {
        int?[] values = [1, 2];
        Expression<Func<int?>> max = () => values.Max();
        Expression<Func<int?>> maxOfWhere = () => values.Where(v => v > 1).Max();
        Expression<Func<IEnumerable<int?>>> union = () => values.Concat(values);

        Assert.Same(max, Rewrite(max));
        Assert.Same(maxOfWhere, Rewrite(maxOfWhere));
        Assert.Same(union, Rewrite(union));
    }

    [Fact]
    public void OtherAggregatesOfAUnion_AreLeftAlone()
    {
        int?[] first = [1];
        int?[] second = [2];
        Expression<Func<int>> count = () => first.Concat(second).Count();
        Expression<Func<int?>> sum = () => first.Concat(second).Sum();
        Expression<Func<double?>> average = () => first.Concat(second).Average();

        Assert.Same(count, Rewrite(count));
        Assert.Same(sum, Rewrite(sum));
        Assert.Same(average, Rewrite(average));
    }

    [Fact]
    public void UnionWithAnInMemoryList_IsLeftAlone()
    {
        var first = new List<Row>().AsQueryable();
        List<Row> inMemory = [new(1)];
        Expression<Func<int?>> original = () => first.Concat(inMemory).Max(r => r.Number);

        Assert.Same(original, Rewrite(original));
    }

    [Fact]
    public void MaxWithAComparer_IsLeftAlone()
    {
        int?[] first = [1];
        int?[] second = [2];
        Expression<Func<int?>> original = () => first.Concat(second).Max(Comparer<int?>.Default);

        Assert.Same(original, Rewrite(original));
    }

    [Fact]
    public void DatePlayedOrder_OnMariaDb_HasNoDerivedTable()
    {
        var sql = DatePlayedSql("11.4.13-mariadb");

        Assert.DoesNotContain("UNION", sql, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"FROM\s*\(", RegexOptions.IgnoreCase), sql);
        Assert.Contains("MAX(`u`.`LastPlayedDate`)", sql, StringComparison.Ordinal);
        Assert.Contains("`b`.`Id`", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void DatePlayedOrder_OnMariaDb_MatchesTheOuterRowInsideEachScalarSubquery()
    {
        var sql = DatePlayedSql("11.4.13-mariadb");

        Assert.Contains("(`u0`.`ItemId` = `b`.`Id`)", sql, StringComparison.Ordinal);
        Assert.Contains("(`b0`.`PrimaryVersionId` = `b`.`Id`)", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void DatePlayedOrder_OnMySql_IsNotRewritten()
    {
        var sql = DatePlayedSql("8.4-mysql");

        Assert.Contains("UNION ALL", sql, StringComparison.Ordinal);
    }

    private static string DatePlayedSql(string server)
    {
        using var context = OfflineContexts.MySql(server);
        var user = new User("test", "auth", "reset");
        var query = new InternalItemsQuery(user);
        var order = OrderMapper.MapOrderByField(ItemSortBy.DatePlayed, query, context);
        return context.BaseItems
            .OrderByDescending(order)
            .ThenBy(e => e.SortName)
            .Select(e => e.Id)
            .Take(100)
            .ToQueryString();
    }

    private static T Evaluate<T>(Expression<Func<T>> original)
    {
        var rewritten = (Expression<Func<T>>)Rewrite(original);
        Assert.NotSame(original, rewritten);
        Assert.Equal(original.Compile()(), rewritten.Compile()());
        return rewritten.Compile()();
    }

    private static void AssertSameValue<T>(Expression<Func<T>> original, bool expectedRewrite)
    {
        var rewritten = (Expression<Func<T>>)Rewrite(original);

        Assert.Equal(expectedRewrite, !ReferenceEquals(original, rewritten));
        Assert.Equal(original.Compile()(), rewritten.Compile()());
    }

    private static LambdaExpression Rewrite(LambdaExpression original)
    {
        return (LambdaExpression)UnionAggregateInterceptor.Instance.QueryCompilationStarting(original, null!);
    }

    private static List<string> CallNames(Expression expression)
    {
        var collector = new CallCollector();
        collector.Visit(expression);
        return collector.Names;
    }

    private sealed record Row(int? Number, DateTime? Date = null);

    private sealed class CallCollector : ExpressionVisitor
    {
        public List<string> Names { get; } = [];

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Names.Add(node.Method.Name);
            return base.VisitMethodCall(node);
        }
    }

    private sealed class LambdaCollector : ExpressionVisitor
    {
        public List<ParameterExpression> Parameters { get; } = [];

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            Parameters.AddRange(node.Parameters);
            return base.VisitLambda(node);
        }
    }
}
