using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Jellyfin.Database.Providers.MySql;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

public class MySqlCollectionParameterInterceptorTests
{
    [Fact]
    public void GuidList_IsPassedWithOneParameterPerValue()
    {
        var call = Rewrite<List<Guid>>([Guid.NewGuid()]);

        Assert.Equal("MultipleParameters", call.Method.Name);
        Assert.Equal(typeof(List<Guid>), call.Method.GetGenericArguments().Single());
    }

    [Fact]
    public void StringList_IsPassedWithOneParameterPerValue()
    {
        var call = Rewrite<List<string>>(["a", "b"]);

        Assert.Equal("MultipleParameters", call.Method.Name);
    }

    [Fact]
    public void IntArray_IsPassedWithOneParameterPerValue()
    {
        var call = Rewrite<int[]>([1, 2]);

        Assert.Equal("MultipleParameters", call.Method.Name);
        Assert.Equal(typeof(int[]), call.Method.GetGenericArguments().Single());
    }

    [Fact]
    public void ReadOnlyListAndEnumerable_AreRewritten()
    {
        Assert.Equal("MultipleParameters", Rewrite<IReadOnlyList<Guid>>([Guid.NewGuid()]).Method.Name);
        Assert.Equal("MultipleParameters", Rewrite<IEnumerable<string>>(["a"]).Method.Name);
    }

    [Fact]
    public void ScalarAndString_AreLeftAlone()
    {
        Assert.Equal("Parameter", Rewrite("text").Method.Name);
        Assert.Equal("Parameter", Rewrite(5).Method.Name);
        Assert.Equal("Parameter", Rewrite(Guid.NewGuid()).Method.Name);
    }

    [Fact]
    public void QueryWithoutAParameterCall_IsReturnedUnchanged()
    {
        Expression<Func<int, bool>> query = x => x > 3;

        Assert.Same(query, CollectionParameterInterceptor.Instance.QueryCompilationStarting(query, null!));
    }

    [Fact]
    public void CallNestedInAQuery_IsRewrittenInPlace()
    {
        var ids = new List<Guid> { Guid.NewGuid() };
        Expression<Func<IQueryable<Guid>, IQueryable<Guid>>> query = source => source.Where(id => Enumerable.Contains(EF.Parameter(ids), id));

        var rewritten = CollectionParameterInterceptor.Instance.QueryCompilationStarting(query, null!);

        var calls = new CallCollector();
        calls.Visit(rewritten);
        Assert.Contains("MultipleParameters", calls.Names);
        Assert.DoesNotContain("Parameter", calls.Names);
    }

    private static MethodCallExpression Rewrite<T>(T value)
    {
        Expression<Func<T>> original = () => EF.Parameter(value);
        var rewritten = (LambdaExpression)CollectionParameterInterceptor.Instance.QueryCompilationStarting(original, null!);
        return (MethodCallExpression)rewritten.Body;
    }

    private sealed class CallCollector : ExpressionVisitor
    {
        public List<string> Names { get; } = [];

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Names.Add(node.Method.Name);
            return base.VisitMethodCall(node);
        }
    }
}
