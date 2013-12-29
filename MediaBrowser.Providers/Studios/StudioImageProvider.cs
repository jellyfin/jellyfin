using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Studios
{
    public class StudioImageProvider : BaseMetadataProvider
    {
        private readonly IProviderManager _providerManager;
        private readonly SemaphoreSlim _resourcePool = new SemaphoreSlim(5, 5);

        public StudioImageProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            _providerManager = providerManager;
        }

        public override bool Supports(BaseItem item)
        {
            return item is Studio;
        }

        public override bool RequiresInternet
        {
            get
            {
                return true;
            }
        }

        public override ItemUpdateType ItemUpdateType
        {
            get
            {
                return ItemUpdateType.ImageUpdate;
            }
        }

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (!string.IsNullOrEmpty(item.PrimaryImagePath))
            {
                return false;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        protected override bool RefreshOnVersionChange
        {
            get
            {
                return true;
            }
        }

        protected override string ProviderVersion
        {
            get
            {
                return "1";
            }
        }

        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(item.PrimaryImagePath))
            {
                var list = GetAvailableImages();

                var match = FindMatch(item, list);

                if (!string.IsNullOrEmpty(match))
                {
                    var url = GetUrl(match);

                    await _providerManager.SaveImage(item, url, _resourcePool, ImageType.Primary, null, cancellationToken).ConfigureAwait(false);
                }
            }

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return true;
        }

        private string FindMatch(BaseItem item, IEnumerable<string> images)
        {
            var name = GetComparableName(item.Name);

            return images.FirstOrDefault(i => string.Equals(name, GetComparableName(i), StringComparison.OrdinalIgnoreCase));
        }

        private string GetComparableName(string name)
        {
            return name.Replace(" ", string.Empty).Replace(".", string.Empty).Replace("&", string.Empty).Replace("!", string.Empty);
        }

        private string GetUrl(string image)
        {
            return string.Format("https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/images/studios/{0}/folder.jpg", image);
        }

        private IEnumerable<string> GetAvailableImages()
        {
            var path = GetType().Namespace + ".images.txt";

            using (var stream = GetType().Assembly.GetManifestResourceStream(path))
            {
                using (var reader = new StreamReader(stream))
                {
                    var lines = new List<string>();

                    while (!reader.EndOfStream)
                    {
                        var text = reader.ReadLine();

                        lines.Add(text);
                    }

                    return lines;
                }
            }
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Third; }
        }
    }
}
