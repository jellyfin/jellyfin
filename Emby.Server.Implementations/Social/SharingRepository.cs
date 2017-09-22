using System;
using System.Collections.Generic;
using System.IO;
using Emby.Server.Implementations.Data;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Social;
using SQLitePCL.pretty;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Social
{
    public class SharingRepository : BaseSqliteRepository, ISharingRepository
    {
        protected IFileSystem FileSystem { get; private set; }

        public SharingRepository(ILogger logger, IApplicationPaths appPaths, IFileSystem fileSystem)
            : base(logger)
        {
            FileSystem = fileSystem;
            DbFilePath = Path.Combine(appPaths.DataPath, "shares.db");
        }

        public void Initialize()
        {
            try
            {
                InitializeInternal();
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error loading database file. Will reset and retry.", ex);

                FileSystem.DeleteFile(DbFilePath);

                InitializeInternal();
            }
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        private void InitializeInternal()
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

        public void CreateShare(SocialShareInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            if (string.IsNullOrWhiteSpace(info.Id))
            {
                throw new ArgumentNullException("info.Id");
            }

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        var commandText = "replace into Shares (Id, ItemId, UserId, ExpirationDate) values (?, ?, ?, ?)";

                        db.Execute(commandText,
                            info.Id.ToGuidBlob(),
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

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var commandText = "select Id, ItemId, UserId, ExpirationDate from Shares where id = ?";

                    var paramList = new List<object>();
                    paramList.Add(id.ToGuidBlob());

                    foreach (var row in connection.Query(commandText, paramList.ToArray(paramList.Count)))
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

            info.Id = reader[0].ReadGuidFromBlob().ToString("N");
            info.ItemId = reader[1].ToString();
            info.UserId = reader[2].ToString();
            info.ExpirationDate = reader[3].ReadDateTime();

            return info;
        }

        public void DeleteShare(string id)
        {

        }
    }
}
