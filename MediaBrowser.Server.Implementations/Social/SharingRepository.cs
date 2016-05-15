using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Social;
using MediaBrowser.Server.Implementations.Persistence;
using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Social
{
    public class SharingRepository : BaseSqliteRepository
    {
        private IDbConnection _connection;
        private IDbCommand _saveShareCommand;
        private readonly IApplicationPaths _appPaths;

        public SharingRepository(ILogManager logManager, IApplicationPaths appPaths)
            : base(logManager)
        {
            _appPaths = appPaths;
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize(IDbConnector dbConnector)
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "shares.db");

            _connection = await dbConnector.Connect(dbFile).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists Shares (Id GUID, ItemId TEXT, UserId TEXT, ExpirationDate DateTime, PRIMARY KEY (Id))",
                                "create index if not exists idx_Shares on Shares(Id)",

                                //pragmas
                                "pragma temp_store = memory",

                                "pragma shrink_memory"
                               };

            _connection.RunQueries(queries, Logger);

            PrepareStatements();
        }

        /// <summary>
        /// Prepares the statements.
        /// </summary>
        private void PrepareStatements()
        {
            _saveShareCommand = _connection.CreateCommand();
            _saveShareCommand.CommandText = "replace into Shares (Id, ItemId, UserId, ExpirationDate) values (@Id, @ItemId, @UserId, @ExpirationDate)";

            _saveShareCommand.Parameters.Add(_saveShareCommand, "@Id");
            _saveShareCommand.Parameters.Add(_saveShareCommand, "@ItemId");
            _saveShareCommand.Parameters.Add(_saveShareCommand, "@UserId");
            _saveShareCommand.Parameters.Add(_saveShareCommand, "@ExpirationDate");
        }

        public async Task CreateShare(SocialShareInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            if (string.IsNullOrWhiteSpace(info.Id))
            {
                throw new ArgumentNullException("info.Id");
            }

            var cancellationToken = CancellationToken.None;

            cancellationToken.ThrowIfCancellationRequested();

            await WriteLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                _saveShareCommand.GetParameter(0).Value = new Guid(info.Id);
                _saveShareCommand.GetParameter(1).Value = info.ItemId;
                _saveShareCommand.GetParameter(2).Value = info.UserId;
                _saveShareCommand.GetParameter(3).Value = info.ExpirationDate;

                _saveShareCommand.Transaction = transaction;

                _saveShareCommand.ExecuteNonQuery();

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
                Logger.ErrorException("Failed to save share:", e);

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

        public SocialShareInfo GetShareInfo(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }

            var cmd = _connection.CreateCommand();
            cmd.CommandText = "select Id, ItemId, UserId, ExpirationDate from Shares where id = @id";

            cmd.Parameters.Add(cmd, "@id", DbType.Guid).Value = new Guid(id);

            using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow))
            {
                if (reader.Read())
                {
                    return GetSocialShareInfo(reader);
                }
            }

            return null;
        }

        private SocialShareInfo GetSocialShareInfo(IDataReader reader)
        {
            var info = new SocialShareInfo();

            info.Id = reader.GetGuid(0).ToString("N");
            info.ItemId = reader.GetString(1);
            info.UserId = reader.GetString(2);
            info.ExpirationDate = reader.GetDateTime(3).ToUniversalTime();

            return info;
        }

        public async Task DeleteShare(string id)
        {
            
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
