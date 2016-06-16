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
        public SharingRepository(ILogManager logManager, IApplicationPaths appPaths, IDbConnector dbConnector)
            : base(logManager, dbConnector)
        {
            DbFilePath = Path.Combine(appPaths.DataPath, "shares.db");
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize()
        {
            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                string[] queries = {

                                "create table if not exists Shares (Id GUID, ItemId TEXT, UserId TEXT, ExpirationDate DateTime, PRIMARY KEY (Id))",
                                "create index if not exists idx_Shares on Shares(Id)",

                                "pragma shrink_memory"
                               };

                connection.RunQueries(queries, Logger);
            }
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

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                using (var saveShareCommand = connection.CreateCommand())
                {
                    saveShareCommand.CommandText = "replace into Shares (Id, ItemId, UserId, ExpirationDate) values (@Id, @ItemId, @UserId, @ExpirationDate)";

                    saveShareCommand.Parameters.Add(saveShareCommand, "@Id");
                    saveShareCommand.Parameters.Add(saveShareCommand, "@ItemId");
                    saveShareCommand.Parameters.Add(saveShareCommand, "@UserId");
                    saveShareCommand.Parameters.Add(saveShareCommand, "@ExpirationDate");

                    IDbTransaction transaction = null;

                    try
                    {
                        transaction = connection.BeginTransaction();

                        saveShareCommand.GetParameter(0).Value = new Guid(info.Id);
                        saveShareCommand.GetParameter(1).Value = info.ItemId;
                        saveShareCommand.GetParameter(2).Value = info.UserId;
                        saveShareCommand.GetParameter(3).Value = info.ExpirationDate;

                        saveShareCommand.Transaction = transaction;

                        saveShareCommand.ExecuteNonQuery();

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
                    }
                }
            }
        }

        public SocialShareInfo GetShareInfo(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }

            using (var connection = CreateConnection(true).Result)
            {
                var cmd = connection.CreateCommand();
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
    }
}
