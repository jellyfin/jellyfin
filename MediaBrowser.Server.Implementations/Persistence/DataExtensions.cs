using System.Text;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Data;
using System.IO;

namespace MediaBrowser.Server.Implementations.Persistence
{
    static class DataExtensions
    {
        /// <summary>
        /// Determines whether the specified conn is open.
        /// </summary>
        /// <param name="conn">The conn.</param>
        /// <returns><c>true</c> if the specified conn is open; otherwise, <c>false</c>.</returns>
        public static bool IsOpen(this IDbConnection conn)
        {
            return conn.State == ConnectionState.Open;
        }

        public static IDataParameter GetParameter(this IDbCommand cmd, int index)
        {
            return (IDataParameter)cmd.Parameters[index];
        }

        public static IDataParameter Add(this IDataParameterCollection paramCollection, IDbCommand cmd, string name, DbType type)
        {
            var param = cmd.CreateParameter();

            param.ParameterName = name;
            param.DbType = type;

            paramCollection.Add(param);

            return param;
        }

        public static IDataParameter Add(this IDataParameterCollection paramCollection, IDbCommand cmd, string name)
        {
            var param = cmd.CreateParameter();

            param.ParameterName = name;

            paramCollection.Add(param);

            return param;
        }


        /// <summary>
        /// Gets a stream from a DataReader at a given ordinal
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="ordinal">The ordinal.</param>
        /// <returns>Stream.</returns>
        /// <exception cref="System.ArgumentNullException">reader</exception>
        public static Stream GetMemoryStream(this IDataReader reader, int ordinal)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            var memoryStream = new MemoryStream();
            var num = 0L;
            var array = new byte[4096];
            long bytes;
            do
            {
                bytes = reader.GetBytes(ordinal, num, array, 0, array.Length);
                memoryStream.Write(array, 0, (int)bytes);
                num += bytes;
            }
            while (bytes > 0L);
            memoryStream.Position = 0;
            return memoryStream;
        }

        /// <summary>
        /// Runs the queries.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="queries">The queries.</param>
        /// <param name="logger">The logger.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        /// <exception cref="System.ArgumentNullException">queries</exception>
        public static void RunQueries(this IDbConnection connection, string[] queries, ILogger logger)
        {
            if (queries == null)
            {
                throw new ArgumentNullException("queries");
            }

            using (var tran = connection.BeginTransaction())
            {
                try
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        foreach (var query in queries)
                        {
                            cmd.Transaction = tran;
                            cmd.CommandText = query;
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tran.Commit();
                }
                catch (Exception e)
                {
                    logger.ErrorException("Error running queries", e);
                    tran.Rollback();
                    throw;
                }
            }
        }

        public static void Attach(IDbConnection db, string path, string alias)
        {
            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText = string.Format("attach @dbPath as {0};", alias);
                cmd.Parameters.Add(cmd, "@dbPath", DbType.String);
                cmd.GetParameter(0).Value = path;

                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Serializes to bytes.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Byte[][].</returns>
        /// <exception cref="System.ArgumentNullException">obj</exception>
        public static byte[] SerializeToBytes(this IJsonSerializer json, object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            using (var stream = new MemoryStream())
            {
                json.SerializeToStream(obj, stream);
                return stream.ToArray();
            }
        }

        public static void AddColumn(this IDbConnection connection, ILogger logger, string table, string columnName, string type)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(" + table + ")";

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(1))
                        {
                            var name = reader.GetString(1);

                            if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            var builder = new StringBuilder();

            builder.AppendLine("alter table " + table);
            builder.AppendLine("add column " + columnName + " " + type);

            connection.RunQueries(new[] { builder.ToString() }, logger);
        }
    }
}