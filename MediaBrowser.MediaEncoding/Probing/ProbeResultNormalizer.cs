using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using CommonIO;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.MediaEncoding.Probing
{
    public class ProbeResultNormalizer
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IMemoryStreamProvider _memoryStreamProvider;

        public ProbeResultNormalizer(ILogger logger, IFileSystem fileSystem, IMemoryStreamProvider memoryStreamProvider)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _memoryStreamProvider = memoryStreamProvider;
        }

        public MediaInfo GetMediaInfo(InternalMediaInfoResult data, VideoType videoType, bool isAudio, string path, MediaProtocol protocol)
        {
            var info = new MediaInfo
            {
                Path = path,
                Protocol = protocol
            };

            FFProbeHelpers.NormalizeFFProbeResult(data);
            SetSize(data, info);

            var internalStreams = data.streams ?? new MediaStreamInfo[] { };

            info.MediaStreams = internalStreams.Select(s => GetMediaStream(isAudio, s, data.format))
                .Where(i => i != null)
                .ToList();

            if (data.format != null)
            {
                info.Container = data.format.format_name;

                if (!string.IsNullOrEmpty(data.format.bit_rate))
                {
                    int value;
                    if (int.TryParse(data.format.bit_rate, NumberStyles.Any, _usCulture, out value))
                    {
                        info.Bitrate = value;
                    }
                }
            }

            var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var tagStreamType = isAudio ? "audio" : "video";

            if (data.streams != null)
            {
                var tagStream = data.streams.FirstOrDefault(i => string.Equals(i.codec_type, tagStreamType, StringComparison.OrdinalIgnoreCase));

                if (tagStream != null && tagStream.tags != null)
                {
                    foreach (var pair in tagStream.tags)
                    {
                        tags[pair.Key] = pair.Value;
                    }
                }
            }

            if (data.format != null && data.format.tags != null)
            {
                foreach (var pair in data.format.tags)
                {
                    tags[pair.Key] = pair.Value;
                }
            }

            FetchGenres(info, tags);
            var shortOverview = FFProbeHelpers.GetDictionaryValue(tags, "description");
            var overview = FFProbeHelpers.GetDictionaryValue(tags, "synopsis");

            if (string.IsNullOrWhiteSpace(overview))
            {
                overview = shortOverview;
                shortOverview = null;
            }
            if (string.IsNullOrWhiteSpace(overview))
            {
                overview = FFProbeHelpers.GetDictionaryValue(tags, "desc");
            }

            if (!string.IsNullOrWhiteSpace(overview))
            {
                info.Overview = overview;
            }

            if (!string.IsNullOrWhiteSpace(shortOverview))
            {
                info.ShortOverview = shortOverview;
            }

            var title = FFProbeHelpers.GetDictionaryValue(tags, "title");
            if (!string.IsNullOrWhiteSpace(title))
            {
                info.Name = title;
            }

            info.ProductionYear = FFProbeHelpers.GetDictionaryNumericValue(tags, "date");

            // Several different forms of retaildate
            info.PremiereDate = FFProbeHelpers.GetDictionaryDateTime(tags, "retaildate") ??
                FFProbeHelpers.GetDictionaryDateTime(tags, "retail date") ??
                FFProbeHelpers.GetDictionaryDateTime(tags, "retail_date") ??
                FFProbeHelpers.GetDictionaryDateTime(tags, "date");

            if (isAudio)
            {
                SetAudioRuntimeTicks(data, info);

                // tags are normally located under data.format, but we've seen some cases with ogg where they're part of the info stream
                // so let's create a combined list of both

                SetAudioInfoFromTags(info, tags);
            }
            else
            {
                FetchStudios(info, tags, "copyright");

                var iTunEXTC = FFProbeHelpers.GetDictionaryValue(tags, "iTunEXTC");
                if (!string.IsNullOrWhiteSpace(iTunEXTC))
                {
                    var parts = iTunEXTC.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    // Example 
                    // mpaa|G|100|For crude humor
                    if (parts.Length > 1)
                    {
                        info.OfficialRating = parts[1];

                        if (parts.Length > 3)
                        {
                            info.OfficialRatingDescription = parts[3];
                        }
                    }
                }

                var itunesXml = FFProbeHelpers.GetDictionaryValue(tags, "iTunMOVI");
                if (!string.IsNullOrWhiteSpace(itunesXml))
                {
                    FetchFromItunesInfo(itunesXml, info);
                }

                if (data.format != null && !string.IsNullOrEmpty(data.format.duration))
                {
                    info.RunTimeTicks = TimeSpan.FromSeconds(double.Parse(data.format.duration, _usCulture)).Ticks;
                }

                FetchWtvInfo(info, data);

                if (data.Chapters != null)
                {
                    info.Chapters = data.Chapters.Select(GetChapterInfo).ToList();
                }

                ExtractTimestamp(info);

                var stereoMode = GetDictionaryValue(tags, "stereo_mode");
                if (string.Equals(stereoMode, "left_right", StringComparison.OrdinalIgnoreCase))
                {
                    info.Video3DFormat = Video3DFormat.FullSideBySide;
                }
            }

            return info;
        }

        private void FetchFromItunesInfo(string xml, MediaInfo info)
        {
            // Make things simpler and strip out the dtd
            xml = xml.Substring(xml.IndexOf("<plist", StringComparison.OrdinalIgnoreCase));
            xml = "<?xml version=\"1.0\"?>" + xml;

            // <?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n<plist version=\"1.0\">\n<dict>\n\t<key>cast</key>\n\t<array>\n\t\t<dict>\n\t\t\t<key>name</key>\n\t\t\t<string>Blender Foundation</string>\n\t\t</dict>\n\t\t<dict>\n\t\t\t<key>name</key>\n\t\t\t<string>Janus Bager Kristensen</string>\n\t\t</dict>\n\t</array>\n\t<key>directors</key>\n\t<array>\n\t\t<dict>\n\t\t\t<key>name</key>\n\t\t\t<string>Sacha Goedegebure</string>\n\t\t</dict>\n\t</array>\n\t<key>studio</key>\n\t<string>Blender Foundation</string>\n</dict>\n</plist>\n
            using (var stream = _memoryStreamProvider.CreateNew(Encoding.UTF8.GetBytes(xml)))
            {
                using (var streamReader = new StreamReader(stream))
                {
                    // Use XmlReader for best performance
                    using (var reader = XmlReader.Create(streamReader))
                    {
                        reader.MoveToContent();

                        // Loop through each element
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                switch (reader.Name)
                                {
                                    case "dict":
                                        using (var subtree = reader.ReadSubtree())
                                        {
                                            ReadFromDictNode(subtree, info);
                                        }
                                        break;
                                    default:
                                        reader.Skip();
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ReadFromDictNode(XmlReader reader, MediaInfo info)
        {
            reader.MoveToContent();

            string currentKey = null;
            List<NameValuePair> pairs = new List<NameValuePair>();

            // Loop through each element
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "key":
                            if (!string.IsNullOrWhiteSpace(currentKey))
                            {
                                ProcessPairs(currentKey, pairs, info);
                            }
                            currentKey = reader.ReadElementContentAsString();
                            pairs = new List<NameValuePair>();
                            break;
                        case "string":
                            var value = reader.ReadElementContentAsString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                pairs.Add(new NameValuePair
                                {
                                    Name = value,
                                    Value = value
                                });
                            }
                            break;
                        case "array":
                            if (!string.IsNullOrWhiteSpace(currentKey))
                            {
                                using (var subtree = reader.ReadSubtree())
                                {
                                    pairs.AddRange(ReadValueArray(subtree));
                                }
                            }
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        private List<NameValuePair> ReadValueArray(XmlReader reader)
        {
            reader.MoveToContent();

            List<NameValuePair> pairs = new List<NameValuePair>();

            // Loop through each element
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "dict":
                            using (var subtree = reader.ReadSubtree())
                            {
                                var dict = GetNameValuePair(subtree);
                                if (dict != null)
                                {
                                    pairs.Add(dict);
                                }
                            }
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            return pairs;
        }

        private void ProcessPairs(string key, List<NameValuePair> pairs, MediaInfo info)
        {
            if (string.Equals(key, "studio", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var pair in pairs)
                {
                    info.Studios.Add(pair.Value);
                }

                info.Studios = info.Studios
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            }
            else if (string.Equals(key, "screenwriters", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var pair in pairs)
                {
                    info.People.Add(new BaseItemPerson
                    {
                        Name = pair.Value,
                        Type = PersonType.Writer
                    });
                }
            }
            else if (string.Equals(key, "producers", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var pair in pairs)
                {
                    info.People.Add(new BaseItemPerson
                    {
                        Name = pair.Value,
                        Type = PersonType.Producer
                    });
                }
            }
            else if (string.Equals(key, "directors", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var pair in pairs)
                {
                    info.People.Add(new BaseItemPerson
                    {
                        Name = pair.Value,
                        Type = PersonType.Director
                    });
                }
            }
        }

        private NameValuePair GetNameValuePair(XmlReader reader)
        {
            reader.MoveToContent();

            string name = null;
            string value = null;

            // Loop through each element
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "key":
                            name = reader.ReadElementContentAsString();
                            break;
                        case "string":
                            value = reader.ReadElementContentAsString();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return new NameValuePair
            {
                Name = name,
                Value = value
            };
        }

        private string NormalizeSubtitleCodec(string codec)
        {
            if (string.Equals(codec, "dvb_subtitle", StringComparison.OrdinalIgnoreCase))
            {
                codec = "dvbsub";
            }
            else if ((codec ?? string.Empty).IndexOf("PGS", StringComparison.OrdinalIgnoreCase) != -1)
            {
                codec = "PGSSUB";
            }
            else if ((codec ?? string.Empty).IndexOf("DVD", StringComparison.OrdinalIgnoreCase) != -1)
            {
                codec = "DVDSUB";
            }

            return codec;
        }

        /// <summary>
        /// Converts ffprobe stream info to our MediaStream class
        /// </summary>
        /// <param name="isAudio">if set to <c>true</c> [is info].</param>
        /// <param name="streamInfo">The stream info.</param>
        /// <param name="formatInfo">The format info.</param>
        /// <returns>MediaStream.</returns>
        private MediaStream GetMediaStream(bool isAudio, MediaStreamInfo streamInfo, MediaFormatInfo formatInfo)
        {
            // These are mp4 chapters
            if (string.Equals(streamInfo.codec_name, "mov_text", StringComparison.OrdinalIgnoreCase))
            {
                // Edit: but these are also sometimes subtitles?
                //return null;
            }

            var stream = new MediaStream
            {
                Codec = streamInfo.codec_name,
                Profile = streamInfo.profile,
                Level = streamInfo.level,
                Index = streamInfo.index,
                PixelFormat = streamInfo.pix_fmt,
                NalLengthSize = streamInfo.nal_length_size,
                TimeBase = streamInfo.time_base,
                CodecTimeBase = streamInfo.codec_time_base
            };

            if (string.Equals(streamInfo.is_avc, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(streamInfo.is_avc, "1", StringComparison.OrdinalIgnoreCase))
            {
                stream.IsAVC = true;
            }
            else if (string.Equals(streamInfo.is_avc, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(streamInfo.is_avc, "0", StringComparison.OrdinalIgnoreCase))
            {
                stream.IsAVC = false;
            }

            // Filter out junk
            if (!string.IsNullOrWhiteSpace(streamInfo.codec_tag_string) && streamInfo.codec_tag_string.IndexOf("[0]", StringComparison.OrdinalIgnoreCase) == -1)
            {
                stream.CodecTag = streamInfo.codec_tag_string;
            }

            if (streamInfo.tags != null)
            {
                stream.Language = GetDictionaryValue(streamInfo.tags, "language");
                stream.Comment = GetDictionaryValue(streamInfo.tags, "comment");
                stream.Title = GetDictionaryValue(streamInfo.tags, "title");
            }

            if (string.Equals(streamInfo.codec_type, "audio", StringComparison.OrdinalIgnoreCase))
            {
                stream.Type = MediaStreamType.Audio;

                stream.Channels = streamInfo.channels;

                if (!string.IsNullOrEmpty(streamInfo.sample_rate))
                {
                    int value;
                    if (int.TryParse(streamInfo.sample_rate, NumberStyles.Any, _usCulture, out value))
                    {
                        stream.SampleRate = value;
                    }
                }

                stream.ChannelLayout = ParseChannelLayout(streamInfo.channel_layout);

                if (streamInfo.bits_per_sample > 0)
                {
                    stream.BitDepth = streamInfo.bits_per_sample;
                }
                else if (streamInfo.bits_per_raw_sample > 0)
                {
                    stream.BitDepth = streamInfo.bits_per_raw_sample;
                }
            }
            else if (string.Equals(streamInfo.codec_type, "subtitle", StringComparison.OrdinalIgnoreCase))
            {
                stream.Type = MediaStreamType.Subtitle;
                stream.Codec = NormalizeSubtitleCodec(stream.Codec);
            }
            else if (string.Equals(streamInfo.codec_type, "video", StringComparison.OrdinalIgnoreCase))
            {
                stream.Type = isAudio || string.Equals(stream.Codec, "mjpeg", StringComparison.OrdinalIgnoreCase) || string.Equals(stream.Codec, "gif", StringComparison.OrdinalIgnoreCase) || string.Equals(stream.Codec, "png", StringComparison.OrdinalIgnoreCase)
                    ? MediaStreamType.EmbeddedImage
                    : MediaStreamType.Video;

                stream.Width = streamInfo.width;
                stream.Height = streamInfo.height;
                stream.AspectRatio = GetAspectRatio(streamInfo);

                stream.AverageFrameRate = GetFrameRate(streamInfo.avg_frame_rate);
                stream.RealFrameRate = GetFrameRate(streamInfo.r_frame_rate);

                if (streamInfo.bits_per_sample > 0)
                {
                    stream.BitDepth = streamInfo.bits_per_sample;
                }
                else if (streamInfo.bits_per_raw_sample > 0)
                {
                    stream.BitDepth = streamInfo.bits_per_raw_sample;
                }

                //stream.IsAnamorphic = string.Equals(streamInfo.sample_aspect_ratio, "0:1", StringComparison.OrdinalIgnoreCase) ||
                //    string.Equals(stream.AspectRatio, "2.35:1", StringComparison.OrdinalIgnoreCase) ||
                //    string.Equals(stream.AspectRatio, "2.40:1", StringComparison.OrdinalIgnoreCase);

                // http://stackoverflow.com/questions/17353387/how-to-detect-anamorphic-video-with-ffprobe
                stream.IsAnamorphic = string.Equals(streamInfo.sample_aspect_ratio, "0:1", StringComparison.OrdinalIgnoreCase);

                if (streamInfo.refs > 0)
                {
                    stream.RefFrames = streamInfo.refs;
                }
            }
            else
            {
                return null;
            }

            // Get stream bitrate
            var bitrate = 0;

            if (!string.IsNullOrEmpty(streamInfo.bit_rate))
            {
                int value;
                if (int.TryParse(streamInfo.bit_rate, NumberStyles.Any, _usCulture, out value))
                {
                    bitrate = value;
                }
            }

            if (bitrate == 0 && formatInfo != null && !string.IsNullOrEmpty(formatInfo.bit_rate) && stream.Type == MediaStreamType.Video)
            {
                // If the stream info doesn't have a bitrate get the value from the media format info
                int value;
                if (int.TryParse(formatInfo.bit_rate, NumberStyles.Any, _usCulture, out value))
                {
                    bitrate = value;
                }
            }

            if (bitrate > 0)
            {
                stream.BitRate = bitrate;
            }

            if (streamInfo.disposition != null)
            {
                var isDefault = GetDictionaryValue(streamInfo.disposition, "default");
                var isForced = GetDictionaryValue(streamInfo.disposition, "forced");

                stream.IsDefault = string.Equals(isDefault, "1", StringComparison.OrdinalIgnoreCase);

                stream.IsForced = string.Equals(isForced, "1", StringComparison.OrdinalIgnoreCase);
            }

            NormalizeStreamTitle(stream);

            return stream;
        }

        private void NormalizeStreamTitle(MediaStream stream)
        {
            if (string.Equals(stream.Title, "cc", StringComparison.OrdinalIgnoreCase))
            {
                stream.Title = null;
            }

            if (stream.Type == MediaStreamType.EmbeddedImage)
            {
                stream.Title = null;
            }
        }

        /// <summary>
        /// Gets a string from an FFProbeResult tags dictionary
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <param name="key">The key.</param>
        /// <returns>System.String.</returns>
        private string GetDictionaryValue(Dictionary<string, string> tags, string key)
        {
            if (tags == null)
            {
                return null;
            }

            string val;

            tags.TryGetValue(key, out val);
            return val;
        }

        private string ParseChannelLayout(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return input.Split('(').FirstOrDefault();
        }

        private string GetAspectRatio(MediaStreamInfo info)
        {
            var original = info.display_aspect_ratio;

            int height;
            int width;

            var parts = (original ?? string.Empty).Split(':');
            if (!(parts.Length == 2 &&
                int.TryParse(parts[0], NumberStyles.Any, _usCulture, out width) &&
                int.TryParse(parts[1], NumberStyles.Any, _usCulture, out height) &&
                width > 0 &&
                height > 0))
            {
                width = info.width;
                height = info.height;
            }

            if (width > 0 && height > 0)
            {
                double ratio = width;
                ratio /= height;

                if (IsClose(ratio, 1.777777778, .03))
                {
                    return "16:9";
                }

                if (IsClose(ratio, 1.3333333333, .05))
                {
                    return "4:3";
                }

                if (IsClose(ratio, 1.41))
                {
                    return "1.41:1";
                }

                if (IsClose(ratio, 1.5))
                {
                    return "1.5:1";
                }

                if (IsClose(ratio, 1.6))
                {
                    return "1.6:1";
                }

                if (IsClose(ratio, 1.66666666667))
                {
                    return "5:3";
                }

                if (IsClose(ratio, 1.85, .02))
                {
                    return "1.85:1";
                }

                if (IsClose(ratio, 2.35, .025))
                {
                    return "2.35:1";
                }

                if (IsClose(ratio, 2.4, .025))
                {
                    return "2.40:1";
                }
            }

            return original;
        }

        private bool IsClose(double d1, double d2, double variance = .005)
        {
            return Math.Abs(d1 - d2) <= variance;
        }

        /// <summary>
        /// Gets a frame rate from a string value in ffprobe output
        /// This could be a number or in the format of 2997/125.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Nullable{System.Single}.</returns>
        private float? GetFrameRate(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var parts = value.Split('/');

                float result;

                if (parts.Length == 2)
                {
                    result = float.Parse(parts[0], _usCulture) / float.Parse(parts[1], _usCulture);
                }
                else
                {
                    result = float.Parse(parts[0], _usCulture);
                }

                return float.IsNaN(result) ? (float?)null : result;
            }

            return null;
        }

        private void SetAudioRuntimeTicks(InternalMediaInfoResult result, MediaInfo data)
        {
            if (result.streams != null)
            {
                // Get the first info stream
                var stream = result.streams.FirstOrDefault(s => string.Equals(s.codec_type, "audio", StringComparison.OrdinalIgnoreCase));

                if (stream != null)
                {
                    // Get duration from stream properties
                    var duration = stream.duration;

                    // If it's not there go into format properties
                    if (string.IsNullOrEmpty(duration))
                    {
                        duration = result.format.duration;
                    }

                    // If we got something, parse it
                    if (!string.IsNullOrEmpty(duration))
                    {
                        data.RunTimeTicks = TimeSpan.FromSeconds(double.Parse(duration, _usCulture)).Ticks;
                    }
                }
            }
        }

        private void SetSize(InternalMediaInfoResult data, Model.MediaInfo.MediaInfo info)
        {
            if (data.format != null)
            {
                if (!string.IsNullOrEmpty(data.format.size))
                {
                    info.Size = long.Parse(data.format.size, _usCulture);
                }
                else
                {
                    info.Size = null;
                }
            }
        }

        private void SetAudioInfoFromTags(MediaInfo audio, Dictionary<string, string> tags)
        {
            var composer = FFProbeHelpers.GetDictionaryValue(tags, "composer");
            if (!string.IsNullOrWhiteSpace(composer))
            {
                foreach (var person in Split(composer, false))
                {
                    audio.People.Add(new BaseItemPerson { Name = person, Type = PersonType.Composer });
                }
            }

            var conductor = FFProbeHelpers.GetDictionaryValue(tags, "conductor");
            if (!string.IsNullOrWhiteSpace(conductor))
            {
                foreach (var person in Split(conductor, false))
                {
                    audio.People.Add(new BaseItemPerson { Name = person, Type = PersonType.Conductor });
                }
            }

            var lyricist = FFProbeHelpers.GetDictionaryValue(tags, "lyricist");

            if (!string.IsNullOrWhiteSpace(lyricist))
            {
                foreach (var person in Split(lyricist, false))
                {
                    audio.People.Add(new BaseItemPerson { Name = person, Type = PersonType.Lyricist });
                }
            }
            // Check for writer some music is tagged that way as alternative to composer/lyricist
            var writer = FFProbeHelpers.GetDictionaryValue(tags, "writer");

            if (!string.IsNullOrWhiteSpace(writer))
            {
                foreach (var person in Split(writer, false))
                {
                    audio.People.Add(new BaseItemPerson { Name = person, Type = PersonType.Writer });
                }
            }

            audio.Album = FFProbeHelpers.GetDictionaryValue(tags, "album");

            var artists = FFProbeHelpers.GetDictionaryValue(tags, "artists");

            if (!string.IsNullOrWhiteSpace(artists))
            {
                audio.Artists = SplitArtists(artists, new[] { '/', ';' }, false)
                    .DistinctNames()
                    .ToList();
            }
            else
            {
                var artist = FFProbeHelpers.GetDictionaryValue(tags, "artist");
                if (string.IsNullOrWhiteSpace(artist))
                {
                    audio.Artists.Clear();
                }
                else
                {
                    audio.Artists = SplitArtists(artist, _nameDelimiters, true)
                    .DistinctNames()
                        .ToList();
                }
            }

            var albumArtist = FFProbeHelpers.GetDictionaryValue(tags, "albumartist");
            if (string.IsNullOrWhiteSpace(albumArtist))
            {
                albumArtist = FFProbeHelpers.GetDictionaryValue(tags, "album artist");
            }
            if (string.IsNullOrWhiteSpace(albumArtist))
            {
                albumArtist = FFProbeHelpers.GetDictionaryValue(tags, "album_artist");
            }

            if (string.IsNullOrWhiteSpace(albumArtist))
            {
                audio.AlbumArtists = new List<string>();
            }
            else
            {
                audio.AlbumArtists = SplitArtists(albumArtist, _nameDelimiters, true)
                    .DistinctNames()
                    .ToList();

            }

            if (audio.AlbumArtists.Count == 0)
            {
                audio.AlbumArtists = audio.Artists.Take(1).ToList();
            }

            // Track number
            audio.IndexNumber = GetDictionaryDiscValue(tags, "track");

            // Disc number
            audio.ParentIndexNumber = GetDictionaryDiscValue(tags, "disc");

            // If we don't have a ProductionYear try and get it from PremiereDate
            if (audio.PremiereDate.HasValue && !audio.ProductionYear.HasValue)
            {
                audio.ProductionYear = audio.PremiereDate.Value.ToLocalTime().Year;
            }

            // There's several values in tags may or may not be present
            FetchStudios(audio, tags, "organization");
            FetchStudios(audio, tags, "ensemble");
            FetchStudios(audio, tags, "publisher");
            FetchStudios(audio, tags, "label");

            // These support mulitple values, but for now we only store the first.
            var mb = GetMultipleMusicBrainzId(FFProbeHelpers.GetDictionaryValue(tags, "MusicBrainz Album Artist Id"));
            if (mb == null) mb = GetMultipleMusicBrainzId(FFProbeHelpers.GetDictionaryValue(tags, "MUSICBRAINZ_ALBUMARTISTID"));
            audio.SetProviderId(MetadataProviders.MusicBrainzAlbumArtist, mb);

            mb = GetMultipleMusicBrainzId(FFProbeHelpers.GetDictionaryValue(tags, "MusicBrainz Artist Id"));
            if (mb == null) mb = GetMultipleMusicBrainzId(FFProbeHelpers.GetDictionaryValue(tags, "MUSICBRAINZ_ARTISTID"));
            audio.SetProviderId(MetadataProviders.MusicBrainzArtist, mb);

            mb = GetMultipleMusicBrainzId(FFProbeHelpers.GetDictionaryValue(tags, "MusicBrainz Album Id"));
            if (mb == null) mb = GetMultipleMusicBrainzId(FFProbeHelpers.GetDictionaryValue(tags, "MUSICBRAINZ_ALBUMID"));
            audio.SetProviderId(MetadataProviders.MusicBrainzAlbum, mb);

            mb = GetMultipleMusicBrainzId(FFProbeHelpers.GetDictionaryValue(tags, "MusicBrainz Release Group Id"));
            if (mb == null) mb = GetMultipleMusicBrainzId(FFProbeHelpers.GetDictionaryValue(tags, "MUSICBRAINZ_RELEASEGROUPID"));
            audio.SetProviderId(MetadataProviders.MusicBrainzReleaseGroup, mb);

            mb = GetMultipleMusicBrainzId(FFProbeHelpers.GetDictionaryValue(tags, "MusicBrainz Release Track Id"));
            if (mb == null) mb = GetMultipleMusicBrainzId(FFProbeHelpers.GetDictionaryValue(tags, "MUSICBRAINZ_RELEASETRACKID"));
            audio.SetProviderId(MetadataProviders.MusicBrainzTrack, mb);
        }

        private string GetMultipleMusicBrainzId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Trim())
                .FirstOrDefault(i => !string.IsNullOrWhiteSpace(i));
        }

        private readonly char[] _nameDelimiters = { '/', '|', ';', '\\' };

        /// <summary>
        /// Splits the specified val.
        /// </summary>
        /// <param name="val">The val.</param>
        /// <param name="allowCommaDelimiter">if set to <c>true</c> [allow comma delimiter].</param>
        /// <returns>System.String[][].</returns>
        private IEnumerable<string> Split(string val, bool allowCommaDelimiter)
        {
            // Only use the comma as a delimeter if there are no slashes or pipes. 
            // We want to be careful not to split names that have commas in them
            var delimeter = !allowCommaDelimiter || _nameDelimiters.Any(i => val.IndexOf(i) != -1) ?
                _nameDelimiters :
                new[] { ',' };

            return val.Split(delimeter, StringSplitOptions.RemoveEmptyEntries)
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => i.Trim());
        }

        private const string ArtistReplaceValue = " | ";

        private IEnumerable<string> SplitArtists(string val, char[] delimiters, bool splitFeaturing)
        {
            if (splitFeaturing)
            {
                val = val.Replace(" featuring ", ArtistReplaceValue, StringComparison.OrdinalIgnoreCase)
                    .Replace(" feat. ", ArtistReplaceValue, StringComparison.OrdinalIgnoreCase);
            }

            var artistsFound = new List<string>();

            foreach (var whitelistArtist in GetSplitWhitelist())
            {
                var originalVal = val;
                val = val.Replace(whitelistArtist, "|", StringComparison.OrdinalIgnoreCase);

                if (!string.Equals(originalVal, val, StringComparison.OrdinalIgnoreCase))
                {
                    artistsFound.Add(whitelistArtist);
                }
            }

            var artists = val.Split(delimiters, StringSplitOptions.RemoveEmptyEntries)
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => i.Trim());

            artistsFound.AddRange(artists);
            return artistsFound;
        }


        private List<string> _splitWhiteList = null;

        private IEnumerable<string> GetSplitWhitelist()
        {
            if (_splitWhiteList == null)
            {
                var file = GetType().Namespace + ".whitelist.txt";

                using (var stream = GetType().Assembly.GetManifestResourceStream(file))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var list = new List<string>();

                        while (!reader.EndOfStream)
                        {
                            var val = reader.ReadLine();

                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                list.Add(val);
                            }
                        }

                        _splitWhiteList = list;
                    }
                }
            }

            return _splitWhiteList;
        }

        /// <summary>
        /// Gets the studios from the tags collection
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="tags">The tags.</param>
        /// <param name="tagName">Name of the tag.</param>
        private void FetchStudios(MediaInfo info, Dictionary<string, string> tags, string tagName)
        {
            var val = FFProbeHelpers.GetDictionaryValue(tags, tagName);

            if (!string.IsNullOrEmpty(val))
            {
                var studios = Split(val, true);

                foreach (var studio in studios)
                {
                    // Sometimes the artist name is listed here, account for that
                    if (info.Artists.Contains(studio, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (info.AlbumArtists.Contains(studio, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    info.Studios.Add(studio);
                }

                info.Studios = info.Studios
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets the genres from the tags collection
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="tags">The tags.</param>
        private void FetchGenres(MediaInfo info, Dictionary<string, string> tags)
        {
            var val = FFProbeHelpers.GetDictionaryValue(tags, "genre");

            if (!string.IsNullOrEmpty(val))
            {
                foreach (var genre in Split(val, true))
                {
                    info.Genres.Add(genre);
                }

                info.Genres = info.Genres
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets the disc number, which is sometimes can be in the form of '1', or '1/3'
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <param name="tagName">Name of the tag.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        private int? GetDictionaryDiscValue(Dictionary<string, string> tags, string tagName)
        {
            var disc = FFProbeHelpers.GetDictionaryValue(tags, tagName);

            if (!string.IsNullOrEmpty(disc))
            {
                disc = disc.Split('/')[0];

                int num;

                if (int.TryParse(disc, out num))
                {
                    return num;
                }
            }

            return null;
        }

        private ChapterInfo GetChapterInfo(MediaChapter chapter)
        {
            var info = new ChapterInfo();

            if (chapter.tags != null)
            {
                string name;
                if (chapter.tags.TryGetValue("title", out name))
                {
                    info.Name = name;
                }
            }

            // Limit accuracy to milliseconds to match xml saving
            var secondsString = chapter.start_time;
            double seconds;

            if (double.TryParse(secondsString, NumberStyles.Any, CultureInfo.InvariantCulture, out seconds))
            {
                var ms = Math.Round(TimeSpan.FromSeconds(seconds).TotalMilliseconds);
                info.StartPositionTicks = TimeSpan.FromMilliseconds(ms).Ticks;
            }

            return info;
        }

        private const int MaxSubtitleDescriptionExtractionLength = 100; // When extracting subtitles, the maximum length to consider (to avoid invalid filenames)

        private void FetchWtvInfo(MediaInfo video, InternalMediaInfoResult data)
        {
            if (data.format == null || data.format.tags == null)
            {
                return;
            }

            var genres = FFProbeHelpers.GetDictionaryValue(data.format.tags, "WM/Genre");

            if (!string.IsNullOrWhiteSpace(genres))
            {
                var genreList = genres.Split(new[] { ';', '/', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Select(i => i.Trim())
                    .ToList();

                // If this is empty then don't overwrite genres that might have been fetched earlier
                if (genreList.Count > 0)
                {
                    video.Genres = genreList;
                }
            }

            var officialRating = FFProbeHelpers.GetDictionaryValue(data.format.tags, "WM/ParentalRating");

            if (!string.IsNullOrWhiteSpace(officialRating))
            {
                video.OfficialRating = officialRating;
            }

            var people = FFProbeHelpers.GetDictionaryValue(data.format.tags, "WM/MediaCredits");

            if (!string.IsNullOrEmpty(people))
            {
                video.People = people.Split(new[] { ';', '/' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Select(i => new BaseItemPerson { Name = i.Trim(), Type = PersonType.Actor })
                    .ToList();
            }

            var year = FFProbeHelpers.GetDictionaryValue(data.format.tags, "WM/OriginalReleaseTime");
            if (!string.IsNullOrWhiteSpace(year))
            {
                int val;

                if (int.TryParse(year, NumberStyles.Integer, _usCulture, out val))
                {
                    video.ProductionYear = val;
                }
            }

            var premiereDateString = FFProbeHelpers.GetDictionaryValue(data.format.tags, "WM/MediaOriginalBroadcastDateTime");
            if (!string.IsNullOrWhiteSpace(premiereDateString))
            {
                DateTime val;

                // Credit to MCEBuddy: https://mcebuddy2x.codeplex.com/
                // DateTime is reported along with timezone info (typically Z i.e. UTC hence assume None)
                if (DateTime.TryParse(year, null, DateTimeStyles.None, out val))
                {
                    video.PremiereDate = val.ToUniversalTime();
                }
            }

            var description = FFProbeHelpers.GetDictionaryValue(data.format.tags, "WM/SubTitleDescription");

            var subTitle = FFProbeHelpers.GetDictionaryValue(data.format.tags, "WM/SubTitle");

            // For below code, credit to MCEBuddy: https://mcebuddy2x.codeplex.com/

            // Sometimes for TV Shows the Subtitle field is empty and the subtitle description contains the subtitle, extract if possible. See ticket https://mcebuddy2x.codeplex.com/workitem/1910
            // The format is -> EPISODE/TOTAL_EPISODES_IN_SEASON. SUBTITLE: DESCRIPTION
            // OR -> COMMENT. SUBTITLE: DESCRIPTION
            // e.g. -> 4/13. The Doctor's Wife: Science fiction drama. When he follows a Time Lord distress signal, the Doctor puts Amy, Rory and his beloved TARDIS in grave danger. Also in HD. [AD,S]
            // e.g. -> CBeebies Bedtime Hour. The Mystery: Animated adventures of two friends who live on an island in the middle of the big city. Some of Abney and Teal's favourite objects are missing. [S]
            if (String.IsNullOrWhiteSpace(subTitle) && !String.IsNullOrWhiteSpace(description) && description.Substring(0, Math.Min(description.Length, MaxSubtitleDescriptionExtractionLength)).Contains(":")) // Check within the Subtitle size limit, otherwise from description it can get too long creating an invalid filename
            {
                string[] parts = description.Split(':');
                if (parts.Length > 0)
                {
                    string subtitle = parts[0];
                    try
                    {
                        if (subtitle.Contains("/")) // It contains a episode number and season number
                        {
                            string[] numbers = subtitle.Split(' ');
                            video.IndexNumber = int.Parse(numbers[0].Replace(".", "").Split('/')[0]);
                            int totalEpisodesInSeason = int.Parse(numbers[0].Replace(".", "").Split('/')[1]);

                            description = String.Join(" ", numbers, 1, numbers.Length - 1).Trim(); // Skip the first, concatenate the rest, clean up spaces and save it
                        }
                        else
                            throw new Exception(); // Switch to default parsing
                    }
                    catch // Default parsing
                    {
                        if (subtitle.Contains(".")) // skip the comment, keep the subtitle
                            description = String.Join(".", subtitle.Split('.'), 1, subtitle.Split('.').Length - 1).Trim(); // skip the first
                        else
                            description = subtitle.Trim(); // Clean up whitespaces and save it
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                video.Overview = description;
            }
        }

        private void ExtractTimestamp(MediaInfo video)
        {
            if (video.VideoType == VideoType.VideoFile)
            {
                if (string.Equals(video.Container, "mpeg2ts", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(video.Container, "m2ts", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(video.Container, "ts", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        video.Timestamp = GetMpegTimestamp(video.Path);

                        _logger.Debug("Video has {0} timestamp", video.Timestamp);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error extracting timestamp info from {0}", ex, video.Path);
                        video.Timestamp = null;
                    }
                }
            }
        }

        private TransportStreamTimestamp GetMpegTimestamp(string path)
        {
            var packetBuffer = new byte['Å'];

            using (var fs = _fileSystem.GetFileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Read(packetBuffer, 0, packetBuffer.Length);
            }

            if (packetBuffer[0] == 71)
            {
                return TransportStreamTimestamp.None;
            }

            if ((packetBuffer[4] == 71) && (packetBuffer['Ä'] == 71))
            {
                if ((packetBuffer[0] == 0) && (packetBuffer[1] == 0) && (packetBuffer[2] == 0) && (packetBuffer[3] == 0))
                {
                    return TransportStreamTimestamp.Zero;
                }

                return TransportStreamTimestamp.Valid;
            }

            return TransportStreamTimestamp.None;
        }
    }
}
