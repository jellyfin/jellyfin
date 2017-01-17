define(['events', 'datetime', 'appSettings', 'pluginManager', 'userSettings', 'globalize', 'connectionManager', 'loading', 'serverNotifications', 'apphost', 'fullscreenManager', 'layoutManager'], function (events, datetime, appSettings, pluginManager, userSettings, globalize, connectionManager, loading, serverNotifications, apphost, fullscreenManager, layoutManager) {
    'use strict';

    function enableLocalPlaylistManagement(player) {

        if (player.isLocalPlayer) {

            return true;
        }

        return false;
    }

    function bindToFullscreenChange(player) {
        events.on(fullscreenManager, 'fullscreenchange', function () {
            events.trigger(player, 'fullscreenchange');
        });
    }

    function PlaybackManager() {

        var self = this;

        var players = [];
        var currentPlayer;
        var currentTargetInfo;
        var lastLocalPlayer;
        var currentPairingId = null;

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

        function triggerPlayerChange(newPlayer, newTarget, previousPlayer, previousTargetInfo) {

            if (!newPlayer && !previousPlayer) {
                return;
            }

            if (newTarget && previousTargetInfo) {

                if (newTarget.id === previousTargetInfo.id) {
                    return;
                }
            }

            events.trigger(self, 'playerchange', [newPlayer, newTarget, previousPlayer]);
        }

        self.beginPlayerUpdates = function (player) {
            if (player.beginPlayerUpdates) {
                player.beginPlayerUpdates();
            }
        };

        self.endPlayerUpdates = function (player) {
            if (player.endPlayerUpdates) {
                player.endPlayerUpdates();
            }
        };

        self.getPlayerInfo = function () {

            var player = currentPlayer;

            if (!player) {
                return null;
            }

            var target = currentTargetInfo || {};

            return {

                name: player.name,
                isLocalPlayer: player.isLocalPlayer,
                id: target.id,
                deviceName: target.deviceName,
                playableMediaTypes: target.playableMediaTypes,
                supportedCommands: target.supportedCommands
            };
        };

        self.setActivePlayer = function (player, targetInfo) {

            if (player === 'localplayer' || player.name === 'localplayer') {
                if (currentPlayer && currentPlayer.isLocalPlayer) {
                    return;
                }
                setCurrentPlayerInternal(null, null);
                return;
            }

            if (typeof (player) === 'string') {
                player = players.filter(function (p) {
                    return p.name === player;
                })[0];
            }

            if (!player) {
                throw new Error('null player');
            }

            setCurrentPlayerInternal(player, targetInfo);
        };

        function displayPlayerInLocalGroup(player) {

            return player.isLocalPlayer;
        }

        self.trySetActivePlayer = function (player, targetInfo) {

            if (player === 'localplayer' || player.name === 'localplayer') {
                if (currentPlayer && currentPlayer.isLocalPlayer) {
                    return;
                }
                return;
            }

            if (typeof (player) === 'string') {
                player = players.filter(function (p) {
                    return p.name === player;
                })[0];
            }

            if (!player) {
                throw new Error('null player');
            }

            if (currentPairingId === targetInfo.id) {
                return;
            }

            currentPairingId = targetInfo.id;

            var promise = player.tryPair ?
                player.tryPair(targetInfo) :
                Promise.resolve();

            promise.then(function () {

                setCurrentPlayerInternal(player, targetInfo);
            }, function () {

                if (currentPairingId === targetInfo.id) {
                    currentPairingId = null;
                }
            });
        };

        self.trySetActiveDeviceName = function (name) {

            function normalizeName(t) {
                return t.toLowerCase().replace(' ', '');
            }

            name = normalizeName(name);

            self.getTargets().then(function (result) {

                var target = result.filter(function (p) {
                    return normalizeName(p.name) === name;
                })[0];

                if (target) {
                    self.trySetActivePlayer(target.playerName, target);
                }

            });
        };

        function getSupportedCommands(player) {

            if (player.isLocalPlayer) {
                // Full list
                // https://github.com/MediaBrowser/MediaBrowser/blob/master/MediaBrowser.Model/Session/GeneralCommand.cs
                var list = [
                    "GoHome",
                    "GoToSettings",
                    "VolumeUp",
                    "VolumeDown",
                    "Mute",
                    "Unmute",
                    "ToggleMute",
                    "SetVolume",
                    "SetAudioStreamIndex",
                    "SetSubtitleStreamIndex",
                    "SetMaxStreamingBitrate",
                    "DisplayContent",
                    "GoToSearch",
                    "DisplayMessage",
                    "SetRepeatMode"
                ];

                if (apphost.supports('fullscreenchange') && !layoutManager.tv) {
                    list.push('ToggleFullscreen');
                }

                if (player.supports && player.supports('pictureinpicture')) {
                    list.push('PictureInPicture');
                }

                return list;
            }

            throw new Error('player must define supported commands');
        }

        function createTarget(player) {
            return {
                name: player.name,
                id: player.id,
                playerName: player.name,
                playableMediaTypes: ['Audio', 'Video', 'Game'].map(player.canPlayMediaType),
                isLocalPlayer: player.isLocalPlayer,
                supportedCommands: getSupportedCommands(player)
            };
        }

        function getPlayerTargets(player) {
            if (player.getTargets) {
                return player.getTargets();
            }

            return Promise.resolve([createTarget(player)]);
        }

        self.setDefaultPlayerActive = function () {

            self.setActivePlayer('localplayer');
        };

        self.removeActivePlayer = function (name) {

            var playerInfo = self.getPlayerInfo();
            if (playerInfo) {
                if (playerInfo.name === name) {
                    self.setDefaultPlayerActive();
                }
            }
        };

        self.removeActiveTarget = function (id) {

            var playerInfo = self.getPlayerInfo();
            if (playerInfo) {
                if (playerInfo.id === id) {
                    self.setDefaultPlayerActive();
                }
            }
        };

        self.disconnectFromPlayer = function () {

            var playerInfo = self.getPlayerInfo();

            if (!playerInfo) {
                return;
            }

            if (playerInfo.supportedCommands.indexOf('EndSession') !== -1) {

                require(['dialog'], function (dialog) {

                    var menuItems = [];

                    menuItems.push({
                        name: globalize.translate('ButtonYes'),
                        id: 'yes'
                    });
                    menuItems.push({
                        name: globalize.translate('ButtonNo'),
                        id: 'no'
                    });

                    dialog({
                        buttons: menuItems,
                        //positionTo: positionTo,
                        text: globalize.translate('ConfirmEndPlayerSession')

                    }).then(function (id) {
                        switch (id) {

                            case 'yes':
                                self.getCurrentPlayer().endSession();
                                self.setDefaultPlayerActive();
                                break;
                            case 'no':
                                self.setDefaultPlayerActive();
                                break;
                            default:
                                break;
                        }
                    });

                });


            } else {

                self.setDefaultPlayerActive();
            }
        };

        self.getTargets = function () {

            var promises = players.filter(function (p) {
                return !displayPlayerInLocalGroup(p);
            }).map(getPlayerTargets);

            return Promise.all(promises).then(function (responses) {

                var targets = [];

                targets.push({
                    name: globalize.translate('sharedcomponents#HeaderMyDevice'),
                    id: 'localplayer',
                    playerName: 'localplayer',
                    playableMediaTypes: ['Audio', 'Video', 'Game'],
                    isLocalPlayer: true,
                    supportedCommands: getSupportedCommands({
                        isLocalPlayer: true
                    })
                });

                for (var i = 0; i < responses.length; i++) {

                    var subTargets = responses[i];

                    for (var j = 0; j < subTargets.length; j++) {

                        targets.push(subTargets[j]);
                    }

                }

                targets = targets.sort(function (a, b) {

                    var aVal = a.isLocalPlayer ? 0 : 1;
                    var bVal = b.isLocalPlayer ? 0 : 1;

                    aVal = aVal.toString() + a.name;
                    bVal = bVal.toString() + b.name;

                    return aVal.localeCompare(bVal);
                });

                return targets;
            });
        };

        self.displayContent = function (options, player) {
            player = player || currentPlayer;
            if (player && player.displayContent) {
                player.displayContent(options);
            }
        };

        self.sendCommand = function (cmd, player) {

            // Full list
            // https://github.com/MediaBrowser/MediaBrowser/blob/master/MediaBrowser.Model/Session/GeneralCommand.cs#L23
            console.log('MediaController received command: ' + cmd.Name);
            switch (cmd.Name) {

                case 'SetRepeatMode':
                    self.setRepeatMode(cmd.Arguments.RepeatMode, player);
                    break;
                case 'VolumeUp':
                    self.volumeUp(player);
                    break;
                case 'VolumeDown':
                    self.volumeDown(player);
                    break;
                case 'Mute':
                    self.setMute(true, player);
                    break;
                case 'Unmute':
                    self.setMute(false, player);
                    break;
                case 'ToggleMute':
                    self.toggleMute(player);
                    break;
                case 'SetVolume':
                    self.setVolume(cmd.Arguments.Volume, player);
                    break;
                case 'SetAudioStreamIndex':
                    self.setAudioStreamIndex(parseInt(cmd.Arguments.Index), player);
                    break;
                case 'SetSubtitleStreamIndex':
                    self.setSubtitleStreamIndex(parseInt(cmd.Arguments.Index), player);
                    break;
                case 'SetMaxStreamingBitrate':
                    self.setMaxStreamingBitrate(parseInt(cmd.Arguments.Bitrate), player);
                    break;
                case 'ToggleFullscreen':
                    self.toggleFullscreen(player);
                    break;
                default:
                    {
                        if (player.sendCommand) {
                            player.sendCommand(cmd);
                        }
                        break;
                    }
            }
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

        self.getCurrentPlayer = function () {
            return currentPlayer;
        };

        function setCurrentPlayerInternal(player, targetInfo) {

            var previousPlayer = currentPlayer;
            var previousTargetInfo = currentTargetInfo;

            if (player && !targetInfo && player.isLocalPlayer) {
                targetInfo = createTarget(player);
            }

            if (player && !targetInfo) {
                throw new Error('targetInfo cannot be null');
            }

            currentPairingId = null;
            currentPlayer = player;
            currentTargetInfo = targetInfo;

            if (targetInfo) {
                console.log('Active player: ' + JSON.stringify(targetInfo));
            }

            if (player && player.isLocalPlayer) {
                lastLocalPlayer = player;
            }

            if (previousPlayer) {
                self.endPlayerUpdates(previousPlayer);
            }

            if (player) {
                self.beginPlayerUpdates(player);
            }

            triggerPlayerChange(player, targetInfo, previousPlayer, previousTargetInfo);
        }

        self.isPlaying = function (player) {
            player = player || currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.isPlaying();
            }
            return player != null && player.currentSrc() != null;
        };

        self.isPlayingLocally = function (mediaTypes, player) {

            player = player || currentPlayer;

            if (!player || !player.isLocalPlayer) {
                return false;
            }

            var playerData = getPlayerData(player) || {};

            return mediaTypes.indexOf((playerData.streamInfo || {}).mediaType || '') !== -1;
        };

        self.isPlayingVideo = function (player) {

            player = player || currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.isPlayingVideo();
            }

            if (self.isPlaying()) {
                var playerData = getPlayerData(player);

                return playerData.streamInfo.mediaType === 'Video';
            }

            return false;
        };

        self.isPlayingAudio = function (player) {
            player = player || currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.isPlayingAudio();
            }

            if (self.isPlaying()) {
                var playerData = getPlayerData(player);

                return playerData.streamInfo.mediaType === 'Audio';
            }

            return false;
        };

        self.getPlayers = function () {

            return players;
        };

        function getAutomaticPlayers() {

            var player = currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return [player];
            }

            return self.getPlayers().filter(enableLocalPlaylistManagement);
        }

        self.canPlay = function (item) {

            var itemType = item.Type;
            var locationType = item.LocationType;

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

            //var mediaType = item.MediaType;
            return getPlayer(item, {}) != null;
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

        self.isMuted = function (player) {

            player = player || currentPlayer;

            if (player) {
                return player.isMuted();
            }

            return false;
        };

        self.setMute = function (mute, player) {

            player = player || currentPlayer;

            if (player) {
                player.setMute(mute);
            }
        };

        self.toggleMute = function (mute, player) {

            player = player || currentPlayer;
            if (player) {

                if (player.toggleMute) {
                    player.toggleMute();
                } else {
                    player.setMute(!player.isMuted());
                }
            }
        };

        self.setVolume = function (val, player) {

            player = player || currentPlayer;

            if (player) {
                player.setVolume(val);
            }
        };

        self.getVolume = function (player) {

            player = player || currentPlayer;

            if (player) {
                return player.getVolume();
            }
        };

        self.volumeUp = function (player) {

            player = player || currentPlayer;

            if (player) {
                player.volumeUp();
            }
        };

        self.volumeDown = function (player) {

            player = player || currentPlayer;

            if (player) {
                player.volumeDown();
            }
        };

        self.getAudioStreamIndex = function (player) {

            player = player || currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.getAudioStreamIndex();
            }

            return getPlayerData(player).audioStreamIndex;
        };

        self.setAudioStreamIndex = function (index, player) {

            player = player || currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.setAudioStreamIndex(index);
            }

            if (getPlayerData(player).streamInfo.playMethod === 'Transcode' || !player.canSetAudioStreamIndex()) {

                changeStream(player, getCurrentTicks(player), { AudioStreamIndex: index });
                getPlayerData(player).audioStreamIndex = index;

            } else {
                player.setAudioStreamIndex(index);
                getPlayerData(player).audioStreamIndex = index;
            }
        };

        self.getMaxStreamingBitrate = function (player) {

            player = player || currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.getMaxStreamingBitrate();
            }

            return getPlayerData(player).maxStreamingBitrate || appSettings.maxStreamingBitrate();
        };

        self.setMaxStreamingBitrate = function (bitrate, player) {

            player = player || currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.setMaxStreamingBitrate(bitrate);
            }

            if (bitrate) {
                appSettings.enableAutomaticBitrateDetection(false);
            } else {
                appSettings.enableAutomaticBitrateDetection(true);
            }

            appSettings.maxStreamingBitrate(bitrate);

            changeStream(player, getCurrentTicks(player), {
                MaxStreamingBitrate: bitrate
            });
        };

        self.isFullscreen = function (player) {

            player = player || currentPlayer;
            if (!player.isLocalPlayer || player.isFullscreen) {
                return player.isFullscreen();
            }

            return fullscreenManager.isFullScreen();
        };

        self.toggleFullscreen = function (player) {

            player = player || currentPlayer;
            if (!player.isLocalPlayer || player.toggleFulscreen) {
                return player.toggleFulscreen();
            }

            if (fullscreenManager.isFullScreen()) {
                fullscreenManager.exitFullscreen();
            } else {
                fullscreenManager.requestFullscreen();
            }
        };

        self.togglePictureInPicture = function (player) {
            player = player || currentPlayer;
            return player.togglePictureInPicture();
        };

        self.getSubtitleStreamIndex = function (player) {

            player = player || currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.getSubtitleStreamIndex();
            }

            return getPlayerData(player).subtitleStreamIndex;
        };

        self.setSubtitleStreamIndex = function (index, player) {

            player = player || currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.setSubtitleStreamIndex(index);
            }

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
                appSettings.set('displaymirror', val);

                if (enabled) {
                    mirrorIfEnabled();
                }
                return;
            }

            return (appSettings.get('displaymirror') || '') !== '0';
        };

        self.stop = function (player) {

            player = player || currentPlayer;

            if (player) {
                playNextAfterEnded = false;
                // TODO: remove second param
                return player.stop(true, true);
            }

            return Promise.resolve();
        };

        self.playPause = function (player) {

            player = player || currentPlayer;

            if (player) {

                if (player.playPause) {
                    return player.playPause();
                }

                if (player.paused()) {
                    return self.unpause(player);
                } else {
                    return self.pause(player);
                }
            }
        };

        self.paused = function (player) {

            player = player || currentPlayer;

            if (player) {
                return player.paused();
            }
        };

        self.pause = function (player) {
            player = player || currentPlayer;

            if (player) {
                player.pause();
            }
        };

        self.unpause = function (player) {
            player = player || currentPlayer;

            if (player) {
                player.unpause();
            }
        };

        self.seek = function (ticks, player) {

            ticks = Math.max(0, ticks);

            player = player || currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.seek(ticks);
            }

            changeStream(player, ticks);
        };

        self.nextChapter = function () {

            var player = currentPlayer;
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

            var player = currentPlayer;
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

            var player = currentPlayer;

            if (player.fastForward != null) {
                player.fastForward(userSettings.skipForwardLength());
                return;
            }

            var ticks = getCurrentTicks(player);

            // Go back 15 seconds
            ticks += userSettings.skipForwardLength() * 10000;

            var runTimeTicks = self.duration(player) || 0;

            if (ticks < runTimeTicks) {
                self.seek(ticks);
            }
        };

        self.rewind = function () {

            var player = currentPlayer;

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
                return player.duration();
            }
        }

        function changeStream(player, ticks, params) {

            if (canPlayerSeek(player) && params == null) {

                player.currentTime(parseInt(ticks / 10000));
                return;
            }

            params = params || {};

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

                var maxBitrate = params.MaxStreamingBitrate || self.getMaxStreamingBitrate(player);

                getPlaybackInfo(apiClient, currentItem.Id, deviceProfile, maxBitrate, ticks, currentMediaSource, audioStreamIndex, subtitleStreamIndex, liveStreamId).then(function (result) {

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
                            getPlayerData(player).maxStreamingBitrate = maxBitrate;

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

            var ticks = self.duration(player) || 0;

            percent /= 100;
            ticks *= percent;
            self.seek(parseInt(ticks));
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
                            Type: 'Trailer',
                            ServerId: apiClient.serverId()
                        };
                    })
                });
            }
        };

        self.play = function (options) {

            normalizePlayOptions(options);

            if (currentPlayer) {
                if (options.enableRemotePlayers === false && !currentPlayer.isLocalPlayer) {
                    return Promise.reject();
                }

                if (!enableLocalPlaylistManagement(currentPlayer)) {
                    return currentPlayer.play(options);
                }
            }

            if (options.fullscreen) {
                loading.show();
            }

            if (options.items) {

                return translateItemsForPlayback(options.items, options).then(function (items) {

                    return playWithIntros(items, options);
                });

            } else {

                if (!options.serverId) {
                    throw new Error('serverId required!');
                }

                return getItemsForPlayback(options.serverId, {

                    Ids: options.ids.join(',')

                }).then(function (result) {

                    return translateItemsForPlayback(result.Items, options).then(function (items) {

                        return playWithIntros(items, options);
                    });

                });
            }
        };

        self.instantMix = function (item, player) {

            player = player || currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.instantMix(item);
            }

            var apiClient = connectionManager.getApiClient(item.ServerId);

            var options = {};
            options.UserId = apiClient.getCurrentUserId();
            options.Fields = 'MediaSources';

            apiClient.getInstantMixFromItem(item.Id, options).then(function (result) {
                self.play({
                    items: result.Items
                });
            });
        };

        self.shuffle = function (shuffleItem, player) {

            player = player || currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.shuffle(shuffleItem);
            }

            var apiClient = connectionManager.getApiClient(shuffleItem.ServerId);

            apiClient.getItem(apiClient.getCurrentUserId(), shuffleItem.Id).then(function (item) {

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
                    query.ParentId = item.Id;

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

            if (!enableLocalPlaylistManagement(player)) {
                return player.getPlayerState();
            }

            var playerData = getPlayerData(player);
            var streamInfo = playerData.streamInfo;
            var item = streamInfo ? streamInfo.item : null;
            var mediaSource = streamInfo ? streamInfo.mediaSource : null;

            var state = {
                PlayState: {}
            };

            if (player) {

                state.PlayState.VolumeLevel = player.getVolume();
                state.PlayState.IsMuted = player.isMuted();
                state.PlayState.IsPaused = player.paused();
                state.PlayState.RepeatMode = self.getRepeatMode(player);
                state.PlayState.MaxStreamingBitrate = self.getMaxStreamingBitrate(player);

                if (streamInfo) {
                    state.PlayState.PositionTicks = getCurrentTicks(player);

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

            state.MediaSource = mediaSource;

            return Promise.resolve(state);
        };

        self.currentTime = function (player) {

            player = player || currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.currentTime();
            }

            return getCurrentTicks(player);
        };

        self.duration = function (player) {

            player = player || currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.duration();
            }

            var streamInfo = getPlayerData(player).streamInfo;

            if (streamInfo && streamInfo.mediaSource && streamInfo.mediaSource.RunTimeTicks) {
                return streamInfo.mediaSource.RunTimeTicks;
            }

            var playerDuration = player.duration();

            if (playerDuration) {
                playerDuration *= 10000;
            }

            return playerDuration;
        };

        function getCurrentTicks(player) {

            var playerTime = Math.floor(10000 * (player || currentPlayer).currentTime());
            playerTime += getPlayerData(player).streamInfo.transcodingOffsetTicks || 0;

            return playerTime;
        }

        function getNowPlayingItemForReporting(player, item, mediaSource) {

            var nowPlayingItem = Object.assign({}, item);

            if (mediaSource) {
                nowPlayingItem.RunTimeTicks = mediaSource.RunTimeTicks;
            }

            nowPlayingItem.RunTimeTicks = nowPlayingItem.RunTimeTicks || player.duration() * 10000;

            return nowPlayingItem;
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
                setCurrentPlayerInternal(null);

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
                        getPlayerData(player).maxStreamingBitrate = maxBitrate;

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

            var serverItem = isServerItem(item);

            return getAutomaticPlayers().filter(function (p) {

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

        self.setCurrentPlaylistIndex = function (i) {

            var newItem = playlist[i];

            var playOptions = Object.assign({}, currentPlayOptions, {
                startPositionTicks: 0
            });

            playInternal(newItem, playOptions, function () {
                currentPlaylistIndex = i;
            });
        };

        self.getCurrentPlaylistIndex = function (i) {

            return currentPlaylistIndex;
        };

        self.setRepeatMode = function (value, player) {

            player = player || currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.setRepeatMode(value);
            }

            repeatMode = value;
            events.trigger(self, 'repeatmodechange');
        };

        self.getRepeatMode = function (player) {

            player = player || currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.getRepeatMode();
            }

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

        self.nextTrack = function (player) {

            player = player || currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.nextTrack();
            }

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

        self.previousTrack = function (player) {

            player = player || currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.previousTrack();
            }

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

        self.queue = function (options, player) {
            queue(options, '', player);
        };

        self.queueNext = function (options, player) {
            queue(options, 'next', player);
        };

        function queue(options, mode, player) {

            player = player || currentPlayer;

            if (!player) {
                return self.play(options);
            }

            if (!enableLocalPlaylistManagement(player)) {

                if (mode === 'next') {
                    return player.queueNext(item);
                }
                return player.queue(item);
            }

            if (options.items) {

                return translateItemsForPlayback(options.items, options).then(function (items) {

                    queueAll(items, mode);
                });

            } else {

                if (!options.serverId) {
                    throw new Error('serverId required!');
                }

                return getItemsForPlayback(options.serverId, {

                    Ids: options.ids.join(',')

                }).then(function (result) {

                    return translateItemsForPlayback(result.Items, options).then(function (items) {
                        queueAll(items, mode);
                    });
                });
            }
        }

        function queueAll(items, mode) {
            for (var i = 0, length = items.length; i < length; i++) {
                playlist.push(items[i]);
            }
        }

        function onPlaybackStarted(player, streamInfo, mediaSource) {

            setCurrentPlayerInternal(player);
            getPlayerData(player).streamInfo = streamInfo;

            if (mediaSource) {
                getPlayerData(player).audioStreamIndex = mediaSource.DefaultAudioStreamIndex;
                getPlayerData(player).subtitleStreamIndex = mediaSource.DefaultSubtitleStreamIndex;
            } else {
                getPlayerData(player).audioStreamIndex = null;
                getPlayerData(player).subtitleStreamIndex = null;
            }

            playNextAfterEnded = true;

            self.getPlayerState(player).then(function (state) {

                reportPlayback(state, getPlayerData(player).streamInfo.item.ServerId, 'reportPlaybackStart');

                startProgressInterval(player);

                events.trigger(player, 'playbackstart', [state]);
                events.trigger(self, 'playbackstart', [player, state]);
            });
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
            self.getPlayerState(player).then(function (state) {

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

                state.nextMediaType = nextMediaType;
                state.nextItem = playbackStopInfo.nextItem;

                if (!nextItem) {
                    playlist = [];
                    currentPlaylistIndex = -1;
                }

                events.trigger(player, 'playbackstop', [state]);
                events.trigger(self, 'playbackstop', [playbackStopInfo]);

                var newPlayer = nextItem ? getPlayer(nextItem.item, currentPlayOptions) : null;

                if (newPlayer !== player) {
                    player.destroy();
                    setCurrentPlayerInternal(null);
                }

                if (nextItem) {
                    self.nextTrack();
                }
            });
        }

        function onPlaybackChanging(activePlayer, newPlayer, newItem) {

            return self.getPlayerState(activePlayer).then(function (state) {
                var serverId = getPlayerData(activePlayer).streamInfo.item.ServerId;

                // User started playing something new while existing content is playing
                var promise;

                unbindStopped(activePlayer);

                if (activePlayer === newPlayer) {

                    // If we're staying with the same player, stop it
                    // TODO: remove second param
                    promise = activePlayer.stop(false, true);

                } else {

                    // If we're switching players, tear down the current one
                    // TODO: remove second param
                    promise = activePlayer.stop(true, true);
                }

                return promise.then(function () {

                    bindStopped(activePlayer);

                    reportPlayback(state, serverId, 'reportPlaybackStopped');

                    clearProgressInterval(activePlayer);

                    events.trigger(self, 'playbackstop', [{
                        player: activePlayer,
                        state: state,
                        nextItem: newItem,
                        nextMediaType: newItem.MediaType
                    }]);
                });
            });
        }

        function bindStopped(player) {

            if (enableLocalPlaylistManagement(player)) {
                events.off(player, 'stopped', onPlaybackStopped);
                events.on(player, 'stopped', onPlaybackStopped);
            }
        }

        function unbindStopped(player) {

            events.off(player, 'stopped', onPlaybackStopped);
        }

        function initLegacyVolumeMethods(player) {
            player.getVolume = function () {
                return player.volume();
            };
            player.setVolume = function (val) {
                return player.volume(val);
            };
        }

        function initMediaPlayer(player) {

            players.push(player);
            players.sort(function (a, b) {

                return (a.priority || 0) - (b.priority || 0);
            });

            if (player.isLocalPlayer !== false) {
                player.isLocalPlayer = true;
            }

            player.currentState = {};

            if (!player.getVolume || !player.setVolume) {
                initLegacyVolumeMethods(player);
            }

            if (enableLocalPlaylistManagement(player)) {
                events.on(player, 'error', onPlaybackError);
            }

            if (player.isLocalPlayer) {
                bindToFullscreenChange(player);
            }
            bindStopped(player);
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

            self.getPlayerState(player).then(function (state) {
                var currentItem = getPlayerData(player).streamInfo.item;
                reportPlayback(state, currentItem.ServerId, 'reportPlaybackProgress');
            });
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

    return new PlaybackManager();
});
