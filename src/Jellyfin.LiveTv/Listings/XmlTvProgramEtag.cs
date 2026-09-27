#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MediaBrowser.Controller.LiveTv;

namespace Jellyfin.LiveTv.Listings
{
    internal static class XmlTvProgramEtag
    {
        internal const string Prefix = "xmltv-sha256-v1:";

        internal static bool IsXmlTvEtag(string? etag)
            => !string.IsNullOrWhiteSpace(etag)
                && etag.StartsWith(Prefix, StringComparison.Ordinal);

        // Returns true only when the incoming etag is XMLTV-style AND equals the stored value.
        // The IsXmlTvEtag gate keeps other providers (e.g. Schedules Direct) on the
        // field-by-field update path even if their etag strings happen to match.
        internal static bool MatchesStored(string? incomingEtag, string? storedEtag)
            => IsXmlTvEtag(incomingEtag)
                && string.Equals(incomingEtag, storedEtag, StringComparison.OrdinalIgnoreCase);

        internal static bool TryCreate(ProgramInfo programInfo, out string? etag, out string? reason)
        {
            etag = null;

            if (string.IsNullOrWhiteSpace(programInfo.Id))
            {
                reason = "program id is empty";
                return false;
            }

            if (string.IsNullOrWhiteSpace(programInfo.ChannelId))
            {
                reason = "channel id is empty";
                return false;
            }

            if (programInfo.StartDate == default)
            {
                reason = "start date is empty";
                return false;
            }

            if (programInfo.EndDate == default)
            {
                reason = "end date is empty";
                return false;
            }

            if (programInfo.EndDate <= programInfo.StartDate)
            {
                reason = "end date is not after start date";
                return false;
            }

            var builder = new StringBuilder(1024);

            // Keep this list aligned with the ProgramInfo fields consumed by GuideManager.
            AppendValue(builder, "schema", "xmltv-programinfo-v1");
            AppendValue(builder, nameof(programInfo.Id), programInfo.Id);
            AppendValue(builder, nameof(programInfo.ChannelId), programInfo.ChannelId);
            AppendValue(builder, nameof(programInfo.Name), programInfo.Name);
            AppendValue(builder, nameof(programInfo.OfficialRating), programInfo.OfficialRating);
            AppendValue(builder, nameof(programInfo.Overview), programInfo.Overview);
            AppendValue(builder, nameof(programInfo.StartDate), programInfo.StartDate);
            AppendValue(builder, nameof(programInfo.EndDate), programInfo.EndDate);
            AppendList(builder, nameof(programInfo.Genres), programInfo.Genres);
            AppendValue(builder, nameof(programInfo.OriginalAirDate), programInfo.OriginalAirDate);
            AppendValue(builder, nameof(programInfo.IsHD), programInfo.IsHD);
            AppendValue(builder, nameof(programInfo.Audio), programInfo.Audio?.ToString());
            AppendValue(builder, nameof(programInfo.CommunityRating), programInfo.CommunityRating);
            AppendValue(builder, nameof(programInfo.IsRepeat), programInfo.IsRepeat);
            AppendValue(builder, nameof(programInfo.EpisodeTitle), programInfo.EpisodeTitle);
            AppendValue(builder, nameof(programInfo.ImagePath), programInfo.ImagePath);
            AppendValue(builder, nameof(programInfo.ImageUrl), programInfo.ImageUrl);
            AppendValue(builder, nameof(programInfo.ThumbImageUrl), programInfo.ThumbImageUrl);
            AppendValue(builder, nameof(programInfo.LogoImageUrl), programInfo.LogoImageUrl);
            AppendValue(builder, nameof(programInfo.BackdropImageUrl), programInfo.BackdropImageUrl);
            AppendValue(builder, nameof(programInfo.IsMovie), programInfo.IsMovie);
            AppendValue(builder, nameof(programInfo.IsSports), programInfo.IsSports);
            AppendValue(builder, nameof(programInfo.IsSeries), programInfo.IsSeries);
            AppendValue(builder, nameof(programInfo.IsLive), programInfo.IsLive);
            AppendValue(builder, nameof(programInfo.IsNews), programInfo.IsNews);
            AppendValue(builder, nameof(programInfo.IsKids), programInfo.IsKids);
            AppendValue(builder, nameof(programInfo.IsPremiere), programInfo.IsPremiere);
            AppendValue(builder, nameof(programInfo.ProductionYear), programInfo.ProductionYear);
            AppendValue(builder, nameof(programInfo.SeriesId), programInfo.SeriesId);
            AppendValue(builder, nameof(programInfo.ShowId), programInfo.ShowId);
            AppendValue(builder, nameof(programInfo.SeasonNumber), programInfo.SeasonNumber);
            AppendValue(builder, nameof(programInfo.EpisodeNumber), programInfo.EpisodeNumber);
            AppendDictionary(builder, nameof(programInfo.ProviderIds), programInfo.ProviderIds);
            AppendDictionary(builder, nameof(programInfo.SeriesProviderIds), programInfo.SeriesProviderIds);

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
            etag = Prefix + Convert.ToHexString(hash);
            reason = null;
            return true;
        }

        private static void AppendValue(StringBuilder builder, string name, string? value)
        {
            builder.Append(name).Append('|');
            if (value is null)
            {
                builder.Append('N').Append("|0|");
            }
            else
            {
                builder.Append('S')
                    .Append('|')
                    .Append(value.Length.ToString(CultureInfo.InvariantCulture))
                    .Append('|')
                    .Append(value);
            }

            builder.Append('\n');
        }

        private static void AppendValue(StringBuilder builder, string name, DateTime value)
            => AppendValue(builder, name, FormatDateTime(value));

        private static void AppendValue(StringBuilder builder, string name, DateTime? value)
            => AppendValue(builder, name, value.HasValue ? FormatDateTime(value.Value) : null);

        private static void AppendValue(StringBuilder builder, string name, bool value)
            => AppendValue(builder, name, value ? "true" : "false");

        private static void AppendValue(StringBuilder builder, string name, bool? value)
            => AppendValue(builder, name, value switch { true => "true", false => "false", null => null });

        private static void AppendValue(StringBuilder builder, string name, int? value)
            => AppendValue(builder, name, value?.ToString(CultureInfo.InvariantCulture));

        private static void AppendValue(StringBuilder builder, string name, float? value)
            => AppendValue(builder, name, value?.ToString("R", CultureInfo.InvariantCulture));

        // Treat Unspecified as UTC so the etag does not vary with the server's local timezone.
        private static string FormatDateTime(DateTime value)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
                _ => value.ToUniversalTime(),
            };

            return utc.ToString("O", CultureInfo.InvariantCulture);
        }

        private static void AppendList(StringBuilder builder, string name, IReadOnlyList<string> values)
        {
            AppendValue(builder, name + ".Count", values.Count.ToString(CultureInfo.InvariantCulture));
            for (var i = 0; i < values.Count; i++)
            {
                AppendValue(builder, $"{name}[{i}]", values[i]);
            }
        }

        private static void AppendDictionary(StringBuilder builder, string name, IReadOnlyDictionary<string, string?> values)
        {
            AppendValue(builder, name + ".Count", values.Count.ToString(CultureInfo.InvariantCulture));
            if (values.Count == 0)
            {
                return;
            }

            var index = 0;
            foreach (var (key, value) in values
                .OrderBy(i => i.Key, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.Key, StringComparer.Ordinal))
            {
                AppendValue(builder, $"{name}[{index}].Key", key);
                AppendValue(builder, $"{name}[{index}].Value", value);
                index++;
            }
        }
    }
}
