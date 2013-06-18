using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Server.Implementations.Persistence
{
    /// <summary>
    /// Class SQLiteExtensions
    /// </summary>
    static class SqliteExtensions
    {
        /// <summary>
        /// Adds the param.
        /// </summary>
        /// <param name="cmd">The CMD.</param>
        /// <param name="param">The param.</param>
        /// <returns>SQLiteParameter.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static SQLiteParameter AddParam(this SQLiteCommand cmd, string param)
        {
            if (string.IsNullOrEmpty(param))
            {
                throw new ArgumentNullException();
            }

            var sqliteParam = new SQLiteParameter(param);
            cmd.Parameters.Add(sqliteParam);
            return sqliteParam;
        }

        /// <summary>
        /// Adds the param.
        /// </summary>
        /// <param name="cmd">The CMD.</param>
        /// <param name="param">The param.</param>
        /// <param name="data">The data.</param>
        /// <returns>SQLiteParameter.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static SQLiteParameter AddParam(this SQLiteCommand cmd, string param, object data)
        {
            if (string.IsNullOrEmpty(param))
            {
                throw new ArgumentNullException();
            }

            var sqliteParam = AddParam(cmd, param);
            sqliteParam.Value = data;
            return sqliteParam;
        }

        /// <summary>
        /// Determines whether the specified conn is open.
        /// </summary>
        /// <param name="conn">The conn.</param>
        /// <returns><c>true</c> if the specified conn is open; otherwise, <c>false</c>.</returns>
        public static bool IsOpen(this SQLiteConnection conn)
        {
            return conn.State == ConnectionState.Open;
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

        /// <summary>
        /// Connects to db.
        /// </summary>
        /// <param name="dbPath">The db path.</param>
        /// <returns>Task{IDbConnection}.</returns>
        /// <exception cref="System.ArgumentNullException">dbPath</exception>
        public static async Task<SQLiteConnection> ConnectToDb(string dbPath)
        {
            if (string.IsNullOrEmpty(dbPath))
            {
                throw new ArgumentNullException("dbPath");
            }

            var connectionstr = new SQLiteConnectionStringBuilder
            {
                PageSize = 4096,
                CacheSize = 4096,
                SyncMode = SynchronizationModes.Off,
                DataSource = dbPath,
                JournalMode = SQLiteJournalModeEnum.Wal
            };

            var connection = new SQLiteConnection(connectionstr.ConnectionString);

            await connection.OpenAsync().ConfigureAwait(false);

            return connection;
        }
    }
}