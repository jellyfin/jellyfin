using System;
using System.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Data
{
    public static class SqliteExtensions
    {
        public static void RunQueries(this SQLiteDatabaseConnection connection, string[] queries)
        {
            if (queries == null)
            {
                throw new ArgumentNullException("queries");
            }

            connection.RunInTransaction(conn =>
            {
                //foreach (var query in queries)
                //{
                //    conn.Execute(query);
                //}
                conn.ExecuteAll(string.Join(";", queries));
            });
        }

        public static byte[] ToGuidParamValue(this string str)
        {
            return ToGuidParamValue(new Guid(str));
        }

        public static byte[] ToGuidParamValue(this Guid guid)
        {
            return guid.ToByteArray();
        }

        public static Guid ReadGuid(this IResultSetValue result)
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

        private static string GetDateTimeKindFormat(
           DateTimeKind kind)
        {
            return (kind == DateTimeKind.Utc) ? _datetimeFormatUtc : _datetimeFormatLocal;
        }

        /// <summary>
        /// An array of ISO-8601 DateTime formats that we support parsing.
        /// </summary>
        private static string[] _datetimeFormats = new string[] {
      "THHmmssK",
      "THHmmK",
      "HH:mm:ss.FFFFFFFK",
      "HH:mm:ssK",
      "HH:mmK",
      "yyyy-MM-dd HH:mm:ss.FFFFFFFK", /* NOTE: UTC default (5). */
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
      "yyyy-MM-dd HH:mm:ss.FFFFFFF", /* NOTE: Non-UTC default (19). */
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

        private static string _datetimeFormatUtc = _datetimeFormats[5];
        private static string _datetimeFormatLocal = _datetimeFormats[19];

        public static DateTime ReadDateTime(this IResultSetValue result)
        {
            var dateText = result.ToString();

            return DateTime.ParseExact(
                dateText, _datetimeFormats,
                DateTimeFormatInfo.InvariantInfo,
                DateTimeStyles.None).ToUniversalTime();
        }

        /// <summary>
        /// Serializes to bytes.
        /// </summary>
        /// <returns>System.Byte[][].</returns>
        /// <exception cref="System.ArgumentNullException">obj</exception>
        public static byte[] SerializeToBytes(this IJsonSerializer json, object obj, IMemoryStreamFactory streamProvider)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            using (var stream = streamProvider.CreateNew())
            {
                json.SerializeToStream(obj, stream);
                return stream.ToArray();
            }
        }
    }
}
