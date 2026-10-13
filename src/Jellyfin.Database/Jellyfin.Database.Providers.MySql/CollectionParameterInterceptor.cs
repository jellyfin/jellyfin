using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jellyfin.Database.Providers.MySql;

/// <summary>
/// Turns every collection that a query passes with <see cref="EF.Parameter{T}(T)"/> into one that is passed with
/// <c>EF.MultipleParameters</c>, which sends one parameter per value.
/// </summary>
/// <remarks>
/// A collection parameter that is not expanded needs a way to read the collection back from a single value, which
/// this provider does not use. Shared queries ask for a single parameter when they want the query plan to be reused,
/// and this lets them keep doing so, at the cost of one parameter per value.
/// </remarks>
internal sealed class CollectionParameterInterceptor : IQueryExpressionInterceptor
{
    private static readonly MethodInfo _multipleParameters =
        ((MethodCallExpression)((Expression<Func<IEnumerable<int>, IEnumerable<int>>>)(values => EF.MultipleParameters(values))).Body)
        .Method
        .GetGenericMethodDefinition();

    private CollectionParameterInterceptor()
    {
    }

    /// <summary>
    /// Gets the one instance, so that every context options object of the process holds the same interceptor and EF
    /// Core builds one internal service provider for them.
    /// </summary>
    public static CollectionParameterInterceptor Instance { get; } = new();

    /// <inheritdoc/>
    public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
    {
        return new Rewriter().Visit(queryExpression);
    }

    private sealed class Rewriter : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var method = node.Method;
            if (method.IsGenericMethod
                && method.DeclaringType == typeof(EF)
                && method.Name == nameof(EF.Parameter)
                && IsCollection(method.GetGenericArguments()[0]))
            {
                return Expression.Call(_multipleParameters.MakeGenericMethod(method.GetGenericArguments()[0]), Visit(node.Arguments[0]));
            }

            return base.VisitMethodCall(node);
        }

        private static bool IsCollection(Type type)
        {
            return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
        }
    }
}
