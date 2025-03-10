using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATL;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.MediaInfo
{
    /// <summary>
    /// Probes audio files for metadata.
    /// </summary>
    public class AudioFileProber
    {
        private const char InternalValueSeparator = '\u001F';

        private readonly IMediaEncoder _mediaEncoder;
        private readonly IItemRepository _itemRepo;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<AudioFileProber> _logger;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly LyricResolver _lyricResolver;
        private readonly ILyricManager _lyricManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioFileProber"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
        /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
        /// <param name="itemRepo">Instance of the <see cref="IItemRepository"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="lyricResolver">Instance of the <see cref="LyricResolver"/> interface.</param>
        /// <param name="lyricManager">Instance of the <see cref="ILyricManager"/> interface.</param>
        public AudioFileProber(
            ILogger<AudioFileProber> logger,
            IMediaSourceManager mediaSourceManager,
            IMediaEncoder mediaEncoder,
            IItemRepository itemRepo,
            ILibraryManager libraryManager,
            LyricResolver lyricResolver,
            ILyricManager lyricManager)
        {
            _mediaEncoder = mediaEncoder;
            _itemRepo = itemRepo;
            _libraryManager = libraryManager;
            _logger = logger;
            _mediaSourceManager = mediaSourceManager;
            _lyricResolver = lyricResolver;
            _lyricManager = lyricManager;
            ATL.Settings.DisplayValueSeparator = InternalValueSeparator;
            ATL.Settings.UseFileNameWhenNoTitle = false;
            ATL.Settings.ID3v2_separatev2v3Values = false;
        }

        /// <summary>
        /// Probes the specified item for metadata.
        /// </summary>
        /// <param name="item">The item to probe.</param>
        /// <param name="options">The <see cref="MetadataRefreshOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <typeparam name="T">The type of item to resolve.</typeparam>
        /// <returns>A <see cref="Task"/> probing the item for metadata.</returns>
        public async Task<ItemUpdateType> Probe<T>(
            T item,
            MetadataRefreshOptions options,
            CancellationToken cancellationToken)
            where T : Audio
        {
            var path = item.Path;
            var protocol = item.PathProtocol ?? MediaProtocol.File;

            if (!item.IsShortcut || options.EnableRemoteContentProbe)
            {
                if (item.IsShortcut)
                {
                    path = item.ShortcutPath;
                    protocol = _mediaSourceManager.GetPathProtocol(path);
                }

                var result = await _mediaEncoder.GetMediaInfo(
                    new MediaInfoRequest
                    {
                        MediaType = DlnaProfileType.Audio,
                        MediaSource = new MediaSourceInfo
                        {
                            Path = path,
                            Protocol = protocol
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                await FetchAsync(item, result, options, cancellationToken).ConfigureAwait(false);
            }

            return ItemUpdateType.MetadataImport;
        }

        /// <summary>
        /// Fetches the specified audio.
        /// </summary>
        /// <param name="audio">The <see cref="Audio"/>.</param>
        /// <param name="mediaInfo">The <see cref="Model.MediaInfo.MediaInfo"/>.</param>
        /// <param name="options">The <see cref="MetadataRefreshOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task FetchAsync(
            Audio audio,
            Model.MediaInfo.MediaInfo mediaInfo,
            MetadataRefreshOptions options,
            CancellationToken cancellationToken)
        {
            audio.Container = mediaInfo.Container;
            audio.TotalBitrate = mediaInfo.Bitrate;

            audio.RunTimeTicks = mediaInfo.RunTimeTicks;
            audio.Size = mediaInfo.Size;

            // Add external lyrics first to prevent the lrc file get overwritten on first scan
            var mediaStreams = new List<MediaStream>(mediaInfo.MediaStreams);
            AddExternalLyrics(audio, mediaStreams, options);
            var tryExtractEmbeddedLyrics = mediaStreams.All(s => s.Type != MediaStreamType.Lyric);

            if (!audio.IsLocked)
            {
                await FetchDataFromTags(audio, mediaInfo, options, tryExtractEmbeddedLyrics).ConfigureAwait(false);
                if (tryExtractEmbeddedLyrics)
                {
                    AddExternalLyrics(audio, mediaStreams, options);
                }
            }

            audio.HasLyrics = mediaStreams.Any(s => s.Type == MediaStreamType.Lyric);

            _itemRepo.SaveMediaStreams(audio.Id, mediaStreams, cancellationToken);
        }

        /// <summary>
        /// Fetches data from the tags.
        /// </summary>
        /// <param name="audio">The <see cref="Audio"/>.</param>
        /// <param name="mediaInfo">The <see cref="Model.MediaInfo.MediaInfo"/>.</param>
        /// <param name="options">The <see cref="MetadataRefreshOptions"/>.</param>
        /// <param name="tryExtractEmbeddedLyrics">Whether to extract embedded lyrics to lrc file. </param>
        private async Task FetchDataFromTags(Audio audio, Model.MediaInfo.MediaInfo mediaInfo, MetadataRefreshOptions options, bool tryExtractEmbeddedLyrics)
        {
            var libraryOptions = _libraryManager.GetLibraryOptions(audio);
            Track track = new Track(audio.Path);

            if (track.MetadataFormats
                .All(mf => string.Equals(mf.ShortName, "ID3v1", StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("File {File} only has ID3v1 tags, some fields may be truncated", audio.Path);
            }

            // We should never use the property setter of the ATL.Track class.
            // That setter is meant for its own tag parser and external editor usage and will have unwanted side effects
            // For example, setting the Year property will also set the Date property, which is not what we want here.
            // To properly handle fallback values, we make a clone of those fields when valid.
            var trackTitle = string.IsNullOrEmpty(track.Title) ? mediaInfo.Name : track.Title;
            var trackAlbum = string.IsNullOrEmpty(track.Album) ? mediaInfo.Album : track.Album;
            var trackYear = track.Year is null or 0 ? mediaInfo.ProductionYear : track.Year;
            var trackTrackNumber = track.TrackNumber is null or 0 ? mediaInfo.IndexNumber : track.TrackNumber;
            var trackDiscNumber = track.DiscNumber is null or 0 ? mediaInfo.ParentIndexNumber : track.DiscNumber;

            if (audio.SupportsPeople && !audio.LockedFields.Contains(MetadataField.Cast))
            {
                var people = new List<PersonInfo>();
                var albumArtists = string.IsNullOrEmpty(track.AlbumArtist) ? [] : track.AlbumArtist.Split(InternalValueSeparator);

                if (libraryOptions.UseCustomTagDelimiters)
                {
                    albumArtists = albumArtists.SelectMany(a => SplitWithCustomDelimiter(a, libraryOptions.GetCustomTagDelimiters(), libraryOptions.DelimiterWhitelist)).ToArray();
                }

                foreach (var albumArtist in albumArtists)
                {
                    if (!string.IsNullOrEmpty(albumArtist))
                    {
                        PeopleHelper.AddPerson(people, new PersonInfo
                        {
                            Name = albumArtist,
                            Type = PersonKind.AlbumArtist
                        });
                    }
                }

                string[]? performers = null;
                if (libraryOptions.PreferNonstandardArtistsTag)
                {
                    track.AdditionalFields.TryGetValue("ARTISTS", out var artistsTagString);
                    if (artistsTagString is not null)
                    {
                        performers = artistsTagString.Split(InternalValueSeparator);
                    }
                }

                if (performers is null || performers.Length == 0)
                {
                    performers = string.IsNullOrEmpty(track.Artist) ? [] : track.Artist.Split(InternalValueSeparator);
                }

                if (libraryOptions.UseCustomTagDelimiters)
                {
                    performers = performers.SelectMany(p => SplitWithCustomDelimiter(p, libraryOptions.GetCustomTagDelimiters(), libraryOptions.DelimiterWhitelist)).ToArray();
                }

                foreach (var performer in performers)
                {
                    if (!string.IsNullOrEmpty(performer))
                    {
                        PeopleHelper.AddPerson(people, new PersonInfo
                        {
                            Name = performer,
                            Type = PersonKind.Artist
                        });
                    }
                }

                foreach (var composer in track.Composer.Split(InternalValueSeparator))
                {
                    if (!string.IsNullOrEmpty(composer))
                    {
                        PeopleHelper.AddPerson(people, new PersonInfo
                        {
                            Name = composer,
                            Type = PersonKind.Composer
                        });
                    }
                }

                _libraryManager.UpdatePeople(audio, people);

                if (options.ReplaceAllMetadata && performers.Length != 0)
                {
                    audio.Artists = performers;
                }
                else if (!options.ReplaceAllMetadata
                         && (audio.Artists is null || audio.Artists.Count == 0))
                {
                    audio.Artists = performers;
                }

                if (albumArtists.Length == 0)
                {
                    // Album artists not provided, fall back to performers (artists).
                    albumArtists = performers;
                }

                if (options.ReplaceAllMetadata && albumArtists.Length != 0)
                {
                    audio.AlbumArtists = albumArtists;
                }
                else if (!options.ReplaceAllMetadata
                         && (audio.AlbumArtists is null || audio.AlbumArtists.Count == 0))
                {
                    audio.AlbumArtists = albumArtists;
                }
            }

            if (!audio.LockedFields.Contains(MetadataField.Name) && !string.IsNullOrEmpty(trackTitle))
            {
                audio.Name = trackTitle;
            }

            if (options.ReplaceAllMetadata)
            {
                audio.Album = trackAlbum;
                audio.IndexNumber = trackTrackNumber;
                audio.ParentIndexNumber = trackDiscNumber;
            }
            else
            {
                audio.Album ??= trackAlbum;
                audio.IndexNumber ??= trackTrackNumber;
                audio.ParentIndexNumber ??= trackDiscNumber;
            }

            if (track.Date.HasValue)
            {
                audio.PremiereDate = track.Date;
            }

            if (trackYear.HasValue)
            {
                var year = trackYear.Value;
                audio.ProductionYear = year;

                // ATL library handles such fallback this with its own internal logic, but we also need to handle it here for the ffprobe fallbacks.
                if (!audio.PremiereDate.HasValue)
                {
                    try
                    {
                        audio.PremiereDate = new DateTime(year, 01, 01);
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        _logger.LogError(ex, "Error parsing YEAR tag in {File}. '{TagValue}' is an invalid year", audio.Path, trackYear);
                    }
                }
            }

            if (!audio.LockedFields.Contains(MetadataField.Genres))
            {
                var genres = string.IsNullOrEmpty(track.Genre) ? [] : track.Genre.Split(InternalValueSeparator).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

                if (libraryOptions.UseCustomTagDelimiters)
                {
                    genres = genres.SelectMany(g => SplitWithCustomDelimiter(g, libraryOptions.GetCustomTagDelimiters(), libraryOptions.DelimiterWhitelist)).ToArray();
                }

                audio.Genres = options.ReplaceAllMetadata || audio.Genres is null || audio.Genres.Length == 0
                    ? genres
                    : audio.Genres;
            }

            track.AdditionalFields.TryGetValue("REPLAYGAIN_TRACK_GAIN", out var trackGainTag);

            if (trackGainTag is not null)
            {
                if (trackGainTag.EndsWith("db", StringComparison.OrdinalIgnoreCase))
                {
                    trackGainTag = trackGainTag[..^2].Trim();
                }

                if (float.TryParse(trackGainTag, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    audio.NormalizationGain = value;
                }
            }

            if (options.ReplaceAllMetadata || !audio.TryGetProviderId(MetadataProvider.MusicBrainzArtist, out _))
            {
                if ((track.AdditionalFields.TryGetValue("MUSICBRAINZ_ARTISTID", out var musicBrainzArtistTag)
                     || track.AdditionalFields.TryGetValue("MusicBrainz Artist Id", out musicBrainzArtistTag))
                    && !string.IsNullOrEmpty(musicBrainzArtistTag))
                {
                    var id = GetFirstMusicBrainzId(musicBrainzArtistTag, libraryOptions.UseCustomTagDelimiters, libraryOptions.GetCustomTagDelimiters(), libraryOptions.DelimiterWhitelist);
                    audio.TrySetProviderId(MetadataProvider.MusicBrainzArtist, id);
                }
            }

            if (options.ReplaceAllMetadata || !audio.TryGetProviderId(MetadataProvider.MusicBrainzAlbumArtist, out _))
            {
                if ((track.AdditionalFields.TryGetValue("MUSICBRAINZ_ALBUMARTISTID", out var musicBrainzReleaseArtistIdTag)
                     || track.AdditionalFields.TryGetValue("MusicBrainz Album Artist Id", out musicBrainzReleaseArtistIdTag))
                    && !string.IsNullOrEmpty(musicBrainzReleaseArtistIdTag))
                {
                    var id = GetFirstMusicBrainzId(musicBrainzReleaseArtistIdTag, libraryOptions.UseCustomTagDelimiters, libraryOptions.GetCustomTagDelimiters(), libraryOptions.DelimiterWhitelist);
                    audio.TrySetProviderId(MetadataProvider.MusicBrainzAlbumArtist, id);
                }
            }

            if (options.ReplaceAllMetadata || !audio.TryGetProviderId(MetadataProvider.MusicBrainzAlbum, out _))
            {
                if ((track.AdditionalFields.TryGetValue("MUSICBRAINZ_ALBUMID", out var musicBrainzReleaseIdTag)
                     || track.AdditionalFields.TryGetValue("MusicBrainz Album Id", out musicBrainzReleaseIdTag))
                    && !string.IsNullOrEmpty(musicBrainzReleaseIdTag))
                {
                    var id = GetFirstMusicBrainzId(musicBrainzReleaseIdTag, libraryOptions.UseCustomTagDelimiters, libraryOptions.GetCustomTagDelimiters(), libraryOptions.DelimiterWhitelist);
                    audio.TrySetProviderId(MetadataProvider.MusicBrainzAlbum, id);
                }
            }

            if (options.ReplaceAllMetadata || !audio.TryGetProviderId(MetadataProvider.MusicBrainzReleaseGroup, out _))
            {
                if ((track.AdditionalFields.TryGetValue("MUSICBRAINZ_RELEASEGROUPID", out var musicBrainzReleaseGroupIdTag)
                     || track.AdditionalFields.TryGetValue("MusicBrainz Release Group Id", out musicBrainzReleaseGroupIdTag))
                    && !string.IsNullOrEmpty(musicBrainzReleaseGroupIdTag))
                {
                    var id = GetFirstMusicBrainzId(musicBrainzReleaseGroupIdTag, libraryOptions.UseCustomTagDelimiters, libraryOptions.GetCustomTagDelimiters(), libraryOptions.DelimiterWhitelist);
                    audio.TrySetProviderId(MetadataProvider.MusicBrainzReleaseGroup, id);
                }
            }

            if (options.ReplaceAllMetadata || !audio.TryGetProviderId(MetadataProvider.MusicBrainzTrack, out _))
            {
                if ((track.AdditionalFields.TryGetValue("MUSICBRAINZ_RELEASETRACKID", out var trackMbId)
                     || track.AdditionalFields.TryGetValue("MusicBrainz Release Track Id", out trackMbId))
                    && !string.IsNullOrEmpty(trackMbId))
                {
                    var id = GetFirstMusicBrainzId(trackMbId, libraryOptions.UseCustomTagDelimiters, libraryOptions.GetCustomTagDelimiters(), libraryOptions.DelimiterWhitelist);
                    audio.TrySetProviderId(MetadataProvider.MusicBrainzTrack, id);
                }
            }

            // Save extracted lyrics if they exist,
            // and if the audio doesn't yet have lyrics.
            var lyrics = track.Lyrics.SynchronizedLyrics.Count > 0 ? track.Lyrics.FormatSynchToLRC() : track.Lyrics.UnsynchronizedLyrics;
            if (!string.IsNullOrWhiteSpace(lyrics)
                && tryExtractEmbeddedLyrics)
            {
                await _lyricManager.SaveLyricAsync(audio, "lrc", lyrics).ConfigureAwait(false);
            }
        }

        private void AddExternalLyrics(
            Audio audio,
            List<MediaStream> currentStreams,
            MetadataRefreshOptions options)
        {
            var startIndex = currentStreams.Count == 0 ? 0 : (currentStreams.Select(i => i.Index).Max() + 1);
            var externalLyricFiles = _lyricResolver.GetExternalStreams(audio, startIndex, options.DirectoryService, false);

            audio.LyricFiles = externalLyricFiles.Select(i => i.Path).Distinct().ToArray();
            if (externalLyricFiles.Count > 0)
            {
                currentStreams.Add(externalLyricFiles[0]);
            }
        }

        private List<string> SplitWithCustomDelimiter(string val, char[] tagDelimiters, string[] whitelist)
        {
            var items = new List<string>();
            var temp = val;
            foreach (var whitelistItem in whitelist)
            {
                if (string.IsNullOrWhiteSpace(whitelistItem))
                {
                    continue;
                }

                var originalTemp = temp;
                temp = temp.Replace(whitelistItem, string.Empty, StringComparison.OrdinalIgnoreCase);

                if (!string.Equals(temp, originalTemp, StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(whitelistItem);
                }
            }

            var items2 = temp.Split(tagDelimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).DistinctNames();
            items.AddRange(items2);

            return items;
        }

        // MusicBrainz IDs are multi-value tags, so we need to split them
        // However, our current provider can only have one single ID, which means we need to pick the first one
        private string? GetFirstMusicBrainzId(string tag, bool useCustomTagDelimiters, char[] tagDelimiters, string[] whitelist)
        {
            var val = tag.Split(InternalValueSeparator).FirstOrDefault();
            if (val is not null && useCustomTagDelimiters)
            {
                val = SplitWithCustomDelimiter(val, tagDelimiters, whitelist).FirstOrDefault();
            }

            return val;
        }
    }
}
