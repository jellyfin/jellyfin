using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.Probing
{
    public class ProbeResultNormalizer
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly ILogger _logger;
        private readonly ILocalizationManager _localization;

        public ProbeResultNormalizer(ILogger logger, ILocalizationManager localization)
        {
            _logger = logger;
            _localization = localization;
        }

        public MediaInfo GetMediaInfo(InternalMediaInfoResult data, VideoType? videoType, bool isAudio, string path, MediaProtocol protocol)
        {
            var info = new MediaInfo
            {
                Path = path,
                Protocol = protocol
            };

            FFProbeHelpers.NormalizeFFProbeResult(data);
            SetSize(data, info);

            var internalStreams = data.Streams ?? Array.Empty<MediaStreamInfo>();

            info.MediaStreams = internalStreams.Select(s => GetMediaStream(isAudio, s, data.Format))
                .Where(i => i != null)
                // Drop subtitle streams if we don't know the codec because it will just cause failures if we don't know how to handle them
                .Where(i => i.Type != MediaStreamType.Subtitle || !string.IsNullOrWhiteSpace(i.Codec))
                .ToList();

            info.MediaAttachments = internalStreams.Select(s => GetMediaAttachment(s))
                .Where(i => i != null)
                .ToList();

            if (data.Format != null)
            {
                info.Container = NormalizeFormat(data.Format.FormatName);

                if (!string.IsNullOrEmpty(data.Format.BitRate))
                {
                    if (int.TryParse(data.Format.BitRate, NumberStyles.Any, _usCulture, out var value))
                    {
                        info.Bitrate = value;
                    }
                }
            }

            var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var tagStreamType = isAudio ? "audio" : "video";

            if (data.Streams != null)
            {
                var tagStream = data.Streams.FirstOrDefault(i => string.Equals(i.CodecType, tagStreamType, StringComparison.OrdinalIgnoreCase));

                if (tagStream != null && tagStream.Tags != null)
                {
                    foreach (var pair in tagStream.Tags)
                    {
                        tags[pair.Key] = pair.Value;
                    }
                }
            }

            if (data.Format != null && data.Format.Tags != null)
            {
                foreach (var pair in data.Format.Tags)
                {
                    tags[pair.Key] = pair.Value;
                }
            }

            FetchGenres(info, tags);
            var overview = FFProbeHelpers.GetDictionaryValue(tags, "synopsis");

            if (string.IsNullOrWhiteSpace(overview))
            {
                overview = FFProbeHelpers.GetDictionaryValue(tags, "description");
            }
            if (string.IsNullOrWhiteSpace(overview))
            {
                overview = FFProbeHelpers.GetDictionaryValue(tags, "desc");
            }

            if (!string.IsNullOrWhiteSpace(overview))
            {
                info.Overview = overview;
            }

            var title = FFProbeHelpers.GetDictionaryValue(tags, "title");
            if (!string.IsNullOrWhiteSpace(title))
            {
                info.Name = title;
            }

            info.IndexNumber = FFProbeHelpers.GetDictionaryNumericValue(tags, "episode_sort");
            info.ParentIndexNumber = FFProbeHelpers.GetDictionaryNumericValue(tags, "season_number");
            info.ShowName = FFProbeHelpers.GetDictionaryValue(tags, "show_name");
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

                if (data.Format != null && !string.IsNullOrEmpty(data.Format.Duration))
                {
                    info.RunTimeTicks = TimeSpan.FromSeconds(double.Parse(data.Format.Duration, _usCulture)).Ticks;
                }

                FetchWtvInfo(info, data);

                if (data.Chapters != null)
                {
                    info.Chapters = data.Chapters.Select(GetChapterInfo).ToArray();
                }

                ExtractTimestamp(info);

                var stereoMode = GetDictionaryValue(tags, "stereo_mode");
                if (string.Equals(stereoMode, "left_right", StringComparison.OrdinalIgnoreCase))
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

        private string NormalizeFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return null;
            }

            if (string.Equals(format, "mpegvideo", StringComparison.OrdinalIgnoreCase))
            {
                return "mpeg";
            }

            format = format.Replace("matroska", "mkv", StringComparison.OrdinalIgnoreCase);

            return format;
        }

        private int? GetEstimatedAudioBitrate(string codec, int? channels)
        {
            if (!channels.HasValue)
            {
                return null;
            }

            var channelsValue = channels.Value;

            if (string.Equals(codec, "aac", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(codec, "mp3", StringComparison.OrdinalIgnoreCase))
            {
                if (channelsValue <= 2)
                {
                    return 192000;
                }

                if (channelsValue >= 5)
                {
                    return 320000;
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
                else
                {
                    reader.Read();
                }
            }

            return pairs;
        }

        private void ProcessPairs(string key, List<NameValuePair> pairs, MediaInfo info)
        {
            IList<BaseItemPerson> peoples = new List<BaseItemPerson>();
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
                        Type = PersonType.Writer
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
                        Type = PersonType.Producer
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
                        Type = PersonType.Director
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
        /// Converts ffprobe stream info to our MediaAttachment class
        /// </summary>
        /// <param name="streamInfo">The stream info.</param>
        /// <returns>MediaAttachments.</returns>
        private MediaAttachment GetMediaAttachment(MediaStreamInfo streamInfo)
        {
            if (!string.Equals(streamInfo.CodecType, "attachment", StringComparison.OrdinalIgnoreCase))
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

            if (streamInfo.Tags != null)
            {
                attachment.FileName = GetDictionaryValue(streamInfo.Tags, "filename");
                attachment.MimeType = GetDictionaryValue(streamInfo.Tags, "mimetype");
                attachment.Comment = GetDictionaryValue(streamInfo.Tags, "comment");
            }

            return attachment;
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
            if (string.Equals(streamInfo.CodecName, "mov_text", StringComparison.OrdinalIgnoreCase))
            {
                // Edit: but these are also sometimes subtitles?
                //return null;
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
                CodecTimeBase = streamInfo.CodecTimeBase
            };

            if (string.Equals(streamInfo.IsAvc, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(streamInfo.IsAvc, "1", StringComparison.OrdinalIgnoreCase))
            {
                stream.IsAVC = true;
            }
            else if (string.Equals(streamInfo.IsAvc, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(streamInfo.IsAvc, "0", StringComparison.OrdinalIgnoreCase))
            {
                stream.IsAVC = false;
            }

            if (!string.IsNullOrWhiteSpace(streamInfo.FieldOrder) && !string.Equals(streamInfo.FieldOrder, "progressive", StringComparison.OrdinalIgnoreCase))
            {
                stream.IsInterlaced = true;
            }

            // Filter out junk
            if (!string.IsNullOrWhiteSpace(streamInfo.CodecTagString) && streamInfo.CodecTagString.IndexOf("[0]", StringComparison.OrdinalIgnoreCase) == -1)
            {
                stream.CodecTag = streamInfo.CodecTagString;
            }

            if (streamInfo.Tags != null)
            {
                stream.Language = GetDictionaryValue(streamInfo.Tags, "language");
                stream.Comment = GetDictionaryValue(streamInfo.Tags, "comment");
                stream.Title = GetDictionaryValue(streamInfo.Tags, "title");
            }

            if (string.Equals(streamInfo.CodecType, "audio", StringComparison.OrdinalIgnoreCase))
            {
                stream.Type = MediaStreamType.Audio;

                stream.Channels = streamInfo.Channels;

                if (!string.IsNullOrEmpty(streamInfo.SampleRate))
                {
                    if (int.TryParse(streamInfo.SampleRate, NumberStyles.Any, _usCulture, out var value))
                    {
                        stream.SampleRate = value;
                    }
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
            }
            else if (string.Equals(streamInfo.CodecType, "subtitle", StringComparison.OrdinalIgnoreCase))
            {
                stream.Type = MediaStreamType.Subtitle;
                stream.Codec = NormalizeSubtitleCodec(stream.Codec);
                stream.localizedUndefined = _localization.GetLocalizedString("Undefined");
                stream.localizedDefault = _localization.GetLocalizedString("Default");
                stream.localizedForced = _localization.GetLocalizedString("Forced");
            }
            else if (string.Equals(streamInfo.CodecType, "video", StringComparison.OrdinalIgnoreCase))
            {
                stream.Type = isAudio || string.Equals(stream.Codec, "mjpeg", StringComparison.OrdinalIgnoreCase) || string.Equals(stream.Codec, "gif", StringComparison.OrdinalIgnoreCase) || string.Equals(stream.Codec, "png", StringComparison.OrdinalIgnoreCase)
                    ? MediaStreamType.EmbeddedImage
                    : MediaStreamType.Video;

                stream.AverageFrameRate = GetFrameRate(streamInfo.AverageFrameRate);
                stream.RealFrameRate = GetFrameRate(streamInfo.RFrameRate);

                if (isAudio || string.Equals(stream.Codec, "gif", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(stream.Codec, "png", StringComparison.OrdinalIgnoreCase))
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

                //stream.IsAnamorphic = string.Equals(streamInfo.sample_aspect_ratio, "0:1", StringComparison.OrdinalIgnoreCase) ||
                //    string.Equals(stream.AspectRatio, "2.35:1", StringComparison.OrdinalIgnoreCase) ||
                //    string.Equals(stream.AspectRatio, "2.40:1", StringComparison.OrdinalIgnoreCase);

                // http://stackoverflow.com/questions/17353387/how-to-detect-anamorphic-video-with-ffprobe
                stream.IsAnamorphic = string.Equals(streamInfo.SampleAspectRatio, "0:1", StringComparison.OrdinalIgnoreCase);

                if (streamInfo.Refs > 0)
                {
                    stream.RefFrames = streamInfo.Refs;
                }

                if (!string.IsNullOrEmpty(streamInfo.ColorTransfer))
                {
                    stream.ColorTransfer = streamInfo.ColorTransfer;
                }

                if (!string.IsNullOrEmpty(streamInfo.ColorPrimaries))
                {
                    stream.ColorPrimaries = streamInfo.ColorPrimaries;
                }
            }
            else
            {
                return null;
            }

            // Get stream bitrate
            var bitrate = 0;

            if (!string.IsNullOrEmpty(streamInfo.BitRate))
            {
                if (int.TryParse(streamInfo.BitRate, NumberStyles.Any, _usCulture, out var value))
                {
                    bitrate = value;
                }
            }

            if (bitrate == 0 && formatInfo != null && !string.IsNullOrEmpty(formatInfo.BitRate) && stream.Type == MediaStreamType.Video)
            {
                // If the stream info doesn't have a bitrate get the value from the media format info
                if (int.TryParse(formatInfo.BitRate, NumberStyles.Any, _usCulture, out var value))
                {
                    bitrate = value;
                }
            }

            if (bitrate > 0)
            {
                stream.BitRate = bitrate;
            }

            var disposition = streamInfo.Disposition;
            if (disposition != null)
            {
                if (disposition.GetValueOrDefault("default") == 1)
                {
                    stream.IsDefault = true;
                }

                if (disposition.GetValueOrDefault("forced") == 1)
                {
                    stream.IsForced = true;
                }
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
        private string GetDictionaryValue(IReadOnlyDictionary<string, string> tags, string key)
        {
            if (tags == null)
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
                return input;
            }

            return input.Split('(').FirstOrDefault();
        }

        private string GetAspectRatio(MediaStreamInfo info)
        {
            var original = info.DisplayAspectRatio;

            var parts = (original ?? string.Empty).Split(':');
            if (!(parts.Length == 2 &&
                int.TryParse(parts[0], NumberStyles.Any, _usCulture, out var width) &&
                int.TryParse(parts[1], NumberStyles.Any, _usCulture, out var height) &&
                width > 0 &&
                height > 0))
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
            if (result.Streams != null)
            {
                // Get the first info stream
                var stream = result.Streams.FirstOrDefault(s => string.Equals(s.CodecType, "audio", StringComparison.OrdinalIgnoreCase));

                if (stream != null)
                {
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
                        data.RunTimeTicks = TimeSpan.FromSeconds(double.Parse(duration, _usCulture)).Ticks;
                    }
                }
            }
        }

        private void SetSize(InternalMediaInfoResult data, MediaInfo info)
        {
            if (data.Format != null)
            {
                if (!string.IsNullOrEmpty(data.Format.Size))
                {
                    info.Size = long.Parse(data.Format.Size, _usCulture);
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
                var peoples = new List<BaseItemPerson>();
                foreach (var person in Split(composer, false))
                {
                    peoples.Add(new BaseItemPerson { Name = person, Type = PersonType.Composer });
                }
                audio.People = peoples.ToArray();
            }

            //var conductor = FFProbeHelpers.GetDictionaryValue(tags, "conductor");
            //if (!string.IsNullOrWhiteSpace(conductor))
            //{
            //    foreach (var person in Split(conductor, false))
            //    {
            //        audio.People.Add(new BaseItemPerson { Name = person, Type = PersonType.Conductor });
            //    }
            //}

            //var lyricist = FFProbeHelpers.GetDictionaryValue(tags, "lyricist");
            //if (!string.IsNullOrWhiteSpace(lyricist))
            //{
            //    foreach (var person in Split(lyricist, false))
            //    {
            //        audio.People.Add(new BaseItemPerson { Name = person, Type = PersonType.Lyricist });
            //    }
            //}

            // Check for writer some music is tagged that way as alternative to composer/lyricist
            var writer = FFProbeHelpers.GetDictionaryValue(tags, "writer");

            if (!string.IsNullOrWhiteSpace(writer))
            {
                var peoples = new List<BaseItemPerson>();
                foreach (var person in Split(writer, false))
                {
                    peoples.Add(new BaseItemPerson { Name = person, Type = PersonType.Writer });
                }
                audio.People = peoples.ToArray();
            }

            audio.Album = FFProbeHelpers.GetDictionaryValue(tags, "album");

            var artists = FFProbeHelpers.GetDictionaryValue(tags, "artists");

            if (!string.IsNullOrWhiteSpace(artists))
            {
                audio.Artists = SplitArtists(artists, new[] { '/', ';' }, false)
                    .DistinctNames()
                    .ToArray();
            }
            else
            {
                var artist = FFProbeHelpers.GetDictionaryValue(tags, "artist");
                if (string.IsNullOrWhiteSpace(artist))
                {
                    audio.Artists = new string[] { };
                }
                else
                {
                    audio.Artists = SplitArtists(artist, _nameDelimiters, true)
                    .DistinctNames()
                        .ToArray();
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
                audio.AlbumArtists = new string[] { };
            }
            else
            {
                audio.AlbumArtists = SplitArtists(albumArtist, _nameDelimiters, true)
                    .DistinctNames()
                    .ToArray();

            }

            if (audio.AlbumArtists.Length == 0)
            {
                audio.AlbumArtists = audio.Artists;
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
                _splitWhiteList = new List<string>
                        {
                            "AC/DC"
                        };
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
                var studioList = new List<string>();

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

                    studioList.Add(studio);
                }

                info.Studios = studioList
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
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
                var genres = new List<string>(info.Genres);
                foreach (var genre in Split(val, true))
                {
                    genres.Add(genre);
                }

                info.Genres = genres
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
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

                if (int.TryParse(disc, out var num))
                {
                    return num;
                }
            }

            return null;
        }

        private ChapterInfo GetChapterInfo(MediaChapter chapter)
        {
            var info = new ChapterInfo();

            if (chapter.Tags != null)
            {
                if (chapter.Tags.TryGetValue("title", out string name))
                {
                    info.Name = name;
                }
            }

            // Limit accuracy to milliseconds to match xml saving
            var secondsString = chapter.StartTime;

            if (double.TryParse(secondsString, NumberStyles.Any, CultureInfo.InvariantCulture, out var seconds))
            {
                var ms = Math.Round(TimeSpan.FromSeconds(seconds).TotalMilliseconds);
                info.StartPositionTicks = TimeSpan.FromMilliseconds(ms).Ticks;
            }

            return info;
        }

        private const int MaxSubtitleDescriptionExtractionLength = 100; // When extracting subtitles, the maximum length to consider (to avoid invalid filenames)

        private void FetchWtvInfo(MediaInfo video, InternalMediaInfoResult data)
        {
            if (data.Format == null || data.Format.Tags == null)
            {
                return;
            }

            var genres = FFProbeHelpers.GetDictionaryValue(data.Format.Tags, "WM/Genre");

            if (!string.IsNullOrWhiteSpace(genres))
            {
                var genreList = genres.Split(new[] { ';', '/', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Select(i => i.Trim())
                    .ToList();

                // If this is empty then don't overwrite genres that might have been fetched earlier
                if (genreList.Count > 0)
                {
                    video.Genres = genreList.ToArray();
                }
            }

            var officialRating = FFProbeHelpers.GetDictionaryValue(data.Format.Tags, "WM/ParentalRating");

            if (!string.IsNullOrWhiteSpace(officialRating))
            {
                video.OfficialRating = officialRating;
            }

            var people = FFProbeHelpers.GetDictionaryValue(data.Format.Tags, "WM/MediaCredits");

            if (!string.IsNullOrEmpty(people))
            {
                video.People = people.Split(new[] { ';', '/' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Select(i => new BaseItemPerson { Name = i.Trim(), Type = PersonType.Actor })
                    .ToArray();
            }

            var year = FFProbeHelpers.GetDictionaryValue(data.Format.Tags, "WM/OriginalReleaseTime");
            if (!string.IsNullOrWhiteSpace(year))
            {
                if (int.TryParse(year, NumberStyles.Integer, _usCulture, out var val))
                {
                    video.ProductionYear = val;
                }
            }

            var premiereDateString = FFProbeHelpers.GetDictionaryValue(data.Format.Tags, "WM/MediaOriginalBroadcastDateTime");
            if (!string.IsNullOrWhiteSpace(premiereDateString))
            {
                // Credit to MCEBuddy: https://mcebuddy2x.codeplex.com/
                // DateTime is reported along with timezone info (typically Z i.e. UTC hence assume None)
                if (DateTime.TryParse(year, null, DateTimeStyles.None, out var val))
                {
                    video.PremiereDate = val.ToUniversalTime();
                }
            }

            var description = FFProbeHelpers.GetDictionaryValue(data.Format.Tags, "WM/SubTitleDescription");

            var subTitle = FFProbeHelpers.GetDictionaryValue(data.Format.Tags, "WM/SubTitle");

            // For below code, credit to MCEBuddy: https://mcebuddy2x.codeplex.com/

            // Sometimes for TV Shows the Subtitle field is empty and the subtitle description contains the subtitle, extract if possible. See ticket https://mcebuddy2x.codeplex.com/workitem/1910
            // The format is -> EPISODE/TOTAL_EPISODES_IN_SEASON. SUBTITLE: DESCRIPTION
            // OR -> COMMENT. SUBTITLE: DESCRIPTION
            // e.g. -> 4/13. The Doctor's Wife: Science fiction drama. When he follows a Time Lord distress signal, the Doctor puts Amy, Rory and his beloved TARDIS in grave danger. Also in HD. [AD,S]
            // e.g. -> CBeebies Bedtime Hour. The Mystery: Animated adventures of two friends who live on an island in the middle of the big city. Some of Abney and Teal's favourite objects are missing. [S]
            if (string.IsNullOrWhiteSpace(subTitle) && !string.IsNullOrWhiteSpace(description) && description.Substring(0, Math.Min(description.Length, MaxSubtitleDescriptionExtractionLength)).Contains(":")) // Check within the Subtitle size limit, otherwise from description it can get too long creating an invalid filename
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

                            description = string.Join(" ", numbers, 1, numbers.Length - 1).Trim(); // Skip the first, concatenate the rest, clean up spaces and save it
                        }
                        else
                            throw new Exception(); // Switch to default parsing
                    }
                    catch // Default parsing
                    {
                        if (subtitle.Contains(".")) // skip the comment, keep the subtitle
                            description = string.Join(".", subtitle.Split('.'), 1, subtitle.Split('.').Length - 1).Trim(); // skip the first
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

                        _logger.LogDebug("Video has {Timestamp} timestamp", video.Timestamp);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error extracting timestamp info from {Path}", video.Path);
                        video.Timestamp = null;
                    }
                }
            }
        }

        // REVIEW: find out why the byte array needs to be 197 bytes long and comment the reason
        private TransportStreamTimestamp GetMpegTimestamp(string path)
        {
            var packetBuffer = new byte[197];

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Read(packetBuffer);
            }

            if (packetBuffer[0] == 71)
            {
                return TransportStreamTimestamp.None;
            }

            if ((packetBuffer[4] == 71) && (packetBuffer[196] == 71))
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
