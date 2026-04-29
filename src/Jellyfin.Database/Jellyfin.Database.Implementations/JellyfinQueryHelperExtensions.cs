#pragma warning disable RS0030 // Do not use banned APIs
// Do not enforce culture rules because EFCore cannot deal with cultures well.
#pragma warning disable CA1304 // Specify CultureInfo
#pragma warning disable CA1311 // Specify a culture or use an invariant version
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons

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
public static class JellyfinQueryHelperExtensions
{
    private static readonly MethodInfo _containsMethodGenericCache = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static).First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2);
    private static readonly MethodInfo _efParameterInstruction = typeof(EF).GetMethod(nameof(EF.Parameter), BindingFlags.Public | BindingFlags.Static)!;
    private static readonly ConcurrentDictionary<Type, MethodInfo> _containsQueryCache = new();

    /// <summary>
    /// Builds an optimised query checking one property against a list of values while maintaining an optimal query.
    /// </summary>
    /// <typeparam name="TEntity">The entity.</typeparam>
    /// <typeparam name="TProperty">The property type to compare.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="oneOf">The list of items to check.</param>
    /// <param name="property">Property expression.</param>
    /// <returns>A Query.</returns>
    public static IQueryable<TEntity> WhereOneOrMany<TEntity, TProperty>(this IQueryable<TEntity> query, IList<TProperty> oneOf, Expression<Func<TEntity, TProperty>> property)
    {
        return query.Where(OneOrManyExpressionBuilder(oneOf, property));
    }

    /// <summary>
    /// Builds a query that checks referenced ItemValues for a cross BaseItem lookup.
    /// </summary>
    /// <param name="baseQuery">The source query.</param>
    /// <param name="context">The database context.</param>
    /// <param name="itemValueType">The type of item value to reference.</param>
    /// <param name="referenceIds">The list of BaseItem ids to check matches.</param>
    /// <param name="invert">If set an exclusion check is performed instead.</param>
    /// <returns>A Query.</returns>
    public static IQueryable<BaseItemEntity> WhereReferencedItem(
        this IQueryable<BaseItemEntity> baseQuery,
        JellyfinDbContext context,
        ItemValueType itemValueType,
        IList<Guid> referenceIds,
        bool invert = false)
    {
        return baseQuery.Where(ReferencedItemFilterExpressionBuilder(context, itemValueType, referenceIds, invert));
    }

    /// <summary>
    /// Builds a query that checks referenced ItemValues for a cross BaseItem lookup.
    /// </summary>
    /// <param name="baseQuery">The source query.</param>
    /// <param name="context">The database context.</param>
    /// <param name="itemValueTypes">The type of item value to reference.</param>
    /// <param name="referenceIds">The list of BaseItem ids to check matches.</param>
    /// <param name="invert">If set an exclusion check is performed instead.</param>
    /// <returns>A Query.</returns>
    public static IQueryable<BaseItemEntity> WhereReferencedItemMultipleTypes(
        this IQueryable<BaseItemEntity> baseQuery,
        JellyfinDbContext context,
        IList<ItemValueType> itemValueTypes,
        IList<Guid> referenceIds,
        bool invert = false)
    {
        var itemFilter = OneOrManyExpressionBuilder<BaseItemEntity, Guid>(referenceIds, f => f.Id);
        var typeFilter = OneOrManyExpressionBuilder<ItemValue, ItemValueType>(itemValueTypes, iv => iv.Type);

        return baseQuery.Where(item =>
            context.ItemValues
                .Where(typeFilter)
                .Join(context.ItemValuesMap, e => e.ItemValueId, e => e.ItemValueId, (itemVal, map) => new { itemVal, map })
                .Any(val =>
                    context.BaseItems.Where(itemFilter).Any(e => e.CleanName == val.itemVal.CleanValue)
                    && val.map.ItemId == item.Id) == EF.Constant(!invert));
    }

    /// <summary>
    /// Builds a query expression that checks referenced ItemValues for a cross BaseItem lookup.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="itemValueType">The type of item value to reference.</param>
    /// <param name="referenceIds">The list of BaseItem ids to check matches.</param>
    /// <param name="invert">If set an exclusion check is performed instead.</param>
    /// <returns>A Query.</returns>
    public static Expression<Func<BaseItemEntity, bool>> ReferencedItemFilterExpressionBuilder(
        this JellyfinDbContext context,
        ItemValueType itemValueType,
        IList<Guid> referenceIds,
        bool invert = false)
    {
        // Well genre/artist/album etc items do not actually set the ItemValue of thier specitic types so we cannot match it that way.
        /*
        "(guid in (select itemid from ItemValues where CleanValue = (select CleanName from TypedBaseItems where guid=@GenreIds and Type=2)))"
        */

        var itemFilter = OneOrManyExpressionBuilder<BaseItemEntity, Guid>(referenceIds, f => f.Id);

        return item =>
          context.ItemValues
              .Join(context.ItemValuesMap, e => e.ItemValueId, e => e.ItemValueId, (item, map) => new { item, map })
              .Any(val =>
                  val.item.Type == itemValueType
                  && context.BaseItems.Where(itemFilter).Any(e => e.CleanName == val.item.CleanValue)
                  && val.map.ItemId == item.Id) == EF.Constant(!invert);
    }

    /// <summary>
    /// Builds a query that filters items by matching any of the specified provider ID/value pairs.
    /// Uses string-based matching with Contains for EF Core translation.
    /// </summary>
    /// <param name="baseQuery">The source query.</param>
    /// <param name="providerIds">Dictionary mapping provider names to arrays of provider values to match.</param>
    /// <returns>A filtered query.</returns>
    public static IQueryable<BaseItemEntity> WhereHasAnyProviderIds(
        this IQueryable<BaseItemEntity> baseQuery,
        IReadOnlyDictionary<string, string[]> providerIds)
    {
        if (providerIds.Count == 0)
        {
            return baseQuery;
        }

        var providerKeys = providerIds
            .SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key}:{v}"))
            .ToList();

        if (providerKeys.Count == 0)
        {
            return baseQuery;
        }

        return baseQuery.Where(e => e.Provider!
            .Select(p => p.ProviderId + ":" + p.ProviderValue)
            .Any(key => providerKeys.Contains(key)));
    }

    /// <summary>
    /// Builds a query that filters items by matching any of the specified provider ID/value pairs.
    /// Uses string-based matching with Contains for EF Core translation.
    /// </summary>
    /// <param name="baseQuery">The source query.</param>
    /// <param name="providerIds">Dictionary mapping provider names to optional values. Empty/null values match any item with that provider.</param>
    /// <returns>A filtered query.</returns>
    public static IQueryable<BaseItemEntity> WhereHasAnyProviderId(
        this IQueryable<BaseItemEntity> baseQuery,
        IReadOnlyDictionary<string, string> providerIds)
    {
        if (providerIds.Count == 0)
        {
            return baseQuery;
        }

        var existenceOnly = providerIds
            .Where(e => string.IsNullOrEmpty(e.Value))
            .Select(e => e.Key)
            .ToList();

        var specificValues = providerIds
            .Where(e => !string.IsNullOrEmpty(e.Value))
            .Select(e => $"{e.Key}:{e.Value}")
            .ToList();

        if (existenceOnly.Count > 0 && specificValues.Count > 0)
        {
            // Both existence checks and specific value checks
            return baseQuery.Where(e =>
                e.Provider!.Any(p => existenceOnly.Contains(p.ProviderId)) ||
                e.Provider!.Select(p => p.ProviderId + ":" + p.ProviderValue).Any(key => specificValues.Contains(key)));
        }
        else if (existenceOnly.Count > 0)
        {
            // Only existence checks
            return baseQuery.Where(e => e.Provider!.Any(p => existenceOnly.Contains(p.ProviderId)));
        }
        else
        {
            // Only specific value checks
            return baseQuery.Where(e =>
                e.Provider!.Select(p => p.ProviderId + ":" + p.ProviderValue).Any(key => specificValues.Contains(key)));
        }
    }

    /// <summary>
    /// Builds a query that excludes items matching the specified provider ID/value pairs.
    /// </summary>
    /// <param name="baseQuery">The source query.</param>
    /// <param name="providerIds">Dictionary mapping provider names to values to exclude.</param>
    /// <returns>A filtered query.</returns>
    public static IQueryable<BaseItemEntity> WhereExcludeProviderIds(
        this IQueryable<BaseItemEntity> baseQuery,
        IReadOnlyDictionary<string, string> providerIds)
    {
        if (providerIds.Count == 0)
        {
            return baseQuery;
        }

        var excludeKeys = providerIds
            .Select(e => $"{e.Key}:{e.Value}")
            .ToList();

        return baseQuery.Where(e =>
            e.Provider!.Select(p => p.ProviderId + ":" + p.ProviderValue).All(key => !excludeKeys.Contains(key)));
    }

    /// <summary>
    /// Builds a query that checks if items have a specific provider set (case-insensitive).
    /// </summary>
    /// <param name="baseQuery">The source query.</param>
    /// <param name="providerName">The provider name to check for.</param>
    /// <param name="hasProvider">True to include items with the provider, false to exclude them.</param>
    /// <returns>A filtered query.</returns>
    public static IQueryable<BaseItemEntity> WhereHasProvider(
        this IQueryable<BaseItemEntity> baseQuery,
        string providerName,
        bool hasProvider)
    {
        var lowerProviderName = providerName.ToLowerInvariant();

        return hasProvider
            ? baseQuery.Where(e => e.Provider!.Any(f => f.ProviderId.ToLower() == lowerProviderName))
            : baseQuery.Where(e => e.Provider!.All(f => f.ProviderId.ToLower() != lowerProviderName));
    }

    /// <summary>
    /// Builds an optimised query expression checking one property against a list of values while maintaining an optimal query.
    /// </summary>
    /// <typeparam name="TEntity">The entity.</typeparam>
    /// <typeparam name="TProperty">The property type to compare.</typeparam>
    /// <param name="oneOf">The list of items to check.</param>
    /// <param name="property">Property expression.</param>
    /// <returns>A Query.</returns>
    public static Expression<Func<TEntity, bool>> OneOrManyExpressionBuilder<TEntity, TProperty>(this IList<TProperty> oneOf, Expression<Func<TEntity, TProperty>> property)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "item");
        property = ParameterReplacer.Replace<Func<TEntity, TProperty>, Func<TEntity, TProperty>>(property, property.Parameters[0], parameter);
        if (oneOf.Count == 1)
        {
            var value = oneOf[0];
            if (typeof(TProperty).IsValueType)
            {
                return Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(property.Body, Expression.Constant(value)), parameter);
            }
            else
            {
                return Expression.Lambda<Func<TEntity, bool>>(Expression.ReferenceEqual(property.Body, Expression.Constant(value)), parameter);
            }
        }

        var containsMethodInfo = _containsQueryCache.GetOrAdd(typeof(TProperty), static (key) => _containsMethodGenericCache.MakeGenericMethod(key));

        return Expression.Lambda<Func<TEntity, bool>>(
            Expression.Call(
                null,
                containsMethodInfo,
                Expression.Call(null, _efParameterInstruction.MakeGenericMethod(oneOf.GetType()), Expression.Constant(oneOf)),
                property.Body),
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
