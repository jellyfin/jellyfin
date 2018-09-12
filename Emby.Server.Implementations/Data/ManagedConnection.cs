using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Data
{
    public class ManagedConnection :  IDisposable
    {
        private SQLiteDatabaseConnection db;
        private readonly bool _closeOnDispose;

        public ManagedConnection(SQLiteDatabaseConnection db, bool closeOnDispose)
        {
            this.db = db;
            _closeOnDispose = closeOnDispose;
        }

        public IStatement PrepareStatement(string sql)
        {
            return db.PrepareStatement(sql);
        }

        public IEnumerable<IStatement> PrepareAll(string sql)
        {
            return db.PrepareAll(sql);
        }

        public void ExecuteAll(string sql)
        {
            db.ExecuteAll(sql);
        }

        public void Execute(string sql, params object[] values)
        {
            db.Execute(sql, values);
        }

        public void RunQueries(string[] sql)
        {
            db.RunQueries(sql);
        }

        public void RunInTransaction(Action<IDatabaseConnection> action, TransactionMode mode)
        {
            db.RunInTransaction(action, mode);
        }

        public T RunInTransaction<T>(Func<IDatabaseConnection, T> action, TransactionMode mode)
        {
            return db.RunInTransaction<T>(action, mode);
        }

        public IEnumerable<IReadOnlyList<IResultSetValue>> Query(string sql)
        {
            return db.Query(sql);
        }

        public IEnumerable<IReadOnlyList<IResultSetValue>> Query(string sql, params object[] values)
        {
            return db.Query(sql, values);
        }

        public void Close()
        {
            using (db)
            {

            }
        }

        public void Dispose()
        {
            if (_closeOnDispose)
            {
                Close();
            }
        }
    }
}
