using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Data;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Social;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Social
{
    public class SharingRepository : BaseSqliteRepository, ISharingRepository
    {
        public SharingRepository(ILogger logger, IApplicationPaths appPaths)
            : base(logger)
        {
            DbFilePath = Path.Combine(appPaths.DataPath, "shares.db");
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public void Initialize()
        {
            using (var connection = CreateConnection())
            {
                RunDefaultInitialization(connection);

                string[] queries = {

                                "create table if not exists Shares (Id GUID, ItemId TEXT, UserId TEXT, ExpirationDate DateTime, PRIMARY KEY (Id))",
                                "create index if not exists idx_Shares on Shares(Id)",

                                "pragma shrink_memory"
                               };

                connection.RunQueries(queries);
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

            using (var connection = CreateConnection())
            {
                using (WriteLock.Write())
                {
                    connection.RunInTransaction(db =>
                    {
                        var commandText = "replace into Shares (Id, ItemId, UserId, ExpirationDate) values (?, ?, ?, ?)";

                        db.Execute(commandText,
                            info.Id.ToGuidParamValue(),
                            info.ItemId,
                            info.UserId,
                            info.ExpirationDate.ToDateTimeParamValue());
                    }, TransactionMode);
                }
            }
        }

        public SocialShareInfo GetShareInfo(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }

            using (var connection = CreateConnection(true))
            {
                using (WriteLock.Read())
                {
                    var commandText = "select Id, ItemId, UserId, ExpirationDate from Shares where id = ?";

                    var paramList = new List<object>();
                    paramList.Add(id.ToGuidParamValue());

                    foreach (var row in connection.Query(commandText, paramList.ToArray()))
                    {
                        return GetSocialShareInfo(row);
                    }
                }
            }

            return null;
        }

        private SocialShareInfo GetSocialShareInfo(IReadOnlyList<IResultSetValue> reader)
        {
            var info = new SocialShareInfo();

            info.Id = reader[0].ReadGuid().ToString("N");
            info.ItemId = reader[1].ToString();
            info.UserId = reader[2].ToString();
            info.ExpirationDate = reader[3].ReadDateTime();

            return info;
        }

        public async Task DeleteShare(string id)
        {

        }
    }
}
