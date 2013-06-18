using System;
using System.Data;
using System.Data.SQLite;

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
    }
}