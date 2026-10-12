namespace Jellyfin.Database.Providers.Postgres.Migrations;

/// <summary>
/// The SQL that gives PostgreSQL a <c>min</c> aggregate over <c>uuid</c>, which it does not have. Queries that group items
/// and pick the lowest id of each group depend on it.
/// </summary>
/// <remarks>
/// There is deliberately no <c>max</c>. The objects are created in the schema of the migrated tables and need neither a
/// superuser nor an extension, so a dump of the database carries them.
/// </remarks>
internal static class PostgresUuidAggregate
{
    /// <summary>
    /// The name of the function the aggregate is built on.
    /// </summary>
    public const string FunctionName = "jf_uuid_smaller";

    /// <summary>
    /// Creates the function and the aggregate, unless <c>min(uuid)</c> already resolves. A later PostgreSQL version may
    /// bring its own, which must not be shadowed. Both are created in <c>current_schema()</c>, where the migrated tables
    /// go as well, and every reference between them is qualified, so a changed search path cannot redirect them.
    /// </summary>
    public const string CreateSql = """
        DO $jf$
        DECLARE
            schema_name text := format('%I', current_schema());
        BEGIN
            IF to_regprocedure('min(uuid)') IS NULL THEN
                EXECUTE format(
                    'CREATE FUNCTION %s.jf_uuid_smaller(a uuid, b uuid) RETURNS uuid '
                    || 'LANGUAGE sql IMMUTABLE STRICT PARALLEL SAFE '
                    || 'AS $body$ SELECT CASE WHEN b < a THEN b ELSE a END $body$',
                    schema_name);

                EXECUTE format(
                    'CREATE AGGREGATE %s.min(uuid) ('
                    || 'SFUNC = %s.jf_uuid_smaller, STYPE = uuid, COMBINEFUNC = %s.jf_uuid_smaller, '
                    || 'SORTOP = OPERATOR(pg_catalog.<), PARALLEL = SAFE)',
                    schema_name, schema_name, schema_name);
            END IF;
        END
        $jf$;
        """;

    /// <summary>
    /// Drops the aggregate if it is built on our function, and then the function, both in <c>current_schema()</c>.
    /// Nothing is dropped with <c>CASCADE</c>: when an object of the administrator, such as a view, depends on the
    /// aggregate or the function, the statement fails as a whole and leaves everything in place. A <c>min(uuid)</c>
    /// that is not built on our function is never touched.
    /// </summary>
    public const string DropSql = """
        DO $jf$
        DECLARE
            schema_name text := format('%I', current_schema());
        BEGIN
            IF EXISTS (
                SELECT 1
                FROM pg_aggregate
                WHERE aggfnoid::oid = to_regprocedure(schema_name || '.min(uuid)')::oid
                  AND aggtransfn::oid = to_regprocedure(schema_name || '.jf_uuid_smaller(uuid, uuid)')::oid) THEN
                EXECUTE format('DROP AGGREGATE %s.min(uuid) RESTRICT', schema_name);
            END IF;

            IF to_regprocedure(schema_name || '.jf_uuid_smaller(uuid, uuid)') IS NOT NULL THEN
                EXECUTE format('DROP FUNCTION %s.jf_uuid_smaller(uuid, uuid) RESTRICT', schema_name);
            END IF;
        END
        $jf$;
        """;
}
