#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Emby.Server.Implementations.Data
{
    public static class SqliteExtensions
    {
        private const string DatetimeFormatUtc = "yyyy-MM-dd HH:mm:ss.FFFFFFFK";
        private const string DatetimeFormatLocal = "yyyy-MM-dd HH:mm:ss.FFFFFFF";

        /// <summary>
        /// An array of ISO-8601 DateTime formats that we support parsing.
        /// </summary>
        private static readonly string[] _datetimeFormats = new string[]
        {
            "THHmmssK",
            "THHmmK",
            "HH:mm:ss.FFFFFFFK",
            "HH:mm:ssK",
            "HH:mmK",
            DatetimeFormatUtc,
            "yyyy-MM-dd HH:mm:ssK",
            "yyyy-MM-dd HH:mmK",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
            "yyyy-MM-ddTHH:mmK",
            "yyyy-MM-ddTHH:mm:ssK",
            "yyyyMMddHHmmssK",
            "yyyyMMddHHmmK",
            "yyyyMMddTHHmmssFFFFFFFK",
            "THHmmss",
            "THHmm",
            "HH:mm:ss.FFFFFFF",
            "HH:mm:ss",
            "HH:mm",
            DatetimeFormatLocal,
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
            "yyyy-MM-ddTHH:mm",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyyMMddHHmmss",
            "yyyyMMddHHmm",
            "yyyyMMddTHHmmssFFFFFFF",
            "yyyy-MM-dd",
            "yyyyMMdd",
            "yy-MM-dd"
        };

        public static IEnumerable<SqliteDataReader> Query(this SqliteConnection sqliteConnection, string commandText)
        {
            if (sqliteConnection.State != ConnectionState.Open)
            {
                sqliteConnection.Open();
            }

            using var command = sqliteConnection.CreateCommand();
            command.CommandText = commandText;
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    yield return reader;
                }
            }
        }

        public static void Execute(this SqliteConnection sqliteConnection, string commandText)
        {
            using var command = sqliteConnection.CreateCommand();
            command.CommandText = commandText;
            command.ExecuteNonQuery();
        }

        public static string ToDateTimeParamValue(this DateTime dateValue)
        {
            var kind = DateTimeKind.Utc;

            return (dateValue.Kind == DateTimeKind.Unspecified)
                ? DateTime.SpecifyKind(dateValue, kind).ToString(
                    GetDateTimeKindFormat(kind),
                    CultureInfo.InvariantCulture)
                : dateValue.ToString(
                    GetDateTimeKindFormat(dateValue.Kind),
                    CultureInfo.InvariantCulture);
        }

        private static string GetDateTimeKindFormat(DateTimeKind kind)
            => (kind == DateTimeKind.Utc) ? DatetimeFormatUtc : DatetimeFormatLocal;

        public static bool TryReadDateTime(this SqliteDataReader reader, int index, out DateTime result)
        {
            if (reader.IsDBNull(index))
            {
                result = default;
                return false;
            }

            var dateText = reader.GetString(index);

            if (DateTime.TryParseExact(dateText, _datetimeFormats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AdjustToUniversal, out var dateTimeResult))
            {
                // If the resulting DateTimeKind is Unspecified it is actually Utc.
                // This is required downstream for the Json serializer.
                if (dateTimeResult.Kind == DateTimeKind.Unspecified)
                {
                    dateTimeResult = DateTime.SpecifyKind(dateTimeResult, DateTimeKind.Utc);
                }

                result = dateTimeResult;
                return true;
            }

            result = default;
            return false;
        }

        public static bool TryGetGuid(this SqliteDataReader reader, int index, out Guid result)
        {
            if (reader.IsDBNull(index))
            {
                result = default;
                return false;
            }

            try
            {
                result = reader.GetGuid(index);
                return true;
            }
            catch
            {
                result = Guid.Empty;
                return false;
            }
        }

        public static bool TryGetString(this SqliteDataReader reader, int index, out string result)
        {
            result = string.Empty;

            if (reader.IsDBNull(index))
            {
                return false;
            }

            result = reader.GetString(index);
            return true;
        }

        public static bool TryGetBoolean(this SqliteDataReader reader, int index, out bool result)
        {
            if (reader.IsDBNull(index))
            {
                result = default;
                return false;
            }

            result = reader.GetBoolean(index);
            return true;
        }

        public static bool TryGetInt32(this SqliteDataReader reader, int index, out int result)
        {
            if (reader.IsDBNull(index))
            {
                result = default;
                return false;
            }

            result = reader.GetInt32(index);
            return true;
        }

        public static bool TryGetInt64(this SqliteDataReader reader, int index, out long result)
        {
            if (reader.IsDBNull(index))
            {
                result = default;
                return false;
            }

            result = reader.GetInt64(index);
            return true;
        }

        public static bool TryGetSingle(this SqliteDataReader reader, int index, out float result)
        {
            if (reader.IsDBNull(index))
            {
                result = default;
                return false;
            }

            result = reader.GetFloat(index);
            return true;
        }

        public static bool TryGetDouble(this SqliteDataReader reader, int index, out double result)
        {
            if (reader.IsDBNull(index))
            {
                result = default;
                return false;
            }

            result = reader.GetDouble(index);
            return true;
        }

        public static void TryBind(this SqliteCommand statement, string name, Guid value)
        {
            statement.TryBind(name, value, true);
        }

        public static void TryBind(this SqliteCommand statement, string name, object? value, bool isBlob = false)
        {
            var preparedValue = value ?? DBNull.Value;
            if (statement.Parameters.Contains(name))
            {
                statement.Parameters[name].Value = preparedValue;
            }
            else
            {
                // Blobs aren't always detected automatically
                if (isBlob)
                {
                    statement.Parameters.Add(new SqliteParameter(name, SqliteType.Blob) { Value = value });
                }
                else
                {
                    statement.Parameters.AddWithValue(name, preparedValue);
                }
            }
        }

        public static void TryBindNull(this SqliteCommand statement, string name)
        {
            statement.TryBind(name, DBNull.Value);
        }

        public static IEnumerable<SqliteDataReader> ExecuteQuery(this SqliteCommand command)
        {
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    yield return reader;
                }
            }
        }

        public static int SelectScalarInt(this SqliteCommand command)
        {
            var result = command.ExecuteScalar();
            // Can't be null since the method is used to retrieve Count
            return Convert.ToInt32(result!, CultureInfo.InvariantCulture);
        }

        public static SqliteCommand PrepareStatement(this SqliteConnection sqliteConnection, string sql)
        {
            var command = sqliteConnection.CreateCommand();
            command.CommandText = sql;
            return command;
        }
    }
}
