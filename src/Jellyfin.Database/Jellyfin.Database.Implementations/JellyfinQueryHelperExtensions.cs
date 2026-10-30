#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Database.Implementations;

/// <summary>
/// Contains a number of query related extensions.
/// </summary>
/// <remarks>
/// Every helper here binds its values through <see cref="EF.Parameter{T}(T)"/>. Values embedded as bare
/// constants are inlined into the SQL as literals, which gives each distinct value its own entry in EF's
/// compiled query cache and its own statement for the database to plan.
/// </remarks>
public static class JellyfinQueryHelperExtensions
{
    private static readonly MethodInfo _containsMethodGenericCache = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static).First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2);
    private static readonly MethodInfo _efParameterInstruction = typeof(EF).GetMethod(nameof(EF.Parameter), BindingFlags.Public | BindingFlags.Static)!;
    private static readonly ConcurrentDictionary<Type, MethodInfo> _containsQueryCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> _efParameterCache = new();

    /// <summary>
    /// Builds an optimised query checking one property against a list of values while maintaining an optimal query.
    /// </summary>
    /// <typeparam name="TEntity">The entity.</typeparam>
    /// <typeparam name="TProperty">The property type to compare.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="oneOf">The list of items to check. An empty list matches nothing.</param>
    /// <param name="property">Property expression.</param>
    /// <returns>A Query.</returns>
    public static IQueryable<TEntity> WhereOneOrMany<TEntity, TProperty>(this IQueryable<TEntity> query, IReadOnlyList<TProperty> oneOf, Expression<Func<TEntity, TProperty>> property)
    {
        return query.Where(OneOrManyExpressionBuilder(oneOf, property));
    }

    /// <summary>
    /// Builds an optimised query expression checking one property against a list of values while maintaining an optimal query.
    /// </summary>
    /// <typeparam name="TEntity">The entity.</typeparam>
    /// <typeparam name="TProperty">The property type to compare.</typeparam>
    /// <param name="oneOf">The list of items to check. An empty list matches nothing.</param>
    /// <param name="property">Property expression.</param>
    /// <returns>A Query.</returns>
    public static Expression<Func<TEntity, bool>> OneOrManyExpressionBuilder<TEntity, TProperty>(this IReadOnlyList<TProperty> oneOf, Expression<Func<TEntity, TProperty>> property)
    {
        ArgumentNullException.ThrowIfNull(oneOf);
        ArgumentNullException.ThrowIfNull(property);

        var parameter = Expression.Parameter(typeof(TEntity), "item");
        property = ParameterReplacer.Replace<Func<TEntity, TProperty>, Func<TEntity, TProperty>>(property, property.Parameters[0], parameter);

        if (oneOf.Count == 0)
        {
            // Fail closed, and without asking the database to unpack an empty collection to prove it.
            return Expression.Lambda<Func<TEntity, bool>>(Expression.Constant(false), parameter);
        }

        if (oneOf.Count == 1)
        {
            var value = Expression.Call(
                null,
                EfParameterFor(typeof(TProperty)),
                Expression.Constant(oneOf[0], typeof(TProperty)));

            return Expression.Lambda<Func<TEntity, bool>>(
                typeof(TProperty).IsValueType
                    ? Expression.Equal(property.Body, value)
                    : Expression.ReferenceEqual(property.Body, value),
                parameter);
        }

        var containsMethodInfo = _containsQueryCache.GetOrAdd(typeof(TProperty), static (key) => _containsMethodGenericCache.MakeGenericMethod(key));

        // Binding the whole collection as one parameter keeps the statement identical for any element
        // count, instead of emitting one placeholder per element.
        return Expression.Lambda<Func<TEntity, bool>>(
            Expression.Call(
                null,
                containsMethodInfo,
                Expression.Call(null, EfParameterFor(oneOf.GetType()), Expression.Constant(oneOf)),
                property.Body),
            parameter);
    }

    private static MethodInfo EfParameterFor(Type type)
    {
        return _efParameterCache.GetOrAdd(type, static (key) => _efParameterInstruction.MakeGenericMethod(key));
    }

    /// <summary>
    /// Filters items that have any of the specified providers, optionally restricted to given values.
    /// </summary>
    /// <param name="baseQuery">The source query.</param>
    /// <param name="providerIds">Dictionary mapping provider names to values to match. An empty value array matches any value for that provider.</param>
    /// <returns>A filtered query.</returns>
    public static IQueryable<BaseItemEntity> WhereHasAnyProviderIds(
        this IQueryable<BaseItemEntity> baseQuery,
        IReadOnlyDictionary<string, string[]> providerIds)
    {
        return baseQuery.WhereProviderMatch(Flatten(providerIds), false);
    }

    /// <summary>
    /// Filters items that have any of the specified providers, optionally restricted to a given value.
    /// </summary>
    /// <param name="baseQuery">The source query.</param>
    /// <param name="providerIds">Dictionary mapping provider names to optional values. An empty value matches any value for that provider.</param>
    /// <returns>A filtered query.</returns>
    public static IQueryable<BaseItemEntity> WhereHasAnyProviderId(
        this IQueryable<BaseItemEntity> baseQuery,
        IReadOnlyDictionary<string, string> providerIds)
    {
        return baseQuery.WhereProviderMatch(providerIds, false);
    }

    /// <summary>
    /// Excludes items that have any of the specified providers, optionally restricted to a given value.
    /// </summary>
    /// <param name="baseQuery">The source query.</param>
    /// <param name="providerIds">Dictionary mapping provider names to optional values. An empty value excludes any value for that provider.</param>
    /// <returns>A filtered query.</returns>
    public static IQueryable<BaseItemEntity> WhereExcludeProviderIds(
        this IQueryable<BaseItemEntity> baseQuery,
        IReadOnlyDictionary<string, string> providerIds)
    {
        return baseQuery.WhereProviderMatch(providerIds, true);
    }

    private static IEnumerable<KeyValuePair<string, string>> Flatten(IReadOnlyDictionary<string, string[]> providerIds)
    {
        ArgumentNullException.ThrowIfNull(providerIds);

        foreach (var (provider, values) in providerIds)
        {
            if (values is null || values.Length == 0)
            {
                yield return new KeyValuePair<string, string>(provider, string.Empty);
                continue;
            }

            foreach (var value in values)
            {
                yield return new KeyValuePair<string, string>(provider, value);
            }
        }
    }

    /// <summary>
    /// Matches items against a set of (provider, value) pairs, where an empty value means any value for
    /// that provider. Emits a single EXISTS over the provider collection with the predicates OR'd, rather
    /// than one subquery per predicate group.
    /// </summary>
    private static IQueryable<BaseItemEntity> WhereProviderMatch(
        this IQueryable<BaseItemEntity> baseQuery,
        IEnumerable<KeyValuePair<string, string>> providerIds,
        bool invert)
    {
        ArgumentNullException.ThrowIfNull(providerIds);

        var existenceOnly = new List<string>();
        var specificValues = new List<string>();
        foreach (var (provider, value) in providerIds)
        {
            if (string.IsNullOrEmpty(value))
            {
                existenceOnly.Add(provider);
            }
            else
            {
                specificValues.Add(provider + ":" + value);
            }
        }

        if (existenceOnly.Count == 0 && specificValues.Count == 0)
        {
            return baseQuery;
        }

        var predicate = ProviderPredicate(existenceOnly, specificValues);

        // NOT EXISTS rather than NOT IN: the latter yields no rows at all if the subquery can produce NULL.
        return invert
            ? baseQuery.Where(e => !e.Provider!.AsQueryable().Any(predicate))
            : baseQuery.Where(e => e.Provider!.AsQueryable().Any(predicate));
    }

    private static Expression<Func<BaseItemProvider, bool>> ProviderPredicate(
        IReadOnlyList<string> existenceOnly,
        IReadOnlyList<string> specificValues)
    {
        var byProvider = existenceOnly.OneOrManyExpressionBuilder<BaseItemProvider, string>(p => p.ProviderId);
        var byPair = specificValues.OneOrManyExpressionBuilder<BaseItemProvider, string>(p => p.ProviderId + ":" + p.ProviderValue);

        // Both builders mint their own parameter; rebind so the two bodies can share one lambda.
        var parameter = byProvider.Parameters[0];
        var reboundPair = ParameterReplacer.Replace<Func<BaseItemProvider, bool>, Func<BaseItemProvider, bool>>(byPair, byPair.Parameters[0], parameter);

        return Expression.Lambda<Func<BaseItemProvider, bool>>(
            Expression.OrElse(byProvider.Body, reboundPair.Body),
            parameter);
    }

    internal static class ParameterReplacer
    {
        // Produces an expression identical to 'expression'
        // except with 'source' parameter replaced with 'target' expression.
        internal static Expression<TOutput> Replace<TInput, TOutput>(
                        Expression<TInput> expression,
                        ParameterExpression source,
                        ParameterExpression target)
        {
            return new ParameterReplacerVisitor<TOutput>(source, target)
                        .VisitAndConvert(expression);
        }

        private sealed class ParameterReplacerVisitor<TOutput> : ExpressionVisitor
        {
            private readonly ParameterExpression _source;
            private readonly ParameterExpression _target;

            public ParameterReplacerVisitor(ParameterExpression source, ParameterExpression target)
            {
                _source = source;
                _target = target;
            }

            internal Expression<TOutput> VisitAndConvert<T>(Expression<T> root)
            {
                return (Expression<TOutput>)VisitLambda(root);
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                // Leave all parameters alone except the one we want to replace.
                var parameters = node.Parameters.Select(p => p == _source ? _target : p);

                return Expression.Lambda<TOutput>(Visit(node.Body), parameters);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                // Replace the source with the target, visit other params as usual.
                return node == _source ? _target : base.VisitParameter(node);
            }
        }
    }
}
