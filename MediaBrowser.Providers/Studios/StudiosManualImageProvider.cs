using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Studios
{
    public class StudiosManualImageProvider : IImageProvider
    {
        public string Name
        {
            get { return ProviderName; }
        }

        public static string ProviderName
        {
            get { return "Media Browser"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is Studio;
        }

        public Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, ImageType imageType, CancellationToken cancellationToken)
        {
            return GetImages(item, imageType == ImageType.Primary, imageType == ImageType.Backdrop, cancellationToken);
        }

        public Task<IEnumerable<RemoteImageInfo>> GetAllImages(IHasImages item, CancellationToken cancellationToken)
        {
            return GetImages(item, true, true, cancellationToken);
        }

        private Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, bool posters, bool backdrops, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            if (posters)
            {
                list.Add(GetImage(item, "posters.txt", ImageType.Primary, "folder"));
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (backdrops)
            {
                list.Add(GetImage(item, "backdrops.txt", ImageType.Backdrop, "backdrop"));
            }
            
            return Task.FromResult(list.Where(i => i != null));
        }

        private RemoteImageInfo GetImage(IHasImages item, string filename, ImageType type, string remoteFilename)
        {
            var url = GetUrl(item, filename, remoteFilename);

            if (url != null)
            {
                return new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = type,
                    Url = url
                };
            }

            return null;
        }

        private string GetUrl(IHasImages item, string listingFilename, string remoteFilename)
        {
            var list = GetAvailableImages(listingFilename);

            var match = FindMatch(item, list);

            if (!string.IsNullOrEmpty(match))
            {
                return GetUrl(match, remoteFilename);
            }

            return null;
        }

        private string FindMatch(IHasImages item, IEnumerable<string> images)
        {
            var name = GetComparableName(item.Name);

            return images.FirstOrDefault(i => string.Equals(name, GetComparableName(i), StringComparison.OrdinalIgnoreCase));
        }

        private string GetComparableName(string name)
        {
            return name.Replace(" ", string.Empty).Replace(".", string.Empty).Replace("&", string.Empty).Replace("!", string.Empty);
        }

        private string GetUrl(string image, string filename)
        {
            return string.Format("https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/images/studios/{0}/{1}.jpg", image, filename);
        }

        private IEnumerable<string> GetAvailableImages(string filename)
        {
            var path = GetType().Namespace + "." + filename;

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

        public int Priority
        {
            get { return 0; }
        }
    }
}
