using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Jellyfin.Database.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Jellyfin.Database.Testing.Conformance;

/// <summary>
/// Kit row: every key and index of the model whose strings all have a maximum length takes a row with each of those
/// strings at its maximum, in characters of three bytes each, keeps apart two rows whose strings differ only in their
/// last character, and finds each of them by its values. An engine that limits the size of an index entry, or indexes
/// only a prefix of a string, fails here.
/// </summary>
/// <remarks>
/// A string in a key or an index that holds free text has no maximum length and is left out: such a column is
/// indexed as it is on SQLite and PostgreSQL, and a provider that cannot do that decides how to index it.
/// </remarks>
public abstract class CompositeKeyConformance : ConformanceTestBase
{
    private static readonly MethodInfo _countMatching = typeof(CompositeKeyConformance).GetMethod(nameof(CountMatching), BindingFlags.NonPublic | BindingFlags.Static)!;

    [ConformanceFact]
    public void EveryBoundedKeyAndIndex_StringsAtTheirMaximumLength_KeepTwoRowsApartAndFindEach()
    {
        var wrong = new List<string>();
        var tested = 0;
        foreach (var table in Tables())
        {
            IEnumerable<(string Kind, IReadOnlyList<IProperty> Properties)> keys =
            [
                .. table.GetKeys().Select(key => ("key", key.Properties)),
                .. table.GetIndexes().Select(index => (index.IsUnique ? "unique index" : "index", index.Properties))
            ];
            foreach (var (kind, properties) in keys)
            {
                var strings = properties.Where(property => property.ClrType == typeof(string)).ToList();
                if (strings.Count == 0 || strings.Any(property => property.GetMaxLength() is null))
                {
                    continue;
                }

                tested++;
                var name = $"{kind} {table.GetTableName()} ({string.Join(", ", properties.Select(property => property.Name))})";
                try
                {
                    // A filler of its own for every key, so that the rows of one key do not match the values of another.
                    var filler = (char)('一' + tested);
                    var first = Rows.Save(table, AtMaximum(strings, filler, '本')).Key;
                    Dictionary<IProperty, object?> firstValues;
                    using (var context = CreateDbContext())
                    {
                        var stored = ModelRowFactory.Find(context, table, first)!;
                        firstValues = properties.ToDictionary(property => property, property => stored.Property(property.Name).CurrentValue);
                    }

                    // The second row takes the other values of the first, except a primary key that the index only
                    // contains, which the second row could not share.
                    var primaryKey = table.FindPrimaryKey()!.Properties;
                    var shared = properties.Except(strings).Where(property => primaryKey.SequenceEqual(properties) || !primaryKey.Contains(property));
                    var secondOverrides = shared.ToDictionary(property => property.Name, property => firstValues[property], StringComparer.Ordinal);
                    foreach (var (property, value) in AtMaximum(strings, filler, '語'))
                    {
                        secondOverrides[property] = value;
                    }

                    var second = Rows.Save(table, secondOverrides).Key;

                    using var verification = CreateDbContext();
                    var secondStored = ModelRowFactory.Find(verification, table, second)!;
                    var secondValues = properties.ToDictionary(property => property, property => secondStored.Property(property.Name).CurrentValue);
                    foreach (var values in new[] { firstValues, secondValues })
                    {
                        var found = (int)_countMatching.MakeGenericMethod(table.ClrType).Invoke(null, [verification, values])!;
                        if (found != 1)
                        {
                            wrong.Add($"{name}: found {found} row(s) by the values of one row");
                        }
                    }
                }
                catch (Exception ex) when (ex is DbUpdateException or InvalidOperationException)
                {
                    wrong.Add($"{name}: {ex.GetType().Name}: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        AssertNone(wrong);
        Assert.True(tested > 0, "The model has no key or index of bounded strings to test.");
    }

    private static Dictionary<string, object?> AtMaximum(IEnumerable<IProperty> strings, char filler, char last)
        => strings.ToDictionary(property => property.Name, property => (object?)(new string(filler, property.GetMaxLength()!.Value - 1) + last), StringComparer.Ordinal);

    private static int CountMatching<TEntity>(JellyfinDbContext context, Dictionary<IProperty, object?> values)
        where TEntity : class
    {
        var entity = Expression.Parameter(typeof(TEntity), "e");
        Expression? body = null;
        foreach (var (property, value) in values)
        {
            // Each value is captured as a closure variable, so that EF Core sends it as a parameter.
            var box = new StrongBox<object?>(value);
            var column = Expression.Call(typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(property.ClrType), entity, Expression.Constant(property.Name));
            var parameter = Expression.Convert(Expression.Field(Expression.Constant(box), nameof(StrongBox<object>.Value)), property.ClrType);
            var equal = Expression.Equal(column, parameter);
            body = body is null ? equal : Expression.AndAlso(body, equal);
        }

        return context.Set<TEntity>().Count(Expression.Lambda<Func<TEntity, bool>>(body!, entity));
    }
}
