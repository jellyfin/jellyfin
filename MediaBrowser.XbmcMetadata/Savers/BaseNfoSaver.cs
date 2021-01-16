#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public abstract class BaseNfoSaver : IMetadataFileSaver
    {
        private static readonly HashSet<string> _commonTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "plot",
            "customrating",
            "lockdata",
            "dateadded",
            "title",
            "rating",
            "year",
            "sorttitle",
            "mpaa",
            "aspectratio",
            "collectionnumber",
            "tmdbid",
            "rottentomatoesid",
            "language",
            "tvcomid",
            "tagline",
            "studio",
            "genre",
            "tag",
            "runtime",
            "actor",
            "criticrating",
            "fileinfo",
            "director",
            "writer",
            "trailer",
            "premiered",
            "releasedate",
            "outline",
            "id",
            "credits",
            "originaltitle",
            "watched",
            "playcount",
            "lastplayed",
            "art",
            "resume",
            "biography",
            "formed",
            "review",
            "style",
            "imdbid",
            "imdb_id",
            "country",
            "audiodbalbumid",
            "audiodbartistid",
            "enddate",
            "lockedfields",
            "zap2itid",
            "tvrageid",

            "musicbrainzartistid",
            "musicbrainzalbumartistid",
            "musicbrainzalbumid",
            "musicbrainzreleasegroupid",
            "tvdbid",
            "collectionitem",

            "isuserfavorite",
            "userrating",

            "countrycode"
        };

        protected BaseNfoSaver(
            IFileSystem fileSystem,
            IServerConfigurationManager configurationManager,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IUserDataManager userDataManager,
            ILogger<BaseNfoSaver> logger)
        {
            Logger = logger;
            UserDataManager = userDataManager;
            UserManager = userManager;
            LibraryManager = libraryManager;
            ConfigurationManager = configurationManager;
            FileSystem = fileSystem;
        }

        protected IFileSystem FileSystem { get; }

        protected IServerConfigurationManager ConfigurationManager { get; }

        protected ILibraryManager LibraryManager { get; }

        protected IUserManager UserManager { get; }

        protected IUserDataManager UserDataManager { get; }

        protected ILogger<BaseNfoSaver> Logger { get; }

        /// <inheritdoc />
        public string Name => SaverName;

        public static string SaverName => "Nfo";

        /// <inheritdoc />
        public string GetSavePath(BaseItem item)
            => GetLocalSavePath(item);

        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><see cref="string" />.</returns>
        protected abstract string GetLocalSavePath(BaseItem item);

        /// <summary>
        /// Gets the name of the root element.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><see cref="string" />.</returns>
        protected abstract string GetRootElementName(BaseItem item);

        /// <inheritdoc />
        public abstract bool IsEnabledFor(BaseItem item, ItemUpdateType updateType);

        protected virtual List<string> GetTagsUsed(BaseItem item)
        {
            var list = new List<string>();
            foreach (var providerKey in item.ProviderIds.Keys)
            {
                var providerIdTagName = GetTagForProviderKey(providerKey);
                if (!_commonTags.Contains(providerIdTagName))
                {
                    list.Add(providerIdTagName);
                }
            }

            return list;
        }

        /// <inheritdoc />
        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var path = GetSavePath(item);

            using (var memoryStream = new MemoryStream())
            {
                Save(item, memoryStream, path);
            }
        }

        private void Save(BaseItem item, Stream stream, string xmlPath)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                CloseOutput = false
            };

            using (var writer = XmlWriter.Create(stream, settings))
            {
                var root = GetRootElementName(item);

                writer.WriteStartDocument(true);

                writer.WriteStartElement(root);

                var baseItem = item;

                if (baseItem != null)
                {
                    AddCommonNodes(baseItem, writer, LibraryManager, UserManager, UserDataManager, ConfigurationManager);
                }

                // leave the tags we don't know as is
                var tagsUsed = GetTagsUsed(item);

                try
                {
                    AddCustomTags(xmlPath, tagsUsed, writer, Logger);
                }
                catch (XmlException ex)
                {
                    Logger.LogError(ex, "Error reading existing nfo");
                }

                writer.WriteEndElement();

                writer.WriteEndDocument();
            }
        }

        protected abstract void WriteCustomElements(BaseItem item, XmlWriter writer);

        /// <summary>
        /// Adds the common nodes.
        /// </summary>
        private void AddCommonNodes(
            BaseItem item,
            XmlWriter writer,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IUserDataManager userDataRepo,
            IServerConfigurationManager config)
        {
            var writtenProviderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (item.ProviderIds != null)
            {
                foreach (var providerKey in item.ProviderIds.Keys)
                {
                    var providerId = item.ProviderIds[providerKey];
                    if (!string.IsNullOrEmpty(providerId) && !writtenProviderIds.Contains(providerKey))
                    {
                        try
                        {
                            var tagName = GetTagForProviderKey(providerKey);
                            Logger.LogDebug("Verifying custom provider tagname {0}", tagName);
                            XmlConvert.VerifyName(tagName);
                            Logger.LogDebug("Saving custom provider tagname {0}", tagName);

                            writer.WriteElementString(GetTagForProviderKey(providerKey), providerId);
                        }
                        catch (ArgumentException)
                        {
                            // catch invalid names without failing the entire operation
                        }
                        catch (XmlException)
                        {
                            // catch invalid names without failing the entire operation
                        }
                    }
                }
            }
        }

        private void AddCustomTags(string path, List<string> xmlTagsUsed, XmlWriter writer, ILogger<BaseNfoSaver> logger)
        {
            var settings = new XmlReaderSettings()
            {
                ValidationType = ValidationType.None,
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true
            };

            using (var fileStream = File.OpenRead(path))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            using (var reader = XmlReader.Create(streamReader, settings))
            {
                try
                {
                    reader.MoveToContent();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error reading existing xml tags from {Path}.", path);
                    return;
                }

                reader.Read();

                // Loop through each element
                while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        var name = reader.Name;

                        if (!_commonTags.Contains(name)
                            && !xmlTagsUsed.Contains(name, StringComparer.OrdinalIgnoreCase))
                        {
                            writer.WriteNode(reader, false);
                        }
                        else
                        {
                            reader.Skip();
                        }
                    }
                    else
                    {
                        reader.Read();
                    }
                }
            }
        }

        private string GetTagForProviderKey(string providerKey)
            => providerKey.ToLowerInvariant() + "id";
    }
}
