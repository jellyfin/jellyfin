using System;
using System.Globalization;
using System.Security;
using System.Text;

namespace MediaBrowser.Api.Playback.Dash
{
    public class ManifestBuilder
    {
        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

        public string GetManifestText(StreamState state, string playlistUrl)
        {
            var builder = new StringBuilder();

            var time = TimeSpan.FromTicks(state.RunTimeTicks.Value);

            var duration = "PT" + time.Hours.ToString("00", UsCulture) + "H" + time.Minutes.ToString("00", UsCulture) + "M" + time.Seconds.ToString("00", UsCulture) + ".00S";

            builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");

            builder.AppendFormat(
                "<MPD xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"urn:mpeg:dash:schema:mpd:2011\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" xsi:schemaLocation=\"urn:mpeg:DASH:schema:MPD:2011 http://standards.iso.org/ittf/PubliclyAvailableStandards/MPEG-DASH_schema_files/DASH-MPD.xsd\" profiles=\"urn:mpeg:dash:profile:isoff-live:2011\" type=\"static\" mediaPresentationDuration=\"{0}\" minBufferTime=\"PT5.0S\">",
                duration);

            builder.Append("<ProgramInformation>");
            builder.Append("</ProgramInformation>");

            builder.Append("<Period start=\"PT0S\">");
            builder.Append(GetVideoAdaptationSet(state, playlistUrl));
            builder.Append(GetAudioAdaptationSet(state, playlistUrl));
            builder.Append("</Period>");

            builder.Append("</MPD>");

            return builder.ToString();
        }

        private string GetVideoAdaptationSet(StreamState state, string playlistUrl)
        {
            var builder = new StringBuilder();

            builder.Append("<AdaptationSet id=\"video\" segmentAlignment=\"true\" bitstreamSwitching=\"true\">");
            builder.Append(GetVideoRepresentationOpenElement(state));

            AppendSegmentList(state, builder, "0", playlistUrl);

            builder.Append("</Representation>");
            builder.Append("</AdaptationSet>");

            return builder.ToString();
        }

        private string GetAudioAdaptationSet(StreamState state, string playlistUrl)
        {
            var builder = new StringBuilder();

            builder.Append("<AdaptationSet id=\"audio\" segmentAlignment=\"true\" bitstreamSwitching=\"true\">");
            builder.Append(GetAudioRepresentationOpenElement(state));

            builder.Append("<AudioChannelConfiguration schemeIdUri=\"urn:mpeg:dash:23003:3:audio_channel_configuration:2011\" value=\"6\" />");

            AppendSegmentList(state, builder, "1", playlistUrl);

            builder.Append("</Representation>");
            builder.Append("</AdaptationSet>");

            return builder.ToString();
        }

        private string GetVideoRepresentationOpenElement(StreamState state)
        {
            var codecs = GetVideoCodecDescriptor(state);

            var mime = "video/mp4";

            var xml = "<Representation id=\"0\" mimeType=\"" + mime + "\" codecs=\"" + codecs + "\"";

            if (state.OutputWidth.HasValue)
            {
                xml += " width=\"" + state.OutputWidth.Value.ToString(UsCulture) + "\"";
            }
            if (state.OutputHeight.HasValue)
            {
                xml += " height=\"" + state.OutputHeight.Value.ToString(UsCulture) + "\"";
            }
            if (state.OutputVideoBitrate.HasValue)
            {
                xml += " bandwidth=\"" + state.OutputVideoBitrate.Value.ToString(UsCulture) + "\"";
            }

            xml += ">";

            return xml;
        }

        private string GetAudioRepresentationOpenElement(StreamState state)
        {
            var codecs = GetAudioCodecDescriptor(state);

            var mime = "audio/mp4";

            var xml = "<Representation id=\"1\" mimeType=\"" + mime + "\" codecs=\"" + codecs + "\"";

            if (state.OutputAudioSampleRate.HasValue)
            {
                xml += " audioSamplingRate=\"" + state.OutputAudioSampleRate.Value.ToString(UsCulture) + "\"";
            }
            if (state.OutputAudioBitrate.HasValue)
            {
                xml += " bandwidth=\"" + state.OutputAudioBitrate.Value.ToString(UsCulture) + "\"";
            }

            xml += ">";

            return xml;
        }

        private string GetVideoCodecDescriptor(StreamState state)
        {
            // https://developer.apple.com/library/ios/documentation/networkinginternet/conceptual/streamingmediaguide/FrequentlyAskedQuestions/FrequentlyAskedQuestions.html
            // http://www.chipwreck.de/blog/2010/02/25/html-5-video-tag-and-attributes/

            var level = state.TargetVideoLevel ?? 0;
            var profile = state.TargetVideoProfile ?? string.Empty;

            if (profile.IndexOf("high", StringComparison.OrdinalIgnoreCase) != -1)
            {
                if (level >= 4.1)
                {
                    return "avc1.640028";
                }

                if (level >= 4)
                {
                    return "avc1.640028";
                }

                return "avc1.64001f";
            }

            if (profile.IndexOf("main", StringComparison.OrdinalIgnoreCase) != -1)
            {
                if (level >= 4)
                {
                    return "avc1.4d0028";
                }

                if (level >= 3.1)
                {
                    return "avc1.4d001f";
                }

                return "avc1.4d001e";
            }

            if (level >= 3.1)
            {
                return "avc1.42001f";
            }

            return "avc1.42E01E";
        }

        private string GetAudioCodecDescriptor(StreamState state)
        {
            // https://developer.apple.com/library/ios/documentation/networkinginternet/conceptual/streamingmediaguide/FrequentlyAskedQuestions/FrequentlyAskedQuestions.html

            if (string.Equals(state.OutputAudioCodec, "mp3", StringComparison.OrdinalIgnoreCase))
            {
                return "mp4a.40.34";
            }

            // AAC 5ch
            if (state.OutputAudioChannels.HasValue && state.OutputAudioChannels.Value >= 5)
            {
                return "mp4a.40.5";
            }

            // AAC 2ch
            return "mp4a.40.2";
        }

        private void AppendSegmentList(StreamState state, StringBuilder builder, string type, string playlistUrl)
        {
            var extension = ".m4s";

            var seconds = TimeSpan.FromTicks(state.RunTimeTicks ?? 0).TotalSeconds;

            var queryStringIndex = playlistUrl.IndexOf('?');
            var queryString = queryStringIndex == -1 ? string.Empty : playlistUrl.Substring(queryStringIndex);

            var index = 0;
            var duration = 1000000 * state.SegmentLength;
            builder.AppendFormat("<SegmentList timescale=\"1000000\" duration=\"{0}\" startNumber=\"1\">", duration.ToString(CultureInfo.InvariantCulture));

            while (seconds > 0)
            {
                var segmentUrl = string.Format("dash/{3}/{0}{1}{2}",
                    index.ToString(UsCulture),
                    extension,
                    SecurityElement.Escape(queryString),
                    type);

                if (index == 0)
                {
                    builder.AppendFormat("<Initialization sourceURL=\"{0}\"/>", segmentUrl);
                }
                else
                {
                    builder.AppendFormat("<SegmentURL media=\"{0}\"/>", segmentUrl);
                }

                seconds -= state.SegmentLength;
                index++;
            }
            builder.Append("</SegmentList>");
        }
    }
}
