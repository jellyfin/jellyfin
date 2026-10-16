using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Database.Providers.SqlServer;

/// <summary>
/// Translates adding a time span to a constant <see cref="DateTime"/> so that the constant is a <c>datetime2</c>.
/// </summary>
/// <remarks>
/// EF Core writes a <see cref="DateTime"/> constant as a string literal, and <c>DATEADD</c> reads a string literal as a
/// <c>datetime</c>, whose range starts in the year 1753. A result before that year fails the whole query (Msg 517), as a
/// sort by premiere date does for an item of the year 1, while <c>datetime2</c>, the type of every date column, covers the
/// years 1 to 9999. The constant is cast to <c>datetime2</c> and everything else is translated as EF Core does it.
/// </remarks>
internal sealed class DateTimeConstantTranslator : IMethodCallTranslator
{
    private static readonly Dictionary<MethodInfo, string> _dateParts = new()
    {
        [Method(nameof(DateTime.AddYears), typeof(int))] = "year",
        [Method(nameof(DateTime.AddMonths), typeof(int))] = "month",
        [Method(nameof(DateTime.AddDays), typeof(double))] = "day",
        [Method(nameof(DateTime.AddHours), typeof(double))] = "hour",
        [Method(nameof(DateTime.AddMinutes), typeof(double))] = "minute",
        [Method(nameof(DateTime.AddSeconds), typeof(double))] = "second",
        [Method(nameof(DateTime.AddMilliseconds), typeof(double))] = "millisecond"
    };

    private readonly ISqlExpressionFactory _sqlExpressionFactory;
    private readonly RelationalTypeMapping _dateTime2;

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeConstantTranslator"/> class.
    /// </summary>
    /// <param name="sqlExpressionFactory">The factory of SQL expressions.</param>
    /// <param name="typeMappingSource">The source of the database types of the provider.</param>
    public DateTimeConstantTranslator(ISqlExpressionFactory sqlExpressionFactory, IRelationalTypeMappingSource typeMappingSource)
    {
        _sqlExpressionFactory = sqlExpressionFactory;
        _dateTime2 = typeMappingSource.FindMapping("datetime2")
            ?? throw new InvalidOperationException("The provider has no mapping for datetime2.");
    }

    /// <inheritdoc/>
    public SqlExpression? Translate(SqlExpression? instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (instance is not SqlConstantExpression { Value: DateTime } constant || !_dateParts.TryGetValue(method, out var datePart))
        {
            return null;
        }

        var typed = new SqlUnaryExpression(ExpressionType.Convert, _sqlExpressionFactory.ApplyTypeMapping(constant, _dateTime2), typeof(DateTime), _dateTime2);
        return _sqlExpressionFactory.Function(
            "DATEADD",
            [_sqlExpressionFactory.Fragment(datePart), _sqlExpressionFactory.Convert(arguments[0], typeof(int)), typed],
            nullable: true,
            argumentsPropagateNullability: [false, true, true],
            typeof(DateTime),
            _dateTime2);
    }

    private static MethodInfo Method(string name, Type argument)
        => typeof(DateTime).GetMethod(name, [argument]) ?? throw new MissingMethodException(nameof(DateTime), name);

    /// <summary>
    /// Adds the translator to the method translators of EF Core.
    /// </summary>
    /// <param name="sqlExpressionFactory">The factory of SQL expressions.</param>
    /// <param name="typeMappingSource">The source of the database types of the provider.</param>
    internal sealed class Plugin(ISqlExpressionFactory sqlExpressionFactory, IRelationalTypeMappingSource typeMappingSource) : IMethodCallTranslatorPlugin
    {
        /// <inheritdoc/>
        public IEnumerable<IMethodCallTranslator> Translators { get; } = [new DateTimeConstantTranslator(sqlExpressionFactory, typeMappingSource)];
    }

    /// <summary>
    /// The options extension that registers <see cref="Plugin"/>. Every instance configures the same services, so all of
    /// them share one internal service provider of EF Core.
    /// </summary>
    internal sealed class Extension : IDbContextOptionsExtension
    {
        private DbContextOptionsExtensionInfo? _info;

        /// <inheritdoc/>
        public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

        /// <inheritdoc/>
        public void ApplyServices(IServiceCollection services)
            => services.AddScoped<IMethodCallTranslatorPlugin, Plugin>();

        /// <inheritdoc/>
        public void Validate(IDbContextOptions options)
        {
        }

        private sealed class ExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
        {
            public override bool IsDatabaseProvider => false;

            public override string LogFragment => string.Empty;

            public override int GetServiceProviderHashCode() => 0;

            public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => other is ExtensionInfo;

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
                => debugInfo["Jellyfin.SqlServer:DateTimeConstantTranslator"] = "1";
        }
    }
}
