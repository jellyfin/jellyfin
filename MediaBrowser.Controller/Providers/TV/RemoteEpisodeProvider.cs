using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Resolvers.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Controller.Providers.TV
{

    /// <summary>
    /// Class RemoteEpisodeProvider
    /// </summary>
    [Export(typeof(BaseMetadataProvider))]
    class RemoteEpisodeProvider : BaseMetadataProvider
    {

        /// <summary>
        /// The episode query
        /// </summary>
        private const string episodeQuery = "http://www.thetvdb.com/api/{0}/series/{1}/default/{2}/{3}/{4}.xml";
        /// <summary>
        /// The abs episode query
        /// </summary>
        private const string absEpisodeQuery = "http://www.thetvdb.com/api/{0}/series/{1}/absolute/{2}/{3}.xml";

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Episode;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Second; }
        }

        /// <summary>
        /// Gets a value indicating whether [requires internet].
        /// </summary>
        /// <value><c>true</c> if [requires internet]; otherwise, <c>false</c>.</value>
        public override bool RequiresInternet
        {
            get { return true; }
        }

        protected override bool RefreshOnFileSystemStampChange
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            bool fetch = false;
            var episode = (Episode)item;
            var downloadDate = providerInfo.LastRefreshed;

            if (Kernel.Instance.Configuration.MetadataRefreshDays == -1 && downloadDate != DateTime.MinValue)
            {
                return false;
            }

            if (!item.DontFetchMeta && !HasLocalMeta(episode))
            {
                fetch = Kernel.Instance.Configuration.MetadataRefreshDays != -1 &&
                    DateTime.Today.Subtract(downloadDate).TotalDays > Kernel.Instance.Configuration.MetadataRefreshDays;
            }

            return fetch;
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <returns>Task{System.Boolean}.</returns>
        protected override async Task<bool> FetchAsyncInternal(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var episode = (Episode)item;
            if (!item.DontFetchMeta && !HasLocalMeta(episode))
            {
                var seriesId = episode.Series != null ? episode.Series.GetProviderId(MetadataProviders.Tvdb) : null;

                if (seriesId != null)
                {
                    await FetchEpisodeData(episode, seriesId, cancellationToken).ConfigureAwait(false);
                    SetLastRefreshed(item, DateTime.UtcNow);
                    return true;
                }
                Logger.Info("Episode provider cannot determine Series Id for " + item.Path);
                return false;
            }
            Logger.Info("Episode provider not fetching because local meta exists or requested to ignore: " + item.Name);
            return false;
        }


        /// <summary>
        /// Fetches the episode data.
        /// </summary>
        /// <param name="episode">The episode.</param>
        /// <param name="seriesId">The series id.</param>
        /// <returns>Task{System.Boolean}.</returns>
        private async Task<bool> FetchEpisodeData(Episode episode, string seriesId, CancellationToken cancellationToken)
        {

            string name = episode.Name;
            string location = episode.Path;

            Logger.Debug("TvDbProvider: Fetching episode data for: " + name);
            string epNum = TVUtils.EpisodeNumberFromFile(location, episode.Season != null);

            if (epNum == null)
            {
                Logger.Warn("TvDbProvider: Could not determine episode number for: " + episode.Path);
                return false;
            }

            var episodeNumber = Int32.Parse(epNum);

            episode.IndexNumber = episodeNumber;
            var usingAbsoluteData = false;

            if (string.IsNullOrEmpty(seriesId)) return false;

            var seasonNumber = "";
            if (episode.Parent is Season)
            {
                seasonNumber = episode.Parent.IndexNumber.ToString();
            }

            if (string.IsNullOrEmpty(seasonNumber))
                seasonNumber = TVUtils.SeasonNumberFromEpisodeFile(location); // try and extract the season number from the file name for S1E1, 1x04 etc.

            if (!string.IsNullOrEmpty(seasonNumber))
            {
                seasonNumber = seasonNumber.TrimStart('0');

                if (string.IsNullOrEmpty(seasonNumber))
                {
                    seasonNumber = "0"; // Specials
                }

                var url = string.Format(episodeQuery, TVUtils.TVDBApiKey, seriesId, seasonNumber, episodeNumber, Kernel.Instance.Configuration.PreferredMetadataLanguage);
                var doc = new XmlDocument();

                try
                {
                    using (var result = await Kernel.Instance.HttpManager.Get(url, Kernel.Instance.ResourcePools.TvDb, cancellationToken).ConfigureAwait(false))
                    {
                        doc.Load(result);
                    }
                }
                catch (HttpException)
                {
                }

                //episode does not exist under this season, try absolute numbering.
                //still assuming it's numbered as 1x01
                //this is basicly just for anime.
                if (!doc.HasChildNodes && Int32.Parse(seasonNumber) == 1)
                {
                    url = string.Format(absEpisodeQuery, TVUtils.TVDBApiKey, seriesId, episodeNumber, Kernel.Instance.Configuration.PreferredMetadataLanguage);

                    try
                    {
                        using (var result = await Kernel.Instance.HttpManager.Get(url, Kernel.Instance.ResourcePools.TvDb, cancellationToken).ConfigureAwait(false))
                        {
                            if (result != null) doc.Load(result);
                            usingAbsoluteData = true;
                        }
                    }
                    catch (HttpException)
                    {
                    }
                }

                if (doc.HasChildNodes)
                {
                    var p = doc.SafeGetString("//filename");
                    if (p != null)
                    {
                        if (!Directory.Exists(episode.MetaLocation)) Directory.CreateDirectory(episode.MetaLocation);

                        try
                        {
                            episode.PrimaryImagePath = await Kernel.Instance.ProviderManager.DownloadAndSaveImage(episode, TVUtils.BannerUrl + p, Path.GetFileName(p), Kernel.Instance.ResourcePools.TvDb, cancellationToken);
                        }
                        catch (HttpException)
                        {
                        }
                        catch (IOException)
                        {

                        }
                    }

                    episode.Overview = doc.SafeGetString("//Overview");
                    if (usingAbsoluteData)
                        episode.IndexNumber = doc.SafeGetInt32("//absolute_number", -1);
                    if (episode.IndexNumber < 0)
                        episode.IndexNumber = doc.SafeGetInt32("//EpisodeNumber");

                    episode.Name = episode.IndexNumber.Value.ToString("000") + " - " + doc.SafeGetString("//EpisodeName");
                    episode.CommunityRating = doc.SafeGetSingle("//Rating", -1, 10);
                    var firstAired = doc.SafeGetString("//FirstAired");
                    DateTime airDate;
                    if (DateTime.TryParse(firstAired, out airDate) && airDate.Year > 1850)
                    {
                        episode.PremiereDate = airDate.ToUniversalTime();
                        episode.ProductionYear = airDate.Year;
                    }

                    var actors = doc.SafeGetString("//GuestStars");
                    if (actors != null)
                    {
                        episode.AddPeople(actors.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(str => new PersonInfo { Type = "Actor", Name = str }));
                    }


                    var directors = doc.SafeGetString("//Director");
                    if (directors != null)
                    {
                        episode.AddPeople(directors.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(str => new PersonInfo { Type = "Director", Name = str }));
                    }


                    var writers = doc.SafeGetString("//Writer");
                    if (writers != null)
                    {
                        episode.AddPeople(writers.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(str => new PersonInfo { Type = "Writer", Name = str }));
                    }

                    if (Kernel.Instance.Configuration.SaveLocalMeta)
                    {
                        if (!Directory.Exists(episode.MetaLocation)) Directory.CreateDirectory(episode.MetaLocation);
                        var ms = new MemoryStream();
                        doc.Save(ms);

                        await Kernel.Instance.FileSystemManager.SaveToLibraryFilesystem(episode, Path.Combine(episode.MetaLocation, Path.GetFileNameWithoutExtension(episode.Path) + ".xml"), ms, cancellationToken).ConfigureAwait(false);
                    }

                    return true;
                }

            }

            return false;
        }

        /// <summary>
        /// Determines whether [has local meta] [the specified episode].
        /// </summary>
        /// <param name="episode">The episode.</param>
        /// <returns><c>true</c> if [has local meta] [the specified episode]; otherwise, <c>false</c>.</returns>
        private bool HasLocalMeta(Episode episode)
        {
            return (episode.Parent.ResolveArgs.ContainsMetaFileByName(Path.GetFileNameWithoutExtension(episode.Path) + ".xml"));
        }
    }
}
