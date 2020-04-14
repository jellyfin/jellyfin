using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MediaBrowser.Model.Serialization;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Data
{
    /// <summary>
    /// A utility class containing methods related to database queries.
    /// </summary>
    public static class SqliteExtensions
    {
        private const string DatetimeFormatUtc = "yyyy-MM-dd HH:mm:ss.FFFFFFFK";
        private const string DatetimeFormatLocal = "yyyy-MM-dd HH:mm:ss.FFFFFFF";

        /// <summary>
        /// An array of ISO-8601 DateTime formats that we support parsing.
        /// </summary>
        private static readonly string[] _datetimeFormats =
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

        /// <summary>
        /// Runs the provided queries.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="queries">The queries.</param>
        /// <exception cref="ArgumentNullException">If queries is null.</exception>
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

        /// <summary>
        /// Reads a <see cref="Guid"/> from the provided blob.
        /// </summary>
        /// <param name="result">the blob.</param>
        /// <returns>The Guid.</returns>
        public static Guid ReadGuidFromBlob(this IResultSetValue result)
        {
            return new Guid(result.ToBlob());
        }

        /// <summary>
        /// Converts a <see cref="DateTime"/> object to a SQL-friendly string.
        /// </summary>
        /// <param name="dateValue">The provided date.</param>
        /// <returns>A SQL-friendly string.</returns>
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

        /// <summary>
        /// Creates a <see cref="DateTime"/> object from a SQL query result.
        /// </summary>
        /// <param name="result">The query result.</param>
        /// <returns>A <see cref="DateTime"/> object.</returns>
        public static DateTime ReadDateTime(this IResultSetValue result)
        {
            var dateText = result.ToString();

            return DateTime.ParseExact(
                dateText,
                _datetimeFormats,
                DateTimeFormatInfo.InvariantInfo,
                DateTimeStyles.None).ToUniversalTime();
        }

        /// <summary>
        /// Attempts to read a DateTime from the provided result.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>A <see cref="DateTime"/> object, or null</returns>
        public static DateTime? TryReadDateTime(this IResultSetValue result)
        {
            var dateText = result.ToString();

            if (DateTime.TryParseExact(dateText, _datetimeFormats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out var dateTimeResult))
            {
                return dateTimeResult.ToUniversalTime();
            }

            return null;
        }

        /// <summary>
        /// Serializes to bytes.
        /// </summary>
        /// <returns>System.Byte[][].</returns>
        /// <exception cref="ArgumentNullException">If obj is null.</exception>
        public static byte[] SerializeToBytes(this IJsonSerializer json, object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            using var stream = new MemoryStream();
            json.SerializeToStream(obj, stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Attaches a statement based on the provided alias at the specified path.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="path">The path.</param>
        /// <param name="alias">The alias.</param>
        public static void Attach(SQLiteDatabaseConnection db, string path, string alias)
        {
            var commandText = string.Format(
                CultureInfo.InvariantCulture,
                "attach @path as {0};",
                alias);

            using var statement = db.PrepareStatement(commandText);
            statement.TryBind("@path", path);
            statement.MoveNext();
        }

        /// <summary>
        /// Returns whether the result at the provided index is null.
        /// </summary>
        /// <param name="result">The results.</param>
        /// <param name="index">The index.</param>
        /// <returns>Whether the result at the provided index is null.</returns>
        public static bool IsDBNull(this IReadOnlyList<IResultSetValue> result, int index)
        {
            return result[index].SQLiteType == SQLiteType.Null;
        }

        /// <summary>
        /// Returns the result at the provided index as a string.
        /// </summary>
        /// <param name="result">The results.</param>
        /// <param name="index">The index.</param>
        /// <returns>A string representation of the specified result.</returns>
        public static string GetString(this IReadOnlyList<IResultSetValue> result, int index)
        {
            return result[index].ToString();
        }

        /// <summary>
        /// Returns the result at the provided index as a boolean.
        /// </summary>
        /// <param name="result">The results.</param>
        /// <param name="index">The index.</param>
        /// <returns>A boolean representation of the specified result.</returns>
        public static bool GetBoolean(this IReadOnlyList<IResultSetValue> result, int index)
        {
            return result[index].ToBool();
        }

        /// <summary>
        /// Returns the result at the specified index as a 32-bit integer.
        /// </summary>
        /// <param name="result">The results.</param>
        /// <param name="index">The index.</param>
        /// <returns>A 32-bit integer representation of the specified result.</returns>
        public static int GetInt32(this IReadOnlyList<IResultSetValue> result, int index)
        {
            return result[index].ToInt();
        }

        /// <summary>
        /// Returns the result at the specified index as a 64-bit integer.
        /// </summary>
        /// <param name="result">The results.</param>
        /// <param name="index">The index.</param>
        /// <returns>A 64-bit integer representation of the specified result.</returns>
        public static long GetInt64(this IReadOnlyList<IResultSetValue> result, int index)
        {
            return result[index].ToInt64();
        }

        /// <summary>
        /// Returns the result at the specified index as a float.
        /// </summary>
        /// <param name="result">The results.</param>
        /// <param name="index">The index.</param>
        /// <returns>A float representation of the specified result.</returns>
        public static float GetFloat(this IReadOnlyList<IResultSetValue> result, int index)
        {
            return result[index].ToFloat();
        }

        /// <summary>
        /// Returns the result at the specified index as a Guid.
        /// </summary>
        /// <param name="result">The results.</param>
        /// <param name="index">The index.</param>
        /// <returns>A Guid representation of the specified result.</returns>
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

        /// <summary>
        /// Tries to bind the provided value to the statement based on the given name.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Tries to bind the provided value to the statement based on the given name.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Tries to bind the provided value to the statement based on the given name.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Tries to bind the provided value to the statement based on the given name.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Tries to bind the provided value to the statement based on the given name.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Tries to bind the provided value to the statement based on the given name.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Tries to bind the provided value to the statement based on the given name.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Tries to bind the provided value to the statement based on the given name.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Tries to bind the provided value to the statement based on the given name.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Tries to bind null to the statement based on the given name.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="name">The name.</param>
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

        /// <summary>
        /// Tries to bind the provided value to the statement based on the given name.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Tries to bind the provided value to the statement based on the given name.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Tries to bind the provided value to the statement based on the given name.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Tries to bind the provided value to the statement based on the given name.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Tries to bind the provided value to the statement based on the given name.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Tries to bind the provided value to the statement based on the given name.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Executes the provided statement.
        /// </summary>
        /// <param name="This">The statement to execute.</param>
        /// <returns>A lazily-evaluated <see cref="IEnumerable{T}"/> containing the results of the statement.</returns>
        public static IEnumerable<IReadOnlyList<IResultSetValue>> ExecuteQuery(this IStatement This)
        {
            while (This.MoveNext())
            {
                yield return This.Current;
            }
        }
    }
}
