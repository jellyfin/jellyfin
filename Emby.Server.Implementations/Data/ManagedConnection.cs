using System;
using System.Collections.Generic;
using System.Threading;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Data
{
    /// <summary>
    /// A managed database connection.
    /// </summary>
    public class ManagedConnection : IDisposable
    {
        private SQLiteDatabaseConnection _db;
        private readonly SemaphoreSlim _writeLock;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedConnection"/> class.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="writeLock">The write lock.</param>
        public ManagedConnection(SQLiteDatabaseConnection db, SemaphoreSlim writeLock)
        {
            _db = db;
            _writeLock = writeLock;
        }

        /// <summary>
        /// Prepares the provided SQL statement.
        /// </summary>
        /// <param name="sql">The SQL statement.</param>
        /// <returns>The prepared statement.</returns>
        public IStatement PrepareStatement(string sql)
        {
            return _db.PrepareStatement(sql);
        }

        /// <summary>
        /// Prepares all of the provided SQL statements.
        /// </summary>
        /// <param name="sql">The SQL statements.</param>
        /// <returns>A lazily evaluated <see cref="IEnumerable{T}"/>.</returns>
        public IEnumerable<IStatement> PrepareAll(string sql)
        {
            return _db.PrepareAll(sql);
        }

        /// <summary>
        /// Executes the provided SQL statements.
        /// </summary>
        /// <param name="sql">The SQL statements.</param>
        public void ExecuteAll(string sql)
        {
            _db.ExecuteAll(sql);
        }

        /// <summary>
        /// Executes a SQL statement with the provided parameters
        /// </summary>
        /// <param name="sql">The SQl statement.</param>
        /// <param name="values">The Bind parameter values</param>
        public void Execute(string sql, params object[] values)
        {
            _db.Execute(sql, values);
        }

        /// <summary>
        /// Runs the provided queries.
        /// </summary>
        /// <param name="sql">The queries.</param>
        public void RunQueries(string[] sql)
        {
            _db.RunQueries(sql);
        }

        /// <summary>
        /// Runs the provided action in a transaction.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <param name="mode">The mode.</param>
        public void RunInTransaction(Action<IDatabaseConnection> action, TransactionMode mode)
        {
            _db.RunInTransaction(action, mode);
        }

        /// <summary>
        /// Runs the provided function in a transaction and returns the result.
        /// </summary>
        /// <param name="action">The function to run</param>
        /// <param name="mode">The transaction mode.</param>
        /// <typeparam name="T">The result type</typeparam>
        /// <returns>The result.</returns>
        public T RunInTransaction<T>(Func<IDatabaseConnection, T> action, TransactionMode mode)
        {
            return _db.RunInTransaction(action, mode);
        }

        /// <summary>
        /// Queries the database with the provided statement.
        /// </summary>
        /// <param name="sql">The SQl statement.</param>
        /// <returns>The results of the query.</returns>
        public IEnumerable<IReadOnlyList<IResultSetValue>> Query(string sql)
        {
            return _db.Query(sql);
        }

        /// <summary>
        /// Queries the database with the provided statement and parameters.
        /// </summary>
        /// <param name="sql">The SQL statement.</param>
        /// <param name="values">The bind parameter values.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of rows in the result set.</returns>
        public IEnumerable<IReadOnlyList<IResultSetValue>> Query(string sql, params object[] values)
        {
            return _db.Query(sql, values);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _writeLock.Release();

            _db = null; // Don't dispose it
            _disposed = true;
        }
    }
}
