using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Persistence
{
    class SqliteProviderInfoRepository
    {
        private IDbConnection _connection;

        private readonly ILogger _logger;

        private IDbCommand _deleteInfosCommand;
        private IDbCommand _saveInfoCommand;

        public SqliteProviderInfoRepository(IDbConnection connection, ILogManager logManager)
        {
            _connection = connection;

            _logger = logManager.GetLogger(GetType().Name);
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public void Initialize()
        {
            var createTableCommand
                = "create table if not exists providerinfos ";

            createTableCommand += "(ItemId GUID, ProviderId GUID, ProviderVersion TEXT, FileStamp GUID, LastRefreshStatus TEXT, LastRefreshed datetime, PRIMARY KEY (ItemId, ProviderId))";

            string[] queries = {

                                createTableCommand,
                                "create index if not exists idx_providerinfos on providerinfos(ItemId, ProviderId)",

                                //pragmas
                                "pragma temp_store = memory"
                               };

            _connection.RunQueries(queries, _logger);

            PrepareStatements();
        }

        private static readonly string[] SaveColumns =
        {
            "ItemId",
            "ProviderId",
            "ProviderVersion",
            "FileStamp",
            "LastRefreshStatus",
            "LastRefreshed"
        };

        private readonly string[] _selectColumns = SaveColumns.Skip(1).ToArray();

        /// <summary>
        /// The _write lock
        /// </summary>
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Prepares the statements.
        /// </summary>
        private void PrepareStatements()
        {
            _deleteInfosCommand = _connection.CreateCommand();
            _deleteInfosCommand.CommandText = "delete from providerinfos where ItemId=@ItemId";
            _deleteInfosCommand.Parameters.Add(_deleteInfosCommand, "@ItemId");

            _saveInfoCommand = _connection.CreateCommand();

            _saveInfoCommand.CommandText = string.Format("replace into providerinfos ({0}) values ({1})",
                string.Join(",", SaveColumns),
                string.Join(",", SaveColumns.Select(i => "@" + i).ToArray()));

            foreach (var col in SaveColumns)
            {
                _saveInfoCommand.Parameters.Add(_saveInfoCommand, "@" + col);
            }
        }

        public IEnumerable<BaseProviderInfo> GetBaseProviderInfos(Guid itemId)
        {
            if (itemId == Guid.Empty)
            {
                throw new ArgumentNullException("itemId");
            }

            using (var cmd = _connection.CreateCommand())
            {
                var cmdText = "select " + string.Join(",", _selectColumns) + " from providerinfos where";

                cmdText += " ItemId=@ItemId";
                cmd.Parameters.Add(cmd, "@ItemId", DbType.Guid).Value = itemId;

                cmd.CommandText = cmdText;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        yield return GetBaseProviderInfo(reader);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the chapter.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>ChapterInfo.</returns>
        private BaseProviderInfo GetBaseProviderInfo(IDataReader reader)
        {
            var item = new BaseProviderInfo
            {
                ProviderId = reader.GetGuid(0)
            };

            if (!reader.IsDBNull(1))
            {
                item.ProviderVersion = reader.GetString(1);
            }

            item.FileStamp = reader.GetGuid(2);
            item.LastRefreshStatus = (ProviderRefreshStatus)Enum.Parse(typeof(ProviderRefreshStatus), reader.GetString(3), true);
            item.LastRefreshed = reader.GetDateTime(4).ToUniversalTime();

            return item;
        }

        public async Task SaveProviderInfos(Guid id, IEnumerable<BaseProviderInfo> infos, CancellationToken cancellationToken)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            if (infos == null)
            {
                throw new ArgumentNullException("infos");
            }

            cancellationToken.ThrowIfCancellationRequested();

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                // First delete chapters
                _deleteInfosCommand.GetParameter(0).Value = id;

                _deleteInfosCommand.Transaction = transaction;

                _deleteInfosCommand.ExecuteNonQuery();

                foreach (var stream in infos)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _saveInfoCommand.GetParameter(0).Value = id;
                    _saveInfoCommand.GetParameter(1).Value = stream.ProviderId;
                    _saveInfoCommand.GetParameter(2).Value = stream.ProviderVersion;
                    _saveInfoCommand.GetParameter(3).Value = stream.FileStamp;
                    _saveInfoCommand.GetParameter(4).Value = stream.LastRefreshStatus.ToString();
                    _saveInfoCommand.GetParameter(5).Value = stream.LastRefreshed;

                    _saveInfoCommand.Transaction = transaction;
                    _saveInfoCommand.ExecuteNonQuery();
                }

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
                _logger.ErrorException("Failed to save provider info:", e);

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

                _writeLock.Release();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private readonly object _disposeLock = new object();

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                try
                {
                    lock (_disposeLock)
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
                catch (Exception ex)
                {
                    _logger.ErrorException("Error disposing database", ex);
                }
            }
        }
    }
}
