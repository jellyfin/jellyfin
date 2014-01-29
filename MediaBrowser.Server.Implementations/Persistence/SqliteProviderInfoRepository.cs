using System.IO;
using MediaBrowser.Common.Configuration;
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
    public class SqliteProviderInfoRepository : IProviderRepository
    {
        private IDbConnection _connection;

        private readonly ILogger _logger;

        private IDbCommand _deleteInfosCommand;
        private IDbCommand _saveInfoCommand;
        private IDbCommand _saveStatusCommand;
        private readonly IApplicationPaths _appPaths;

        public SqliteProviderInfoRepository(IApplicationPaths appPaths, ILogManager logManager)
        {
            _appPaths = appPaths;
            _logger = logManager.GetLogger(GetType().Name);
        }

        private SqliteShrinkMemoryTimer _shrinkMemoryTimer;

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
            var dbFile = Path.Combine(_appPaths.DataPath, "providerinfo.db");

            _connection = await SqliteExtensions.ConnectToDb(dbFile, _logger).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists providerinfos (ItemId GUID, ProviderId GUID, ProviderVersion TEXT, FileStamp GUID, LastRefreshStatus TEXT, LastRefreshed datetime, PRIMARY KEY (ItemId, ProviderId))",
                                "create index if not exists idx_providerinfos on providerinfos(ItemId, ProviderId)",

                                "create table if not exists MetadataStatus (ItemId GUID PRIMARY KEY, DateLastMetadataRefresh datetime, DateLastImagesRefresh datetime, LastStatus TEXT, LastErrorMessage TEXT, MetadataProvidersRefreshed TEXT, ImageProvidersRefreshed TEXT)",
                                "create index if not exists idx_MetadataStatus on MetadataStatus(ItemId)",

                                //pragmas
                                "pragma temp_store = memory",

                                "pragma shrink_memory"
                               };

            _connection.RunQueries(queries, _logger);

            PrepareStatements();

            _shrinkMemoryTimer = new SqliteShrinkMemoryTimer(_connection, _writeLock, _logger);
        }

        private static readonly string[] SaveHistoryColumns =
        {
            "ItemId",
            "ProviderId",
            "ProviderVersion",
            "FileStamp",
            "LastRefreshStatus",
            "LastRefreshed"
        };

        private readonly string[] _historySelectColumns = SaveHistoryColumns.Skip(1).ToArray();

        private static readonly string[] StatusColumns =
        {
            "ItemId",
            "DateLastMetadataRefresh",
            "DateLastImagesRefresh",
            "LastStatus",
            "LastErrorMessage",
            "MetadataProvidersRefreshed",
            "ImageProvidersRefreshed"
        };

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
                string.Join(",", SaveHistoryColumns),
                string.Join(",", SaveHistoryColumns.Select(i => "@" + i).ToArray()));

            foreach (var col in SaveHistoryColumns)
            {
                _saveInfoCommand.Parameters.Add(_saveInfoCommand, "@" + col);
            }

            _saveStatusCommand = _connection.CreateCommand();

            _saveStatusCommand.CommandText = string.Format("replace into MetadataStatus ({0}) values ({1})",
                string.Join(",", StatusColumns),
                string.Join(",", StatusColumns.Select(i => "@" + i).ToArray()));

            foreach (var col in StatusColumns)
            {
                _saveStatusCommand.Parameters.Add(_saveStatusCommand, "@" + col);
            }
        }

        public IEnumerable<BaseProviderInfo> GetProviderHistory(Guid itemId)
        {
            if (itemId == Guid.Empty)
            {
                throw new ArgumentNullException("itemId");
            }

            using (var cmd = _connection.CreateCommand())
            {
                var cmdText = "select " + string.Join(",", _historySelectColumns) + " from providerinfos where";

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
        /// Gets the base provider information.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>BaseProviderInfo.</returns>
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

        public async Task SaveProviderHistory(Guid id, IEnumerable<BaseProviderInfo> infos, CancellationToken cancellationToken)
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
                result.LastStatus = (ProviderRefreshStatus)Enum.Parse(typeof(ProviderRefreshStatus), reader.GetString(3), true);
            }

            if (!reader.IsDBNull(4))
            {
                result.LastErrorMessage = reader.GetString(4);
            }

            if (!reader.IsDBNull(5))
            {
                result.MetadataProvidersRefreshed = reader.GetString(5).Split('|').Where(i => !string.IsNullOrEmpty(i)).Select(i => new Guid(i)).ToList();
            }

            if (!reader.IsDBNull(6))
            {
                result.ImageProvidersRefreshed = reader.GetString(6).Split('|').Where(i => !string.IsNullOrEmpty(i)).Select(i => new Guid(i)).ToList();
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

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();
                
                _saveStatusCommand.GetParameter(0).Value = status.ItemId;
                _saveStatusCommand.GetParameter(1).Value = status.DateLastMetadataRefresh;
                _saveStatusCommand.GetParameter(2).Value = status.DateLastImagesRefresh;
                _saveStatusCommand.GetParameter(3).Value = status.LastStatus.ToString();
                _saveStatusCommand.GetParameter(4).Value = status.LastErrorMessage;
                _saveStatusCommand.GetParameter(5).Value = string.Join("|", status.MetadataProvidersRefreshed.ToArray());
                _saveStatusCommand.GetParameter(6).Value = string.Join("|", status.ImageProvidersRefreshed.ToArray());

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
                        if (_shrinkMemoryTimer != null)
                        {
                            _shrinkMemoryTimer.Dispose();
                            _shrinkMemoryTimer = null;
                        }

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
