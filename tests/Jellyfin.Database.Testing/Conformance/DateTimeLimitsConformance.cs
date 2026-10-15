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
/// Kit row: every date column of the model stores <see cref="DateTime.MinValue"/> and <see cref="DateTime.MaxValue"/>
/// of any kind exactly, gives them back as UTC, and finds them by equality, while the process runs in a zone that is
/// not UTC.
/// </summary>
/// <remarks>
/// The row changes the time zone of the whole process, so the derived class has to be in a collection that does not
/// run in parallel with any other test; the constructor checks that it is in one.
/// </remarks>
public abstract class DateTimeLimitsConformance : ConformanceTestBase
{
    private static readonly MethodInfo _countEqual = typeof(DateTimeLimitsConformance).GetMethod(nameof(CountEqual), BindingFlags.NonPublic | BindingFlags.Static)!;

    protected DateTimeLimitsConformance()
    {
        Assert.True(
            GetType().GetCustomAttributes(typeof(CollectionAttribute), true).Length > 0,
            $"{GetType().Name} changes the time zone of the process, so it has to be in a collection that runs alone.");
    }

    [ConformanceTheory]
    [InlineData("America/New_York", 0, DateTimeKind.Utc)]
    [InlineData("America/New_York", 0, DateTimeKind.Local)]
    [InlineData("America/New_York", 0, DateTimeKind.Unspecified)]
    [InlineData("America/New_York", 3155378975999999999, DateTimeKind.Utc)]
    [InlineData("America/New_York", 3155378975999999999, DateTimeKind.Local)]
    [InlineData("America/New_York", 3155378975999999999, DateTimeKind.Unspecified)]
    [InlineData("Asia/Kolkata", 0, DateTimeKind.Utc)]
    [InlineData("Asia/Kolkata", 0, DateTimeKind.Local)]
    [InlineData("Asia/Kolkata", 0, DateTimeKind.Unspecified)]
    [InlineData("Asia/Kolkata", 3155378975999999999, DateTimeKind.Utc)]
    [InlineData("Asia/Kolkata", 3155378975999999999, DateTimeKind.Local)]
    [InlineData("Asia/Kolkata", 3155378975999999999, DateTimeKind.Unspecified)]
    public void EveryDateColumn_LimitOfAnyKindInAZoneThatIsNotUtc_IsStoredExactlyAndFoundByEquality(string zoneId, long ticks, DateTimeKind kind)
    {
        using var zone = new TemporaryTimeZone(zoneId);
        var value = new DateTime(ticks, kind);
        var wrong = new List<string>();
        var columns = 0;

        foreach (var table in Tables())
        {
            var dates = table.GetProperties()
                .Where(property => (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(DateTime))
                .ToList();
            if (dates.Count == 0)
            {
                continue;
            }

            columns += dates.Count;
            var key = Rows.Save(table, dates.ToDictionary(property => property.Name, _ => (object?)value, StringComparer.Ordinal)).Key;

            using var context = CreateDbContext();
            var stored = ModelRowFactory.Find(context, table, key)!;
            foreach (var property in dates)
            {
                var name = $"{table.GetTableName()}.{property.GetColumnName()}";
                if (stored.Property(property.Name).CurrentValue is not DateTime read || read.Ticks != ticks || read.Kind != DateTimeKind.Utc)
                {
                    wrong.Add($"{name}: read back {stored.Property(property.Name).CurrentValue:o}");
                }

                var found = (int)_countEqual.MakeGenericMethod(table.ClrType, property.ClrType).Invoke(null, [context, property.Name, value])!;
                if (found != 1)
                {
                    wrong.Add($"{name}: found {found} row(s) by equality");
                }
            }
        }

        AssertNone(wrong);
        Assert.True(columns > 0, "The model has no date column to test.");
    }

    private static int CountEqual<TEntity, TValue>(JellyfinDbContext context, string propertyName, DateTime value)
        where TEntity : class
    {
        // The value is captured as a closure variable, so that EF Core sends it as a parameter, as the server does.
        var box = new StrongBox<TValue>((TValue)(object)value);
        var entity = Expression.Parameter(typeof(TEntity), "e");
        var column = Expression.Call(typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(TValue)), entity, Expression.Constant(propertyName));
        var parameter = Expression.Field(Expression.Constant(box), nameof(StrongBox<TValue>.Value));
        var predicate = Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(column, parameter), entity);
        return context.Set<TEntity>().Count(predicate);
    }
}
