using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;

namespace Jellyfin.Database.Providers.Postgres;

/// <summary>
/// Creates the query SQL generator of Npgsql with an explicit null placement on the orderings that can contain nulls,
/// so that PostgreSQL sorts nulls like SQLite does: first when ascending and last when descending.
/// </summary>
#pragma warning disable EF1001 // The generator of Npgsql and its options are internal and have no public counterpart.
internal sealed class NullOrderingQuerySqlGeneratorFactory : IQuerySqlGeneratorFactory
{
    private readonly QuerySqlGeneratorDependencies _dependencies;
    private readonly IRelationalTypeMappingSource _typeMappingSource;
    private readonly INpgsqlSingletonOptions _npgsqlOptions;

    public NullOrderingQuerySqlGeneratorFactory(
        QuerySqlGeneratorDependencies dependencies,
        IRelationalTypeMappingSource typeMappingSource,
        INpgsqlSingletonOptions npgsqlOptions)
    {
        _dependencies = dependencies;
        _typeMappingSource = typeMappingSource;
        _npgsqlOptions = npgsqlOptions;
    }

    public QuerySqlGenerator Create()
        => new NullOrderingQuerySqlGenerator(_dependencies, _typeMappingSource, _npgsqlOptions);

    private sealed class NullOrderingQuerySqlGenerator : NpgsqlQuerySqlGenerator
    {
        public NullOrderingQuerySqlGenerator(
            QuerySqlGeneratorDependencies dependencies,
            IRelationalTypeMappingSource typeMappingSource,
            INpgsqlSingletonOptions npgsqlOptions)
            : base(dependencies, typeMappingSource, reverseNullOrderingEnabled: false, npgsqlOptions.PostgresVersion)
        {
        }

        protected override Expression VisitOrdering(OrderingExpression orderingExpression)
        {
            var result = base.VisitOrdering(orderingExpression);

            // The index of a non-nullable column is built with NULLS LAST, which a different placement could not use.
            if (orderingExpression.Expression is not ColumnExpression { IsNullable: false })
            {
                Sql.Append(orderingExpression.IsAscending ? " NULLS FIRST" : " NULLS LAST");
            }

            return result;
        }
    }
}
#pragma warning restore EF1001
