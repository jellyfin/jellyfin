using System;
using System.Globalization;
using System.Linq;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    internal static class NfoSubtreeParsers<T>
        where T : BaseItem
    {
        internal static void ReadFileinfoSubtree(XmlReader reader, BaseItem item)
        {
            if (reader.IsEmptyElement)
            {
                reader.Read();
            }

            using var subtreeFileinfo = reader.ReadSubtree();
            subtreeFileinfo.MoveToContent();
            subtreeFileinfo.Read();

            // Loop through each subtree element
            while (!subtreeFileinfo.EOF && subtreeFileinfo.ReadState == ReadState.Interactive)
            {
                if (subtreeFileinfo.NodeType == XmlNodeType.Element)
                {
                    switch (subtreeFileinfo.Name)
                    {
                        case "streamdetails":
                        {
                            if (subtreeFileinfo.IsEmptyElement)
                            {
                                subtreeFileinfo.Read();
                                continue;
                            }

                            using var subtreeStreamdetail = reader.ReadSubtree();
                            ReadStreamdetailSubtree(subtreeStreamdetail, item);

                            break;
                        }

                        default:
                            subtreeFileinfo.Skip();
                            break;
                    }
                }
                else
                {
                    subtreeFileinfo.Read();
                }
            }
        }

        internal static void ReadActorNode(XmlReader reader, MetadataResult<T> metadataResult)
        {
            if (!reader.IsEmptyElement)
            {
                using var subtree = reader.ReadSubtree();
                var person = ReadActorSubtree(subtree);

                if (!string.IsNullOrWhiteSpace(person.Name))
                {
                    metadataResult.AddPerson(person);
                }
            }
            else
            {
                reader.Read();
            }
        }

        internal static void ReadThumbNode(XmlReader reader, MetadataResult<T> metadataResult, IDirectoryService directoryService, ILogger logger)
        {
            var item = (BaseItem)metadataResult.Item;
            var artType = reader.GetAttribute("aspect");
            var val = reader.ReadElementContentAsString();

            // skip:
            // - empty aspect tag
            // - empty uri
            // - tag containing '.' because we can't set images for seasons, episodes or movie sets within series or movies
            if (string.IsNullOrEmpty(artType) || string.IsNullOrEmpty(val) || artType.Contains('.', StringComparison.Ordinal))
            {
                return;
            }

            ImageType imageType = NfoParserHelpers.GetImageType(artType);

            if (!Uri.TryCreate(val, UriKind.Absolute, out var uri))
            {
                logger.LogError("Image location {Path} specified in nfo file for {ItemName} is not a valid URL or file path.", val, item.Name);
                return;
            }

            if (uri.IsFile)
            {
                // only allow one item of each type
                if (metadataResult.Images.Any(x => x.Type == imageType))
                {
                    return;
                }

                var fileSystemMetadata = directoryService.GetFile(val);
                // non existing file returns null
                if (fileSystemMetadata == null || !fileSystemMetadata.Exists)
                {
                    logger.LogWarning("Artwork file {Path} specified in nfo file for {ItemName} does not exist.", uri, item.Name);
                    return;
                }

                metadataResult.Images.Add(new LocalImageInfo()
                {
                    FileInfo = fileSystemMetadata,
                    Type = imageType
                });
            }
            else
            {
                // only allow one item of each type
                if (metadataResult.RemoteImages.Any(x => x.type == imageType))
                {
                    return;
                }

                metadataResult.RemoteImages.Add((uri.ToString(), imageType));
            }
        }

        internal static void ReadPersonInfoFromNfo(XmlReader reader, MetadataResult<T> metadataResult, string personType)
        {
            var val = reader.ReadElementContentAsString();
            foreach (var p in NfoParserHelpers.SplitNames(val).Select(v => new PersonInfo { Name = v.Trim(), Type = personType }))
            {
                if (string.IsNullOrWhiteSpace(p.Name))
                {
                    continue;
                }

                metadataResult.AddPerson(p);
            }
        }

        internal static void ReadSetNode(XmlReader reader, Movie movie)
        {
            var tmdbcolid = reader.GetAttribute("tmdbcolid");
            if (!string.IsNullOrWhiteSpace(tmdbcolid))
            {
                movie.SetProviderId(MetadataProvider.TmdbCollection, tmdbcolid);
            }

            using var subReader = reader.ReadSubtree();
            subReader.MoveToContent();
            subReader.Read();
            subReader.MoveToContent();

            // check if tag content is just text or has child tags
            if (subReader.NodeType == XmlNodeType.Text)
            {
                movie.CollectionName = subReader.ReadString();
            }
            else if (subReader.NodeType == XmlNodeType.Element)
            {
                while (!subReader.EOF && subReader.ReadState == ReadState.Interactive)
                {
                    if (subReader.NodeType == XmlNodeType.Element)
                    {
                        switch (subReader.Name)
                        {
                            case "name":
                                movie.CollectionName = subReader.ReadElementContentAsString();
                                break;
                            default:
                                subReader.Skip();
                                break;
                        }
                    }
                    else
                    {
                        subReader.Read();
                    }
                }
            }
        }

        internal static void ReadRatingsNode(XmlReader parentReader, BaseItem item)
        {
            if (parentReader.IsEmptyElement)
            {
                parentReader.Read();
                return;
            }

            using var reader = parentReader.ReadSubtree();

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "rating":
                        {
                            if (reader.IsEmptyElement)
                            {
                                reader.Read();
                                continue;
                            }

                            var ratingName = reader.GetAttribute("name");

                            using var subtree = reader.ReadSubtree();
                            ReadRatingNode(subtree, item, ratingName);

                            break;
                        }

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

        private static void ReadStreamdetailSubtree(XmlReader reader, BaseItem item)
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
                        case "video":
                        {
                            if (reader.IsEmptyElement)
                            {
                                reader.Read();
                                continue;
                            }

                            using var subtree = reader.ReadSubtree();
                            ReadVideoSubtree(subtree, item);

                            break;
                        }

                        case "subtitle":
                        {
                            if (reader.IsEmptyElement)
                            {
                                reader.Read();
                                continue;
                            }

                            using var subtree = reader.ReadSubtree();
                            ReadSubtitleSubtree(subtree, item);

                            break;
                        }

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

        private static void ReadVideoSubtree(XmlReader reader, BaseItem item)
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
                        case "format3d":
                            {
                                var val = reader.ReadElementContentAsString();

                                var video = item as Video;

                                if (video != null)
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

                        case "aspect":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (item is Video video)
                                {
                                    video.AspectRatio = val;
                                }

                                break;
                            }

                        case "width":
                            {
                                var val = reader.ReadElementContentAsInt();

                                if (item is Video video)
                                {
                                    video.Width = val;
                                }

                                break;
                            }

                        case "height":
                            {
                                var val = reader.ReadElementContentAsInt();

                                if (item is Video video)
                                {
                                    video.Height = val;
                                }

                                break;
                            }

                        case "durationinseconds":
                            {
                                var val = reader.ReadElementContentAsInt();

                                if (item is Video video)
                                {
                                    video.RunTimeTicks = new TimeSpan(0, 0, val).Ticks;
                                }

                                break;
                            }

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

        private static void ReadSubtitleSubtree(XmlReader reader, BaseItem item)
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
                        case "language":
                            {
                                _ = reader.ReadElementContentAsString();

                                if (item is Video video)
                                {
                                    video.HasSubtitles = true;
                                }

                                break;
                            }

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

        private static PersonInfo ReadActorSubtree(XmlReader reader)
        {
            var name = string.Empty;
            var type = PersonType.Actor;  // If type is not specified assume actor
            var role = string.Empty;
            int? sortOrder = null;
            string? imageUrl = null;

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "name":
                            name = reader.ReadStringFromNfo() ?? name;
                            break;

                        case "role":
                            role = reader.ReadStringFromNfo() ?? role;
                            break;

                        case "type":
                            type = NfoParserHelpers.GetPersonType(reader.ReadStringFromNfo() ?? type);
                            break;

                        case "order":
                        case "sortorder":
                            sortOrder = reader.ReadIntFromNfo() ?? sortOrder;
                            break;

                        case "thumb":
                            imageUrl = reader.ReadStringFromNfo() ?? imageUrl;
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

            return new PersonInfo
            {
                Name = name.Trim(),
                Role = role,
                Type = type,
                SortOrder = sortOrder,
                ImageUrl = imageUrl
            };
        }

        private static void ReadRatingNode(XmlReader reader, BaseItem item, string? ratingName)
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
                        case "value":
                            var val = reader.ReadElementContentAsString();

                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                if (float.TryParse(val, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var ratingValue))
                                {
                                    // if ratingName contains tomato --> assume critic rating
                                    if (ratingName != null &&
                                        ratingName.Contains("tomato", StringComparison.OrdinalIgnoreCase) &&
                                        !ratingName.Contains("audience", StringComparison.OrdinalIgnoreCase))
                                    {
                                        item.CriticRating = ratingValue;
                                    }
                                    else
                                    {
                                        item.CommunityRating = ratingValue;
                                    }
                                }
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
    }
}
