define(['events', 'datetime', 'appSettings', 'pluginManager', 'userSettings', 'globalize', 'connectionManager', 'loading', 'serverNotifications'], function (events, datetime, appSettings, pluginManager, userSettings, globalize, connectionManager, loading, serverNotifications) {
    'use strict';

    function playbackManager() {

        var self = this;

        var currentPlayer;
        var lastLocalPlayer;
        var repeatMode = 'RepeatNone';
        var playlist = [];
        var currentPlaylistIndex;
        var currentPlayOptions;
        var playNextAfterEnded = true;
        var playerStates = {};

        self.currentItem = function (player) {
            var data = getPlayerData(player);
            return data.streamInfo ? data.streamInfo.item : null;
        };

        self.currentMediaSource = function (player) {
            var data = getPlayerData(player);
            return data.streamInfo ? data.streamInfo.mediaSource : null;
        };

        function getCurrentSubtitleStream(player) {

            var index = getPlayerData(player).subtitleStreamIndex;

            if (index == null || index === -1) {
                return null;
            }

            return getSubtitleStream(player, index);
        }

        function getSubtitleStream(player, index) {
            return self.currentMediaSource(player).MediaStreams.filter(function (s) {
                return s.Type === 'Subtitle' && s.Index === index;
            })[0];
        }

        self.audioTracks = function (player) {
            var mediaSource = self.currentMediaSource(player);

            var mediaStreams = (mediaSource || {}).MediaStreams || [];
            return mediaStreams.filter(function (s) {
                return s.Type === 'Audio';
            });
        };

        self.subtitleTracks = function (player) {
            var mediaSource = self.currentMediaSource(player);

            var mediaStreams = (mediaSource || {}).MediaStreams || [];
            return mediaStreams.filter(function (s) {
                return s.Type === 'Subtitle';
            });
        };

        self.playlist = function () {
            return playlist.slice(0);
        };

        self.currentPlayer = function () {
            return currentPlayer;
        };

        function setCurrentPlayer(player) {
            currentPlayer = player;
            if (player && player.isLocalPlayer) {
                lastLocalPlayer = player;
            }
        }

        self.isPlaying = function () {
            var player = currentPlayer;
            return player != null && player.currentSrc() != null;
        };

        self.isPlayingVideo = function () {
            if (self.isPlaying()) {
                var playerData = getPlayerData(currentPlayer);

                return playerData.streamInfo.mediaType === 'Video';
            }

            return false;
        };

        self.isPlayingAudio = function () {
            if (self.isPlaying()) {
                var playerData = getPlayerData(currentPlayer);

                return playerData.streamInfo.mediaType === 'Audio';
            }

            return false;
        };

        self.getPlayers = function () {

            var players = pluginManager.ofType('mediaplayer');

            players.sort(function (a, b) {

                return (a.priority || 0) - (b.priority || 0);
            });

            return players;
        };

        self.canPlay = function (item) {

            var itemType = item.Type;
            var locationType = item.LocationType;
            var mediaType = item.MediaType;

            if (itemType === "MusicGenre" || itemType === "Season" || itemType === "Series" || itemType === "BoxSet" || itemType === "MusicAlbum" || itemType === "MusicArtist" || itemType === "Playlist") {
                return true;
            }

            if (locationType === "Virtual") {
                if (itemType !== "Program") {
                    return false;
                }
            }

            if (itemType === "Program") {
                if (new Date().getTime() > datetime.parseISO8601Date(item.EndDate).getTime() || new Date().getTime() < datetime.parseISO8601Date(item.StartDate).getTime()) {
                    return false;
                }
            }

            return self.getPlayers().filter(function (p) {

                return p.canPlayMediaType(mediaType);

            }).length;
        };

        self.canQueue = function (item) {

            if (item.Type === 'MusicAlbum' || item.Type === 'MusicArtist' || item.Type === 'MusicGenre') {
                return self.canQueueMediaType('Audio');
            }
            return self.canQueueMediaType(item.MediaType);
        };

        self.canQueueMediaType = function (mediaType) {

            if (currentPlayer) {
                return currentPlayer.canPlayMediaType(mediaType);
            }

            return false;
        };

        self.isMuted = function () {

            if (currentPlayer) {
                return currentPlayer.isMuted();
            }

            return false;
        };

        self.setMute = function (mute) {

            if (currentPlayer) {
                currentPlayer.setMute(mute);
            }
        };

        self.toggleMute = function (mute) {

            if (currentPlayer) {
                self.setMute(!self.isMuted());
            }
        };

        self.volume = function (val) {

            if (currentPlayer) {
                return currentPlayer.volume(val);
            }
        };

        self.volumeUp = function () {

            if (currentPlayer) {
                currentPlayer.volumeUp();
            }
        };

        self.volumeDown = function () {

            if (currentPlayer) {
                currentPlayer.volumeDown();
            }
        };

        self.setAudioStreamIndex = function (index) {

            var player = currentPlayer;

            if (getPlayerData(player).streamInfo.playMethod === 'Transcode' || !player.canSetAudioStreamIndex()) {

                changeStream(player, getCurrentTicks(player), { AudioStreamIndex: index });
                getPlayerData(player).audioStreamIndex = index;

            } else {
                player.setAudioStreamIndex(index);
                getPlayerData(player).audioStreamIndex = index;
            }
        };

        self.setSubtitleStreamIndex = function (index) {

            var player = currentPlayer;
            var currentStream = getCurrentSubtitleStream(player);

            var newStream = getSubtitleStream(player, index);

            if (!currentStream && !newStream) {
                return;
            }

            var selectedTrackElementIndex = -1;

            if (currentStream && !newStream) {

                if (currentStream.DeliveryMethod === 'Encode') {

                    // Need to change the transcoded stream to remove subs
                    changeStream(player, getCurrentTicks(player), { SubtitleStreamIndex: -1 });
                }
            }
            else if (!currentStream && newStream) {

                if (newStream.DeliveryMethod === 'External' || newStream.DeliveryMethod === 'Embed') {
                    selectedTrackElementIndex = index;
                } else {

                    // Need to change the transcoded stream to add subs
                    changeStream(player, getCurrentTicks(player), { SubtitleStreamIndex: index });
                }
            }
            else if (currentStream && newStream) {

                if (newStream.DeliveryMethod === 'External' || newStream.DeliveryMethod === 'Embed') {
                    selectedTrackElementIndex = index;

                    if (currentStream.DeliveryMethod !== 'External' && currentStream.DeliveryMethod !== 'Embed') {
                        changeStream(player, getCurrentTicks(player), { SubtitleStreamIndex: -1 });
                    }
                } else {

                    // Need to change the transcoded stream to add subs
                    changeStream(player, getCurrentTicks(player), { SubtitleStreamIndex: index });
                }
            }

            player.setSubtitleStreamIndex(selectedTrackElementIndex);

            getPlayerData(player).subtitleStreamIndex = index;
        };

        self.toggleDisplayMirroring = function () {
            self.enableDisplayMirroring(!self.enableDisplayMirroring());
        };

        self.enableDisplayMirroring = function (enabled) {

            if (enabled != null) {

                var val = enabled ? '1' : '0';
                appSettings.set('displaymirror--' + Dashboard.getCurrentUserId(), val);

                if (enabled) {
                    mirrorIfEnabled();
                }
                return;
            }

            return (appSettings.get('displaymirror--' + Dashboard.getCurrentUserId()) || '') != '0';
        };

        self.stop = function () {
            if (currentPlayer) {
                playNextAfterEnded = false;
                currentPlayer.stop(true, true);
            }
        };

        self.playPause = function () {
            if (currentPlayer) {

                if (currentPlayer.paused()) {
                    self.unpause();
                } else {
                    self.pause();
                }
            }
        };

        self.paused = function () {

            if (currentPlayer) {
                return currentPlayer.paused();
            }
        };

        self.pause = function () {
            if (currentPlayer) {
                currentPlayer.pause();
            }
        };

        self.unpause = function () {
            if (currentPlayer) {
                currentPlayer.unpause();
            }
        };

        self.seek = function (ticks) {

            var player = self.currentPlayer();

            changeStream(player, ticks);
        };

        self.nextChapter = function () {

            var player = self.currentPlayer();
            var item = self.currentItem(player);

            var ticks = getCurrentTicks(player);

            var nextChapter = (item.Chapters || []).filter(function (i) {

                return i.StartPositionTicks > ticks;

            })[0];

            if (nextChapter) {
                self.seek(nextChapter.StartPositionTicks);
            } else {
                self.nextTrack();
            }
        };

        self.previousChapter = function () {
            var player = self.currentPlayer();
            var item = self.currentItem(player);

            var ticks = getCurrentTicks(player);

            // Go back 10 seconds
            ticks -= 100000000;

            var previousChapters = (item.Chapters || []).filter(function (i) {

                return i.StartPositionTicks <= ticks;
            });

            if (previousChapters.length) {
                self.seek(previousChapters[previousChapters.length - 1].StartPositionTicks);
            } else {
                self.previousTrack();
            }
        };

        self.fastForward = function () {

            var player = self.currentPlayer();

            if (player.fastForward != null) {
                player.fastForward(userSettings.skipForwardLength());
                return;
            }

            var ticks = getCurrentTicks(player);

            // Go back 15 seconds
            ticks += userSettings.skipForwardLength() * 10000;

            var data = getPlayerData(player).streamInfo;
            var mediaSource = data.mediaSource;

            if (mediaSource) {
                var runTimeTicks = mediaSource.RunTimeTicks || 0;

                if (ticks < runTimeTicks) {
                    self.seek(ticks);
                }
            }
        };

        self.rewind = function () {

            var player = self.currentPlayer();

            if (player.rewind != null) {
                player.rewind(userSettings.skipBackLength());
                return;
            }

            var ticks = getCurrentTicks(player);

            // Go back 15 seconds
            ticks -= userSettings.skipBackLength() * 10000;

            self.seek(Math.max(0, ticks));
        };

        // Returns true if the player can seek using native client-side seeking functions
        function canPlayerSeek(player) {

            var currentSrc = (player.currentSrc() || '').toLowerCase();

            if (currentSrc.indexOf('.m3u8') !== -1) {

                return true;

            } else {
                var duration = player.duration();
                return duration && !isNaN(duration) && duration !== Number.POSITIVE_INFINITY && duration !== Number.NEGATIVE_INFINITY;
            }
        }

        function changeStream(player, ticks, params) {

            if (canPlayerSeek(player) && params == null) {

                player.currentTime(parseInt(ticks / 10000));
                return;
            }

            params = params || {};

            var currentSrc = player.currentSrc();

            var liveStreamId = getPlayerData(player).streamInfo.liveStreamId;
            var playSessionId = getPlayerData(player).streamInfo.playSessionId;

            var playerData = getPlayerData(player);
            var currentItem = playerData.streamInfo.item;

            player.getDeviceProfile(currentItem).then(function (deviceProfile) {

                var audioStreamIndex = params.AudioStreamIndex == null ? getPlayerData(player).audioStreamIndex : params.AudioStreamIndex;
                var subtitleStreamIndex = params.SubtitleStreamIndex == null ? getPlayerData(player).subtitleStreamIndex : params.SubtitleStreamIndex;

                var currentMediaSource = playerData.streamInfo.mediaSource;
                var apiClient = connectionManager.getApiClient(currentItem.ServerId);

                if (ticks) {
                    ticks = parseInt(ticks);
                }

                getPlaybackInfo(apiClient, currentItem.Id, deviceProfile, appSettings.maxStreamingBitrate(), ticks, currentMediaSource, audioStreamIndex, subtitleStreamIndex, liveStreamId).then(function (result) {

                    if (validatePlaybackInfoResult(result)) {

                        currentMediaSource = result.MediaSources[0];
                        createStreamInfo(apiClient, currentItem.MediaType, currentItem, currentMediaSource, ticks).then(function (streamInfo) {

                            streamInfo.fullscreen = currentPlayOptions.fullscreen;

                            if (!streamInfo.url) {
                                showPlaybackInfoErrorMessage('NoCompatibleStream');
                                self.nextTrack();
                                return;
                            }

                            getPlayerData(player).subtitleStreamIndex = subtitleStreamIndex;
                            getPlayerData(player).audioStreamIndex = audioStreamIndex;

                            changeStreamToUrl(apiClient, player, playSessionId, streamInfo);
                        });
                    }
                });
            });
        }

        function changeStreamToUrl(apiClient, player, playSessionId, streamInfo, newPositionTicks) {

            clearProgressInterval(player);

            getPlayerData(player).isChangingStream = true;

            if (getPlayerData(player).MediaType === "Video") {
                apiClient.stopActiveEncodings(playSessionId).then(function () {

                    setSrcIntoPlayer(apiClient, player, streamInfo);
                });

            } else {

                setSrcIntoPlayer(apiClient, player, streamInfo);
            }
        }

        function setSrcIntoPlayer(apiClient, player, streamInfo) {

            player.play(streamInfo).then(function () {

                getPlayerData(player).isChangingStream = false;
                getPlayerData(player).streamInfo = streamInfo;

                startProgressInterval(player);
                sendProgressUpdate(player);
            });
        }

        self.seekPercent = function (percent, player) {

            var data = getPlayerData(player).streamInfo;
            var mediaSource = data.mediaSource;

            if (mediaSource) {
                var ticks = mediaSource.RunTimeTicks || 0;

                percent /= 100;
                ticks *= percent;
                self.seek(parseInt(ticks));
            }
        };

        self.playTrailers = function (item) {

            var apiClient = connectionManager.getApiClient(item.ServerId);

            if (item.LocalTrailerCount) {
                apiClient.getLocalTrailers(apiClient.getCurrentUserId(), item.Id).then(function (result) {

                    self.play({
                        items: result
                    });
                });
            } else {
                var remoteTrailers = item.RemoteTrailers || [];

                if (!remoteTrailers.length) {
                    return;
                }

                self.play({
                    items: remoteTrailers.map(function (t) {
                        return {
                            Name: t.Name || (item.Name + ' Trailer'),
                            Url: t.Url,
                            MediaType: 'Video',
                            Type: 'Trailer'
                        };
                    })
                });
            }
        };

        self.play = function (options) {

            if (typeof (options) === 'string') {
                options = { ids: [options] };
            }

            return playItems(options);
        };

        self.instantMix = function (id, serverId) {

            if (typeof id !== 'string') {
                var item = id;
                id = item.Id;
                serverId = item.ServerId;
            }

            var apiClient = connectionManager.getApiClient(serverId);

            var options = {};
            options.UserId = apiClient.getCurrentUserId();
            options.Fields = 'MediaSources';

            apiClient.getInstantMixFromItem(id, options).then(function (result) {
                self.play({
                    items: result.Items
                });
            });
        };

        self.shuffle = function (id, serverId) {

            if (typeof id !== 'string') {
                var item = id;
                id = item.Id;
                serverId = item.ServerId;
            }

            var apiClient = connectionManager.getApiClient(serverId);

            apiClient.getItem(apiClient.getCurrentUserId(), id).then(function (item) {

                var query = {
                    Fields: "MediaSources,Chapters",
                    Limit: 100,
                    Filters: "IsNotFolder",
                    Recursive: true,
                    SortBy: "Random"
                };

                if (item.Type === "MusicArtist") {

                    query.MediaTypes = "Audio";
                    query.ArtistIds = item.Id;

                }
                else if (item.Type === "MusicGenre") {

                    query.MediaTypes = "Audio";
                    query.Genres = item.Name;

                }
                else if (item.IsFolder) {
                    query.ParentId = id;

                }
                else {
                    return;
                }

                getItemsForPlayback(item.ServerId, query).then(function (result) {

                    self.play({ items: result.Items });

                });
            });
        };

        function getPlayerData(player) {

            if (!player) {
                throw new Error('player cannot be null');
            }
            if (!player.name) {
                throw new Error('player name cannot be null');
            }
            var state = playerStates[player.name];

            if (!state) {
                playerStates[player.name] = {};
                state = playerStates[player.name];
            }

            return player;
        }

        self.getPlayerState = function (player) {

            player = player || currentPlayer;
            var playerData = getPlayerData(player);
            var item = playerData.streamInfo.item;
            var mediaSource = playerData.streamInfo.mediaSource;

            var state = {
                PlayState: {}
            };

            if (player) {

                state.PlayState.VolumeLevel = player.volume();
                state.PlayState.IsMuted = player.isMuted();
                state.PlayState.IsPaused = player.paused();
                state.PlayState.PositionTicks = getCurrentTicks(player);
                state.PlayState.RepeatMode = self.getRepeatMode();

                var currentSrc = player.currentSrc();

                if (currentSrc) {

                    state.PlayState.SubtitleStreamIndex = playerData.subtitleStreamIndex;
                    state.PlayState.AudioStreamIndex = playerData.audioStreamIndex;

                    state.PlayState.PlayMethod = playerData.streamInfo.playMethod;

                    if (mediaSource) {
                        state.PlayState.LiveStreamId = mediaSource.LiveStreamId;
                    }
                    state.PlayState.PlaySessionId = playerData.streamInfo.playSessionId;
                }
            }

            if (mediaSource) {

                state.PlayState.MediaSourceId = mediaSource.Id;

                state.NowPlayingItem = {
                    RunTimeTicks: mediaSource.RunTimeTicks
                };

                state.PlayState.CanSeek = (mediaSource.RunTimeTicks || 0) > 0 || canPlayerSeek(player);
            }

            if (item) {

                state.NowPlayingItem = getNowPlayingItemForReporting(player, item, mediaSource);
            }

            return state;
        };

        self.currentTime = function (player) {
            return getCurrentTicks(player);
        };

        function getCurrentTicks(player) {

            var playerTime = Math.floor(10000 * (player || currentPlayer).currentTime());
            playerTime += getPlayerData(player).streamInfo.transcodingOffsetTicks || 0;

            return playerTime;
        }

        function getNowPlayingItemForReporting(player, item, mediaSource) {

            var nowPlayingItem = {};

            if (mediaSource) {
                nowPlayingItem.RunTimeTicks = mediaSource.RunTimeTicks;
            } else {
                nowPlayingItem.RunTimeTicks = player.duration() * 10000;
            }

            nowPlayingItem.Id = item.Id;
            nowPlayingItem.MediaType = item.MediaType;
            nowPlayingItem.Type = item.Type;
            nowPlayingItem.Name = item.Name;

            nowPlayingItem.IndexNumber = item.IndexNumber;
            nowPlayingItem.IndexNumberEnd = item.IndexNumberEnd;
            nowPlayingItem.ParentIndexNumber = item.ParentIndexNumber;
            nowPlayingItem.ProductionYear = item.ProductionYear;
            nowPlayingItem.PremiereDate = item.PremiereDate;
            nowPlayingItem.SeriesName = item.SeriesName;
            nowPlayingItem.Album = item.Album;
            nowPlayingItem.Artists = item.ArtistItems;

            var imageTags = item.ImageTags || {};

            if (item.SeriesPrimaryImageTag) {

                nowPlayingItem.PrimaryImageItemId = item.SeriesId;
                nowPlayingItem.PrimaryImageTag = item.SeriesPrimaryImageTag;
            }
            else if (imageTags.Primary) {

                nowPlayingItem.PrimaryImageItemId = item.Id;
                nowPlayingItem.PrimaryImageTag = imageTags.Primary;
            }
            else if (item.AlbumPrimaryImageTag) {

                nowPlayingItem.PrimaryImageItemId = item.AlbumId;
                nowPlayingItem.PrimaryImageTag = item.AlbumPrimaryImageTag;
            }
            else if (item.SeriesPrimaryImageTag) {

                nowPlayingItem.PrimaryImageItemId = item.SeriesId;
                nowPlayingItem.PrimaryImageTag = item.SeriesPrimaryImageTag;
            }

            if (item.BackdropImageTags && item.BackdropImageTags.length) {

                nowPlayingItem.BackdropItemId = item.Id;
                nowPlayingItem.BackdropImageTag = item.BackdropImageTags[0];
            }
            else if (item.ParentBackdropImageTags && item.ParentBackdropImageTags.length) {
                nowPlayingItem.BackdropItemId = item.ParentBackdropItemId;
                nowPlayingItem.BackdropImageTag = item.ParentBackdropImageTags[0];
            }

            if (imageTags.Thumb) {

                nowPlayingItem.ThumbItemId = item.Id;
                nowPlayingItem.ThumbImageTag = imageTags.Thumb;
            }

            if (imageTags.Logo) {

                nowPlayingItem.LogoItemId = item.Id;
                nowPlayingItem.LogoImageTag = imageTags.Logo;
            }
            else if (item.ParentLogoImageTag) {

                nowPlayingItem.LogoItemId = item.ParentLogoItemId;
                nowPlayingItem.LogoImageTag = item.ParentLogoImageTag;
            }

            return nowPlayingItem;
        }

        function playItems(options, method) {

            normalizePlayOptions(options);

            if (options.fullscreen) {
                loading.show();
            }

            if (options.items) {

                return translateItemsForPlayback(options.items, options).then(function (items) {

                    return playWithIntros(items, options);
                });

            } else {

                if (!options.serverId) {
                    throw new Error();
                }

                return getItemsForPlayback(options.serverId, {

                    Ids: options.ids.join(',')

                }).then(function (result) {

                    return translateItemsForPlayback(result.Items, options).then(function (items) {

                        return playWithIntros(items, options);
                    });

                });
            }
        }

        function translateItemsForPlayback(items, options) {

            var firstItem = items[0];
            var promise;

            var serverId = firstItem.ServerId;

            if (firstItem.Type === "Program") {

                promise = getItemsForPlayback(serverId, {
                    Ids: firstItem.ChannelId,
                });
            }
            else if (firstItem.Type === "Playlist") {

                promise = getItemsForPlayback(serverId, {
                    ParentId: firstItem.Id,
                });
            }
            else if (firstItem.Type === "MusicArtist") {

                promise = getItemsForPlayback(serverId, {
                    ArtistIds: firstItem.Id,
                    Filters: "IsNotFolder",
                    Recursive: true,
                    SortBy: "SortName",
                    MediaTypes: "Audio"
                });

            }
            else if (firstItem.Type === "MusicGenre") {

                promise = getItemsForPlayback(serverId, {
                    Genres: firstItem.Name,
                    Filters: "IsNotFolder",
                    Recursive: true,
                    SortBy: "SortName",
                    MediaTypes: "Audio"
                });
            }
            else if (firstItem.IsFolder) {

                promise = getItemsForPlayback(serverId, {
                    ParentId: firstItem.Id,
                    Filters: "IsNotFolder",
                    Recursive: true,
                    SortBy: "SortName",
                    MediaTypes: "Audio,Video"
                });
            }
            else if (firstItem.Type === "Episode" && items.length === 1 && getPlayer(firstItem, options).supportsProgress !== false) {

                promise = new Promise(function (resolve, reject) {
                    var apiClient = connectionManager.getApiClient(firstItem.ServerId);

                    apiClient.getCurrentUser().then(function (user) {

                        if (!user.Configuration.EnableNextEpisodeAutoPlay || !firstItem.SeriesId) {
                            resolve(null);
                            return;
                        }

                        apiClient.getEpisodes(firstItem.SeriesId, {
                            IsVirtualUnaired: false,
                            IsMissing: false,
                            UserId: apiClient.getCurrentUserId(),
                            Fields: "MediaSources,Chapters"

                        }).then(function (episodesResult) {

                            var foundItem = false;
                            episodesResult.Items = episodesResult.Items.filter(function (e) {

                                if (foundItem) {
                                    return true;
                                }
                                if (e.Id === firstItem.Id) {
                                    foundItem = true;
                                    return true;
                                }

                                return false;
                            });
                            episodesResult.TotalRecordCount = episodesResult.Items.length;
                            resolve(episodesResult);
                        }, reject);
                    });
                });
            }

            if (promise) {
                return promise.then(function (result) {

                    return result ? result.Items : items;
                });
            } else {
                return Promise.resolve(items);
            }
        }

        function playWithIntros(items, options, user) {

            var firstItem = items[0];

            if (firstItem.MediaType === "Video") {

                //Dashboard.showModalLoadingMsg();
            }

            var afterPlayInternal = function () {
                setPlaylistState(0, items);
                loading.hide();
            };

            if (options.startPositionTicks || firstItem.MediaType !== 'Video' || !isServerItem(firstItem) || options.fullscreen === false || !userSettings.enableCinemaMode()) {

                currentPlayOptions = options;
                return playInternal(firstItem, options, afterPlayInternal);
            }

            var apiClient = connectionManager.getApiClient(firstItem.ServerId);

            return apiClient.getJSON(apiClient.getUrl('Users/' + apiClient.getCurrentUserId() + '/Items/' + firstItem.Id + '/Intros')).then(function (intros) {

                items = intros.Items.concat(items);
                currentPlayOptions = options;
                return playInternal(items[0], options, afterPlayInternal);
            });
        }

        function isServerItem(item) {
            if (!item.Id) {
                return false;
            }
            return true;
        }

        // Set currentPlaylistIndex and playlist. Using a method allows for overloading in derived player implementations
        function setPlaylistState(i, items) {
            if (!isNaN(i)) {
                currentPlaylistIndex = i;
            }
            if (items) {
                playlist = items.slice(0);
            }
        }

        function playInternal(item, playOptions, callback) {

            if (item.IsPlaceHolder) {
                loading.hide();
                showPlaybackInfoErrorMessage('PlaceHolder', true);
                return Promise.reject();
            }

            // Normalize defaults to simplfy checks throughout the process
            normalizePlayOptions(playOptions);

            return runInterceptors(item, playOptions).then(function () {

                if (playOptions.fullscreen) {
                    loading.show();
                }

                if (item.MediaType === 'Video' && isServerItem(item) && appSettings.enableAutomaticBitrateDetection()) {

                    var apiClient = connectionManager.getApiClient(item.ServerId);
                    return apiClient.detectBitrate().then(function (bitrate) {

                        appSettings.maxStreamingBitrate(bitrate);

                        return playAfterBitrateDetect(connectionManager, bitrate, item, playOptions).then(callback);

                    }, function () {

                        return playAfterBitrateDetect(connectionManager, appSettings.maxStreamingBitrate(), item, playOptions).then(callback);
                    });

                } else {

                    return playAfterBitrateDetect(connectionManager, appSettings.maxStreamingBitrate(), item, playOptions).then(callback);
                }

            }, function () {

                var player = currentPlayer;

                if (player) {
                    player.destroy();
                }
                setCurrentPlayer(null);

                events.trigger(self, 'playbackcancelled');

                return Promise.reject();
            });
        }

        function runInterceptors(item, playOptions) {

            return new Promise(function (resolve, reject) {

                var interceptors = pluginManager.ofType('preplayintercept');

                interceptors.sort(function (a, b) {
                    return (a.order || 0) - (b.order || 0);
                });

                if (!interceptors.length) {
                    resolve();
                    return;
                }

                loading.hide();

                var options = Object.assign({}, playOptions);

                options.mediaType = item.MediaType;
                options.item = item;

                runNextPrePlay(interceptors, 0, options, resolve, reject);
            });
        }

        function runNextPrePlay(interceptors, index, options, resolve, reject) {

            if (index >= interceptors.length) {
                resolve();
                return;
            }

            var interceptor = interceptors[index];

            interceptor.intercept(options).then(function () {

                runNextPrePlay(interceptors, index + 1, options, resolve, reject);

            }, reject);
        }

        function playAfterBitrateDetect(connectionManager, maxBitrate, item, playOptions) {

            var startPosition = playOptions.startPositionTicks;

            var player = getPlayer(item, playOptions);
            var activePlayer = currentPlayer;

            var promise;

            if (activePlayer) {

                // TODO: if changing players within the same playlist, this will cause nextItem to be null
                playNextAfterEnded = false;
                promise = onPlaybackChanging(activePlayer, player, item);
            } else {
                promise = Promise.resolve();
            }

            if (!isServerItem(item) || item.MediaType === 'Game') {
                return promise.then(function () {
                    var streamInfo = createStreamInfoFromUrlItem(item);
                    streamInfo.fullscreen = playOptions.fullscreen;
                    getPlayerData(player).isChangingStream = false;
                    return player.play(streamInfo).then(function () {
                        onPlaybackStarted(player, streamInfo);
                        loading.hide();
                        return Promise.resolve();
                    });
                });
            }

            return Promise.all([promise, player.getDeviceProfile(item)]).then(function (responses) {

                var deviceProfile = responses[1];

                var apiClient = connectionManager.getApiClient(item.ServerId);
                return getPlaybackMediaSource(apiClient, deviceProfile, maxBitrate, item, startPosition).then(function (mediaSource) {

                    return createStreamInfo(apiClient, item.MediaType, item, mediaSource, startPosition).then(function (streamInfo) {

                        streamInfo.fullscreen = playOptions.fullscreen;

                        getPlayerData(player).isChangingStream = false;

                        return player.play(streamInfo).then(function () {
                            onPlaybackStarted(player, streamInfo, mediaSource);
                            loading.hide();
                            return Promise.resolve();
                        });
                    });
                });
            });
        }

        function createStreamInfoFromUrlItem(item) {

            // Check item.Path for games
            return {
                url: item.Url || item.Path,
                playMethod: 'DirectPlay',
                item: item,
                textTracks: [],
                mediaType: item.MediaType
            };
        }

        function backdropImageUrl(apiClient, item, options) {

            options = options || {};
            options.type = options.type || "Backdrop";

            // If not resizing, get the original image
            if (!options.maxWidth && !options.width && !options.maxHeight && !options.height) {
                options.quality = 100;
            }

            if (item.BackdropImageTags && item.BackdropImageTags.length) {

                options.tag = item.BackdropImageTags[0];
                return apiClient.getScaledImageUrl(item.Id, options);
            }

            if (item.ParentBackdropImageTags && item.ParentBackdropImageTags.length) {
                options.tag = item.ParentBackdropImageTags[0];
                return apiClient.getScaledImageUrl(item.ParentBackdropItemId, options);
            }

            return null;
        }

        function getMimeType(type, container) {

            container = (container || '').toLowerCase();

            if (type === 'audio') {
                if (container === 'opus') {
                    return 'audio/ogg';
                }
                if (container === 'webma') {
                    return 'audio/webm';
                }
                if (container === 'm4a') {
                    return 'audio/mp4';
                }
            }
            else if (type === 'video') {
                if (container === 'mkv') {
                    return 'video/x-matroska';
                }
                if (container === 'm4v') {
                    return 'video/mp4';
                }
                if (container === 'mov') {
                    return 'video/quicktime';
                }
                if (container === 'mpg') {
                    return 'video/mpeg';
                }
                if (container === 'flv') {
                    return 'video/x-flv';
                }
            }

            return type + '/' + container;
        }

        function createStreamInfo(apiClient, type, item, mediaSource, startPosition) {

            var mediaUrl;
            var contentType;
            var transcodingOffsetTicks = 0;
            var playerStartPositionTicks = startPosition;
            var liveStreamId;

            var playMethod = 'Transcode';

            var mediaSourceContainer = (mediaSource.Container || '').toLowerCase();
            var directOptions;

            if (type === 'Video') {

                contentType = getMimeType('video', mediaSourceContainer);

                if (mediaSource.enableDirectPlay) {
                    mediaUrl = mediaSource.Path;

                    playMethod = 'DirectPlay';

                } else {

                    if (mediaSource.SupportsDirectStream) {

                        directOptions = {
                            Static: true,
                            mediaSourceId: mediaSource.Id,
                            deviceId: apiClient.deviceId(),
                            api_key: apiClient.accessToken()
                        };

                        if (mediaSource.ETag) {
                            directOptions.Tag = mediaSource.ETag;
                        }

                        if (mediaSource.LiveStreamId) {
                            directOptions.LiveStreamId = mediaSource.LiveStreamId;
                            liveStreamId = mediaSource.LiveStreamId;
                        }

                        mediaUrl = apiClient.getUrl('Videos/' + item.Id + '/stream.' + mediaSourceContainer, directOptions);

                        playMethod = 'DirectStream';
                    } else if (mediaSource.SupportsTranscoding) {

                        mediaUrl = apiClient.getUrl(mediaSource.TranscodingUrl);

                        if (mediaSource.TranscodingSubProtocol === 'hls') {

                            contentType = 'application/x-mpegURL';

                        } else {

                            playerStartPositionTicks = null;
                            contentType = getMimeType('video', mediaSource.TranscodingContainer);

                            if (mediaUrl.toLowerCase().indexOf('copytimestamps=true') === -1) {
                                transcodingOffsetTicks = startPosition || 0;
                            }
                        }
                    }
                }

            } else if (type === 'Audio') {

                contentType = getMimeType('audio', mediaSourceContainer);

                if (mediaSource.enableDirectPlay) {

                    mediaUrl = mediaSource.Path;

                    playMethod = 'DirectPlay';

                } else {

                    var isDirectStream = mediaSource.SupportsDirectStream;

                    if (isDirectStream) {

                        directOptions = {
                            Static: true,
                            mediaSourceId: mediaSource.Id,
                            deviceId: apiClient.deviceId(),
                            api_key: apiClient.accessToken()
                        };

                        if (mediaSource.ETag) {
                            directOptions.Tag = mediaSource.ETag;
                        }

                        if (mediaSource.LiveStreamId) {
                            directOptions.LiveStreamId = mediaSource.LiveStreamId;
                            liveStreamId = mediaSource.LiveStreamId;
                        }

                        mediaUrl = apiClient.getUrl('Audio/' + item.Id + '/stream.' + mediaSourceContainer, directOptions);

                        playMethod = 'DirectStream';

                    } else if (mediaSource.SupportsTranscoding) {

                        mediaUrl = apiClient.getUrl(mediaSource.TranscodingUrl);

                        if (mediaSource.TranscodingSubProtocol === 'hls') {

                            contentType = 'application/x-mpegURL';
                        } else {

                            transcodingOffsetTicks = startPosition || 0;
                            playerStartPositionTicks = null;
                            contentType = getMimeType('audio', mediaSource.TranscodingContainer);
                        }
                    }
                }
            } else if (type === 'Game') {

                mediaUrl = mediaSource.Path;
                playMethod = 'DirectPlay';
            }

            var resultInfo = {
                url: mediaUrl,
                mimeType: contentType,
                transcodingOffsetTicks: transcodingOffsetTicks,
                playMethod: playMethod,
                playerStartPositionTicks: playerStartPositionTicks,
                item: item,
                mediaSource: mediaSource,
                textTracks: getTextTracks(apiClient, mediaSource),
                // duplicate this temporarily
                tracks: getTextTracks(apiClient, mediaSource),
                mediaType: type,
                liveStreamId: liveStreamId,
                playSessionId: getParam('playSessionId', mediaUrl),
                title: item.Name
            };

            var backdropUrl = backdropImageUrl(apiClient, item, {});
            if (backdropUrl) {
                resultInfo.backdropUrl = backdropUrl;
            }

            return Promise.resolve(resultInfo);
        }

        function getParam(name, url) {
            name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
            var regexS = "[\\?&]" + name + "=([^&#]*)";
            var regex = new RegExp(regexS, "i");

            var results = regex.exec(url);
            if (results == null) {
                return "";
            }
            else {
                return decodeURIComponent(results[1].replace(/\+/g, " "));
            }
        }

        self.getSubtitleUrl = function (textStream, serverId) {

            var apiClient = connectionManager.getApiClient(serverId);
            var textStreamUrl = !textStream.IsExternalUrl ? apiClient.getUrl(textStream.DeliveryUrl) : textStream.DeliveryUrl;
            return textStreamUrl;
        };

        function getTextTracks(apiClient, mediaSource) {

            var subtitleStreams = mediaSource.MediaStreams.filter(function (s) {
                return s.Type === 'Subtitle';
            });

            var textStreams = subtitleStreams.filter(function (s) {
                return s.DeliveryMethod === 'External';
            });

            var tracks = [];

            for (var i = 0, length = textStreams.length; i < length; i++) {

                var textStream = textStreams[i];
                var textStreamUrl = !textStream.IsExternalUrl ? apiClient.getUrl(textStream.DeliveryUrl) : textStream.DeliveryUrl;

                tracks.push({
                    url: textStreamUrl,
                    language: (textStream.Language || 'und'),
                    isDefault: textStream.Index === mediaSource.DefaultSubtitleStreamIndex,
                    index: textStream.Index,
                    format: textStream.Codec
                });
            }

            return tracks;
        }

        function getPlaybackMediaSource(apiClient, deviceProfile, maxBitrate, item, startPosition, callback) {

            if (item.MediaType === "Video") {

                //Dashboard.showModalLoadingMsg();
            }

            return getPlaybackInfo(apiClient, item.Id, deviceProfile, maxBitrate, startPosition).then(function (playbackInfoResult) {

                if (validatePlaybackInfoResult(playbackInfoResult)) {

                    return getOptimalMediaSource(apiClient, item, playbackInfoResult.MediaSources).then(function (mediaSource) {
                        if (mediaSource) {

                            if (mediaSource.RequiresOpening) {

                                return getLiveStream(apiClient, item.Id, playbackInfoResult.PlaySessionId, deviceProfile, startPosition, mediaSource, null, null).then(function (openLiveStreamResult) {

                                    return supportsDirectPlay(apiClient, openLiveStreamResult.MediaSource).then(function (result) {

                                        openLiveStreamResult.MediaSource.enableDirectPlay = result;
                                        return openLiveStreamResult.MediaSource;
                                    });

                                });

                            } else {
                                return mediaSource;
                            }
                        } else {
                            //Dashboard.hideModalLoadingMsg();
                            showPlaybackInfoErrorMessage('NoCompatibleStream');
                            return Promise.reject();
                        }
                    });
                } else {
                    return Promise.reject();
                }
            });
        }

        function getPlaybackInfo(apiClient, itemId, deviceProfile, maxBitrate, startPosition, mediaSource, audioStreamIndex, subtitleStreamIndex, liveStreamId) {

            var postData = {
                DeviceProfile: deviceProfile
            };

            var query = {
                UserId: apiClient.getCurrentUserId(),
                StartTimeTicks: startPosition || 0
            };

            if (audioStreamIndex != null) {
                query.AudioStreamIndex = audioStreamIndex;
            }
            if (subtitleStreamIndex != null) {
                query.SubtitleStreamIndex = subtitleStreamIndex;
            }
            if (mediaSource) {
                query.MediaSourceId = mediaSource.Id;
            }
            if (liveStreamId) {
                query.LiveStreamId = liveStreamId;
            }
            if (maxBitrate) {
                query.MaxStreamingBitrate = maxBitrate;
            }

            return apiClient.ajax({
                url: apiClient.getUrl('Items/' + itemId + '/PlaybackInfo', query),
                type: 'POST',
                data: JSON.stringify(postData),
                contentType: "application/json",
                dataType: "json"

            });
        }

        function getOptimalMediaSource(apiClient, item, versions) {

            var promises = versions.map(function (v) {
                return supportsDirectPlay(apiClient, v);
            });

            if (!promises.length) {
                return Promise.reject();
            }

            return Promise.all(promises).then(function (results) {

                for (var i = 0, length = versions.length; i < length; i++) {
                    versions[i].enableDirectPlay = results[i] || false;
                }
                var optimalVersion = versions.filter(function (v) {

                    return v.enableDirectPlay;

                })[0];

                if (!optimalVersion) {
                    optimalVersion = versions.filter(function (v) {

                        return v.SupportsDirectStream;

                    })[0];
                }

                optimalVersion = optimalVersion || versions.filter(function (s) {
                    return s.SupportsTranscoding;
                })[0];

                return optimalVersion;
            });
        }

        function getLiveStream(apiClient, itemId, playSessionId, deviceProfile, startPosition, mediaSource, audioStreamIndex, subtitleStreamIndex) {

            var postData = {
                DeviceProfile: deviceProfile,
                OpenToken: mediaSource.OpenToken
            };

            var query = {
                UserId: apiClient.getCurrentUserId(),
                StartTimeTicks: startPosition || 0,
                ItemId: itemId,
                PlaySessionId: playSessionId
            };

            if (audioStreamIndex != null) {
                query.AudioStreamIndex = audioStreamIndex;
            }
            if (subtitleStreamIndex != null) {
                query.SubtitleStreamIndex = subtitleStreamIndex;
            }

            return apiClient.ajax({
                url: apiClient.getUrl('LiveStreams/Open', query),
                type: 'POST',
                data: JSON.stringify(postData),
                contentType: "application/json",
                dataType: "json"

            });
        }

        function supportsDirectPlay(apiClient, mediaSource) {

            return new Promise(function (resolve, reject) {

                if (mediaSource.SupportsDirectPlay) {

                    if (mediaSource.Protocol === 'Http' && !mediaSource.RequiredHttpHeaders.length) {

                        // If this is the only way it can be played, then allow it
                        if (!mediaSource.SupportsDirectStream && !mediaSource.SupportsTranscoding) {
                            resolve(true);
                        }
                        else {
                            var val = mediaSource.Path.toLowerCase().replace('https:', 'http').indexOf(apiClient.serverAddress().toLowerCase().replace('https:', 'http').substring(0, 14)) === 0;
                            resolve(val);
                        }
                    }

                    if (mediaSource.Protocol === 'File') {

                        // Determine if the file can be accessed directly
                        require(['filesystem'], function (filesystem) {

                            var method = mediaSource.VideoType === 'BluRay' || mediaSource.VideoType === 'Dvd' || mediaSource.VideoType === 'HdDvd' ?
                                'directoryExists' :
                                'fileExists';

                            filesystem[method](mediaSource.Path).then(function () {
                                resolve(true);
                            }, function () {
                                resolve(false);
                            });

                        });
                    }
                }
                else {
                    resolve(false);
                }
            });
        }

        function validatePlaybackInfoResult(result) {

            if (result.ErrorCode) {

                showPlaybackInfoErrorMessage(result.ErrorCode);
                return false;
            }

            return true;
        }

        function showPlaybackInfoErrorMessage(errorCode, playNextTrack) {

            require(['alert'], function (alert) {
                alert({
                    text: globalize.translate('core#MessagePlaybackError' + errorCode),
                    title: globalize.translate('core#HeaderPlaybackError')
                }).then(function () {

                    if (playNextTrack) {
                        self.nextTrack();
                    }
                });
            });
        }

        function normalizePlayOptions(playOptions) {
            playOptions.fullscreen = playOptions.fullscreen !== false;
        }

        function getPlayer(item, playOptions) {

            var players = self.getPlayers();

            var serverItem = isServerItem(item);

            return self.getPlayers().filter(function (p) {

                if (p.canPlayMediaType(item.MediaType)) {

                    if (serverItem) {
                        if (p.canPlayItem) {
                            return p.canPlayItem(item, playOptions);
                        }
                        return true;
                    }

                    else if (p.canPlayUrl) {
                        return p.canPlayUrl(item.Url);
                    }
                }

                return false;

            })[0];
        }

        function getItemsForPlayback(serverId, query) {

            var apiClient = connectionManager.getApiClient(serverId);

            if (query.Ids && query.Ids.split(',').length === 1) {

                var itemId = query.Ids.split(',');

                return apiClient.getItem(apiClient.getCurrentUserId(), itemId).then(function (item) {

                    return {
                        Items: [item],
                        TotalRecordCount: 1
                    };
                });
            }
            else {

                query.Limit = query.Limit || 100;
                query.Fields = "MediaSources,Chapters";
                query.ExcludeLocationTypes = "Virtual";

                return apiClient.getItems(apiClient.getCurrentUserId(), query);
            }
        }

        // Gets or sets the current playlist index
        self.currentPlaylistIndex = function (i) {

            if (i == null) {
                return currentPlaylistIndex;
            }

            var newItem = playlist[i];

            var playOptions = Object.assign({}, currentPlayOptions, {
                startPositionTicks: 0
            });

            playInternal(newItem, playOptions, function () {
                self.setPlaylistState(i);
            });
        };

        self.setRepeatMode = function (value) {
            repeatMode = value;
            events.trigger(self, 'repeatmodechange');
        };

        self.getRepeatMode = function () {
            return repeatMode;
        };

        function getNextItemInfo() {

            var newIndex;
            var playlistLength = playlist.length;

            switch (self.getRepeatMode()) {

                case 'RepeatOne':
                    newIndex = currentPlaylistIndex;
                    break;
                case 'RepeatAll':
                    newIndex = currentPlaylistIndex + 1;
                    if (newIndex >= playlistLength) {
                        newIndex = 0;
                    }
                    break;
                default:
                    newIndex = currentPlaylistIndex + 1;
                    break;
            }

            if (newIndex < 0 || newIndex >= playlistLength) {
                return null;
            }

            var item = playlist[newIndex];

            if (!item) {
                return null;
            }

            return {
                item: item,
                index: newIndex
            };
        }

        self.nextTrack = function () {

            var newItemInfo = getNextItemInfo();

            if (newItemInfo) {

                console.log('playing next track');

                var playOptions = Object.assign({}, currentPlayOptions, {
                    startPositionTicks: 0
                });

                playInternal(newItemInfo.item, playOptions, function () {
                    setPlaylistState(newItemInfo.index);
                });
            }
        };

        self.previousTrack = function () {
            var newIndex = currentPlaylistIndex - 1;
            if (newIndex >= 0) {
                var newItem = playlist[newIndex];

                if (newItem) {

                    var playOptions = Object.assign({}, currentPlayOptions, {
                        startPositionTicks: 0
                    });

                    playInternal(newItem, playOptions, function () {
                        setPlaylistState(newIndex);
                    });
                }
            }
        };

        self.queue = function (options) {
            queue(options);
        };

        self.queueNext = function (options) {
            queue(options, 'next');
        };

        function queue(options, mode) {

            if (!currentPlayer) {
                self.play(options);
                return;
            }

            if (typeof (options) === 'string') {
                options = { ids: [options] };
            }

            // TODO
        }

        function onPlaybackStarted(player, streamInfo, mediaSource) {

            setCurrentPlayer(player);
            getPlayerData(player).streamInfo = streamInfo;

            if (mediaSource) {
                getPlayerData(player).audioStreamIndex = mediaSource.DefaultAudioStreamIndex;
                getPlayerData(player).subtitleStreamIndex = mediaSource.DefaultSubtitleStreamIndex;
            } else {
                getPlayerData(player).audioStreamIndex = null;
                getPlayerData(player).subtitleStreamIndex = null;
            }

            playNextAfterEnded = true;

            var state = self.getPlayerState(player);

            reportPlayback(state, getPlayerData(player).streamInfo.item.ServerId, 'reportPlaybackStart');

            startProgressInterval(player);

            events.trigger(self, 'playbackstart', [player]);
        }

        function onPlaybackError(e, error) {

            var player = this;
            error = error || {};

            var menuItems = [];
            menuItems.push({
                name: globalize.translate('Resume'),
                id: 'resume'
            });
            menuItems.push({
                name: globalize.translate('Stop'),
                id: 'stop'
            });

            var msg;

            if (error.type === 'network') {
                msg = 'A network error has occurred. Please check your connection and try again.';
            } else {
                msg = 'A network error has occurred. Please check your connection and try again.';
            }

            require(['actionsheet'], function (actionsheet) {

                actionsheet.show({

                    items: menuItems,
                    text: msg

                }).then(function (id) {
                    switch (id) {

                        case 'stop':
                            self.stop();
                            break;
                        case 'resume':
                            player.resume();
                            break;
                        default:
                            break;
                    }
                });
            });
        }

        function onPlaybackStopped(e) {

            var player = this;

            if (getPlayerData(player).isChangingStream) {
                return;
            }

            // User clicked stop or content ended
            var state = self.getPlayerState(player);
            var streamInfo = getPlayerData(player).streamInfo;

            if (isServerItem(streamInfo.item)) {

                if (player.supportsProgress === false && state.PlayState && !state.PlayState.PositionTicks) {
                    state.PlayState.PositionTicks = streamInfo.item.RunTimeTicks;
                }

                reportPlayback(state, streamInfo.item.ServerId, 'reportPlaybackStopped');
            }

            clearProgressInterval(player);

            var nextItem = playNextAfterEnded ? getNextItemInfo() : null;

            var nextMediaType = (nextItem ? nextItem.item.MediaType : null);

            var playbackStopInfo = {
                player: player,
                state: state,
                nextItem: (nextItem ? nextItem.item : null),
                nextMediaType: nextMediaType
            };

            events.trigger(self, 'playbackstop', [playbackStopInfo]);

            var newPlayer = nextItem ? getPlayer(nextItem.item, currentPlayOptions) : null;

            if (newPlayer !== player) {
                player.destroy();
                setCurrentPlayer(null);
            }

            if (nextItem) {
                self.nextTrack();
            }
        }

        function onPlaybackChanging(activePlayer, newPlayer, newItem) {

            var state = self.getPlayerState(activePlayer);
            var serverId = getPlayerData(activePlayer).streamInfo.item.ServerId;

            // User started playing something new while existing content is playing
            var promise;

            if (activePlayer === newPlayer) {

                // If we're staying with the same player, stop it
                promise = activePlayer.stop(false, false);

            } else {

                // If we're switching players, tear down the current one
                promise = activePlayer.stop(true, false);
            }

            return promise.then(function () {
                reportPlayback(state, serverId, 'reportPlaybackStopped');

                clearProgressInterval(activePlayer);

                events.trigger(self, 'playbackstop', [{
                    player: activePlayer,
                    state: state,
                    nextItem: newItem,
                    nextMediaType: newItem.MediaType
                }]);
            });
        }

        function initMediaPlayer(plugin) {
            plugin.currentState = {};

            events.on(plugin, 'error', onPlaybackError);
            events.on(plugin, 'stopped', onPlaybackStopped);
        }

        events.on(pluginManager, 'registered', function (e, plugin) {

            if (plugin.type === 'mediaplayer') {

                initMediaPlayer(plugin);
            }
        });

        pluginManager.ofType('mediaplayer').map(initMediaPlayer);

        function startProgressInterval(player) {

            clearProgressInterval(player);

            var intervalTime = 800;
            player.lastProgressReport = 0;

            getPlayerData(player).currentProgressInterval = setInterval(function () {

                if ((new Date().getTime() - player.lastProgressReport) > intervalTime) {

                    sendProgressUpdate(player);
                }

            }, 500);
        }

        function sendProgressUpdate(player) {

            player.lastProgressReport = new Date().getTime();

            var state = self.getPlayerState(player);
            var currentItem = getPlayerData(player).streamInfo.item;
            reportPlayback(state, currentItem.ServerId, 'reportPlaybackProgress');
        }

        function reportPlayback(state, serverId, method) {

            if (!serverId) {
                // Not a server item
                // We can expand on this later and possibly report them
                return;
            }

            var info = {
                QueueableMediaTypes: state.NowPlayingItem.MediaType,
                ItemId: state.NowPlayingItem.Id
            };

            for (var i in state.PlayState) {
                info[i] = state.PlayState[i];
            }
            //console.log(method + '-' + JSON.stringify(info));
            var apiClient = connectionManager.getApiClient(serverId);
            apiClient[method](info);
        }

        function clearProgressInterval(player) {

            if (getPlayerData(player).currentProgressInterval) {
                clearTimeout(getPlayerData(player).currentProgressInterval);
                getPlayerData(player).currentProgressInterval = null;
            }
        }

        window.addEventListener("beforeunload", function (e) {

            var player = currentPlayer;

            // Try to report playback stopped before the browser closes
            if (player && getPlayerData(player).currentProgressInterval) {
                playNextAfterEnded = false;
                onPlaybackStopped.call(player);
            }
        });

        events.on(serverNotifications, 'ServerShuttingDown', function (e, apiClient, data) {
            self.setDefaultPlayerActive();
        });

        events.on(serverNotifications, 'ServerRestarting', function (e, apiClient, data) {
            self.setDefaultPlayerActive();
        });
    }

    return new playbackManager();
});
