using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Emby.Server.Implementations.Data;
using MediaBrowser.Controller;
using MediaBrowser.Model.Logging;
using SQLitePCL.pretty;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.IO;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Session;
using MediaBrowser.Controller.Configuration;

namespace Emby.Server.Implementations.Devices
{
    public class SqliteDeviceRepository : BaseSqliteRepository, IDeviceRepository
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        protected IFileSystem FileSystem { get; private set; }
        private readonly object _syncLock = new object();
        private readonly IJsonSerializer _json;
        private IServerApplicationPaths _appPaths;

        public SqliteDeviceRepository(ILogger logger, IServerConfigurationManager config, IFileSystem fileSystem, IJsonSerializer json)
            : base(logger)
        {
            var appPaths = config.ApplicationPaths;

            DbFilePath = Path.Combine(appPaths.DataPath, "devices.db");
            FileSystem = fileSystem;
            _json = json;
            _appPaths = appPaths;
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

        private void InitializeInternal()
        {
            using (var connection = CreateConnection())
            {
                RunDefaultInitialization(connection);

                string[] queries = {
                    "create table if not exists Devices (Id TEXT PRIMARY KEY, Name TEXT NOT NULL, ReportedName TEXT NOT NULL, CustomName TEXT, CameraUploadPath TEXT, LastUserName TEXT, AppName TEXT NOT NULL, AppVersion TEXT NOT NULL, LastUserId TEXT, DateLastModified DATETIME NOT NULL, Capabilities TEXT NOT NULL)",
                    "create index if not exists idx_id on Devices(Id)"
                               };

                connection.RunQueries(queries);

                MigrateDevices();
            }
        }

        private void MigrateDevices()
        {
            List<string> files;
            try
            {
                files = FileSystem
                       .GetFilePaths(GetDevicesPath(), true)
                       .Where(i => string.Equals(Path.GetFileName(i), "device.json", StringComparison.OrdinalIgnoreCase))
                       .ToList();
            }
            catch (IOException)
            {
                return;
            }

            foreach (var file in files)
            {
                try
                {
                    var device = _json.DeserializeFromFile<DeviceInfo>(file);

                    device.Name = string.IsNullOrWhiteSpace(device.CustomName) ? device.ReportedName : device.CustomName;

                    SaveDevice(device);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error reading {0}", ex, file);
                }
                finally
                {
                    try
                    {
                        FileSystem.DeleteFile(file);
                    }
                    catch (IOException)
                    {
                        try
                        {
                            FileSystem.MoveFile(file, Path.ChangeExtension(file, ".old"));
                        }
                        catch (IOException)
                        {
                        }
                    }
                }
            }
        }

        private const string BaseSelectText = "select Id, Name, ReportedName, CustomName, CameraUploadPath, LastUserName, AppName, AppVersion, LastUserId, DateLastModified, Capabilities from Devices";

        public void SaveCapabilities(string deviceId, ClientCapabilities capabilities)
        {
            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        using (var statement = db.PrepareStatement("update devices set Capabilities=@Capabilities where Id=@Id"))
                        {
                            statement.TryBind("@Id", deviceId);

                            if (capabilities == null)
                            {
                                statement.TryBindNull("@Capabilities");
                            }
                            else
                            {
                                statement.TryBind("@Capabilities", _json.SerializeToString(capabilities));
                            }

                            statement.MoveNext();
                        }
                    }, TransactionMode);
                }
            }
        }

        public void SaveDevice(DeviceInfo entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        using (var statement = db.PrepareStatement("replace into Devices (Id, Name, ReportedName, CustomName, CameraUploadPath, LastUserName, AppName, AppVersion, LastUserId, DateLastModified, Capabilities) values (@Id, @Name, @ReportedName, @CustomName, @CameraUploadPath, @LastUserName, @AppName, @AppVersion, @LastUserId, @DateLastModified, @Capabilities)"))
                        {
                            statement.TryBind("@Id", entry.Id);
                            statement.TryBind("@Name", entry.Name);
                            statement.TryBind("@ReportedName", entry.ReportedName);
                            statement.TryBind("@CustomName", entry.CustomName);
                            statement.TryBind("@CameraUploadPath", entry.CameraUploadPath);
                            statement.TryBind("@LastUserName", entry.LastUserName);
                            statement.TryBind("@AppName", entry.AppName);
                            statement.TryBind("@AppVersion", entry.AppVersion);
                            statement.TryBind("@DateLastModified", entry.DateLastModified);

                            if (entry.Capabilities == null)
                            {
                                statement.TryBindNull("@Capabilities");
                            }
                            else
                            {
                                statement.TryBind("@Capabilities", _json.SerializeToString(entry.Capabilities));
                            }

                            statement.MoveNext();
                        }
                    }, TransactionMode);
                }
            }
        }

        public DeviceInfo GetDevice(string id)
        {
            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var statementTexts = new List<string>();
                    statementTexts.Add(BaseSelectText + " where Id=@Id");

                    return connection.RunInTransaction(db =>
                    {
                        var statements = PrepareAllSafe(db, statementTexts).ToList();

                        using (var statement = statements[0])
                        {
                            statement.TryBind("@Id", id);

                            foreach (var row in statement.ExecuteQuery())
                            {
                                return GetEntry(row);
                            }
                        }

                        return null;

                    }, ReadTransactionMode);
                }
            }
        }

        public List<DeviceInfo> GetDevices()
        {
            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var statementTexts = new List<string>();
                    statementTexts.Add(BaseSelectText + " order by DateLastModified desc");

                    return connection.RunInTransaction(db =>
                    {
                        var list = new List<DeviceInfo>();

                        var statements = PrepareAllSafe(db, statementTexts).ToList();

                        using (var statement = statements[0])
                        {
                            foreach (var row in statement.ExecuteQuery())
                            {
                                list.Add(GetEntry(row));
                            }
                        }

                        return list;

                    }, ReadTransactionMode);
                }
            }
        }

        public ClientCapabilities GetCapabilities(string id)
        {
            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var statementTexts = new List<string>();
                    statementTexts.Add("Select Capabilities from Devices where Id=@Id");

                    return connection.RunInTransaction(db =>
                    {
                        var statements = PrepareAllSafe(db, statementTexts).ToList();

                        using (var statement = statements[0])
                        {
                            statement.TryBind("@Id", id);

                            foreach (var row in statement.ExecuteQuery())
                            {
                                if (row[0].SQLiteType != SQLiteType.Null)
                                {
                                    return _json.DeserializeFromString<ClientCapabilities>(row.GetString(0));
                                }
                            }
                        }

                        return null;

                    }, ReadTransactionMode);
                }
            }
        }

        private DeviceInfo GetEntry(IReadOnlyList<IResultSetValue> reader)
        {
            var index = 0;

            var info = new DeviceInfo
            {
                Id = reader.GetString(index)
            };

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                info.Name = reader.GetString(index);
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                info.ReportedName = reader.GetString(index);
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                info.CustomName = reader.GetString(index);
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                info.CameraUploadPath = reader.GetString(index);
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                info.LastUserName = reader.GetString(index);
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                info.AppName = reader.GetString(index);
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                info.AppVersion = reader.GetString(index);
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                info.LastUserId = reader.GetString(index);
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                info.DateLastModified = reader[index].ReadDateTime();
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                info.Capabilities = _json.DeserializeFromString<ClientCapabilities>(reader.GetString(index));
            }

            return info;
        }

        private string GetDevicesPath()
        {
            return Path.Combine(_appPaths.DataPath, "devices");
        }

        private string GetDevicePath(string id)
        {
            return Path.Combine(GetDevicesPath(), id.GetMD5().ToString("N"));
        }

        public ContentUploadHistory GetCameraUploadHistory(string deviceId)
        {
            var path = Path.Combine(GetDevicePath(deviceId), "camerauploads.json");

            lock (_syncLock)
            {
                try
                {
                    return _json.DeserializeFromFile<ContentUploadHistory>(path);
                }
                catch (IOException)
                {
                    return new ContentUploadHistory
                    {
                        DeviceId = deviceId
                    };
                }
            }
        }

        public void AddCameraUpload(string deviceId, LocalFileInfo file)
        {
            var path = Path.Combine(GetDevicePath(deviceId), "camerauploads.json");
            FileSystem.CreateDirectory(FileSystem.GetDirectoryName(path));

            lock (_syncLock)
            {
                ContentUploadHistory history;

                try
                {
                    history = _json.DeserializeFromFile<ContentUploadHistory>(path);
                }
                catch (IOException)
                {
                    history = new ContentUploadHistory
                    {
                        DeviceId = deviceId
                    };
                }

                history.DeviceId = deviceId;

                var list = history.FilesUploaded.ToList();
                list.Add(file);
                history.FilesUploaded = list.ToArray(list.Count);

                _json.SerializeToFile(history, path);
            }
        }

        public void DeleteDevice(string id)
        {
            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        using (var statement = db.PrepareStatement("delete from devices where Id=@Id"))
                        {
                            statement.TryBind("@Id", id);

                            statement.MoveNext();
                        }
                    }, TransactionMode);
                }
            }

            var path = GetDevicePath(id);

            lock (_syncLock)
            {
                try
                {
                    FileSystem.DeleteDirectory(path, true);
                }
                catch (IOException)
                {
                }
            }
        }
    }
}
