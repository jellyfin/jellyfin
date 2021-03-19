using System;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    internal static class NfoSubtreeParsers
    {
        internal static void ReadFileinfoSubtree(XmlReader reader, BaseItem item)
        {
            if (!reader.IsEmptyElement)
            {
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
            else
            {
                reader.Read();
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
    }
}
