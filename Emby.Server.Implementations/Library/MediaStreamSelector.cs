#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Model.Entities;

namespace Emby.Server.Implementations.Library
{
    public static class MediaStreamSelector
    {
        public static int? GetDefaultAudioStreamIndex(IReadOnlyList<MediaStream> streams, IReadOnlyList<string> preferredLanguages, bool preferDefaultTrack)
        {
            var sortedStreams = GetSortedStreams(streams, MediaStreamType.Audio, preferredLanguages).ToList();

            if (preferDefaultTrack)
            {
                var defaultStream = sortedStreams.FirstOrDefault(i => i.IsDefault);

                if (defaultStream is not null)
                {
                    return defaultStream.Index;
                }
            }

            return sortedStreams.FirstOrDefault()?.Index;
        }

        public static int? GetDefaultSubtitleStreamIndex(
            IEnumerable<MediaStream> streams,
            IReadOnlyList<string> preferredLanguages,
            SubtitlePlaybackMode mode,
            string audioTrackLanguage)
        {
            if (mode == SubtitlePlaybackMode.None)
            {
                return null;
            }

            // Sort in the following order: Default > No tag > Forced
            var sortedStreams = streams
                .Where(i => i.Type == MediaStreamType.Subtitle)
                .OrderByDescending(x => x.IsExternal)
                .ThenByDescending(x => x.IsDefault)
                .ThenByDescending(x => !x.IsForced && MatchesPreferredLanguage(x.Language, preferredLanguages) && !x.Title.Contains("latin", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(x => x.IsForced && MatchesPreferredLanguage(x.Language, preferredLanguages) && !x.Title.Contains("latin", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(x => x.IsForced && IsLanguageUndefined(x.Language))
                .ThenByDescending(x => x.IsForced)
                .ToList();

            MediaStream? stream = null;

            if (mode == SubtitlePlaybackMode.Default)
            {
                // Load subtitles according to external, default and forced flags.
                stream = sortedStreams.FirstOrDefault(x => x.IsExternal || x.IsDefault || x.IsForced);
            }
            else if (mode == SubtitlePlaybackMode.Smart)
            {
                // Only attempt to load subtitles if the audio language is not one of the user's preferred subtitle languages.
                // If no subtitles of preferred language available, use none.
                // If the audio language is one of the user's preferred subtitle languages behave like OnlyForced.
                if (!preferredLanguages.Contains(audioTrackLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    stream = sortedStreams.FirstOrDefault(x => MatchesPreferredLanguage(x.Language, preferredLanguages));
                }
                else
                {
                    stream = BehaviorOnlyForced(sortedStreams, preferredLanguages).FirstOrDefault();
                }
            }
            else if (mode == SubtitlePlaybackMode.Always)
            {
                // Always load (full/non-forced) subtitles of the user's preferred subtitle language if possible, otherwise OnlyForced behaviour.
                stream = sortedStreams.FirstOrDefault(x => !x.IsForced && MatchesPreferredLanguage(x.Language, preferredLanguages)) ??
                    BehaviorOnlyForced(sortedStreams, preferredLanguages).FirstOrDefault();
            }
            else if (mode == SubtitlePlaybackMode.OnlyForced)
            {
                // Load subtitles that are flagged forced of the user's preferred subtitle language or with an undefined language
                stream = BehaviorOnlyForced(sortedStreams, preferredLanguages).FirstOrDefault();
            }

            return stream?.Index;
        }

        private static IEnumerable<MediaStream> GetSortedStreams(IEnumerable<MediaStream> streams, MediaStreamType type, IReadOnlyList<string> languagePreferences)
        {
            // Give some preference to external text subs for better performance
            return streams
                .Where(i => i.Type == type)
                .OrderByDescending(i => GetStreamScore(i, languagePreferences));
        }

        public static void SetSubtitleStreamScores(
            IReadOnlyList<MediaStream> streams,
            IReadOnlyList<string> preferredLanguages,
            SubtitlePlaybackMode mode,
            string audioTrackLanguage)
        {
            if (mode == SubtitlePlaybackMode.None)
            {
                return;
            }

            var sortedStreams = GetSortedStreams(streams, MediaStreamType.Subtitle, preferredLanguages).ToList();

            List<MediaStream>? filteredStreams = null;

            if (mode == SubtitlePlaybackMode.Default)
            {
                // Prefer embedded metadata over smart logic
                // Load subtitles according to external, default, and forced flags.
                filteredStreams = sortedStreams.Where(s => s.IsExternal || s.IsDefault || s.IsForced)
                    .ToList();
            }
            else if (mode == SubtitlePlaybackMode.Smart)
            {
                // Prefer smart logic over embedded metadata
                // Only attempt to load subtitles if the audio language is not one of the user's preferred subtitle languages, otherwise OnlyForced behavior.
                if (!preferredLanguages.Contains(audioTrackLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    filteredStreams = sortedStreams.Where(s => MatchesPreferredLanguage(s.Language, preferredLanguages))
                        .ToList();
                }
                else
                {
                    filteredStreams = BehaviorOnlyForced(sortedStreams, preferredLanguages);
                }
            }
            else if (mode == SubtitlePlaybackMode.Always)
            {
                // Always load (full/non-forced) subtitles of the user's preferred subtitle language if possible, otherwise OnlyForced behavior.
                filteredStreams = sortedStreams.Where(s => !s.IsForced && MatchesPreferredLanguage(s.Language, preferredLanguages))
                    .ToList() ?? BehaviorOnlyForced(sortedStreams, preferredLanguages);
            }
            else if (mode == SubtitlePlaybackMode.OnlyForced)
            {
                // Load subtitles that are flagged forced of the user's preferred subtitle language or with an undefined language
                filteredStreams = BehaviorOnlyForced(sortedStreams, preferredLanguages);
            }

            // If filteredStreams is null, initialize it as an empty list to avoid null reference errors
            filteredStreams ??= new List<MediaStream>();

            foreach (var stream in filteredStreams)
            {
                stream.Score = GetStreamScore(stream, preferredLanguages);
            }
        }

        private static bool MatchesPreferredLanguage(string language, IReadOnlyList<string> preferredLanguages)
        {
            // If preferredLanguages is empty, treat it as "any language" (wildcard)
            return preferredLanguages.Count == 0 ||
                preferredLanguages.Contains(language, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLanguageUndefined(string language)
        {
            // Check for null, empty, or known placeholders
            return string.IsNullOrEmpty(language) ||
                language.Equals("und", StringComparison.OrdinalIgnoreCase) ||
                language.Equals("unknown", StringComparison.OrdinalIgnoreCase) ||
                language.Equals("undetermined", StringComparison.OrdinalIgnoreCase) ||
                language.Equals("mul", StringComparison.OrdinalIgnoreCase) ||
                language.Equals("zxx", StringComparison.OrdinalIgnoreCase);
        }

        private static List<MediaStream> BehaviorOnlyForced(IEnumerable<MediaStream> sortedStreams, IReadOnlyList<string> preferredLanguages)
        {
            return sortedStreams
                .Where(s => s.IsForced && (MatchesPreferredLanguage(s.Language, preferredLanguages) || IsLanguageUndefined(s.Language)))
                .OrderByDescending(s => MatchesPreferredLanguage(s.Language, preferredLanguages))
                .ThenByDescending(s => IsLanguageUndefined(s.Language))
                .ToList();
        }

        internal static int GetStreamScore(MediaStream stream, IReadOnlyList<string> languagePreferences)
        {
            var index = languagePreferences.FindIndex(x => string.Equals(x, stream.Language, StringComparison.OrdinalIgnoreCase));
            var score = index == -1 ? 1 : 101 - index;
            score = (score * 10) + (stream.IsForced ? 2 : 1);
            score = (score * 10) + (stream.IsDefault ? 2 : 1);
            score = (score * 10) + (stream.SupportsExternalStream ? 2 : 1);
            score = (score * 10) + (stream.IsTextSubtitleStream ? 2 : 1);
            score = (score * 10) + (stream.IsExternal ? 2 : 1);
            return score;
        }
    }
}
