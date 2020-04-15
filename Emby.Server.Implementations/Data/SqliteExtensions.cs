#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using SQLitePCL.pretty;

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

        public static void RunQueries(this SQLiteDatabaseConnection connection, string[] queries)
        {
            if (queries == null)
            {
                throw new ArgumentNullException(nameof(queries));
            }

            connection.RunInTransaction(conn =>
            {
                conn.ExecuteAll(string.Join(";", queries));
            });
        }

        public static Guid ReadGuidFromBlob(this IResultSetValue result)
        {
            return new Guid(result.ToBlob());
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

        public static DateTime ReadDateTime(this IResultSetValue result)
        {
            var dateText = result.ToString();

            return DateTime.ParseExact(
                dateText,
                _datetimeFormats,
                DateTimeFormatInfo.InvariantInfo,
                DateTimeStyles.None).ToUniversalTime();
        }

        public static DateTime? TryReadDateTime(this IResultSetValue result)
        {
            var dateText = result.ToString();

            if (DateTime.TryParseExact(dateText, _datetimeFormats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out var dateTimeResult))
            {
                return dateTimeResult.ToUniversalTime();
            }

            return null;
        }

        public static void Attach(SQLiteDatabaseConnection db, string path, string alias)
        {
            var commandText = string.Format(
                CultureInfo.InvariantCulture,
                "attach @path as {0};",
                alias);

            using (var statement = db.PrepareStatement(commandText))
            {
                statement.TryBind("@path", path);
                statement.MoveNext();
            }
        }

        public static bool IsDBNull(this IReadOnlyList<IResultSetValue> result, int index)
        {
            return result[index].SQLiteType == SQLiteType.Null;
        }

        public static string GetString(this IReadOnlyList<IResultSetValue> result, int index)
        {
            return result[index].ToString();
        }

        public static bool GetBoolean(this IReadOnlyList<IResultSetValue> result, int index)
        {
            return result[index].ToBool();
        }

        public static int GetInt32(this IReadOnlyList<IResultSetValue> result, int index)
        {
            return result[index].ToInt();
        }

        public static long GetInt64(this IReadOnlyList<IResultSetValue> result, int index)
        {
            return result[index].ToInt64();
        }

        public static float GetFloat(this IReadOnlyList<IResultSetValue> result, int index)
        {
            return result[index].ToFloat();
        }

        public static Guid GetGuid(this IReadOnlyList<IResultSetValue> result, int index)
        {
            return result[index].ReadGuidFromBlob();
        }

        private static void CheckName(string name)
        {
#if DEBUG
            throw new ArgumentException("Invalid param name: " + name, nameof(name));
#endif
        }

        public static void TryBind(this IStatement statement, string name, double value)
        {
            if (statement.BindParameters.TryGetValue(name, out IBindParameter bindParam))
            {
                bindParam.Bind(value);
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, string name, string value)
        {
            if (statement.BindParameters.TryGetValue(name, out IBindParameter bindParam))
            {
                if (value == null)
                {
                    bindParam.BindNull();
                }
                else
                {
                    bindParam.Bind(value);
                }
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, string name, bool value)
        {
            if (statement.BindParameters.TryGetValue(name, out IBindParameter bindParam))
            {
                bindParam.Bind(value);
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, string name, float value)
        {
            if (statement.BindParameters.TryGetValue(name, out IBindParameter bindParam))
            {
                bindParam.Bind(value);
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, string name, int value)
        {
            if (statement.BindParameters.TryGetValue(name, out IBindParameter bindParam))
            {
                bindParam.Bind(value);
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, string name, Guid value)
        {
            if (statement.BindParameters.TryGetValue(name, out IBindParameter bindParam))
            {
                bindParam.Bind(value.ToByteArray());
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, string name, DateTime value)
        {
            if (statement.BindParameters.TryGetValue(name, out IBindParameter bindParam))
            {
                bindParam.Bind(value.ToDateTimeParamValue());
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, string name, long value)
        {
            if (statement.BindParameters.TryGetValue(name, out IBindParameter bindParam))
            {
                bindParam.Bind(value);
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, string name, ReadOnlySpan<byte> value)
        {
            if (statement.BindParameters.TryGetValue(name, out IBindParameter bindParam))
            {
                bindParam.Bind(value);
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBindNull(this IStatement statement, string name)
        {
            if (statement.BindParameters.TryGetValue(name, out IBindParameter bindParam))
            {
                bindParam.BindNull();
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, string name, DateTime? value)
        {
            if (value.HasValue)
            {
                TryBind(statement, name, value.Value);
            }
            else
            {
                TryBindNull(statement, name);
            }
        }

        public static void TryBind(this IStatement statement, string name, Guid? value)
        {
            if (value.HasValue)
            {
                TryBind(statement, name, value.Value);
            }
            else
            {
                TryBindNull(statement, name);
            }
        }

        public static void TryBind(this IStatement statement, string name, double? value)
        {
            if (value.HasValue)
            {
                TryBind(statement, name, value.Value);
            }
            else
            {
                TryBindNull(statement, name);
            }
        }

        public static void TryBind(this IStatement statement, string name, int? value)
        {
            if (value.HasValue)
            {
                TryBind(statement, name, value.Value);
            }
            else
            {
                TryBindNull(statement, name);
            }
        }

        public static void TryBind(this IStatement statement, string name, float? value)
        {
            if (value.HasValue)
            {
                TryBind(statement, name, value.Value);
            }
            else
            {
                TryBindNull(statement, name);
            }
        }

        public static void TryBind(this IStatement statement, string name, bool? value)
        {
            if (value.HasValue)
            {
                TryBind(statement, name, value.Value);
            }
            else
            {
                TryBindNull(statement, name);
            }
        }

        public static IEnumerable<IReadOnlyList<IResultSetValue>> ExecuteQuery(this IStatement statement)
        {
            while (statement.MoveNext())
            {
                yield return statement.Current;
            }
        }
    }
}
