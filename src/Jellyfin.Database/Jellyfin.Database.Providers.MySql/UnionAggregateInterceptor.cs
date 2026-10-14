using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jellyfin.Database.Providers.MySql;

/// <summary>
/// Turns <c>Max</c> and <c>Min</c> of two concatenated sequences into the greater and the lesser of the aggregates of
/// the two sequences, so that the query does not need a derived table.
/// </summary>
/// <remarks>
/// <para>
/// Concatenating two sequences is translated to a <c>UNION ALL</c>, and the aggregate over it to a scalar subquery
/// that selects from the union as a derived table. When the sequences read a column of the outer query, MariaDB cannot
/// resolve that column inside the derived table and rejects the query. The aggregate of a union is the null-safe
/// greater (or lesser) of the aggregates of its parts, which are plain scalar subqueries that MariaDB resolves, and
/// each of which can seek an index with the outer column.
/// </para>
/// <para>
/// The second sequence is tested first. It is the one that is usually empty, and then the comparison is skipped.
/// </para>
/// </remarks>
internal sealed class UnionAggregateInterceptor : IQueryExpressionInterceptor
{
    private static readonly HashSet<Type> _comparableTypes =
    [
        typeof(byte),
        typeof(sbyte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(decimal),
        typeof(DateTime)
    ];

    private UnionAggregateInterceptor()
    {
    }

    /// <summary>
    /// Gets the one instance, so that every context options object of the process holds the same interceptor and EF
    /// Core builds one internal service provider for them.
    /// </summary>
    public static UnionAggregateInterceptor Instance { get; } = new();

    /// <inheritdoc/>
    public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
    {
        return new Rewriter().Visit(queryExpression);
    }

    private static bool IsEnumerableMethod(Type? declaringType)
    {
        return declaringType == typeof(Queryable) || declaringType == typeof(Enumerable);
    }

    private static bool IsFunction(Type type)
    {
        return typeof(Delegate).IsAssignableFrom(type)
            || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Expression<>));
    }

    private sealed class Rewriter : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var visited = base.VisitMethodCall(node);
            if (visited is MethodCallExpression call)
            {
                return TryRewrite(call) ?? visited;
            }

            return visited;
        }

        private static Expression? TryRewrite(MethodCallExpression call)
        {
            var method = call.Method;
            if (!IsEnumerableMethod(method.DeclaringType) || (method.Name != nameof(Queryable.Max) && method.Name != nameof(Queryable.Min)))
            {
                return null;
            }

            var arguments = call.Arguments;
            if (arguments.Count is < 1 or > 2 || (arguments.Count == 2 && !IsFunction(arguments[1].Type)))
            {
                return null;
            }

            var underlyingType = Nullable.GetUnderlyingType(call.Type);
            if (underlyingType is null || !_comparableTypes.Contains(underlyingType))
            {
                return null;
            }

            if (arguments[0] is not MethodCallExpression { Method: { Name: nameof(Queryable.Concat) } concatMethod } concat
                || !IsEnumerableMethod(concatMethod.DeclaringType)
                || concat.Arguments.Count != 2)
            {
                return null;
            }

            var sourceType = method.GetParameters()[0].ParameterType;
            var first = concat.Arguments[0];
            var second = concat.Arguments[1];
            if (!sourceType.IsAssignableFrom(first.Type) || !sourceType.IsAssignableFrom(second.Type))
            {
                return null;
            }

            Expression Aggregate(Expression source)
            {
                var cloner = new Cloner();
                var copies = new List<Expression> { cloner.Visit(source) };
                if (arguments.Count == 2)
                {
                    copies.Add(cloner.Visit(arguments[1]));
                }

                var aggregate = Expression.Call(null, method, copies);
                return TryRewrite(aggregate) ?? aggregate;
            }

            var isGreater = method.Name == nameof(Queryable.Max);
            return Expression.Condition(
                Expression.Equal(Aggregate(second), Expression.Constant(null, call.Type)),
                Aggregate(first),
                Expression.Condition(
                    isGreater
                        ? Expression.GreaterThan(Aggregate(first), Aggregate(second))
                        : Expression.LessThan(Aggregate(first), Aggregate(second)),
                    Aggregate(first),
                    Aggregate(second)));
        }
    }

    /// <summary>
    /// Copies an expression with new parameters for every lambda in it, so that no lambda is shared by two places of
    /// the query. Parameters that the copied expression does not declare, such as the outer row, are kept.
    /// </summary>
    private sealed class Cloner : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, ParameterExpression> _parameters = [];

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var parameters = new List<ParameterExpression>(node.Parameters.Count);
            foreach (var parameter in node.Parameters)
            {
                var copy = Expression.Parameter(parameter.Type, parameter.Name);
                _parameters[parameter] = copy;
                parameters.Add(copy);
            }

            return Expression.Lambda<T>(Visit(node.Body), node.TailCall, parameters);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return _parameters.TryGetValue(node, out var copy) ? copy : node;
        }
    }
}
