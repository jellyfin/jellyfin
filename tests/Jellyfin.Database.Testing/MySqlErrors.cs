using System;
using MySqlConnector;

namespace Jellyfin.Database.Testing;

internal static class MySqlErrors
{
    public static bool HasErrorCode(Exception exception, MySqlErrorCode code)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is MySqlException mySql && mySql.ErrorCode == code)
            {
                return true;
            }
        }

        return false;
    }
}
