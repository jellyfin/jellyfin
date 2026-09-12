using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    /// <summary>
    /// Nfo parser for episodes.
    /// </summary>
    public class EpisodeNfoParser : BaseNfoParser<Episode>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeNfoParser"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{BaseNfoParser}"/> interface.</param>
        /// <param name="config">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/> interface.</param>
        public EpisodeNfoParser(
            ILogger logger,
            IConfigurationManager config,
            IProviderManager providerManager,
            IUserManager userManager,
            IUserDataManager userDataManager,
            IDirectoryService directoryService)
            : base(logger, config, providerManager, userManager, userDataManager, directoryService)
        {
        }

        /// <inheritdoc />
        protected override void Fetch(MetadataResult<Episode> item, string metadataFile, XmlReaderSettings settings, CancellationToken cancellationToken)
        {
            item.ResetPeople();

            var xmlFile = File.ReadAllText(metadataFile);

            // Split the nfo into its episodedetails blocks.
            // This is needed because XBMC metadata uses multiple episodedetails blocks instead of an episodenumberend tag.
            const string Srch = "</episodedetails>";
            var blocks = new List<string>();
            int index;
            while ((index = xmlFile.IndexOf(Srch, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                blocks.Add(xmlFile.Substring(0, index + Srch.Length));
                xmlFile = xmlFile.Substring(index + Srch.Length);
            }

            if (blocks.Count == 0)
            {
                // No closing tag, let the xml reader deal with whatever is in the file
                blocks.Add(xmlFile);
            }

            // These are not going to be valid xml so no sense in causing the provider to fail and spamming the log with exceptions
            try
            {
                if (blocks.Count == 1)
                {
                    ReadEpisodeDetailsFromXml(item, blocks[0], settings, cancellationToken);
                    return;
                }

                // The blocks are not guaranteed to be written in ascending episode order, so parse them all
                // and sort them before merging.
                var episodes = blocks
                    .Select(block =>
                    {
                        var episode = new MetadataResult<Episode>()
                        {
                            Item = new Episode()
                        };

                        ReadEpisodeDetailsFromXml(episode, block, settings, cancellationToken);

                        return (Xml: block, Result: episode);
                    })
                    .OrderBy(episode => episode.Result.Item.IndexNumber ?? int.MaxValue)
                    .ToList();

                // Extract the details of the lowest numbered episode into the item that is returned to the caller
                ReadEpisodeDetailsFromXml(item, episodes[0].Xml, settings, cancellationToken);

                // Concatenate the name, originalTitle and overview tags of the remaining episodes with the first one
                // and take the highest episode number as the last episode of the file
                var name = new StringBuilder(item.Item.Name);
                var originalTitle = new StringBuilder(item.Item.OriginalTitle);
                var overview = new StringBuilder(item.Item.Overview);
                for (var i = 1; i < episodes.Count; i++)
                {
                    var additionalEpisode = episodes[i].Result;

                    if (!string.IsNullOrEmpty(additionalEpisode.Item.Name))
                    {
                        name.Append(" / ").Append(additionalEpisode.Item.Name);
                    }

                    if (!string.IsNullOrEmpty(additionalEpisode.Item.Overview))
                    {
                        overview.Append(" / ").Append(additionalEpisode.Item.Overview);
                    }

                    if (!string.IsNullOrEmpty(additionalEpisode.Item.OriginalTitle))
                    {
                        originalTitle.Append(" / ").Append(additionalEpisode.Item.OriginalTitle);
                    }

                    if (additionalEpisode.Item.IndexNumber is not null)
                    {
                        item.Item.IndexNumberEnd = Math.Max((int)additionalEpisode.Item.IndexNumber, item.Item.IndexNumberEnd ?? (int)additionalEpisode.Item.IndexNumber);
                    }
                }

                item.Item.Name = name.ToString();
                item.Item.OriginalTitle = originalTitle.ToString();
                item.Item.Overview = overview.ToString();
            }
            catch (XmlException)
            {
            }
        }

        /// <inheritdoc />
        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<Episode> itemResult)
        {
            var item = itemResult.Item;

            switch (reader.Name)
            {
                case "season":
                    if (reader.TryReadInt(out var seasonNumber))
                    {
                        item.ParentIndexNumber = seasonNumber;
                    }

                    break;
                case "episode":
                    if (reader.TryReadInt(out var episodeNumber))
                    {
                        item.IndexNumber = episodeNumber;
                    }

                    break;
                case "episodenumberend":
                    if (reader.TryReadInt(out var episodeNumberEnd))
                    {
                        item.IndexNumberEnd = episodeNumberEnd;
                    }

                    break;
                case "airsbefore_episode":
                case "displayepisode":
                    if (reader.TryReadInt(out var airsBeforeEpisode))
                    {
                        item.AirsBeforeEpisodeNumber = airsBeforeEpisode;
                    }

                    break;
                case "airsafter_season":
                case "displayafterseason":
                    if (reader.TryReadInt(out var airsAfterSeason))
                    {
                        item.AirsAfterSeasonNumber = airsAfterSeason;
                    }

                    break;
                case "airsbefore_season":
                case "displayseason":
                    if (reader.TryReadInt(out var airsBeforeSeason))
                    {
                        item.AirsBeforeSeasonNumber = airsBeforeSeason;
                    }

                    break;
                case "showtitle":
                    item.SeriesName = reader.ReadNormalizedString();
                    break;
                default:
                    base.FetchDataFromXmlNode(reader, itemResult);
                    break;
            }
        }

        /// <summary>
        /// Reads the episode details from the given xml and saves the result in the provided result item.
        /// </summary>
        private void ReadEpisodeDetailsFromXml(MetadataResult<Episode> item, string xml, XmlReaderSettings settings, CancellationToken cancellationToken)
        {
            using (var stringReader = new StringReader(xml))
            using (var reader = XmlReader.Create(stringReader, settings))
            {
                reader.MoveToContent();
                reader.Read();

                // Loop through each element
                while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        FetchDataFromXmlNode(reader, item);
                    }
                    else
                    {
                        reader.Read();
                    }
                }
            }
        }
    }
}
