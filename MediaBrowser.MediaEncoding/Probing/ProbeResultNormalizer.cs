#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.Probing
{
    /// <summary>
    /// Class responsible for normalizing FFprobe output.
    /// </summary>
    public partial class ProbeResultNormalizer
    {
        // When extracting subtitles, the maximum length to consider (to avoid invalid filenames)
        private const int MaxSubtitleDescriptionExtractionLength = 100;

        private const string ArtistReplaceValue = " | ";

        private readonly char[] _nameDelimiters = { '/', '|', ';', '\\' };
        private readonly string[] _webmVideoCodecs = { "av1", "vp8", "vp9" };
        private readonly string[] _webmAudioCodecs = { "opus", "vorbis" };

        private readonly ILogger _logger;
        private readonly ILocalizationManager _localization;

        private string[] _splitWhiteList;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProbeResultNormalizer"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger{ProbeResultNormalizer}"/> for use with the <see cref="ProbeResultNormalizer"/> instance.</param>
        /// <param name="localization">The <see cref="ILocalizationManager"/> for use with the <see cref="ProbeResultNormalizer"/> instance.</param>
        public ProbeResultNormalizer(ILogger logger, ILocalizationManager localization)
        {
            _logger = logger;
            _localization = localization;
        }

        private IReadOnlyList<string> SplitWhitelist => _splitWhiteList ??= new string[]
        {
            "AC/DC",
            "A/T/O/S",
            "As/Hi Soundworks",
            "Au/Ra",
            "Bremer/McCoy",
            "b/bqスタヂオ",
            "DOV/S",
            "DJ'TEKINA//SOMETHING",
            "IX/ON",
            "J-CORE SLi//CER",
            "M(a/u)SH",
            "Kaoru/Brilliance",
            "signum/ii",
            "Richiter(LORB/DUGEM DI BARAT)",
            "이달의 소녀 1/3",
            "R!N / Gemie",
            "LOONA 1/3",
            "LOONA / yyxy",
            "LOONA / ODD EYE CIRCLE",
            "K/DA",
            "22/7",
            "諭吉佳作/men",
            "//dARTH nULL",
            "Phantom/Ghost",
            "She/Her/Hers",
            "5/8erl in Ehr'n",
            "Smith/Kotzen",
            "We;Na",
            "LSR/CITY",
        };

        /// <summary>
        /// Transforms a FFprobe response into its <see cref="MediaInfo"/> equivalent.
        /// </summary>
        /// <param name="data">The <see cref="InternalMediaInfoResult"/>.</param>
        /// <param name="videoType">The <see cref="VideoType"/>.</param>
        /// <param name="isAudio">A boolean indicating whether the media is audio.</param>
        /// <param name="path">Path to media file.</param>
        /// <param name="protocol">Path media protocol.</param>
        /// <returns>The <see cref="MediaInfo"/>.</returns>
        public MediaInfo GetMediaInfo(InternalMediaInfoResult data, VideoType? videoType, bool isAudio, string path, MediaProtocol protocol)
        {
            var info = new MediaInfo
            {
                Path = path,
                Protocol = protocol,
                VideoType = videoType
            };

            FFProbeHelpers.NormalizeFFProbeResult(data);
            SetSize(data, info);

            var internalStreams = data.Streams ?? Array.Empty<MediaStreamInfo>();

            info.MediaStreams = internalStreams.Select(s => GetMediaStream(isAudio, s, data.Format))
                .Where(i => i is not null)
                // Drop subtitle streams if we don't know the codec because it will just cause failures if we don't know how to handle them
                .Where(i => i.Type != MediaStreamType.Subtitle || !string.IsNullOrWhiteSpace(i.Codec))
                .ToList();

            info.MediaAttachments = internalStreams.Select(GetMediaAttachment)
                .Where(i => i is not null)
                .ToList();

            if (data.Format is not null)
            {
                info.Container = NormalizeFormat(data.Format.FormatName, info.MediaStreams);

                if (int.TryParse(data.Format.BitRate, CultureInfo.InvariantCulture, out var value))
                {
                    info.Bitrate = value;
                }
            }

            var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var tagStreamType = isAudio ? CodecType.Audio : CodecType.Video;

            var tagStream = data.Streams?.FirstOrDefault(i => i.CodecType == tagStreamType);

            if (tagStream?.Tags is not null)
            {
                foreach (var (key, value) in tagStream.Tags)
                {
                    tags[key] = value;
                }
            }

            if (data.Format?.Tags is not null)
            {
                foreach (var (key, value) in data.Format.Tags)
                {
                    tags[key] = value;
                }
            }

            FetchGenres(info, tags);

            info.Name = tags.GetFirstNotNullNorWhiteSpaceValue("title", "title-eng");
            info.ForcedSortName = tags.GetFirstNotNullNorWhiteSpaceValue("sort_name", "title-sort", "titlesort");
            info.Overview = tags.GetFirstNotNullNorWhiteSpaceValue("synopsis", "description", "desc");

            info.IndexNumber = FFProbeHelpers.GetDictionaryNumericValue(tags, "episode_sort");
            info.ParentIndexNumber = FFProbeHelpers.GetDictionaryNumericValue(tags, "season_number");
            info.ShowName = tags.GetValueOrDefault("show_name");
            info.ProductionYear = FFProbeHelpers.GetDictionaryNumericValue(tags, "date");

            // Several different forms of retail/premiere date
            info.PremiereDate =
                FFProbeHelpers.GetDictionaryDateTime(tags, "originaldate") ??
                FFProbeHelpers.GetDictionaryDateTime(tags, "retaildate") ??
                FFProbeHelpers.GetDictionaryDateTime(tags, "retail date") ??
                FFProbeHelpers.GetDictionaryDateTime(tags, "retail_date") ??
                FFProbeHelpers.GetDictionaryDateTime(tags, "date_released") ??
                FFProbeHelpers.GetDictionaryDateTime(tags, "date") ??
                FFProbeHelpers.GetDictionaryDateTime(tags, "creation_time");

            // Set common metadata for music (audio) and music videos (video)
            info.Album = tags.GetValueOrDefault("album");

            if (tags.TryGetValue("artists", out var artists) && !string.IsNullOrWhiteSpace(artists))
            {
                info.Artists = SplitDistinctArtists(artists, new[] { '/', ';' }, false).ToArray();
            }
            else
            {
                var artist = tags.GetFirstNotNullNorWhiteSpaceValue("artist");
                info.Artists = artist is null
                    ? Array.Empty<string>()
                    : SplitDistinctArtists(artist, _nameDelimiters, true).ToArray();
            }

            // Guess ProductionYear from PremiereDate if missing
            if (!info.ProductionYear.HasValue && info.PremiereDate.HasValue)
            {
                info.ProductionYear = info.PremiereDate.Value.Year;
            }

            // Set mediaType-specific metadata
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

                var iTunExtc = tags.GetFirstNotNullNorWhiteSpaceValue("iTunEXTC");
                if (iTunExtc is not null)
                {
                    var parts = iTunExtc.Split('|', StringSplitOptions.RemoveEmptyEntries);
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

                var iTunXml = tags.GetFirstNotNullNorWhiteSpaceValue("iTunMOVI");
                if (iTunXml is not null)
                {
                    FetchFromItunesInfo(iTunXml, info);
                }

                if (data.Format is not null && !string.IsNullOrEmpty(data.Format.Duration))
                {
                    info.RunTimeTicks = TimeSpan.FromSeconds(double.Parse(data.Format.Duration, CultureInfo.InvariantCulture)).Ticks;
                }

                FetchWtvInfo(info, data);

                if (data.Chapters is not null)
                {
                    info.Chapters = data.Chapters.Select(GetChapterInfo).ToArray();
                }

                ExtractTimestamp(info);

                if (tags.TryGetValue("stereo_mode", out var stereoMode) && string.Equals(stereoMode, "left_right", StringComparison.OrdinalIgnoreCase))
                {
                    info.Video3DFormat = Video3DFormat.FullSideBySide;
                }

                foreach (var mediaStream in info.MediaStreams)
                {
                    if (mediaStream.Type == MediaStreamType.Audio && !mediaStream.BitRate.HasValue)
                    {
                        mediaStream.BitRate = GetEstimatedAudioBitrate(mediaStream.Codec, mediaStream.Channels);
                    }
                }

                var videoStreamsBitrate = info.MediaStreams.Where(i => i.Type == MediaStreamType.Video).Select(i => i.BitRate ?? 0).Sum();
                // If ffprobe reported the container bitrate as being the same as the video stream bitrate, then it's wrong
                if (videoStreamsBitrate == (info.Bitrate ?? 0))
                {
                    info.InferTotalBitrate(true);
                }
            }

            return info;
        }

        private string NormalizeFormat(string format, IReadOnlyList<MediaStream> mediaStreams)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return null;
            }

            // Input can be a list of multiple, comma-delimited formats - each of them needs to be checked
            var splitFormat = format.Split(',');
            for (var i = 0; i < splitFormat.Length; i++)
            {
                // Handle MPEG-1 container
                if (string.Equals(splitFormat[i], "mpegvideo", StringComparison.OrdinalIgnoreCase))
                {
                    splitFormat[i] = "mpeg";
                }

                // Handle MPEG-TS container
                else if (string.Equals(splitFormat[i], "mpegts", StringComparison.OrdinalIgnoreCase))
                {
                    splitFormat[i] = "ts";
                }

                // Handle matroska container
                else if (string.Equals(splitFormat[i], "matroska", StringComparison.OrdinalIgnoreCase))
                {
                    splitFormat[i] = "mkv";
                }

                // Handle WebM
                else if (string.Equals(splitFormat[i], "webm", StringComparison.OrdinalIgnoreCase))
                {
                    // Limit WebM to supported codecs
                    if (mediaStreams.Any(stream => (stream.Type == MediaStreamType.Video && !_webmVideoCodecs.Contains(stream.Codec, StringComparison.OrdinalIgnoreCase))
                        || (stream.Type == MediaStreamType.Audio && !_webmAudioCodecs.Contains(stream.Codec, StringComparison.OrdinalIgnoreCase))))
                    {
                        splitFormat[i] = string.Empty;
                    }
                }
            }

            return string.Join(',', splitFormat.Where(s => !string.IsNullOrEmpty(s)));
        }

        private int? GetEstimatedAudioBitrate(string codec, int? channels)
        {
            if (!channels.HasValue)
            {
                return null;
            }

            var channelsValue = channels.Value;

            if (string.Equals(codec, "aac", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "mp3", StringComparison.OrdinalIgnoreCase))
            {
                switch (channelsValue)
                {
                    case <= 2:
                        return 192000;
                    case >= 5:
                        return 320000;
                }
            }

            if (string.Equals(codec, "ac3", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "eac3", StringComparison.OrdinalIgnoreCase))
            {
                switch (channelsValue)
                {
                    case <= 2:
                        return 192000;
                    case >= 5:
                        return 640000;
                }
            }

            if (string.Equals(codec, "flac", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "alac", StringComparison.OrdinalIgnoreCase))
            {
                switch (channelsValue)
                {
                    case <= 2:
                        return 960000;
                    case >= 5:
                        return 2880000;
                }
            }

            return null;
        }

        private void FetchFromItunesInfo(string xml, MediaInfo info)
        {
            // Make things simpler and strip out the dtd
            var plistIndex = xml.IndexOf("<plist", StringComparison.OrdinalIgnoreCase);

            if (plistIndex != -1)
            {
                xml = xml.Substring(plistIndex);
            }

            xml = "<?xml version=\"1.0\"?>" + xml;

            // <?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n<plist version=\"1.0\">\n<dict>\n\t<key>cast</key>\n\t<array>\n\t\t<dict>\n\t\t\t<key>name</key>\n\t\t\t<string>Blender Foundation</string>\n\t\t</dict>\n\t\t<dict>\n\t\t\t<key>name</key>\n\t\t\t<string>Janus Bager Kristensen</string>\n\t\t</dict>\n\t</array>\n\t<key>directors</key>\n\t<array>\n\t\t<dict>\n\t\t\t<key>name</key>\n\t\t\t<string>Sacha Goedegebure</string>\n\t\t</dict>\n\t</array>\n\t<key>studio</key>\n\t<string>Blender Foundation</string>\n</dict>\n</plist>\n
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            using (var streamReader = new StreamReader(stream))
            {
                try
                {
                    using (var reader = XmlReader.Create(streamReader))
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
                                    case "dict":
                                        if (reader.IsEmptyElement)
                                        {
                                            reader.Read();
                                            continue;
                                        }

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
                            else
                            {
                                reader.Read();
                            }
                        }
                    }
                }
                catch (XmlException)
                {
                    // I've seen probe examples where the iTunMOVI value is just "<"
                    // So we should not allow this to fail the entire probing operation
                }
            }
        }

        private void ReadFromDictNode(XmlReader reader, MediaInfo info)
        {
            string currentKey = null;
            var pairs = new List<NameValuePair>();

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
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
                            if (reader.IsEmptyElement)
                            {
                                reader.Read();
                                continue;
                            }

                            using (var subtree = reader.ReadSubtree())
                            {
                                if (!string.IsNullOrWhiteSpace(currentKey))
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
                else
                {
                    reader.Read();
                }
            }
        }

        private List<NameValuePair> ReadValueArray(XmlReader reader)
        {
            var pairs = new List<NameValuePair>();

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "dict":

                            if (reader.IsEmptyElement)
                            {
                                reader.Read();
                                continue;
                            }

                            using (var subtree = reader.ReadSubtree())
                            {
                                var dict = GetNameValuePair(subtree);
                                if (dict is not null)
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
                else
                {
                    reader.Read();
                }
            }

            return pairs;
        }

        private void ProcessPairs(string key, List<NameValuePair> pairs, MediaInfo info)
        {
            List<BaseItemPerson> peoples = new List<BaseItemPerson>();
            if (string.Equals(key, "studio", StringComparison.OrdinalIgnoreCase))
            {
                info.Studios = pairs.Select(p => p.Value)
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            else if (string.Equals(key, "screenwriters", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var pair in pairs)
                {
                    peoples.Add(new BaseItemPerson
                    {
                        Name = pair.Value,
                        Type = PersonKind.Writer
                    });
                }
            }
            else if (string.Equals(key, "producers", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var pair in pairs)
                {
                    peoples.Add(new BaseItemPerson
                    {
                        Name = pair.Value,
                        Type = PersonKind.Producer
                    });
                }
            }
            else if (string.Equals(key, "directors", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var pair in pairs)
                {
                    peoples.Add(new BaseItemPerson
                    {
                        Name = pair.Value,
                        Type = PersonKind.Director
                    });
                }
            }

            info.People = peoples.ToArray();
        }

        private NameValuePair GetNameValuePair(XmlReader reader)
        {
            string name = null;
            string value = null;

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
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
                else
                {
                    reader.Read();
                }
            }

            if (string.IsNullOrWhiteSpace(name)
                || string.IsNullOrWhiteSpace(value))
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
                codec = "DVBSUB";
            }
            else if (string.Equals(codec, "dvb_teletext", StringComparison.OrdinalIgnoreCase))
            {
                codec = "DVBTXT";
            }
            else if (string.Equals(codec, "dvd_subtitle", StringComparison.OrdinalIgnoreCase))
            {
                codec = "DVDSUB"; // .sub+.idx
            }
            else if (string.Equals(codec, "hdmv_pgs_subtitle", StringComparison.OrdinalIgnoreCase))
            {
                codec = "PGSSUB"; // .sup
            }

            return codec;
        }

        /// <summary>
        /// Converts ffprobe stream info to our MediaAttachment class.
        /// </summary>
        /// <param name="streamInfo">The stream info.</param>
        /// <returns>MediaAttachments.</returns>
        private MediaAttachment GetMediaAttachment(MediaStreamInfo streamInfo)
        {
            if (streamInfo.CodecType != CodecType.Attachment
                && streamInfo.Disposition?.GetValueOrDefault("attached_pic") != 1)
            {
                return null;
            }

            var attachment = new MediaAttachment
            {
                Codec = streamInfo.CodecName,
                Index = streamInfo.Index
            };

            if (!string.IsNullOrWhiteSpace(streamInfo.CodecTagString))
            {
                attachment.CodecTag = streamInfo.CodecTagString;
            }

            if (streamInfo.Tags is not null)
            {
                attachment.FileName = GetDictionaryValue(streamInfo.Tags, "filename");
                attachment.MimeType = GetDictionaryValue(streamInfo.Tags, "mimetype");
                attachment.Comment = GetDictionaryValue(streamInfo.Tags, "comment");
            }

            return attachment;
        }

        /// <summary>
        /// Converts ffprobe stream info to our MediaStream class.
        /// </summary>
        /// <param name="isAudio">if set to <c>true</c> [is info].</param>
        /// <param name="streamInfo">The stream info.</param>
        /// <param name="formatInfo">The format info.</param>
        /// <returns>MediaStream.</returns>
        private MediaStream GetMediaStream(bool isAudio, MediaStreamInfo streamInfo, MediaFormatInfo formatInfo)
        {
            // These are mp4 chapters
            if (string.Equals(streamInfo.CodecName, "mov_text", StringComparison.OrdinalIgnoreCase))
            {
                // Edit: but these are also sometimes subtitles?
                // return null;
            }

            var stream = new MediaStream
            {
                Codec = streamInfo.CodecName,
                Profile = streamInfo.Profile,
                Level = streamInfo.Level,
                Index = streamInfo.Index,
                PixelFormat = streamInfo.PixelFormat,
                NalLengthSize = streamInfo.NalLengthSize,
                TimeBase = streamInfo.TimeBase,
                CodecTimeBase = streamInfo.CodecTimeBase,
                IsAVC = streamInfo.IsAvc
            };

            // Filter out junk
            if (!string.IsNullOrWhiteSpace(streamInfo.CodecTagString) && !streamInfo.CodecTagString.Contains("[0]", StringComparison.OrdinalIgnoreCase))
            {
                stream.CodecTag = streamInfo.CodecTagString;
            }

            if (streamInfo.Tags is not null)
            {
                stream.Language = GetDictionaryValue(streamInfo.Tags, "language");
                stream.Comment = GetDictionaryValue(streamInfo.Tags, "comment");
                stream.Title = GetDictionaryValue(streamInfo.Tags, "title");
            }

            if (streamInfo.CodecType == CodecType.Audio)
            {
                stream.Type = MediaStreamType.Audio;
                stream.LocalizedDefault = _localization.GetLocalizedString("Default");
                stream.LocalizedExternal = _localization.GetLocalizedString("External");

                stream.Channels = streamInfo.Channels;

                if (int.TryParse(streamInfo.SampleRate, CultureInfo.InvariantCulture, out var sampleRate))
                {
                    stream.SampleRate = sampleRate;
                }

                stream.ChannelLayout = ParseChannelLayout(streamInfo.ChannelLayout);

                if (streamInfo.BitsPerSample > 0)
                {
                    stream.BitDepth = streamInfo.BitsPerSample;
                }
                else if (streamInfo.BitsPerRawSample > 0)
                {
                    stream.BitDepth = streamInfo.BitsPerRawSample;
                }

                if (string.IsNullOrEmpty(stream.Title))
                {
                    // mp4 missing track title workaround: fall back to handler_name if populated and not the default "SoundHandler"
                    string handlerName = GetDictionaryValue(streamInfo.Tags, "handler_name");
                    if (!string.IsNullOrEmpty(handlerName) && !string.Equals(handlerName, "SoundHandler", StringComparison.OrdinalIgnoreCase))
                    {
                        stream.Title = handlerName;
                    }
                }
            }
            else if (streamInfo.CodecType == CodecType.Subtitle)
            {
                stream.Type = MediaStreamType.Subtitle;
                stream.Codec = NormalizeSubtitleCodec(stream.Codec);
                stream.LocalizedUndefined = _localization.GetLocalizedString("Undefined");
                stream.LocalizedDefault = _localization.GetLocalizedString("Default");
                stream.LocalizedForced = _localization.GetLocalizedString("Forced");
                stream.LocalizedExternal = _localization.GetLocalizedString("External");
                stream.LocalizedHearingImpaired = _localization.GetLocalizedString("HearingImpaired");

                // Graphical subtitle may have width and height info
                stream.Width = streamInfo.Width;
                stream.Height = streamInfo.Height;

                if (string.IsNullOrEmpty(stream.Title))
                {
                    // mp4 missing track title workaround: fall back to handler_name if populated and not the default "SubtitleHandler"
                    string handlerName = GetDictionaryValue(streamInfo.Tags, "handler_name");
                    if (!string.IsNullOrEmpty(handlerName) && !string.Equals(handlerName, "SubtitleHandler", StringComparison.OrdinalIgnoreCase))
                    {
                        stream.Title = handlerName;
                    }
                }
            }
            else if (streamInfo.CodecType == CodecType.Video)
            {
                stream.AverageFrameRate = GetFrameRate(streamInfo.AverageFrameRate);
                stream.RealFrameRate = GetFrameRate(streamInfo.RFrameRate);

                stream.IsInterlaced = !string.IsNullOrWhiteSpace(streamInfo.FieldOrder)
                    && !string.Equals(streamInfo.FieldOrder, "progressive", StringComparison.OrdinalIgnoreCase);

                if (isAudio
                    || string.Equals(stream.Codec, "bmp", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(stream.Codec, "gif", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(stream.Codec, "png", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(stream.Codec, "webp", StringComparison.OrdinalIgnoreCase))
                {
                    stream.Type = MediaStreamType.EmbeddedImage;
                }
                else if (string.Equals(stream.Codec, "mjpeg", StringComparison.OrdinalIgnoreCase))
                {
                    // How to differentiate between video and embedded image?
                    // The only difference I've seen thus far is presence of codec tag, also embedded images have high (unusual) framerates
                    if (!string.IsNullOrWhiteSpace(stream.CodecTag))
                    {
                        stream.Type = MediaStreamType.Video;
                    }
                    else
                    {
                        stream.Type = MediaStreamType.EmbeddedImage;
                    }
                }
                else
                {
                    stream.Type = MediaStreamType.Video;
                }

                stream.Width = streamInfo.Width;
                stream.Height = streamInfo.Height;
                stream.AspectRatio = GetAspectRatio(streamInfo);

                if (streamInfo.BitsPerSample > 0)
                {
                    stream.BitDepth = streamInfo.BitsPerSample;
                }
                else if (streamInfo.BitsPerRawSample > 0)
                {
                    stream.BitDepth = streamInfo.BitsPerRawSample;
                }

                if (!stream.BitDepth.HasValue)
                {
                    if (!string.IsNullOrEmpty(streamInfo.PixelFormat))
                    {
                        if (string.Equals(streamInfo.PixelFormat, "yuv420p", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(streamInfo.PixelFormat, "yuv444p", StringComparison.OrdinalIgnoreCase))
                        {
                            stream.BitDepth = 8;
                        }
                        else if (string.Equals(streamInfo.PixelFormat, "yuv420p10le", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(streamInfo.PixelFormat, "yuv444p10le", StringComparison.OrdinalIgnoreCase))
                        {
                            stream.BitDepth = 10;
                        }
                        else if (string.Equals(streamInfo.PixelFormat, "yuv420p12le", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(streamInfo.PixelFormat, "yuv444p12le", StringComparison.OrdinalIgnoreCase))
                        {
                            stream.BitDepth = 12;
                        }
                    }
                }

                // stream.IsAnamorphic = string.Equals(streamInfo.sample_aspect_ratio, "0:1", StringComparison.OrdinalIgnoreCase) ||
                //    string.Equals(stream.AspectRatio, "2.35:1", StringComparison.OrdinalIgnoreCase) ||
                //    string.Equals(stream.AspectRatio, "2.40:1", StringComparison.OrdinalIgnoreCase);

                // http://stackoverflow.com/questions/17353387/how-to-detect-anamorphic-video-with-ffprobe
                stream.IsAnamorphic = string.Equals(streamInfo.SampleAspectRatio, "0:1", StringComparison.OrdinalIgnoreCase);

                if (streamInfo.Refs > 0)
                {
                    stream.RefFrames = streamInfo.Refs;
                }

                if (!string.IsNullOrEmpty(streamInfo.ColorRange))
                {
                    stream.ColorRange = streamInfo.ColorRange;
                }

                if (!string.IsNullOrEmpty(streamInfo.ColorSpace))
                {
                    stream.ColorSpace = streamInfo.ColorSpace;
                }

                if (!string.IsNullOrEmpty(streamInfo.ColorTransfer))
                {
                    stream.ColorTransfer = streamInfo.ColorTransfer;
                }

                if (!string.IsNullOrEmpty(streamInfo.ColorPrimaries))
                {
                    stream.ColorPrimaries = streamInfo.ColorPrimaries;
                }

                if (streamInfo.SideDataList is not null)
                {
                    foreach (var data in streamInfo.SideDataList)
                    {
                        // Parse Dolby Vision metadata from side_data
                        if (string.Equals(data.SideDataType, "DOVI configuration record", StringComparison.OrdinalIgnoreCase))
                        {
                            stream.DvVersionMajor = data.DvVersionMajor;
                            stream.DvVersionMinor = data.DvVersionMinor;
                            stream.DvProfile = data.DvProfile;
                            stream.DvLevel = data.DvLevel;
                            stream.RpuPresentFlag = data.RpuPresentFlag;
                            stream.ElPresentFlag = data.ElPresentFlag;
                            stream.BlPresentFlag = data.BlPresentFlag;
                            stream.DvBlSignalCompatibilityId = data.DvBlSignalCompatibilityId;
                        }

                        // Parse video rotation metadata from side_data
                        else if (string.Equals(data.SideDataType, "Display Matrix", StringComparison.OrdinalIgnoreCase))
                        {
                            stream.Rotation = data.Rotation;
                        }
                    }
                }
            }
            else if (streamInfo.CodecType == CodecType.Data)
            {
                stream.Type = MediaStreamType.Data;
            }
            else
            {
                return null;
            }

            // Get stream bitrate
            var bitrate = 0;

            if (int.TryParse(streamInfo.BitRate, CultureInfo.InvariantCulture, out var value))
            {
                bitrate = value;
            }

            // The bitrate info of FLAC musics and some videos is included in formatInfo.
            if (bitrate == 0
                && formatInfo is not null
                && (stream.Type == MediaStreamType.Video || (isAudio && stream.Type == MediaStreamType.Audio)))
            {
                // If the stream info doesn't have a bitrate get the value from the media format info
                if (int.TryParse(formatInfo.BitRate, CultureInfo.InvariantCulture, out value))
                {
                    bitrate = value;
                }
            }

            if (bitrate > 0)
            {
                stream.BitRate = bitrate;
            }

            // Extract bitrate info from tag "BPS" if possible.
            if (!stream.BitRate.HasValue
                && (streamInfo.CodecType == CodecType.Audio
                    || streamInfo.CodecType == CodecType.Video))
            {
                var bps = GetBPSFromTags(streamInfo);
                if (bps > 0)
                {
                    stream.BitRate = bps;
                }
                else
                {
                    // Get average bitrate info from tag "NUMBER_OF_BYTES" and "DURATION" if possible.
                    var durationInSeconds = GetRuntimeSecondsFromTags(streamInfo);
                    var bytes = GetNumberOfBytesFromTags(streamInfo);
                    if (durationInSeconds is not null && bytes is not null)
                    {
                        bps = Convert.ToInt32(bytes * 8 / durationInSeconds, CultureInfo.InvariantCulture);
                        if (bps > 0)
                        {
                            stream.BitRate = bps;
                        }
                    }
                }
            }

            var disposition = streamInfo.Disposition;
            if (disposition is not null)
            {
                if (disposition.GetValueOrDefault("default") == 1)
                {
                    stream.IsDefault = true;
                }

                if (disposition.GetValueOrDefault("forced") == 1)
                {
                    stream.IsForced = true;
                }

                if (disposition.GetValueOrDefault("hearing_impaired") == 1)
                {
                    stream.IsHearingImpaired = true;
                }
            }

            NormalizeStreamTitle(stream);

            return stream;
        }

        private void NormalizeStreamTitle(MediaStream stream)
        {
            if (string.Equals(stream.Title, "cc", StringComparison.OrdinalIgnoreCase)
                || stream.Type == MediaStreamType.EmbeddedImage)
            {
                stream.Title = null;
            }
        }

        /// <summary>
        /// Gets a string from an FFProbeResult tags dictionary.
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <param name="key">The key.</param>
        /// <returns>System.String.</returns>
        private string GetDictionaryValue(IReadOnlyDictionary<string, string> tags, string key)
        {
            if (tags is null)
            {
                return null;
            }

            tags.TryGetValue(key, out var val);

            return val;
        }

        private string ParseChannelLayout(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            return input.AsSpan().LeftPart('(').ToString();
        }

        private string GetAspectRatio(MediaStreamInfo info)
        {
            var original = info.DisplayAspectRatio;

            var parts = (original ?? string.Empty).Split(':');
            if (!(parts.Length == 2
                    && int.TryParse(parts[0], CultureInfo.InvariantCulture, out var width)
                    && int.TryParse(parts[1], CultureInfo.InvariantCulture, out var height)
                    && width > 0
                    && height > 0))
            {
                width = info.Width;
                height = info.Height;
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
        internal static float? GetFrameRate(ReadOnlySpan<char> value)
        {
            if (value.IsEmpty)
            {
                return null;
            }

            int index = value.IndexOf('/');
            if (index == -1)
            {
                return null;
            }

            if (!float.TryParse(value[..index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dividend)
                || !float.TryParse(value[(index + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var divisor))
            {
                return null;
            }

            return divisor == 0f ? null : dividend / divisor;
        }

        private void SetAudioRuntimeTicks(InternalMediaInfoResult result, MediaInfo data)
        {
            // Get the first info stream
            var stream = result.Streams?.FirstOrDefault(s => s.CodecType == CodecType.Audio);
            if (stream is null)
            {
                return;
            }

            // Get duration from stream properties
            var duration = stream.Duration;

            // If it's not there go into format properties
            if (string.IsNullOrEmpty(duration))
            {
                duration = result.Format.Duration;
            }

            // If we got something, parse it
            if (!string.IsNullOrEmpty(duration))
            {
                data.RunTimeTicks = TimeSpan.FromSeconds(double.Parse(duration, CultureInfo.InvariantCulture)).Ticks;
            }
        }

        private int? GetBPSFromTags(MediaStreamInfo streamInfo)
        {
            if (streamInfo?.Tags is null)
            {
                return null;
            }

            var bps = GetDictionaryValue(streamInfo.Tags, "BPS-eng") ?? GetDictionaryValue(streamInfo.Tags, "BPS");
            if (int.TryParse(bps, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedBps))
            {
                return parsedBps;
            }

            return null;
        }

        private double? GetRuntimeSecondsFromTags(MediaStreamInfo streamInfo)
        {
            if (streamInfo?.Tags is null)
            {
                return null;
            }

            var duration = GetDictionaryValue(streamInfo.Tags, "DURATION-eng") ?? GetDictionaryValue(streamInfo.Tags, "DURATION");
            if (TimeSpan.TryParse(duration, out var parsedDuration))
            {
                return parsedDuration.TotalSeconds;
            }

            return null;
        }

        private long? GetNumberOfBytesFromTags(MediaStreamInfo streamInfo)
        {
            if (streamInfo?.Tags is null)
            {
                return null;
            }

            var numberOfBytes = GetDictionaryValue(streamInfo.Tags, "NUMBER_OF_BYTES-eng")
                                ?? GetDictionaryValue(streamInfo.Tags, "NUMBER_OF_BYTES");
            if (long.TryParse(numberOfBytes, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedBytes))
            {
                return parsedBytes;
            }

            return null;
        }

        private void SetSize(InternalMediaInfoResult data, MediaInfo info)
        {
            if (data.Format is null)
            {
                return;
            }

            info.Size = string.IsNullOrEmpty(data.Format.Size) ? null : long.Parse(data.Format.Size, CultureInfo.InvariantCulture);
        }

        private void SetAudioInfoFromTags(MediaInfo audio, Dictionary<string, string> tags)
        {
            var people = new List<BaseItemPerson>();
            if (tags.TryGetValue("composer", out var composer) && !string.IsNullOrWhiteSpace(composer))
            {
                foreach (var person in Split(composer, false))
                {
                    people.Add(new BaseItemPerson { Name = person, Type = PersonKind.Composer });
                }
            }

            if (tags.TryGetValue("conductor", out var conductor) && !string.IsNullOrWhiteSpace(conductor))
            {
                foreach (var person in Split(conductor, false))
                {
                    people.Add(new BaseItemPerson { Name = person, Type = PersonKind.Conductor });
                }
            }

            if (tags.TryGetValue("lyricist", out var lyricist) && !string.IsNullOrWhiteSpace(lyricist))
            {
                foreach (var person in Split(lyricist, false))
                {
                    people.Add(new BaseItemPerson { Name = person, Type = PersonKind.Lyricist });
                }
            }

            if (tags.TryGetValue("performer", out var performer) && !string.IsNullOrWhiteSpace(performer))
            {
                foreach (var person in Split(performer, false))
                {
                    Match match = PerformerRegex().Match(person);

                    // If the performer doesn't have any instrument/role associated, it won't match. In that case, chances are it's simply a band name, so we skip it.
                    if (match.Success)
                    {
                        people.Add(new BaseItemPerson
                        {
                            Name = match.Groups["name"].Value,
                            Type = PersonKind.Actor,
                            Role = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(match.Groups["instrument"].Value)
                        });
                    }
                }
            }

            // In cases where there isn't sufficient information as to which role a writer performed on a recording, tagging software uses the "writer" tag.
            if (tags.TryGetValue("writer", out var writer) && !string.IsNullOrWhiteSpace(writer))
            {
                foreach (var person in Split(writer, false))
                {
                    people.Add(new BaseItemPerson { Name = person, Type = PersonKind.Writer });
                }
            }

            if (tags.TryGetValue("arranger", out var arranger) && !string.IsNullOrWhiteSpace(arranger))
            {
                foreach (var person in Split(arranger, false))
                {
                    people.Add(new BaseItemPerson { Name = person, Type = PersonKind.Arranger });
                }
            }

            if (tags.TryGetValue("engineer", out var engineer) && !string.IsNullOrWhiteSpace(engineer))
            {
                foreach (var person in Split(engineer, false))
                {
                    people.Add(new BaseItemPerson { Name = person, Type = PersonKind.Engineer });
                }
            }

            if (tags.TryGetValue("mixer", out var mixer) && !string.IsNullOrWhiteSpace(mixer))
            {
                foreach (var person in Split(mixer, false))
                {
                    people.Add(new BaseItemPerson { Name = person, Type = PersonKind.Mixer });
                }
            }

            if (tags.TryGetValue("remixer", out var remixer) && !string.IsNullOrWhiteSpace(remixer))
            {
                foreach (var person in Split(remixer, false))
                {
                    people.Add(new BaseItemPerson { Name = person, Type = PersonKind.Remixer });
                }
            }

            audio.People = people.ToArray();

            // Set album artist
            var albumArtist = tags.GetFirstNotNullNorWhiteSpaceValue("albumartist", "album artist", "album_artist");
            audio.AlbumArtists = albumArtist is not null
                ? SplitDistinctArtists(albumArtist, _nameDelimiters, true).ToArray()
                : Array.Empty<string>();

            // Set album artist to artist if empty
            if (audio.AlbumArtists.Length == 0)
            {
                audio.AlbumArtists = audio.Artists;
            }

            // Track number
            audio.IndexNumber = GetDictionaryTrackOrDiscNumber(tags, "track");

            // Disc number
            audio.ParentIndexNumber = GetDictionaryTrackOrDiscNumber(tags, "disc");

            // There's several values in tags may or may not be present
            FetchStudios(audio, tags, "organization");
            FetchStudios(audio, tags, "ensemble");
            FetchStudios(audio, tags, "publisher");
            FetchStudios(audio, tags, "label");

            // These support multiple values, but for now we only store the first.
            var mb = GetMultipleMusicBrainzId(tags.GetValueOrDefault("MusicBrainz Album Artist Id"))
                ?? GetMultipleMusicBrainzId(tags.GetValueOrDefault("MUSICBRAINZ_ALBUMARTISTID"));
            audio.TrySetProviderId(MetadataProvider.MusicBrainzAlbumArtist, mb);

            mb = GetMultipleMusicBrainzId(tags.GetValueOrDefault("MusicBrainz Artist Id"))
                ?? GetMultipleMusicBrainzId(tags.GetValueOrDefault("MUSICBRAINZ_ARTISTID"));
            audio.TrySetProviderId(MetadataProvider.MusicBrainzArtist, mb);

            mb = GetMultipleMusicBrainzId(tags.GetValueOrDefault("MusicBrainz Album Id"))
                ?? GetMultipleMusicBrainzId(tags.GetValueOrDefault("MUSICBRAINZ_ALBUMID"));
            audio.TrySetProviderId(MetadataProvider.MusicBrainzAlbum, mb);

            mb = GetMultipleMusicBrainzId(tags.GetValueOrDefault("MusicBrainz Release Group Id"))
                 ?? GetMultipleMusicBrainzId(tags.GetValueOrDefault("MUSICBRAINZ_RELEASEGROUPID"));
            audio.TrySetProviderId(MetadataProvider.MusicBrainzReleaseGroup, mb);

            mb = GetMultipleMusicBrainzId(tags.GetValueOrDefault("MusicBrainz Release Track Id"))
                 ?? GetMultipleMusicBrainzId(tags.GetValueOrDefault("MUSICBRAINZ_RELEASETRACKID"));
            audio.TrySetProviderId(MetadataProvider.MusicBrainzTrack, mb);
        }

        private string GetMultipleMusicBrainzId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
        }

        /// <summary>
        /// Splits the specified val.
        /// </summary>
        /// <param name="val">The val.</param>
        /// <param name="allowCommaDelimiter">if set to <c>true</c> [allow comma delimiter].</param>
        /// <returns>System.String[][].</returns>
        private string[] Split(string val, bool allowCommaDelimiter)
        {
            // Only use the comma as a delimiter if there are no slashes or pipes.
            // We want to be careful not to split names that have commas in them
            return !allowCommaDelimiter || _nameDelimiters.Any(i => val.Contains(i, StringComparison.Ordinal)) ?
                val.Split(_nameDelimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) :
                val.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        private IEnumerable<string> SplitDistinctArtists(string val, char[] delimiters, bool splitFeaturing)
        {
            if (splitFeaturing)
            {
                val = val.Replace(" featuring ", ArtistReplaceValue, StringComparison.OrdinalIgnoreCase)
                    .Replace(" feat. ", ArtistReplaceValue, StringComparison.OrdinalIgnoreCase);
            }

            var artistsFound = new List<string>();

            foreach (var whitelistArtist in SplitWhitelist)
            {
                var originalVal = val;
                val = val.Replace(whitelistArtist, "|", StringComparison.OrdinalIgnoreCase);

                if (!string.Equals(originalVal, val, StringComparison.OrdinalIgnoreCase))
                {
                    artistsFound.Add(whitelistArtist);
                }
            }

            var artists = val.Split(delimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            artistsFound.AddRange(artists);
            return artistsFound.DistinctNames();
        }

        /// <summary>
        /// Gets the studios from the tags collection.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="tags">The tags.</param>
        /// <param name="tagName">Name of the tag.</param>
        private void FetchStudios(MediaInfo info, IReadOnlyDictionary<string, string> tags, string tagName)
        {
            var val = tags.GetValueOrDefault(tagName);

            if (string.IsNullOrEmpty(val))
            {
                return;
            }

            var studios = Split(val, true);
            var studioList = new List<string>();

            foreach (var studio in studios)
            {
                if (string.IsNullOrWhiteSpace(studio))
                {
                    continue;
                }

                // Don't add artist/album artist name to studios, even if it's listed there
                if (info.Artists.Contains(studio, StringComparison.OrdinalIgnoreCase)
                    || info.AlbumArtists.Contains(studio, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                studioList.Add(studio);
            }

            info.Studios = studioList
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        /// <summary>
        /// Gets the genres from the tags collection.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="tags">The tags.</param>
        private void FetchGenres(MediaInfo info, IReadOnlyDictionary<string, string> tags)
        {
            var genreVal = tags.GetValueOrDefault("genre");
            if (string.IsNullOrEmpty(genreVal))
            {
                return;
            }

            var genres = new List<string>(info.Genres);
            foreach (var genre in Split(genreVal, true))
            {
                if (string.IsNullOrWhiteSpace(genre))
                {
                    continue;
                }

                genres.Add(genre);
            }

            info.Genres = genres
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        /// <summary>
        /// Gets the track or disc number, which can be in the form of '1', or '1/3'.
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <param name="tagName">Name of the tag.</param>
        /// <returns>The track or disc number, or null, if missing or not parseable.</returns>
        private static int? GetDictionaryTrackOrDiscNumber(IReadOnlyDictionary<string, string> tags, string tagName)
        {
            var disc = tags.GetValueOrDefault(tagName);

            if (int.TryParse(disc.AsSpan().LeftPart('/'), out var discNum))
            {
                return discNum;
            }

            return null;
        }

        private static ChapterInfo GetChapterInfo(MediaChapter chapter)
        {
            var info = new ChapterInfo();

            if (chapter.Tags is not null && chapter.Tags.TryGetValue("title", out string name))
            {
                info.Name = name;
            }

            // Limit accuracy to milliseconds to match xml saving
            var secondsString = chapter.StartTime;

            if (double.TryParse(secondsString, CultureInfo.InvariantCulture, out var seconds))
            {
                var ms = Math.Round(TimeSpan.FromSeconds(seconds).TotalMilliseconds);
                info.StartPositionTicks = TimeSpan.FromMilliseconds(ms).Ticks;
            }

            return info;
        }

        private void FetchWtvInfo(MediaInfo video, InternalMediaInfoResult data)
        {
            var tags = data.Format?.Tags;

            if (tags is null)
            {
                return;
            }

            if (tags.TryGetValue("WM/Genre", out var genres) && !string.IsNullOrWhiteSpace(genres))
            {
                var genreList = genres.Split(new[] { ';', '/', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                // If this is empty then don't overwrite genres that might have been fetched earlier
                if (genreList.Length > 0)
                {
                    video.Genres = genreList;
                }
            }

            if (tags.TryGetValue("WM/ParentalRating", out var officialRating) && !string.IsNullOrWhiteSpace(officialRating))
            {
                video.OfficialRating = officialRating;
            }

            if (tags.TryGetValue("WM/MediaCredits", out var people) && !string.IsNullOrEmpty(people))
            {
                video.People = Array.ConvertAll(
                    people.Split(new[] { ';', '/' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                    i => new BaseItemPerson { Name = i, Type = PersonKind.Actor });
            }

            if (tags.TryGetValue("WM/OriginalReleaseTime", out var year) && int.TryParse(year, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedYear))
            {
                video.ProductionYear = parsedYear;
            }

            // Credit to MCEBuddy: https://mcebuddy2x.codeplex.com/
            // DateTime is reported along with timezone info (typically Z i.e. UTC hence assume None)
            if (tags.TryGetValue("WM/MediaOriginalBroadcastDateTime", out var premiereDateString) && DateTime.TryParse(year, null, DateTimeStyles.AdjustToUniversal, out var parsedDate))
            {
                video.PremiereDate = parsedDate;
            }

            var description = tags.GetValueOrDefault("WM/SubTitleDescription");

            var subTitle = tags.GetValueOrDefault("WM/SubTitle");

            // For below code, credit to MCEBuddy: https://mcebuddy2x.codeplex.com/

            // Sometimes for TV Shows the Subtitle field is empty and the subtitle description contains the subtitle, extract if possible. See ticket https://mcebuddy2x.codeplex.com/workitem/1910
            // The format is -> EPISODE/TOTAL_EPISODES_IN_SEASON. SUBTITLE: DESCRIPTION
            // OR -> COMMENT. SUBTITLE: DESCRIPTION
            // e.g. -> 4/13. The Doctor's Wife: Science fiction drama. When he follows a Time Lord distress signal, the Doctor puts Amy, Rory and his beloved TARDIS in grave danger. Also in HD. [AD,S]
            // e.g. -> CBeebies Bedtime Hour. The Mystery: Animated adventures of two friends who live on an island in the middle of the big city. Some of Abney and Teal's favourite objects are missing. [S]
            if (string.IsNullOrWhiteSpace(subTitle)
                && !string.IsNullOrWhiteSpace(description)
                && description.AsSpan()[..Math.Min(description.Length, MaxSubtitleDescriptionExtractionLength)].Contains(':')) // Check within the Subtitle size limit, otherwise from description it can get too long creating an invalid filename
            {
                string[] descriptionParts = description.Split(':');
                if (descriptionParts.Length > 0)
                {
                    string subtitle = descriptionParts[0];
                    try
                    {
                        // Check if it contains a episode number and season number
                        if (subtitle.Contains('/', StringComparison.Ordinal))
                        {
                            string[] subtitleParts = subtitle.Split(' ');
                            string[] numbers = subtitleParts[0].Replace(".", string.Empty, StringComparison.Ordinal).Split('/');
                            video.IndexNumber = int.Parse(numbers[0], CultureInfo.InvariantCulture);
                            // int totalEpisodesInSeason = int.Parse(numbers[1], CultureInfo.InvariantCulture);

                            // Skip the numbers, concatenate the rest, trim and set as new description
                            description = string.Join(' ', subtitleParts, 1, subtitleParts.Length - 1).Trim();
                        }
                        else if (subtitle.Contains('.', StringComparison.Ordinal))
                        {
                            var subtitleParts = subtitle.Split('.');
                            description = string.Join('.', subtitleParts, 1, subtitleParts.Length - 1).Trim();
                        }
                        else
                        {
                            description = subtitle.Trim();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while parsing subtitle field");

                        // Fallback to default parsing
                        if (subtitle.Contains('.', StringComparison.Ordinal))
                        {
                            var subtitleParts = subtitle.Split('.');
                            description = string.Join('.', subtitleParts, 1, subtitleParts.Length - 1).Trim();
                        }
                        else
                        {
                            description = subtitle.Trim();
                        }
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
            if (video.VideoType != VideoType.VideoFile)
            {
                return;
            }

            if (!string.Equals(video.Container, "mpeg2ts", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(video.Container, "m2ts", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(video.Container, "ts", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                video.Timestamp = GetMpegTimestamp(video.Path);
                _logger.LogDebug("Video has {Timestamp} timestamp", video.Timestamp);
            }
            catch (Exception ex)
            {
                video.Timestamp = null;
                _logger.LogError(ex, "Error extracting timestamp info from {Path}", video.Path);
            }
        }

        // REVIEW: find out why the byte array needs to be 197 bytes long and comment the reason
        private TransportStreamTimestamp GetMpegTimestamp(string path)
        {
            var packetBuffer = new byte[197];

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1))
            {
                fs.ReadExactly(packetBuffer);
            }

            if (packetBuffer[0] == 71)
            {
                return TransportStreamTimestamp.None;
            }

            if ((packetBuffer[4] != 71) || (packetBuffer[196] != 71))
            {
                return TransportStreamTimestamp.None;
            }

            if ((packetBuffer[0] == 0) && (packetBuffer[1] == 0) && (packetBuffer[2] == 0) && (packetBuffer[3] == 0))
            {
                return TransportStreamTimestamp.Zero;
            }

            return TransportStreamTimestamp.Valid;
        }

        [GeneratedRegex("(?<name>.*) \\((?<instrument>.*)\\)")]
        private static partial Regex PerformerRegex();
    }
}
