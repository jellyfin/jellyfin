using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Persistence
{
    public class SqliteProviderInfoRepository : BaseSqliteRepository, IProviderRepository
    {
        private IDbConnection _connection;

        private IDbCommand _saveStatusCommand;
        private readonly IApplicationPaths _appPaths;

        public SqliteProviderInfoRepository(ILogManager logManager, IApplicationPaths appPaths) : base(logManager)
        {
            _appPaths = appPaths;
        }

        /// <summary>
        /// Gets the name of the repository
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get
            {
                return "SQLite";
            }
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize()
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "refreshinfo.db");

            _connection = await SqliteExtensions.ConnectToDb(dbFile, Logger).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists MetadataStatus (ItemId GUID PRIMARY KEY, DateLastMetadataRefresh datetime, DateLastImagesRefresh datetime, ItemDateModified DateTimeNull)",
                                "create index if not exists idx_MetadataStatus on MetadataStatus(ItemId)",

                                //pragmas
                                "pragma temp_store = memory",

                                "pragma shrink_memory"
                               };

            _connection.RunQueries(queries, Logger);

            AddItemDateModifiedCommand();

            PrepareStatements();
        }

        private static readonly string[] StatusColumns =
        {
            "ItemId",
            "DateLastMetadataRefresh",
            "DateLastImagesRefresh",
            "ItemDateModified"
        };

        private void AddItemDateModifiedCommand()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(MetadataStatus)";

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(1))
                        {
                            var name = reader.GetString(1);

                            if (string.Equals(name, "ItemDateModified", StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            var builder = new StringBuilder();

            builder.AppendLine("alter table MetadataStatus");
            builder.AppendLine("add column ItemDateModified DateTime NULL");

            _connection.RunQueries(new[] { builder.ToString() }, Logger);
        }
        
        /// <summary>
        /// Prepares the statements.
        /// </summary>
        private void PrepareStatements()
        {
            _saveStatusCommand = _connection.CreateCommand();

            _saveStatusCommand.CommandText = string.Format("replace into MetadataStatus ({0}) values ({1})",
                string.Join(",", StatusColumns),
                string.Join(",", StatusColumns.Select(i => "@" + i).ToArray()));

            foreach (var col in StatusColumns)
            {
                _saveStatusCommand.Parameters.Add(_saveStatusCommand, "@" + col);
            }
        }

        public MetadataStatus GetMetadataStatus(Guid itemId)
        {
            if (itemId == Guid.Empty)
            {
                throw new ArgumentNullException("itemId");
            }

            using (var cmd = _connection.CreateCommand())
            {
                var cmdText = "select " + string.Join(",", StatusColumns) + " from MetadataStatus where";

                cmdText += " ItemId=@ItemId";
                cmd.Parameters.Add(cmd, "@ItemId", DbType.Guid).Value = itemId;

                cmd.CommandText = cmdText;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow))
                {
                    while (reader.Read())
                    {
                        return GetStatus(reader);
                    }

                    return null;
                }
            }
        }

        private MetadataStatus GetStatus(IDataReader reader)
        {
            var result = new MetadataStatus
            {
                ItemId = reader.GetGuid(0)
            };

            if (!reader.IsDBNull(1))
            {
                result.DateLastMetadataRefresh = reader.GetDateTime(1).ToUniversalTime();
            }

            if (!reader.IsDBNull(2))
            {
                result.DateLastImagesRefresh = reader.GetDateTime(2).ToUniversalTime();
            }

            if (!reader.IsDBNull(3))
            {
                result.ItemDateModified = reader.GetDateTime(3).ToUniversalTime();
            }

            return result;
        }

        public async Task SaveMetadataStatus(MetadataStatus status, CancellationToken cancellationToken)
        {
            if (status == null)
            {
                throw new ArgumentNullException("status");
            }

            cancellationToken.ThrowIfCancellationRequested();

            await WriteLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();
                
                _saveStatusCommand.GetParameter(0).Value = status.ItemId;
                _saveStatusCommand.GetParameter(1).Value = status.DateLastMetadataRefresh;
                _saveStatusCommand.GetParameter(2).Value = status.DateLastImagesRefresh;
                _saveStatusCommand.GetParameter(3).Value = status.ItemDateModified;

                _saveStatusCommand.Transaction = transaction;

                _saveStatusCommand.ExecuteNonQuery();

                transaction.Commit();
            }
            catch (OperationCanceledException)
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }

                throw;
            }
            catch (Exception e)
            {
                Logger.ErrorException("Failed to save provider info:", e);

                if (transaction != null)
                {
                    transaction.Rollback();
                }

                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    transaction.Dispose();
                }

                WriteLock.Release();
            }
        }

        protected override void CloseConnection()
        {
            if (_connection != null)
            {
                if (_connection.IsOpen())
                {
                    _connection.Close();
                }

                _connection.Dispose();
                _connection = null;
            }
        }
    }
}
