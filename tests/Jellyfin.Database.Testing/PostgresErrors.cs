using System;
using System.Linq;
using Npgsql;

namespace Jellyfin.Database.Testing;

internal static class PostgresErrors
{
    public static bool HasSqlState(Exception exception, string sqlState, params string[] messageContainsAny)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres
                && string.Equals(postgres.SqlState, sqlState, StringComparison.Ordinal)
                && (messageContainsAny.Length == 0 || messageContainsAny.Any(e => postgres.MessageText.Contains(e, StringComparison.Ordinal))))
            {
                return true;
            }
        }

        return false;
    }
}
