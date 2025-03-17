using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Jellyfin.Server.Implementations.Extensions;

/// <summary>
/// Provides <see cref="Expression"/> extension methods.
/// </summary>
public static class ExpressionExtensions
{
    /// <summary>
    /// Combines two predicates into a single predicate using a logical OR operation.
    /// </summary>
    /// <typeparam name="T">The predicate parameter type.</typeparam>
    /// <param name="firstPredicate">The first predicate expression to combine.</param>
    /// <param name="secondPredicate">The second predicate expression to combine.</param>
    /// <returns>A new expression representing the OR combination of the input predicates.</returns>
    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> firstPredicate, Expression<Func<T, bool>> secondPredicate)
    {
        ArgumentNullException.ThrowIfNull(firstPredicate);
        ArgumentNullException.ThrowIfNull(secondPredicate);

        var invokedExpression = Expression.Invoke(secondPredicate, firstPredicate.Parameters);
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(firstPredicate.Body, invokedExpression), firstPredicate.Parameters);
    }

    /// <summary>
    /// Combines multiple predicates into a single predicate using a logical OR operation.
    /// </summary>
    /// <typeparam name="T">The predicate parameter type.</typeparam>
    /// <param name="predicates">A collection of predicate expressions to combine.</param>
    /// <returns>A new expression representing the OR combination of all input predicates.</returns>
    public static Expression<Func<T, bool>> Or<T>(this IEnumerable<Expression<Func<T, bool>>> predicates)
    {
        ArgumentNullException.ThrowIfNull(predicates);

        return predicates.Aggregate((aggregatePredicate, nextPredicate) => aggregatePredicate.Or(nextPredicate));
    }

    /// <summary>
    /// Combines two predicates into a single predicate using a logical AND operation.
    /// </summary>
    /// <typeparam name="T">The predicate parameter type.</typeparam>
    /// <param name="firstPredicate">The first predicate expression to combine.</param>
    /// <param name="secondPredicate">The second predicate expression to combine.</param>
    /// <returns>A new expression representing the AND combination of the input predicates.</returns>
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> firstPredicate, Expression<Func<T, bool>> secondPredicate)
    {
        ArgumentNullException.ThrowIfNull(firstPredicate);
        ArgumentNullException.ThrowIfNull(secondPredicate);

        var invokedExpression = Expression.Invoke(secondPredicate, firstPredicate.Parameters);
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(firstPredicate.Body, invokedExpression), firstPredicate.Parameters);
    }

    /// <summary>
    /// Combines multiple predicates into a single predicate using a logical AND operation.
    /// </summary>
    /// <typeparam name="T">The predicate parameter type.</typeparam>
    /// <param name="predicates">A collection of predicate expressions to combine.</param>
    /// <returns>A new expression representing the AND combination of all input predicates.</returns>
    public static Expression<Func<T, bool>> And<T>(this IEnumerable<Expression<Func<T, bool>>> predicates)
    {
        ArgumentNullException.ThrowIfNull(predicates);

        return predicates.Aggregate((aggregatePredicate, nextPredicate) => aggregatePredicate.And(nextPredicate));
    }
}
