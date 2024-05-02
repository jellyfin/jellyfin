using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
using MediaBrowser.Model.MediaInfo;
using TagLib;

namespace MediaBrowser.Providers.MediaInfo
{
    /// <summary>
    /// Probes audio files for metadata.
    /// </summary>
    public class AudioFileProber
    {
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IItemRepository _itemRepo;
        private readonly ILibraryManager _libraryManager;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly LyricResolver _lyricResolver;
        private readonly ILyricManager _lyricManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioFileProber"/> class.
        /// </summary>
        /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
        /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
        /// <param name="itemRepo">Instance of the <see cref="IItemRepository"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="lyricResolver">Instance of the <see cref="LyricResolver"/> interface.</param>
        /// <param name="lyricManager">Instance of the <see cref="ILyricManager"/> interface.</param>
        public AudioFileProber(
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
            _mediaSourceManager = mediaSourceManager;
            _lyricResolver = lyricResolver;
            _lyricManager = lyricManager;
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
            audio.PremiereDate = mediaInfo.PremiereDate;

            // Add external lyrics first to prevent the lrc file get overwritten on first scan
            var mediaStreams = new List<MediaStream>(mediaInfo.MediaStreams);
            AddExternalLyrics(audio, mediaStreams, options);
            var tryExtractEmbeddedLyrics = mediaStreams.All(s => s.Type != MediaStreamType.Lyric);

            if (!audio.IsLocked)
            {
                await FetchDataFromTags(audio, mediaInfo, options, tryExtractEmbeddedLyrics).ConfigureAwait(false);
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
            using var file = TagLib.File.Create(audio.Path);
            var tagTypes = file.TagTypesOnDisk;
            Tag? tags = null;

            if (tagTypes.HasFlag(TagTypes.Id3v2))
            {
                tags = file.GetTag(TagTypes.Id3v2);
            }
            else if (tagTypes.HasFlag(TagTypes.Ape))
            {
                tags = file.GetTag(TagTypes.Ape);
            }
            else if (tagTypes.HasFlag(TagTypes.FlacMetadata))
            {
                tags = file.GetTag(TagTypes.FlacMetadata);
            }
            else if (tagTypes.HasFlag(TagTypes.Apple))
            {
                tags = file.GetTag(TagTypes.Apple);
            }
            else if (tagTypes.HasFlag(TagTypes.Xiph))
            {
                tags = file.GetTag(TagTypes.Xiph);
            }
            else if (tagTypes.HasFlag(TagTypes.AudibleMetadata))
            {
                tags = file.GetTag(TagTypes.AudibleMetadata);
            }
            else if (tagTypes.HasFlag(TagTypes.Id3v1))
            {
                tags = file.GetTag(TagTypes.Id3v1);
            }

            if (tags is not null)
            {
                if (audio.SupportsPeople && !audio.LockedFields.Contains(MetadataField.Cast))
                {
                    var people = new List<PersonInfo>();
                    var albumArtists = tags.AlbumArtists;
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

                    var performers = tags.Performers;
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

                    foreach (var composer in tags.Composers)
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

                if (!audio.LockedFields.Contains(MetadataField.Name) && !string.IsNullOrEmpty(tags.Title))
                {
                    audio.Name = tags.Title;
                }

                if (options.ReplaceAllMetadata)
                {
                    audio.Album = tags.Album;
                    audio.IndexNumber = Convert.ToInt32(tags.Track);
                    audio.ParentIndexNumber = Convert.ToInt32(tags.Disc);
                }
                else
                {
                    audio.Album ??= tags.Album;
                    audio.IndexNumber ??= Convert.ToInt32(tags.Track);
                    audio.ParentIndexNumber ??= Convert.ToInt32(tags.Disc);
                }

                if (tags.Year != 0)
                {
                    var year = Convert.ToInt32(tags.Year);
                    audio.ProductionYear = year;

                    if (!audio.PremiereDate.HasValue)
                    {
                        audio.PremiereDate = new DateTime(year, 01, 01);
                    }
                }

                if (!audio.LockedFields.Contains(MetadataField.Genres))
                {
                    audio.Genres = options.ReplaceAllMetadata || audio.Genres == null || audio.Genres.Length == 0
                        ? tags.Genres.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                        : audio.Genres;
                }

                if (!double.IsNaN(tags.ReplayGainTrackGain))
                {
                    audio.NormalizationGain = (float)tags.ReplayGainTrackGain;
                }

                if (options.ReplaceAllMetadata || !audio.TryGetProviderId(MetadataProvider.MusicBrainzArtist, out _))
                {
                    audio.SetProviderId(MetadataProvider.MusicBrainzArtist, tags.MusicBrainzArtistId);
                }

                if (options.ReplaceAllMetadata || !audio.TryGetProviderId(MetadataProvider.MusicBrainzAlbumArtist, out _))
                {
                    audio.SetProviderId(MetadataProvider.MusicBrainzAlbumArtist, tags.MusicBrainzReleaseArtistId);
                }

                if (options.ReplaceAllMetadata || !audio.TryGetProviderId(MetadataProvider.MusicBrainzAlbum, out _))
                {
                    audio.SetProviderId(MetadataProvider.MusicBrainzAlbum, tags.MusicBrainzReleaseId);
                }

                if (options.ReplaceAllMetadata || !audio.TryGetProviderId(MetadataProvider.MusicBrainzReleaseGroup, out _))
                {
                    audio.SetProviderId(MetadataProvider.MusicBrainzReleaseGroup, tags.MusicBrainzReleaseGroupId);
                }

                if (options.ReplaceAllMetadata || !audio.TryGetProviderId(MetadataProvider.MusicBrainzTrack, out _))
                {
                    // Fallback to ffprobe as TagLib incorrectly provides recording MBID in `tags.MusicBrainzTrackId`.
                    // See https://github.com/mono/taglib-sharp/issues/304
                    var trackMbId = mediaInfo.GetProviderId(MetadataProvider.MusicBrainzTrack);
                    if (trackMbId is not null)
                    {
                        audio.SetProviderId(MetadataProvider.MusicBrainzTrack, trackMbId);
                    }
                }

                // Save extracted lyrics if they exist,
                // and if the audio doesn't yet have lyrics.
                if (!string.IsNullOrWhiteSpace(tags.Lyrics)
                    && tryExtractEmbeddedLyrics)
                {
                    await _lyricManager.SaveLyricAsync(audio, "lrc", tags.Lyrics).ConfigureAwait(false);
                }
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
            currentStreams.AddRange(externalLyricFiles);
        }
    }
}
