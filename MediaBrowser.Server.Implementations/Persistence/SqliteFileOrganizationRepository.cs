using MediaBrowser.Controller;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.FileOrganization;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Persistence
{
    public class SqliteFileOrganizationRepository : IFileOrganizationRepository
    {
        private IDbConnection _connection;

        private readonly ILogger _logger;

        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private SqliteShrinkMemoryTimer _shrinkMemoryTimer;
        private readonly IServerApplicationPaths _appPaths;

        public SqliteFileOrganizationRepository(ILogManager logManager, IServerApplicationPaths appPaths)
        {
            _appPaths = appPaths;

            _logger = logManager.GetLogger(GetType().Name);
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize()
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "fileorganization.db");

            _connection = await SqliteExtensions.ConnectToDb(dbFile, _logger).ConfigureAwait(false);

            string[] queries = {

                                //pragmas
                                "pragma temp_store = memory",

                                "pragma shrink_memory"
                               };

            _connection.RunQueries(queries, _logger);

            PrepareStatements();

            _shrinkMemoryTimer = new SqliteShrinkMemoryTimer(_connection, _writeLock, _logger);
        }

        private void PrepareStatements()
        {
        }

        public Task SaveResult(FileOrganizationResult result, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public IEnumerable<FileOrganizationResult> GetResults(FileOrganizationResultQuery query)
        {
            return new List<FileOrganizationResult>();
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
