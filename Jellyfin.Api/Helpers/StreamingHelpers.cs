using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Api.Models;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Model.Dlna;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// The streaming helpers
    /// </summary>
    public class StreamingHelpers
    {
        /// <summary>
        /// Adds the dlna headers.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <param name="isStaticallyStreamed">if set to <c>true</c> [is statically streamed].</param>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="dlnaManager">Instance of the <see cref="IDlnaManager"/> interface.</param>
        public static void AddDlnaHeaders(
            StreamState state,
            IHeaderDictionary responseHeaders,
            bool isStaticallyStreamed,
            HttpRequest request,
            IDlnaManager dlnaManager)
        {
            if (!state.EnableDlnaHeaders)
            {
                return;
            }

            var profile = state.DeviceProfile;

            StringValues transferMode = request.Headers["transferMode.dlna.org"];
            responseHeaders.Add("transferMode.dlna.org", string.IsNullOrEmpty(transferMode) ? "Streaming" : transferMode.ToString());
            responseHeaders.Add("realTimeInfo.dlna.org", "DLNA.ORG_TLAG=*");

            if (state.RunTimeTicks.HasValue)
            {
                if (string.Equals(request.Headers["getMediaInfo.sec"], "1", StringComparison.OrdinalIgnoreCase))
                {
                    var ms = TimeSpan.FromTicks(state.RunTimeTicks.Value).TotalMilliseconds;
                    responseHeaders.Add("MediaInfo.sec", string.Format(
                        CultureInfo.InvariantCulture,
                        "SEC_Duration={0};",
                        Convert.ToInt32(ms)));
                }

                if (!isStaticallyStreamed && profile != null)
                {
                    AddTimeSeekResponseHeaders(state, responseHeaders);
                }
            }

            if (profile == null)
            {
                profile = dlnaManager.GetDefaultProfile();
            }

            var audioCodec = state.ActualOutputAudioCodec;

            if (state.VideoRequest == null)
            {
                responseHeaders.Add("contentFeatures.dlna.org", new ContentFeatureBuilder(profile).BuildAudioHeader(
                    state.OutputContainer,
                    audioCodec,
                    state.OutputAudioBitrate,
                    state.OutputAudioSampleRate,
                    state.OutputAudioChannels,
                    state.OutputAudioBitDepth,
                    isStaticallyStreamed,
                    state.RunTimeTicks,
                    state.TranscodeSeekInfo));
            }
            else
            {
                var videoCodec = state.ActualOutputVideoCodec;

                responseHeaders.Add("contentFeatures.dlna.org", new ContentFeatureBuilder(profile).BuildVideoHeader(
                    state.OutputContainer,
                    videoCodec,
                    audioCodec,
                    state.OutputWidth,
                    state.OutputHeight,
                    state.TargetVideoBitDepth,
                    state.OutputVideoBitrate,
                    state.TargetTimestamp,
                    isStaticallyStreamed,
                    state.RunTimeTicks,
                    state.TargetVideoProfile,
                    state.TargetVideoLevel,
                    state.TargetFramerate,
                    state.TargetPacketLength,
                    state.TranscodeSeekInfo,
                    state.IsTargetAnamorphic,
                    state.IsTargetInterlaced,
                    state.TargetRefFrames,
                    state.TargetVideoStreamCount,
                    state.TargetAudioStreamCount,
                    state.TargetVideoCodecTag,
                    state.IsTargetAVC).FirstOrDefault() ?? string.Empty);
            }
        }

        /// <summary>
        /// Parses the dlna headers.
        /// </summary>
        /// <param name="startTimeTicks">The start time ticks.</param>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        public void ParseDlnaHeaders(long? startTimeTicks, HttpRequest request)
        {
            if (!startTimeTicks.HasValue)
            {
                var timeSeek = request.Headers["TimeSeekRange.dlna.org"];

                startTimeTicks = ParseTimeSeekHeader(timeSeek);
            }
        }

        /// <summary>
        /// Parses the time seek header.
        /// </summary>
        public long? ParseTimeSeekHeader(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            const string Npt = "npt=";
            if (!value.StartsWith(Npt, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid timeseek header");
            }
            int index = value.IndexOf('-');
            value = index == -1
                ? value.Substring(Npt.Length)
                : value.Substring(Npt.Length, index - Npt.Length);

            if (value.IndexOf(':') == -1)
            {
                // Parses npt times in the format of '417.33'
                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var seconds))
                {
                    return TimeSpan.FromSeconds(seconds).Ticks;
                }

                throw new ArgumentException("Invalid timeseek header");
            }

            // Parses npt times in the format of '10:19:25.7'
            var tokens = value.Split(new[] { ':' }, 3);
            double secondsSum = 0;
            var timeFactor = 3600;

            foreach (var time in tokens)
            {
                if (double.TryParse(time, NumberStyles.Any, CultureInfo.InvariantCulture, out var digit))
                {
                    secondsSum += digit * timeFactor;
                }
                else
                {
                    throw new ArgumentException("Invalid timeseek header");
                }
                timeFactor /= 60;
            }
            return TimeSpan.FromSeconds(secondsSum).Ticks;
        }

        public void AddTimeSeekResponseHeaders(StreamState state, IHeaderDictionary responseHeaders)
        {
            var runtimeSeconds = TimeSpan.FromTicks(state.RunTimeTicks.Value).TotalSeconds.ToString(CultureInfo.InvariantCulture);
            var startSeconds = TimeSpan.FromTicks(state.Request.StartTimeTicks ?? 0).TotalSeconds.ToString(CultureInfo.InvariantCulture);

            responseHeaders.Add("TimeSeekRange.dlna.org", string.Format(
                CultureInfo.InvariantCulture,
                "npt={0}-{1}/{1}",
                startSeconds,
                runtimeSeconds));
            responseHeaders.Add("X-AvailableSeekRange", string.Format(
                CultureInfo.InvariantCulture,
                "1 npt={0}-{1}",
                startSeconds,
                runtimeSeconds));
        }
    }
}
