using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasMediaSources
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        Guid Id { get; }

        /// <summary>
        /// Gets the media sources.
        /// </summary>
        /// <param name="enablePathSubstitution">if set to <c>true</c> [enable path substitution].</param>
        /// <returns>Task{IEnumerable{MediaSourceInfo}}.</returns>
        IEnumerable<MediaSourceInfo> GetMediaSources(bool enablePathSubstitution);
    }

    public static class HasMediaSourceExtensions
    {
        public static IEnumerable<MediaSourceInfo> GetMediaSources(this IHasMediaSources item, bool enablePathSubstitution, User user)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (!(item is Video))
            {
                return item.GetMediaSources(enablePathSubstitution);
            }

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            var sources = item.GetMediaSources(enablePathSubstitution).ToList();

            var preferredAudio = string.IsNullOrEmpty(user.Configuration.AudioLanguagePreference)
            ? new string[] { }
            : new[] { user.Configuration.AudioLanguagePreference };

            var preferredSubs = string.IsNullOrEmpty(user.Configuration.SubtitleLanguagePreference)
                ? new string[] { }
                : new[] { user.Configuration.SubtitleLanguagePreference };

            foreach (var source in sources)
            {
                source.DefaultAudioStreamIndex = MediaStreamSelector.GetDefaultAudioStreamIndex(
                    source.MediaStreams, preferredAudio, user.Configuration.PlayDefaultAudioTrack);

                var defaultAudioIndex = source.DefaultAudioStreamIndex;
                var audioLangage = defaultAudioIndex == null
                    ? null
                    : source.MediaStreams.Where(i => i.Type == MediaStreamType.Audio && i.Index == defaultAudioIndex).Select(i => i.Language).FirstOrDefault();

                source.DefaultSubtitleStreamIndex = MediaStreamSelector.GetDefaultSubtitleStreamIndex(source.MediaStreams,
                    preferredSubs,
                    user.Configuration.SubtitleMode,
                    audioLangage);
            }

            return sources;
        }
    }
}
