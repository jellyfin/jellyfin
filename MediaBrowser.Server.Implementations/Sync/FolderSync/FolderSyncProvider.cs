using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Sync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sync.FolderSync
{
    public class FolderSyncProvider : IServerSyncProvider
    {
        private readonly IApplicationPaths _appPaths;
        private readonly IUserManager _userManager;

        public FolderSyncProvider(IApplicationPaths appPaths, IUserManager userManager)
        {
            _appPaths = appPaths;
            _userManager = userManager;
        }

        public Task SendFile(string inputFile, string path, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            return Task.Run(() => File.Copy(inputFile, path, true), cancellationToken);
        }

        public Task DeleteFile(string path, SyncTarget target, CancellationToken cancellationToken)
        {
            return Task.Run(() => File.Delete(path), cancellationToken);
        }

        public Task<Stream> GetFile(string path, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            return Task.FromResult((Stream)File.OpenRead(path));
        }

        public string GetFullPath(IEnumerable<string> paths, SyncTarget target)
        {
            var account = GetSyncAccounts()
                .FirstOrDefault(i => string.Equals(i.Id, target.Id, StringComparison.OrdinalIgnoreCase));

            if (account == null)
            {
                throw new ArgumentException("Invalid SyncTarget supplied.");
            }

            var list = paths.ToList();
            list.Insert(0, account.Path);

            return Path.Combine(list.ToArray());
        }

        public string GetParentDirectoryPath(string path, SyncTarget target)
        {
            return Path.GetDirectoryName(path);
        }

        public Task<List<DeviceFileInfo>> GetFileSystemEntries(string path, SyncTarget target)
        {
            List<FileInfo> files;

            try
            {
                files = new DirectoryInfo(path).EnumerateFiles("*", SearchOption.TopDirectoryOnly).ToList();
            }
            catch (DirectoryNotFoundException)
            {
                files = new List<FileInfo>();
            }

            return Task.FromResult(files.Select(i => new DeviceFileInfo
            {
                Name = i.Name,
                Path = i.FullName

            }).ToList());
        }

        public ISyncDataProvider GetDataProvider()
        {
            // If single instances are needed, manage them here
            return new FolderSyncDataProvider();
        }

        public string Name
        {
            get { return "Folder Sync"; }
        }

        public IEnumerable<SyncTarget> GetSyncTargets(string userId)
        {
            return GetSyncAccounts()
                .Where(i => i.UserIds.Contains(userId, StringComparer.OrdinalIgnoreCase))
                .Select(GetSyncTarget);
        }

        public IEnumerable<SyncTarget> GetAllSyncTargets()
        {
            return GetSyncAccounts().Select(GetSyncTarget);
        }

        private SyncTarget GetSyncTarget(SyncAccount account)
        {
            return new SyncTarget
            {
                Id = account.Id,
                Name = account.Name
            };
        }

        private IEnumerable<SyncAccount> GetSyncAccounts()
        {
            return new List<SyncAccount>();
            // Dummy this up
            return _userManager
                .Users
                .Select(i => new SyncAccount
                {
                    Id = i.Id.ToString("N"),
                    UserIds = new List<string> { i.Id.ToString("N") },
                    Path = Path.Combine(_appPaths.DataPath, "foldersync", i.Id.ToString("N")),
                    Name = i.Name + "'s Folder Sync"
                });
        }

        // An internal class to manage all configured Folder Sync accounts for differnet users
        class SyncAccount
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            public List<string> UserIds { get; set; }

            public SyncAccount()
            {
                UserIds = new List<string>();
            }
        }
    }
}
