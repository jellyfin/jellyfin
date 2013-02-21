using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Plugins.Trailers
{
    /// <summary>
    /// Fetches Apple's list of current movie trailers
    /// </summary>
    public static class AppleTrailerListingDownloader
    {
        /// <summary>
        /// The trailer feed URL
        /// </summary>
        private const string TrailerFeedUrl = "http://trailers.apple.com/trailers/home/xml/current_720p.xml";

        /// <summary>
        /// Downloads a list of trailer info's from the apple url
        /// </summary>
        /// <returns>Task{List{TrailerInfo}}.</returns>
        public static async Task<List<TrailerInfo>> GetTrailerList(CancellationToken cancellationToken)
        {
            var stream = await Kernel.Instance.HttpManager.Get(TrailerFeedUrl, Kernel.Instance.ResourcePools.AppleTrailerVideos, cancellationToken).ConfigureAwait(false);

            var list = new List<TrailerInfo>();

            using (var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true }))
            {
                await reader.MoveToContentAsync().ConfigureAwait(false);

                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.Name)
                        {
                            case "movieinfo":
                                var trailer = FetchTrailerInfo(reader.ReadSubtree());
                                list.Add(trailer);
                                break;
                        }
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Fetches trailer info from an xml node
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>TrailerInfo.</returns>
        private static TrailerInfo FetchTrailerInfo(XmlReader reader)
        {
            var trailerInfo = new TrailerInfo { };

            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "info":
                            FetchInfo(reader.ReadSubtree(), trailerInfo);
                            break;
                        case "cast":
                            FetchCast(reader.ReadSubtree(), trailerInfo);
                            break;
                        case "genre":
                            FetchGenres(reader.ReadSubtree(), trailerInfo);
                            break;
                        case "poster":
                            FetchPosterUrl(reader.ReadSubtree(), trailerInfo);
                            break;
                        case "preview":
                            FetchTrailerUrl(reader.ReadSubtree(), trailerInfo);
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            return trailerInfo;
        }

        private static readonly CultureInfo USCulture = new CultureInfo("en-US");
        
        /// <summary>
        /// Fetches from the info node
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="info">The info.</param>
        private static void FetchInfo(XmlReader reader, TrailerInfo info)
        {
            reader.MoveToContent();
            reader.Read();

            while (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "title":
                        info.Video.Name = reader.ReadStringSafe();
                        break;
                    case "runtime":
                        {
                            var runtime = reader.ReadStringSafe();

                            if (!string.IsNullOrWhiteSpace(runtime))
                            {
                                if (runtime.StartsWith(":", StringComparison.OrdinalIgnoreCase))
                                {
                                    runtime = "0" + runtime;
                                }

                                TimeSpan runtimeTimeSpan;

                                if (TimeSpan.TryParse(runtime, USCulture, out runtimeTimeSpan))
                                {
                                    info.Video.RunTimeTicks = runtimeTimeSpan.Ticks;
                                }
                            }
                            break;
                        }
                    case "rating":
                        info.Video.OfficialRating = reader.ReadStringSafe();
                        break;
                    case "studio":
                        {
                            var studio = reader.ReadStringSafe();
                            if (!string.IsNullOrWhiteSpace(studio))
                            {
                                info.Video.AddStudio(studio);
                            }
                            break;
                        }
                    case "postdate":
                        {
                            DateTime date;

                            if (DateTime.TryParse(reader.ReadStringSafe(), USCulture, DateTimeStyles.None, out date))
                            {
                                info.PostDate = date;
                            }
                            break;
                        }
                    case "releasedate":
                        {
                            var val = reader.ReadStringSafe();

                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                DateTime date;

                                if (DateTime.TryParse(val, USCulture, DateTimeStyles.None, out date))
                                {
                                    info.Video.PremiereDate = date;
                                    info.Video.ProductionYear = date.Year;
                                }
                            }

                            break;
                        }
                    case "director":
                        {
                            var directors = reader.ReadStringSafe() ?? string.Empty;

                            foreach (var director in directors.Split(',', StringSplitOptions.RemoveEmptyEntries))
                            {
                                var name = director.Trim();

                                if (!string.IsNullOrWhiteSpace(name))
                                {
                                    info.Video.AddPerson(new PersonInfo { Name = name, Type = PersonType.Director });
                                }
                            }
                            break;
                        }
                    case "description":
                        info.Video.Overview = reader.ReadStringSafe();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        /// <summary>
        /// Fetches from the genre node
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="info">The info.</param>
        private static void FetchGenres(XmlReader reader, TrailerInfo info)
        {
            reader.MoveToContent();
            reader.Read();

            while (reader.IsStartElement())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "name":
                            info.Video.AddGenre(reader.ReadStringSafe());
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

        }

        /// <summary>
        /// Fetches from the cast node
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="info">The info.</param>
        private static void FetchCast(XmlReader reader, TrailerInfo info)
        {
            reader.MoveToContent();
            reader.Read();

            while (reader.IsStartElement())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "name":
                            info.Video.AddPerson(new PersonInfo { Name = reader.ReadStringSafe(), Type = PersonType.Actor });
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

        }

        /// <summary>
        /// Fetches from the preview node
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="info">The info.</param>
        private static void FetchTrailerUrl(XmlReader reader, TrailerInfo info)
        {
            reader.MoveToContent();
            reader.Read();

            while (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "large":
                        info.TrailerUrl = reader.ReadStringSafe();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

        }

        /// <summary>
        /// Fetches from the poster node
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="info">The info.</param>
        private static void FetchPosterUrl(XmlReader reader, TrailerInfo info)
        {
            reader.MoveToContent();
            reader.Read();

            while (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "location":
                        info.ImageUrl = reader.ReadStringSafe();
                        break;
                    case "xlarge":
                        info.HdImageUrl = reader.ReadStringSafe();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

        }

    }
}
