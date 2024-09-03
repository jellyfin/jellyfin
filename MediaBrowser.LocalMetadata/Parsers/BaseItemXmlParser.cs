using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.LocalMetadata.Parsers
{
    /// <summary>
    /// Provides a base class for parsing metadata xml.
    /// </summary>
    /// <typeparam name="T">Type of item xml parser.</typeparam>
    public class BaseItemXmlParser<T>
        where T : BaseItem
    {
        private Dictionary<string, string>? _validProviderIds;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemXmlParser{T}" /> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{BaseItemXmlParser}"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        public BaseItemXmlParser(ILogger<BaseItemXmlParser<T>> logger, IProviderManager providerManager)
        {
            Logger = logger;
            ProviderManager = providerManager;
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        protected ILogger<BaseItemXmlParser<T>> Logger { get; private set; }

        /// <summary>
        /// Gets the provider manager.
        /// </summary>
        protected IProviderManager ProviderManager { get; private set; }

        /// <summary>
        /// Fetches metadata for an item from one xml file.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">Item is null.</exception>
        public void Fetch(MetadataResult<T> item, string metadataFile, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentException.ThrowIfNullOrEmpty(metadataFile);

            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.None,
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true
            };

            _validProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var idInfos = ProviderManager.GetExternalIdInfos(item.Item);

            foreach (var info in idInfos)
            {
                var id = info.Key + "Id";
                _validProviderIds.TryAdd(id, info.Key);
            }

            // Additional Mappings
            _validProviderIds.Add("IMDB", "Imdb");

            // Fetch(item, metadataFile, settings, Encoding.GetEncoding("ISO-8859-1"), cancellationToken);
            Fetch(item, metadataFile, settings, Encoding.UTF8, cancellationToken);
        }

        /// <summary>
        /// Fetches the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void Fetch(MetadataResult<T> item, string metadataFile, XmlReaderSettings settings, Encoding encoding, CancellationToken cancellationToken)
        {
            item.ResetPeople();

            using var fileStream = File.OpenRead(metadataFile);
            using var streamReader = new StreamReader(fileStream, encoding);
            using var reader = XmlReader.Create(streamReader, settings);
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

        /// <summary>
        /// Fetches metadata from one Xml Element.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="itemResult">The item result.</param>
        protected virtual void FetchDataFromXmlNode(XmlReader reader, MetadataResult<T> itemResult)
        {
            var item = itemResult.Item;

            switch (reader.Name)
            {
                case "Added":
                    if (reader.TryReadDateTime(out var dateCreated))
                    {
                        item.DateCreated = dateCreated;
                    }

                    break;
                case "OriginalTitle":
                    item.OriginalTitle = reader.ReadNormalizedString();
                    break;
                case "LocalTitle":
                    item.Name = reader.ReadNormalizedString();
                    break;
                case "CriticRating":
                {
                    var text = reader.ReadElementContentAsString();

                    if (float.TryParse(text, CultureInfo.InvariantCulture, out var value))
                    {
                        item.CriticRating = value;
                    }

                    break;
                }

                case "SortTitle":
                    item.ForcedSortName = reader.ReadNormalizedString();
                    break;
                case "Overview":
                case "Description":
                    item.Overview = reader.ReadNormalizedString();
                    break;
                case "Language":
                    item.PreferredMetadataLanguage = reader.ReadNormalizedString();
                    break;
                case "CountryCode":
                    item.PreferredMetadataCountryCode = reader.ReadNormalizedString();
                    break;
                case "PlaceOfBirth":
                    var placeOfBirth = reader.ReadNormalizedString();
                    if (!string.IsNullOrEmpty(placeOfBirth) && item is Person person)
                    {
                        person.ProductionLocations = new[] { placeOfBirth };
                    }

                    break;
                case "LockedFields":
                {
                    var val = reader.ReadElementContentAsString();

                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        item.LockedFields = val.Split('|').Select(i =>
                        {
                            if (Enum.TryParse(i, true, out MetadataField field))
                            {
                                return (MetadataField?)field;
                            }

                            return null;
                        }).Where(i => i.HasValue).Select(i => i!.Value).ToArray();
                    }

                    break;
                }

                case "TagLines":
                {
                    if (!reader.IsEmptyElement)
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            FetchFromTaglinesNode(subtree, item);
                        }
                    }
                    else
                    {
                        reader.Read();
                    }

                    break;
                }

                case "Countries":
                {
                    if (!reader.IsEmptyElement)
                    {
                        reader.Skip();
                    }
                    else
                    {
                        reader.Read();
                    }

                    break;
                }

                case "ContentRating":
                case "MPAARating":
                    item.OfficialRating = reader.ReadNormalizedString();
                    break;
                case "CustomRating":
                    item.CustomRating = reader.ReadNormalizedString();
                    break;
                case "RunningTime":
                    var runtimeText = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(runtimeText))
                    {
                        if (int.TryParse(runtimeText.AsSpan().LeftPart(' '), NumberStyles.Integer, CultureInfo.InvariantCulture, out var runtime))
                        {
                            item.RunTimeTicks = TimeSpan.FromMinutes(runtime).Ticks;
                        }
                    }

                    break;
                case "AspectRatio":
                    var aspectRatio = reader.ReadNormalizedString();
                    if (!string.IsNullOrEmpty(aspectRatio) && item is IHasAspectRatio hasAspectRatio)
                    {
                        hasAspectRatio.AspectRatio = aspectRatio;
                    }

                    break;
                case "LockData":
                    item.IsLocked = string.Equals(reader.ReadElementContentAsString(), "true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "Network":
                    foreach (var name in reader.GetStringArray())
                    {
                        item.AddStudio(name);
                    }

                    break;
                case "Director":
                    foreach (var director in reader.GetPersonArray(PersonKind.Director))
                    {
                        itemResult.AddPerson(director);
                    }

                    break;
                case "Writer":
                    foreach (var writer in reader.GetPersonArray(PersonKind.Writer))
                    {
                        itemResult.AddPerson(writer);
                    }

                    break;
                case "Actors":
                    foreach (var actor in reader.GetPersonArray(PersonKind.Actor))
                    {
                        itemResult.AddPerson(actor);
                    }

                    break;
                case "GuestStars":
                    foreach (var guestStar in reader.GetPersonArray(PersonKind.GuestStar))
                    {
                        itemResult.AddPerson(guestStar);
                    }

                    break;
                case "Trailer":
                    var trailer = reader.ReadNormalizedString();
                    if (!string.IsNullOrEmpty(trailer))
                    {
                        item.AddTrailerUrl(trailer);
                    }

                    break;
                case "DisplayOrder":
                    var displayOrder = reader.ReadNormalizedString();
                    if (!string.IsNullOrEmpty(displayOrder) && item is IHasDisplayOrder hasDisplayOrder)
                    {
                        hasDisplayOrder.DisplayOrder = displayOrder;
                    }

                    break;
                case "Trailers":
                {
                    if (!reader.IsEmptyElement)
                    {
                        using var subtree = reader.ReadSubtree();
                        FetchDataFromTrailersNode(subtree, item);
                    }
                    else
                    {
                        reader.Read();
                    }

                    break;
                }

                case "ProductionYear":
                    if (reader.TryReadInt(out var productionYear) && productionYear > 1850)
                    {
                        item.ProductionYear = productionYear;
                    }

                    break;
                case "Rating":
                case "IMDBrating":
                {
                    var rating = reader.ReadElementContentAsString();

                    if (!string.IsNullOrWhiteSpace(rating))
                    {
                        // All external meta is saving this as '.' for decimal I believe...but just to be sure
                        if (float.TryParse(rating.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var val))
                        {
                            item.CommunityRating = val;
                        }
                    }

                    break;
                }

                case "BirthDate":
                case "PremiereDate":
                case "FirstAired":
                    if (reader.TryReadDateTimeExact("yyyy-MM-dd", out var firstAired))
                    {
                        item.PremiereDate = firstAired;
                        item.ProductionYear = firstAired.Year;
                    }

                    break;
                case "DeathDate":
                case "EndDate":
                    if (reader.TryReadDateTimeExact("yyyy-MM-dd", out var endDate))
                    {
                        item.EndDate = endDate;
                    }

                    break;
                case "CollectionNumber":
                    var tmdbCollection = reader.ReadNormalizedString();
                    item.TrySetProviderId(MetadataProvider.TmdbCollection, tmdbCollection);

                    break;

                case "Genres":
                {
                    if (!reader.IsEmptyElement)
                    {
                        using var subtree = reader.ReadSubtree();
                        FetchFromGenresNode(subtree, item);
                    }
                    else
                    {
                        reader.Read();
                    }

                    break;
                }

                case "Tags":
                {
                    if (!reader.IsEmptyElement)
                    {
                        using var subtree = reader.ReadSubtree();
                        FetchFromTagsNode(subtree, item);
                    }
                    else
                    {
                        reader.Read();
                    }

                    break;
                }

                case "Persons":
                {
                    if (!reader.IsEmptyElement)
                    {
                        using var subtree = reader.ReadSubtree();
                        FetchDataFromPersonsNode(subtree, itemResult);
                    }
                    else
                    {
                        reader.Read();
                    }

                    break;
                }

                case "Studios":
                {
                    if (!reader.IsEmptyElement)
                    {
                        using var subtree = reader.ReadSubtree();
                        FetchFromStudiosNode(subtree, item);
                    }
                    else
                    {
                        reader.Read();
                    }

                    break;
                }

                case "Shares":
                {
                    if (!reader.IsEmptyElement)
                    {
                        using var subtree = reader.ReadSubtree();
                        if (item is IHasShares hasShares)
                        {
                            FetchFromSharesNode(subtree, hasShares);
                        }
                    }
                    else
                    {
                        reader.Read();
                    }

                    break;
                }

                case "OwnerUserId":
                {
                    var val = reader.ReadElementContentAsString();

                    if (Guid.TryParse(val, out var guid) && !guid.Equals(Guid.Empty))
                    {
                        if (item is Playlist playlist)
                        {
                            playlist.OwnerUserId = guid;
                        }
                    }

                    break;
                }

                case "Format3D":
                {
                    var val = reader.ReadElementContentAsString();

                    if (item is Video video)
                    {
                        if (string.Equals("HSBS", val, StringComparison.OrdinalIgnoreCase))
                        {
                            video.Video3DFormat = Video3DFormat.HalfSideBySide;
                        }
                        else if (string.Equals("HTAB", val, StringComparison.OrdinalIgnoreCase))
                        {
                            video.Video3DFormat = Video3DFormat.HalfTopAndBottom;
                        }
                        else if (string.Equals("FTAB", val, StringComparison.OrdinalIgnoreCase))
                        {
                            video.Video3DFormat = Video3DFormat.FullTopAndBottom;
                        }
                        else if (string.Equals("FSBS", val, StringComparison.OrdinalIgnoreCase))
                        {
                            video.Video3DFormat = Video3DFormat.FullSideBySide;
                        }
                        else if (string.Equals("MVC", val, StringComparison.OrdinalIgnoreCase))
                        {
                            video.Video3DFormat = Video3DFormat.MVC;
                        }
                    }

                    break;
                }

                default:
                {
                    string readerName = reader.Name;
                    if (_validProviderIds!.TryGetValue(readerName, out string? providerIdValue))
                    {
                        var id = reader.ReadElementContentAsString();
                        item.TrySetProviderId(providerIdValue, id);
                    }
                    else
                    {
                        reader.Skip();
                    }

                    break;
                }
            }
        }

        private void FetchFromSharesNode(XmlReader reader, IHasShares item)
        {
            var list = new List<PlaylistUserPermissions>();

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Share":
                        {
                            if (reader.IsEmptyElement)
                            {
                                reader.Read();
                                continue;
                            }

                            using (var subReader = reader.ReadSubtree())
                            {
                                var child = GetShare(subReader);

                                if (child is not null)
                                {
                                    list.Add(child);
                                }
                            }

                            break;
                        }

                        default:
                        {
                            reader.Skip();
                            break;
                        }
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            item.Shares = [.. list];
        }

        /// <summary>
        /// Fetches from taglines node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private void FetchFromTaglinesNode(XmlReader reader, T item)
        {
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Tagline":
                            item.Tagline = reader.ReadNormalizedString();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }
        }

        /// <summary>
        /// Fetches from genres node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private void FetchFromGenresNode(XmlReader reader, T item)
        {
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Genre":
                            var genre = reader.ReadNormalizedString();
                            if (!string.IsNullOrEmpty(genre))
                            {
                                item.AddGenre(genre);
                            }

                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }
        }

        private void FetchFromTagsNode(XmlReader reader, BaseItem item)
        {
            reader.MoveToContent();
            reader.Read();

            var tags = new List<string>();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Tag":
                            var tag = reader.ReadNormalizedString();
                            if (!string.IsNullOrEmpty(tag))
                            {
                                tags.Add(tag);
                            }

                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            item.Tags = tags.Distinct(StringComparer.Ordinal).ToArray();
        }

        /// <summary>
        /// Fetches the data from persons node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private void FetchDataFromPersonsNode(XmlReader reader, MetadataResult<T> item)
        {
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Person":
                        case "Actor":
                            var person = reader.GetPersonFromXmlNode();
                            if (person is not null)
                            {
                                item.AddPerson(person);
                            }

                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }
        }

        private void FetchDataFromTrailersNode(XmlReader reader, T item)
        {
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Trailer":
                            var trailer = reader.ReadNormalizedString();
                            if (!string.IsNullOrEmpty(trailer))
                            {
                                item.AddTrailerUrl(trailer);
                            }

                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }
        }

        /// <summary>
        /// Fetches from studios node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private void FetchFromStudiosNode(XmlReader reader, T item)
        {
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Studio":
                            var studio = reader.ReadNormalizedString();
                            if (!string.IsNullOrEmpty(studio))
                            {
                                item.AddStudio(studio);
                            }

                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }
        }

        /// <summary>
        /// Get linked child.
        /// </summary>
        /// <param name="reader">The xml reader.</param>
        /// <returns>The linked child.</returns>
        protected LinkedChild? GetLinkedChild(XmlReader reader)
        {
            var linkedItem = new LinkedChild { Type = LinkedChildType.Manual };

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Path":
                            linkedItem.Path = reader.ReadNormalizedString();
                            break;
                        case "ItemId":
                            linkedItem.LibraryItemId = reader.ReadNormalizedString();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            // This is valid
            if (!string.IsNullOrWhiteSpace(linkedItem.Path) || !string.IsNullOrWhiteSpace(linkedItem.LibraryItemId))
            {
                return linkedItem;
            }

            return null;
        }

        /// <summary>
        /// Get share.
        /// </summary>
        /// <param name="reader">The xml reader.</param>
        /// <returns>The share.</returns>
        protected PlaylistUserPermissions? GetShare(XmlReader reader)
        {
            reader.MoveToContent();
            reader.Read();
            string? userId = null;
            var canEdit = false;

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "UserId":
                            userId = reader.ReadNormalizedString();
                            break;
                        case "CanEdit":
                            canEdit = string.Equals(reader.ReadElementContentAsString(), "true", StringComparison.OrdinalIgnoreCase);
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            // This is valid
            if (!string.IsNullOrWhiteSpace(userId) && Guid.TryParse(userId, out var guid))
            {
                return new PlaylistUserPermissions(guid, canEdit);
            }

            return null;
        }
    }
}
