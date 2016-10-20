using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml;
using CommonIO;

namespace MediaBrowser.LocalMetadata.Savers
{
    /// <summary>
    /// Class XmlHelpers
    /// </summary>
    public static class XmlSaverHelpers
    {
        private static readonly Dictionary<string, string> CommonTags = new[] {     
               
                    "Added",
                    "AspectRatio",
                    "AudioDbAlbumId",
                    "AudioDbArtistId",
                    "AwardSummary",
                    "BirthDate",
                    "Budget",
                    
                    // Deprecated. No longer saving in this field.
                    "certification",
                    
                    "Chapters",
                    "ContentRating",
                    "Countries",
                    "CustomRating",
                    "CriticRating",
                    "CriticRatingSummary",
                    "DeathDate",
                    "DisplayOrder",
                    "EndDate",
                    "Genres",
                    "Genre",
                    "GamesDbId",
                    
                    // Deprecated. No longer saving in this field.
                    "IMDB_ID",
                    
                    "IMDB",
                    
                    // Deprecated. No longer saving in this field.
                    "IMDbId",
                    
                    "Language",
                    "LocalTitle",
                    "OriginalTitle",
                    "LockData",
                    "LockedFields",
                    "Format3D",
                    "Metascore",
                    
                    // Deprecated. No longer saving in this field.
                    "MPAARating",

                    "MPAADescription",

                    "MusicBrainzArtistId",
                    "MusicBrainzAlbumArtistId",
                    "MusicBrainzAlbumId",
                    "MusicBrainzReleaseGroupId",

                    // Deprecated. No longer saving in this field.
                    "MusicbrainzId",

                    "Overview",
                    "ShortOverview",
                    "Persons",
                    "PlotKeywords",
                    "PremiereDate",
                    "ProductionYear",
                    "Rating",
                    "Revenue",
                    "RottenTomatoesId",
                    "RunningTime",
                    
                    // Deprecated. No longer saving in this field.
                    "Runtime",
                    
                    "SortTitle",
                    "Studios",
                    "Tags",
                    
                    // Deprecated. No longer saving in this field.
                    "TagLine",

                    "Taglines",
                    "TMDbCollectionId",
                    "TMDbId",

                    // Deprecated. No longer saving in this field.
                    "Trailer",

                    "Trailers",
                    "TVcomId",
                    "TvDbId",
                    "Type",
                    "TVRageId",
                    "VoteCount",
                    "Website",
                    "Zap2ItId",
                    "CollectionItems",
                    "PlaylistItems",
                    "Shares"

        }.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The us culture
        /// </summary>
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Saves the specified XML.
        /// </summary>
        /// <param name="xml">The XML.</param>
        /// <param name="path">The path.</param>
        /// <param name="xmlTagsUsed">The XML tags used.</param>
        public static void Save(StringBuilder xml, string path, List<string> xmlTagsUsed, IServerConfigurationManager config, IFileSystem fileSystem)
        {
            if (fileSystem.FileExists(path))
            {
                var position = xml.ToString().LastIndexOf("</", StringComparison.OrdinalIgnoreCase);
                xml.Insert(position, GetCustomTags(path, xmlTagsUsed));
            }

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml.ToString());

            //Add the new node to the document.
            xmlDocument.InsertBefore(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", "yes"), xmlDocument.DocumentElement);

            fileSystem.CreateDirectory(Path.GetDirectoryName(path));

            var wasHidden = false;

            var file = new FileInfo(path);

            // This will fail if the file is hidden
            if (file.Exists)
            {
                if ((file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    file.Attributes &= ~FileAttributes.Hidden;

                    wasHidden = true;
                }
            }

            using (var filestream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using (var streamWriter = new StreamWriter(filestream, Encoding.UTF8))
                {
                    xmlDocument.Save(streamWriter);
                }
            }

            if (wasHidden || config.Configuration.SaveMetadataHidden)
            {
                file.Refresh();

                // Add back the attribute
                file.Attributes |= FileAttributes.Hidden;
            }
        }

        /// <summary>
        /// Gets the custom tags.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="xmlTagsUsed">The XML tags used.</param>
        /// <returns>System.String.</returns>
        private static string GetCustomTags(string path, List<string> xmlTagsUsed)
        {
            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            var builder = new StringBuilder();

            using (var streamReader = new StreamReader(path, Encoding.UTF8))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, settings))
                {
                    reader.MoveToContent();

                    // Loop through each element
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            var name = reader.Name;

                            if (!CommonTags.ContainsKey(name) && !xmlTagsUsed.Contains(name, StringComparer.OrdinalIgnoreCase))
                            {
                                builder.AppendLine(reader.ReadOuterXml());
                            }
                            else
                            {
                                reader.Skip();
                            }
                        }
                    }
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Adds the common nodes.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="builder">The builder.</param>
        public static void AddCommonNodes(BaseItem item, ILibraryManager libraryManager, StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(item.OfficialRating))
            {
                builder.Append("<ContentRating>" + SecurityElement.Escape(item.OfficialRating) + "</ContentRating>");
            }

            if (!string.IsNullOrEmpty(item.OfficialRatingDescription))
            {
                builder.Append("<MPAADescription>" + SecurityElement.Escape(item.OfficialRatingDescription) + "</MPAADescription>");
            }

            builder.Append("<Added>" + SecurityElement.Escape(item.DateCreated.ToLocalTime().ToString("G")) + "</Added>");

            builder.Append("<LockData>" + item.IsLocked.ToString().ToLower() + "</LockData>");

            if (item.LockedFields.Count > 0)
            {
                builder.Append("<LockedFields>" + string.Join("|", item.LockedFields.Select(i => i.ToString()).ToArray()) + "</LockedFields>");
            }

            if (!string.IsNullOrEmpty(item.DisplayMediaType))
            {
                builder.Append("<Type>" + SecurityElement.Escape(item.DisplayMediaType) + "</Type>");
            }

            if (item.CriticRating.HasValue)
            {
                builder.Append("<CriticRating>" + SecurityElement.Escape(item.CriticRating.Value.ToString(UsCulture)) + "</CriticRating>");
            }

            if (!string.IsNullOrEmpty(item.CriticRatingSummary))
            {
                builder.Append("<CriticRatingSummary><![CDATA[" + item.CriticRatingSummary + "]]></CriticRatingSummary>");
            }

            if (!string.IsNullOrEmpty(item.Overview))
            {
                builder.Append("<Overview><![CDATA[" + item.Overview + "]]></Overview>");
            }

            var hasOriginalTitle = item as IHasOriginalTitle;
            if (hasOriginalTitle != null)
            {
                if (!string.IsNullOrEmpty(hasOriginalTitle.OriginalTitle))
                {
                    builder.Append("<OriginalTitle>" + SecurityElement.Escape(hasOriginalTitle.OriginalTitle) + "</OriginalTitle>");
                }
            }

            if (!string.IsNullOrEmpty(item.ShortOverview))
            {
                builder.Append("<ShortOverview><![CDATA[" + item.ShortOverview + "]]></ShortOverview>");
            }

            if (!string.IsNullOrEmpty(item.CustomRating))
            {
                builder.Append("<CustomRating>" + SecurityElement.Escape(item.CustomRating) + "</CustomRating>");
            }

            if (!string.IsNullOrEmpty(item.Name) && !(item is Episode))
            {
                builder.Append("<LocalTitle>" + SecurityElement.Escape(item.Name) + "</LocalTitle>");
            }

            if (!string.IsNullOrEmpty(item.ForcedSortName))
            {
                builder.Append("<SortTitle>" + SecurityElement.Escape(item.ForcedSortName) + "</SortTitle>");
            }

            if (item.PremiereDate.HasValue)
            {
                if (item is Person)
                {
                    builder.Append("<BirthDate>" + SecurityElement.Escape(item.PremiereDate.Value.ToLocalTime().ToString("yyyy-MM-dd")) + "</BirthDate>");
                }
                else if (!(item is Episode))
                {
                    builder.Append("<PremiereDate>" + SecurityElement.Escape(item.PremiereDate.Value.ToLocalTime().ToString("yyyy-MM-dd")) + "</PremiereDate>");
                }
            }

            if (item.EndDate.HasValue)
            {
                if (item is Person)
                {
                    builder.Append("<DeathDate>" + SecurityElement.Escape(item.EndDate.Value.ToString("yyyy-MM-dd")) + "</DeathDate>");
                }
                else if (!(item is Episode))
                {
                    builder.Append("<EndDate>" + SecurityElement.Escape(item.EndDate.Value.ToString("yyyy-MM-dd")) + "</EndDate>");
                }
            }

            var hasTrailers = item as IHasTrailers;
            if (hasTrailers != null)
            {
                if (hasTrailers.RemoteTrailers.Count > 0)
                {
                    builder.Append("<Trailers>");

                    foreach (var trailer in hasTrailers.RemoteTrailers)
                    {
                        builder.Append("<Trailer>" + SecurityElement.Escape(trailer.Url) + "</Trailer>");
                    }

                    builder.Append("</Trailers>");
                }
            }

            //if (hasProductionLocations.ProductionLocations.Count > 0)
            //{
            //    builder.Append("<Countries>");

            //    foreach (var name in hasProductionLocations.ProductionLocations)
            //    {
            //        builder.Append("<Country>" + SecurityElement.Escape(name) + "</Country>");
            //    }

            //    builder.Append("</Countries>");
            //}

            var hasDisplayOrder = item as IHasDisplayOrder;
            if (hasDisplayOrder != null && !string.IsNullOrEmpty(hasDisplayOrder.DisplayOrder))
            {
                builder.Append("<DisplayOrder>" + SecurityElement.Escape(hasDisplayOrder.DisplayOrder) + "</DisplayOrder>");
            }

            var hasMetascore = item as IHasMetascore;
            if (hasMetascore != null && hasMetascore.Metascore.HasValue)
            {
                builder.Append("<Metascore>" + SecurityElement.Escape(hasMetascore.Metascore.Value.ToString(UsCulture)) + "</Metascore>");
            }

            var hasAwards = item as IHasAwards;
            if (hasAwards != null && !string.IsNullOrEmpty(hasAwards.AwardSummary))
            {
                builder.Append("<AwardSummary>" + SecurityElement.Escape(hasAwards.AwardSummary) + "</AwardSummary>");
            }

            var hasBudget = item as IHasBudget;
            if (hasBudget != null)
            {
                if (hasBudget.Budget.HasValue)
                {
                    builder.Append("<Budget>" + SecurityElement.Escape(hasBudget.Budget.Value.ToString(UsCulture)) + "</Budget>");
                }

                if (hasBudget.Revenue.HasValue)
                {
                    builder.Append("<Revenue>" + SecurityElement.Escape(hasBudget.Revenue.Value.ToString(UsCulture)) + "</Revenue>");
                }
            }

            if (item.CommunityRating.HasValue)
            {
                builder.Append("<Rating>" + SecurityElement.Escape(item.CommunityRating.Value.ToString(UsCulture)) + "</Rating>");
            }
            if (item.VoteCount.HasValue)
            {
                builder.Append("<VoteCount>" + SecurityElement.Escape(item.VoteCount.Value.ToString(UsCulture)) + "</VoteCount>");
            }

            if (item.ProductionYear.HasValue && !(item is Person))
            {
                builder.Append("<ProductionYear>" + SecurityElement.Escape(item.ProductionYear.Value.ToString(UsCulture)) + "</ProductionYear>");
            }

            if (!string.IsNullOrEmpty(item.HomePageUrl))
            {
                builder.Append("<Website>" + SecurityElement.Escape(item.HomePageUrl) + "</Website>");
            }

            var hasAspectRatio = item as IHasAspectRatio;
            if (hasAspectRatio != null)
            {
                if (!string.IsNullOrEmpty(hasAspectRatio.AspectRatio))
                {
                    builder.Append("<AspectRatio>" + SecurityElement.Escape(hasAspectRatio.AspectRatio) + "</AspectRatio>");
                }
            }

            if (!string.IsNullOrEmpty(item.PreferredMetadataLanguage))
            {
                builder.Append("<Language>" + SecurityElement.Escape(item.PreferredMetadataLanguage) + "</Language>");
            }
            if (!string.IsNullOrEmpty(item.PreferredMetadataCountryCode))
            {
                builder.Append("<CountryCode>" + SecurityElement.Escape(item.PreferredMetadataCountryCode) + "</CountryCode>");
            }

            // Use original runtime here, actual file runtime later in MediaInfo
            var runTimeTicks = item.RunTimeTicks;

            if (runTimeTicks.HasValue)
            {
                var timespan = TimeSpan.FromTicks(runTimeTicks.Value);

                builder.Append("<RunningTime>" + Convert.ToInt32(timespan.TotalMinutes).ToString(UsCulture) + "</RunningTime>");
            }

            if (item.ProviderIds != null)
            {
                foreach (var providerKey in item.ProviderIds.Keys)
                {
                    var providerId = item.ProviderIds[providerKey];
                    if (!string.IsNullOrEmpty(providerId))
                    {
                        builder.Append(string.Format("<{0}>{1}</{0}>", providerKey + "Id", SecurityElement.Escape(providerId)));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(item.Tagline))
            {
                builder.Append("<Taglines>");
                builder.Append("<Tagline>" + SecurityElement.Escape(item.Tagline) + "</Tagline>");
                builder.Append("</Taglines>");
            }

            if (item.Genres.Count > 0)
            {
                builder.Append("<Genres>");

                foreach (var genre in item.Genres)
                {
                    builder.Append("<Genre>" + SecurityElement.Escape(genre) + "</Genre>");
                }

                builder.Append("</Genres>");
            }

            if (item.Studios.Count > 0)
            {
                builder.Append("<Studios>");

                foreach (var studio in item.Studios)
                {
                    builder.Append("<Studio>" + SecurityElement.Escape(studio) + "</Studio>");
                }

                builder.Append("</Studios>");
            }

            if (item.Tags.Count > 0)
            {
                builder.Append("<Tags>");

                foreach (var tag in item.Tags)
                {
                    builder.Append("<Tag>" + SecurityElement.Escape(tag) + "</Tag>");
                }

                builder.Append("</Tags>");
            }

            if (item.Keywords.Count > 0)
            {
                builder.Append("<PlotKeywords>");

                foreach (var tag in item.Keywords)
                {
                    builder.Append("<PlotKeyword>" + SecurityElement.Escape(tag) + "</PlotKeyword>");
                }

                builder.Append("</PlotKeywords>");
            }

            var people = libraryManager.GetPeople(item);

            if (people.Count > 0)
            {
                builder.Append("<Persons>");

                foreach (var person in people)
                {
                    builder.Append("<Person>");
                    builder.Append("<Name>" + SecurityElement.Escape(person.Name) + "</Name>");
                    builder.Append("<Type>" + SecurityElement.Escape(person.Type) + "</Type>");
                    builder.Append("<Role>" + SecurityElement.Escape(person.Role) + "</Role>");

                    if (person.SortOrder.HasValue)
                    {
                        builder.Append("<SortOrder>" + SecurityElement.Escape(person.SortOrder.Value.ToString(UsCulture)) + "</SortOrder>");
                    }

                    builder.Append("</Person>");
                }

                builder.Append("</Persons>");
            }

            var boxset = item as BoxSet;
            if (boxset != null)
            {
                AddLinkedChildren(boxset, builder, "CollectionItems", "CollectionItem");
            }

            var playlist = item as Playlist;
            if (playlist != null)
            {
                AddLinkedChildren(playlist, builder, "PlaylistItems", "PlaylistItem");
            }

            var hasShares = item as IHasShares;
            if (hasShares != null)
            {
                AddShares(hasShares, builder);
            }
        }

        public static void AddShares(IHasShares item, StringBuilder builder)
        {
            builder.Append("<Shares>");

            foreach (var share in item.Shares)
            {
                builder.Append("<Share>");

                builder.Append("<UserId>" + SecurityElement.Escape(share.UserId) + "</UserId>");
                builder.Append("<CanEdit>" + SecurityElement.Escape(share.CanEdit.ToString().ToLower()) + "</CanEdit>");

                builder.Append("</Share>");
            }

            builder.Append("</Shares>");
        }

        public static void AddChapters(Video item, StringBuilder builder, IItemRepository repository)
        {
            var chapters = repository.GetChapters(item.Id);

            builder.Append("<Chapters>");

            foreach (var chapter in chapters)
            {
                builder.Append("<Chapter>");
                builder.Append("<Name>" + SecurityElement.Escape(chapter.Name) + "</Name>");

                var time = TimeSpan.FromTicks(chapter.StartPositionTicks);
                var ms = Convert.ToInt64(time.TotalMilliseconds);

                builder.Append("<StartPositionMs>" + SecurityElement.Escape(ms.ToString(UsCulture)) + "</StartPositionMs>");
                builder.Append("</Chapter>");
            }

            builder.Append("</Chapters>");
        }

        /// <summary>
        /// Appends the media info.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void AddMediaInfo<T>(T item, StringBuilder builder, IItemRepository itemRepository)
            where T : BaseItem
        {
            var video = item as Video;

            if (video != null)
            {
                //AddChapters(video, builder, itemRepository);

                if (video.Video3DFormat.HasValue)
                {
                    switch (video.Video3DFormat.Value)
                    {
                        case Video3DFormat.FullSideBySide:
                            builder.Append("<Format3D>FSBS</Format3D>");
                            break;
                        case Video3DFormat.FullTopAndBottom:
                            builder.Append("<Format3D>FTAB</Format3D>");
                            break;
                        case Video3DFormat.HalfSideBySide:
                            builder.Append("<Format3D>HSBS</Format3D>");
                            break;
                        case Video3DFormat.HalfTopAndBottom:
                            builder.Append("<Format3D>HTAB</Format3D>");
                            break;
                        case Video3DFormat.MVC:
                            builder.Append("<Format3D>MVC</Format3D>");
                            break;
                    }
                }
            }
        }

        public static void AddLinkedChildren(Folder item, StringBuilder builder, string pluralNodeName, string singularNodeName)
        {
            var items = item.LinkedChildren
                .Where(i => i.Type == LinkedChildType.Manual)
                .ToList();

            if (items.Count == 0)
            {
                return;
            }

            builder.Append("<" + pluralNodeName + ">");
            foreach (var link in items)
            {
                builder.Append("<" + singularNodeName + ">");

                if (!string.IsNullOrWhiteSpace(link.Path))
                {
                    builder.Append("<Path>" + SecurityElement.Escape((link.Path)) + "</Path>");
                }

                builder.Append("</" + singularNodeName + ">");
            }
            builder.Append("</" + pluralNodeName + ">");
        }
    }
}
