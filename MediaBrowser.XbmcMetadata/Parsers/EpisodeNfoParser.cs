using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.TV;
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
        protected override void Fetch(MetadataResult<Episode> metadataResult, string nfoPath, XmlReaderSettings settings, CancellationToken cancellationToken)
        {
            using (var fileStream = File.OpenRead(nfoPath))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                metadataResult.ResetPeople();

                var xmlFile = streamReader.ReadToEnd();

                var srch = "</episodedetails>";
                var index = xmlFile.IndexOf(srch, StringComparison.OrdinalIgnoreCase);

                var xml = xmlFile;

                if (index != -1)
                {
                    xml = xmlFile.Substring(0, index + srch.Length);
                    xmlFile = xmlFile.Substring(index + srch.Length);
                }

                // These are not going to be valid xml so no sense in causing the provider to fail and spamming the log with exceptions
                try
                {
                    // Extract episode details from the first episodedetails block
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
                                FetchDataFromXmlNode(reader, metadataResult);
                            }
                            else
                            {
                                reader.Read();
                            }
                        }
                    }

                    // Extract the last episode number from nfo
                    // This is needed because XBMC metadata uses multiple episodedetails blocks instead of episodenumberend tag
                    while ((index = xmlFile.IndexOf(srch, StringComparison.OrdinalIgnoreCase)) != -1)
                    {
                        xml = xmlFile.Substring(0, index + srch.Length);
                        xmlFile = xmlFile.Substring(index + srch.Length);

                        using (var stringReader = new StringReader(xml))
                        using (var reader = XmlReader.Create(stringReader, settings))
                        {
                            reader.MoveToContent();

                            if (reader.ReadToDescendant("episode") && int.TryParse(reader.ReadElementContentAsString(), out var num))
                            {
                                metadataResult.Item.IndexNumberEnd = Math.Max(num, metadataResult.Item.IndexNumberEnd ?? num);
                            }
                        }
                    }
                }
                catch (XmlException)
                {
                }
            }
        }

        /// <inheritdoc />
        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<Episode> itemResult)
        {
            var item = itemResult.Item;

            switch (reader.Name)
            {
                case "showtitle":
                    item.SeriesName = reader.ReadStringFromNfo() ?? item.SeriesName;
                    break;

                case "season":
                    item.ParentIndexNumber = reader.ReadIntFromNfo() ?? item.ParentIndexNumber;
                    break;

                case "episode":
                    item.IndexNumber = reader.ReadIntFromNfo() ?? item.IndexNumber;
                    break;

                case "episodenumberend":
                    item.IndexNumberEnd = reader.ReadIntFromNfo() ?? item.IndexNumberEnd;
                    break;

                case "airsbefore_episode":
                case "displayepisode":
                    item.AirsBeforeEpisodeNumber = reader.ReadIntFromNfo() ?? item.AirsBeforeEpisodeNumber;
                    break;

                case "airsbefore_season":
                case "displayseason":
                    item.AirsBeforeSeasonNumber = reader.ReadIntFromNfo() ?? item.AirsBeforeSeasonNumber;
                    break;

                case "airsafter_season":
                    item.AirsAfterSeasonNumber = reader.ReadIntFromNfo() ?? item.AirsAfterSeasonNumber;
                    break;

                default:
                    base.FetchDataFromXmlNode(reader, itemResult);
                    break;
            }
        }
    }
}
