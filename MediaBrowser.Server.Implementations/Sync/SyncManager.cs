using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class SyncManager : ISyncManager
    {
        private ISyncProvider[] _providers = new ISyncProvider[] { };

        public void AddParts(IEnumerable<ISyncProvider> providers)
        {
            _providers = providers.ToArray();
        }

        public Task<List<SyncJob>> CreateJob(SyncJobRequest request)
        {
            throw new NotImplementedException();
        }

        public QueryResult<SyncJob> GetJobs(SyncJobQuery query)
        {
            throw new NotImplementedException();
        }

        public QueryResult<SyncSchedule> GetSchedules(SyncScheduleQuery query)
        {
            throw new NotImplementedException();
        }

        public Task CancelJob(string id)
        {
            throw new NotImplementedException();
        }

        public Task CancelSchedule(string id)
        {
            throw new NotImplementedException();
        }

        public SyncJob GetJob(string id)
        {
            throw new NotImplementedException();
        }

        public SyncSchedule GetSchedule(string id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<SyncTarget> GetSyncTargets(string userId)
        {
            return _providers
                .SelectMany(GetSyncTargets)
                .OrderBy(i => i.Name);
        }

        private IEnumerable<SyncTarget> GetSyncTargets(ISyncProvider provider)
        {
            var providerId = GetSyncProviderId(provider);

            return provider.GetSyncTargets().Select(i => new SyncTarget
            {
                Name = i.Name,
                Id = providerId + "-" + i.Id
            });
        }

        private ISyncProvider GetSyncProvider(SyncTarget target)
        {
            var providerId = target.Id.Split(new[] { '-' }, 2).First();

            return _providers.First(i => string.Equals(providerId, GetSyncProviderId(i)));
        }

        private string GetSyncProviderId(ISyncProvider provider)
        {
            return (provider.GetType().Name + provider.Name).GetMD5().ToString("N");
        }

        public bool SupportsSync(BaseItem item)
        {
            if (item.LocationType == LocationType.Virtual)
            {
                return false;
            }

            if (string.Equals(item.MediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
            {
                if (item.RunTimeTicks.HasValue)
                {
                    var video = item as Video;

                    if (video != null)
                    {
                        if (video.VideoType != VideoType.VideoFile)
                        {
                            return false;
                        }

                        if (video.IsMultiPart)
                        {
                            return false;
                        }
                    }

                    return true;
                }

                return false;
            }

            return false;
        }
    }
}
