define(['events', 'datetime', 'appSettings', 'itemHelper', 'pluginManager', 'playQueueManager', 'userSettings', 'globalize', 'connectionManager', 'loading', 'apphost', 'fullscreenManager'], function (events, datetime, appSettings, itemHelper, pluginManager, PlayQueueManager, userSettings, globalize, connectionManager, loading, apphost, fullscreenManager) {
    'use strict';

    function enableLocalPlaylistManagement(player) {

        if (player.getPlaylist) {

            return false;
        }

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

    function triggerPlayerChange(playbackManagerInstance, newPlayer, newTarget, previousPlayer, previousTargetInfo) {

        if (!newPlayer && !previousPlayer) {
            return;
        }

        if (newTarget && previousTargetInfo) {

            if (newTarget.id === previousTargetInfo.id) {
                return;
            }
        }

        events.trigger(playbackManagerInstance, 'playerchange', [newPlayer, newTarget, previousPlayer]);
    }

    function reportPlayback(playbackManagerInstance, state, player, reportPlaylist, serverId, method, progressEventName) {

        if (!serverId) {
            // Not a server item
            // We can expand on this later and possibly report them
            return;
        }

        var info = Object.assign({}, state.PlayState);
        info.ItemId = state.NowPlayingItem.Id;

        if (progressEventName) {
            info.EventName = progressEventName;
        }

        if (reportPlaylist) {
            addPlaylistToPlaybackReport(playbackManagerInstance, info, player, serverId);
        }

        //console.log(method + '-' + JSON.stringify(info));
        var apiClient = connectionManager.getApiClient(serverId);
        apiClient[method](info);
    }

    function getPlaylistSync(playbackManagerInstance, player) {
        player = player || playbackManagerInstance._currentPlayer;
        if (player && !enableLocalPlaylistManagement(player)) {
            return player.getPlaylistSync();
        }

        return playbackManagerInstance._playQueueManager.getPlaylist();
    }

    function addPlaylistToPlaybackReport(playbackManagerInstance, info, player, serverId) {

        info.NowPlayingQueue = getPlaylistSync(playbackManagerInstance, player).map(function (i) {

            var itemInfo = {
                Id: i.Id,
                PlaylistItemId: i.PlaylistItemId
            };

            if (i.ServerId !== serverId) {
                itemInfo.ServerId = i.ServerId;
            }

            return itemInfo;
        });
    }

    function normalizeName(t) {
        return t.toLowerCase().replace(' ', '');
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

            query.Limit = query.Limit || 300;
            query.Fields = "Chapters";
            query.ExcludeLocationTypes = "Virtual";
            query.EnableTotalRecordCount = false;
            query.CollapseBoxSetItems = false;

            return apiClient.getItems(apiClient.getCurrentUserId(), query);
        }
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

    function mergePlaybackQueries(obj1, obj2) {

        var query = Object.assign(obj1, obj2);

        var filters = query.Filters ? query.Filters.split(',') : [];
        if (filters.indexOf('IsNotFolder') === -1) {
            filters.push('IsNotFolder');
        }
        query.Filters = filters.join(',');
        return query;
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

    function isAutomaticPlayer(player) {

        if (player.isLocalPlayer) {
            return true;
        }

        return false;
    }

    function getAutomaticPlayers(instance, forceLocalPlayer) {

        if (!forceLocalPlayer) {
            var player = instance._currentPlayer;
            if (player && !isAutomaticPlayer(player)) {
                return [player];
            }
        }

        return instance.getPlayers().filter(isAutomaticPlayer);
    }

    function isServerItem(item) {
        if (!item.Id) {
            return false;
        }
        return true;
    }

    function enableIntros(item) {

        if (item.MediaType !== 'Video') {
            return false;
        }
        if (item.Type === 'TvChannel') {
            return false;
        }
        // disable for in-progress recordings
        if (item.Status === 'InProgress') {
            return false;
        }

        return isServerItem(item);
    }

    function getIntros(firstItem, apiClient, options) {

        if (options.startPositionTicks || options.startIndex || options.fullscreen === false || !enableIntros(firstItem) || !userSettings.enableCinemaMode()) {
            return Promise.resolve({
                Items: []
            });
        }

        return apiClient.getIntros(firstItem.Id).then(function (result) {

            return result;

        }, function (err) {

            return Promise.resolve({
                Items: []
            });
        });
    }

    function getAudioMaxValues(deviceProfile) {

        // TODO - this could vary per codec and should be done on the server using the entire profile
        var maxAudioSampleRate = null;
        var maxAudioBitDepth = null;
        var maxAudioBitrate = null;

        deviceProfile.CodecProfiles.map(function (codecProfile) {

            if (codecProfile.Type === 'Audio') {
                (codecProfile.Conditions || []).map(function (condition) {
                    if (condition.Condition === 'LessThanEqual' && condition.Property === 'AudioBitDepth') {
                        maxAudioBitDepth = condition.Value;
                    }
                    if (condition.Condition === 'LessThanEqual' && condition.Property === 'AudioSampleRate') {
                        maxAudioSampleRate = condition.Value;
                    }
                    if (condition.Condition === 'LessThanEqual' && condition.Property === 'AudioBitrate') {
                        maxAudioBitrate = condition.Value;
                    }
                });
            }
        });

        return {
            maxAudioSampleRate: maxAudioSampleRate,
            maxAudioBitDepth: maxAudioBitDepth,
            maxAudioBitrate: maxAudioBitrate
        };
    }

    var startingPlaySession = new Date().getTime();
    function getAudioStreamUrl(item, transcodingProfile, directPlayContainers, maxBitrate, apiClient, maxAudioSampleRate, maxAudioBitDepth, maxAudioBitrate, startPosition) {

        var url = 'Audio/' + item.Id + '/universal';

        startingPlaySession++;
        return apiClient.getUrl(url, {
            UserId: apiClient.getCurrentUserId(),
            DeviceId: apiClient.deviceId(),
            MaxStreamingBitrate: maxAudioBitrate || maxBitrate,
            Container: directPlayContainers,
            TranscodingContainer: transcodingProfile.Container || null,
            TranscodingProtocol: transcodingProfile.Protocol || null,
            AudioCodec: transcodingProfile.AudioCodec,
            MaxAudioSampleRate: maxAudioSampleRate,
            MaxAudioBitDepth: maxAudioBitDepth,
            api_key: apiClient.accessToken(),
            PlaySessionId: startingPlaySession,
            StartTimeTicks: startPosition || 0,
            EnableRedirection: true,
            EnableRemoteMedia: apphost.supports('remoteaudio')
        });
    }

    function getAudioStreamUrlFromDeviceProfile(item, deviceProfile, maxBitrate, apiClient, startPosition) {

        var transcodingProfile = deviceProfile.TranscodingProfiles.filter(function (p) {
            return p.Type === 'Audio' && p.Context === 'Streaming';
        })[0];

        var directPlayContainers = '';

        deviceProfile.DirectPlayProfiles.map(function (p) {

            if (p.Type === 'Audio') {
                if (directPlayContainers) {
                    directPlayContainers += ',' + p.Container;
                } else {
                    directPlayContainers = p.Container;
                }

                if (p.AudioCodec) {
                    directPlayContainers += '|' + p.AudioCodec;
                }
            }

        });

        var maxValues = getAudioMaxValues(deviceProfile);

        return getAudioStreamUrl(item, transcodingProfile, directPlayContainers, maxBitrate, apiClient, maxValues.maxAudioSampleRate, maxValues.maxAudioBitDepth, maxValues.maxAudioBitrate, startPosition);
    }

    function getStreamUrls(items, deviceProfile, maxBitrate, apiClient, startPosition) {

        var audioTranscodingProfile = deviceProfile.TranscodingProfiles.filter(function (p) {
            return p.Type === 'Audio' && p.Context === 'Streaming';
        })[0];

        var audioDirectPlayContainers = '';

        deviceProfile.DirectPlayProfiles.map(function (p) {

            if (p.Type === 'Audio') {
                if (audioDirectPlayContainers) {
                    audioDirectPlayContainers += ',' + p.Container;
                } else {
                    audioDirectPlayContainers = p.Container;
                }

                if (p.AudioCodec) {
                    audioDirectPlayContainers += '|' + p.AudioCodec;
                }
            }
        });

        var maxValues = getAudioMaxValues(deviceProfile);

        var streamUrls = [];

        for (var i = 0, length = items.length; i < length; i++) {

            var item = items[i];
            var streamUrl;

            if (item.MediaType === 'Audio' && !itemHelper.isLocalItem(item)) {
                streamUrl = getAudioStreamUrl(item, audioTranscodingProfile, audioDirectPlayContainers, maxBitrate, apiClient, maxValues.maxAudioSampleRate, maxValues.maxAudioBitDepth, maxValues.maxAudioBitrate, startPosition);
            }

            streamUrls.push(streamUrl || '');

            if (i === 0) {
                startPosition = 0;
            }
        }

        return Promise.resolve(streamUrls);
    }

    function setStreamUrls(items, deviceProfile, maxBitrate, apiClient, startPosition) {

        return getStreamUrls(items, deviceProfile, maxBitrate, apiClient, startPosition).then(function (streamUrls) {

            for (var i = 0, length = items.length; i < length; i++) {

                var item = items[i];
                var streamUrl = streamUrls[i];

                if (streamUrl) {
                    item.PresetMediaSource = {
                        StreamUrl: streamUrl,
                        Id: item.Id,
                        MediaStreams: [],
                        RunTimeTicks: item.RunTimeTicks
                    };
                }
            }
        });
    }

    function getPlaybackInfo(player,
        apiClient,
        item,
        deviceProfile,
        maxBitrate,
        startPosition,
        isPlayback,
        mediaSourceId,
        audioStreamIndex,
        subtitleStreamIndex,
        liveStreamId,
        enableDirectPlay,
        enableDirectStream,
        allowVideoStreamCopy,
        allowAudioStreamCopy) {

        if (!itemHelper.isLocalItem(item) && item.MediaType === 'Audio') {

            return Promise.resolve({
                MediaSources: [
                    {
                        StreamUrl: getAudioStreamUrlFromDeviceProfile(item, deviceProfile, maxBitrate, apiClient, startPosition),
                        Id: item.Id,
                        MediaStreams: [],
                        RunTimeTicks: item.RunTimeTicks
                    }]
            });
        }

        if (item.PresetMediaSource) {
            return Promise.resolve({
                MediaSources: [item.PresetMediaSource]
            });
        }

        var itemId = item.Id;

        var query = {
            UserId: apiClient.getCurrentUserId(),
            StartTimeTicks: startPosition || 0
        };

        if (isPlayback) {
            query.IsPlayback = true;
            query.AutoOpenLiveStream = true;
        } else {
            query.IsPlayback = false;
            query.AutoOpenLiveStream = false;
        }

        if (audioStreamIndex != null) {
            query.AudioStreamIndex = audioStreamIndex;
        }
        if (subtitleStreamIndex != null) {
            query.SubtitleStreamIndex = subtitleStreamIndex;
        }
        if (enableDirectPlay != null) {
            query.EnableDirectPlay = enableDirectPlay;
        }

        if (enableDirectStream != null) {
            query.EnableDirectStream = enableDirectStream;
        }
        if (allowVideoStreamCopy != null) {
            query.AllowVideoStreamCopy = allowVideoStreamCopy;
        }
        if (allowAudioStreamCopy != null) {
            query.AllowAudioStreamCopy = allowAudioStreamCopy;
        }
        if (mediaSourceId) {
            query.MediaSourceId = mediaSourceId;
        }
        if (liveStreamId) {
            query.LiveStreamId = liveStreamId;
        }
        if (maxBitrate) {
            query.MaxStreamingBitrate = maxBitrate;
        }
        if (player.enableMediaProbe && !player.enableMediaProbe(item)) {
            query.EnableMediaProbe = false;
        }

        // lastly, enforce player overrides for special situations
        if (query.EnableDirectStream !== false) {
            if (player.supportsPlayMethod && !player.supportsPlayMethod('DirectStream', item)) {
                query.EnableDirectStream = false;
            }
        }

        if (player.getDirectPlayProtocols) {
            query.DirectPlayProtocols = player.getDirectPlayProtocols();
        }

        return apiClient.getPlaybackInfo(itemId, query, deviceProfile);
    }

    function getOptimalMediaSource(apiClient, item, versions) {

        var promises = versions.map(function (v) {
            return supportsDirectPlay(apiClient, item, v);
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

            return optimalVersion || versions[0];
        });
    }

    function getLiveStream(player, apiClient, item, playSessionId, deviceProfile, maxBitrate, startPosition, mediaSource, audioStreamIndex, subtitleStreamIndex) {

        var postData = {
            DeviceProfile: deviceProfile,
            OpenToken: mediaSource.OpenToken
        };

        var query = {
            UserId: apiClient.getCurrentUserId(),
            StartTimeTicks: startPosition || 0,
            ItemId: item.Id,
            PlaySessionId: playSessionId
        };

        if (maxBitrate) {
            query.MaxStreamingBitrate = maxBitrate;
        }
        if (audioStreamIndex != null) {
            query.AudioStreamIndex = audioStreamIndex;
        }
        if (subtitleStreamIndex != null) {
            query.SubtitleStreamIndex = subtitleStreamIndex;
        }

        // lastly, enforce player overrides for special situations
        if (query.EnableDirectStream !== false) {
            if (player.supportsPlayMethod && !player.supportsPlayMethod('DirectStream', item)) {
                query.EnableDirectStream = false;
            }
        }

        return apiClient.ajax({
            url: apiClient.getUrl('LiveStreams/Open', query),
            type: 'POST',
            data: JSON.stringify(postData),
            contentType: "application/json",
            dataType: "json"

        });
    }

    function isHostReachable(mediaSource, apiClient) {

        if (mediaSource.IsRemote) {
            return Promise.resolve(true);
        }

        return apiClient.getEndpointInfo().then(function (endpointInfo) {

            if (endpointInfo.IsInNetwork) {

                if (!endpointInfo.IsLocal) {
                    var path = (mediaSource.Path || '').toLowerCase();
                    if (path.indexOf('localhost') !== -1 || path.indexOf('127.0.0.1') !== -1) {
                        // This will only work if the app is on the same machine as the server
                        return Promise.resolve(false);
                    }
                }

                return Promise.resolve(true);
            }

            // media source is in network, but connection is out of network
            return Promise.resolve(false);
        });
    }

    function supportsDirectPlay(apiClient, item, mediaSource) {

        // folder rip hacks due to not yet being supported by the stream building engine
        var isFolderRip = mediaSource.VideoType === 'BluRay' || mediaSource.VideoType === 'Dvd' || mediaSource.VideoType === 'HdDvd';

        if (mediaSource.SupportsDirectPlay || isFolderRip) {

            if (mediaSource.IsRemote && !apphost.supports('remotevideo')) {
                return Promise.resolve(false);
            }

            if (mediaSource.Protocol === 'Http' && !mediaSource.RequiredHttpHeaders.length) {

                // If this is the only way it can be played, then allow it
                if (!mediaSource.SupportsDirectStream && !mediaSource.SupportsTranscoding) {
                    return Promise.resolve(true);
                }
                else {
                    return isHostReachable(mediaSource, apiClient);
                }
            }

            else if (mediaSource.Protocol === 'File') {

                return new Promise(function (resolve, reject) {

                    // Determine if the file can be accessed directly
                    require(['filesystem'], function (filesystem) {

                        var method = isFolderRip ?
                            'directoryExists' :
                            'fileExists';

                        filesystem[method](mediaSource.Path).then(function () {
                            resolve(true);
                        }, function () {
                            resolve(false);
                        });

                    });
                });
            }
        }

        return Promise.resolve(false);
    }

    function validatePlaybackInfoResult(instance, result) {

        if (result.ErrorCode) {

            showPlaybackInfoErrorMessage(instance, result.ErrorCode);
            return false;
        }

        return true;
    }

    function showPlaybackInfoErrorMessage(instance, errorCode, playNextTrack) {

        require(['alert'], function (alert) {
            alert({
                text: globalize.translate('sharedcomponents#PlaybackError' + errorCode),
                title: globalize.translate('sharedcomponents#HeaderPlaybackError')
            }).then(function () {

                if (playNextTrack) {
                    instance.nextTrack();
                }
            });
        });
    }

    function normalizePlayOptions(playOptions) {
        playOptions.fullscreen = playOptions.fullscreen !== false;
    }

    function truncatePlayOptions(playOptions) {

        return {
            fullscreen: playOptions.fullscreen,
            mediaSourceId: playOptions.mediaSourceId,
            audioStreamIndex: playOptions.audioStreamIndex,
            subtitleStreamIndex: playOptions.subtitleStreamIndex,
            startPositionTicks: playOptions.startPositionTicks
        };
    }

    function getNowPlayingItemForReporting(player, item, mediaSource) {

        var nowPlayingItem = Object.assign({}, item);

        if (mediaSource) {
            nowPlayingItem.RunTimeTicks = mediaSource.RunTimeTicks;
            nowPlayingItem.MediaStreams = mediaSource.MediaStreams;

            // not needed
            nowPlayingItem.MediaSources = null;
        }

        nowPlayingItem.RunTimeTicks = nowPlayingItem.RunTimeTicks || player.duration() * 10000;

        return nowPlayingItem;
    }

    function displayPlayerIndividually(player) {

        return !player.isLocalPlayer;
    }

    function createTarget(instance, player) {
        return {
            name: player.name,
            id: player.id,
            playerName: player.name,
            playableMediaTypes: ['Audio', 'Video', 'Game', 'Photo', 'Book'].map(player.canPlayMediaType),
            isLocalPlayer: player.isLocalPlayer,
            supportedCommands: instance.getSupportedCommands(player)
        };
    }

    function getPlayerTargets(player) {
        if (player.getTargets) {
            return player.getTargets();
        }

        return Promise.resolve([createTarget(player)]);
    }

    function sortPlayerTargets(a, b) {

        var aVal = a.isLocalPlayer ? 0 : 1;
        var bVal = b.isLocalPlayer ? 0 : 1;

        aVal = aVal.toString() + a.name;
        bVal = bVal.toString() + b.name;

        return aVal.localeCompare(bVal);
    }

    function PlaybackManager() {

        var self = this;

        var players = [];
        var currentTargetInfo;
        var lastLocalPlayer;
        var currentPairingId = null;

        this._playNextAfterEnded = true;
        var playerStates = {};

        this._playQueueManager = new PlayQueueManager();

        self.currentItem = function (player) {

            if (!player) {
                throw new Error('player cannot be null');
            }

            if (player.currentItem) {
                return player.currentItem();
            }

            var data = getPlayerData(player);
            return data.streamInfo ? data.streamInfo.item : null;
        };

        self.currentMediaSource = function (player) {

            if (!player) {
                throw new Error('player cannot be null');
            }

            if (player.currentMediaSource) {
                return player.currentMediaSource();
            }

            var data = getPlayerData(player);
            return data.streamInfo ? data.streamInfo.mediaSource : null;
        };

        self.playMethod = function (player) {

            if (!player) {
                throw new Error('player cannot be null');
            }

            if (player.playMethod) {
                return player.playMethod();
            }

            var data = getPlayerData(player);
            return data.streamInfo ? data.streamInfo.playMethod : null;
        };

        self.playSessionId = function (player) {

            if (!player) {
                throw new Error('player cannot be null');
            }

            if (player.playSessionId) {
                return player.playSessionId();
            }

            var data = getPlayerData(player);
            return data.streamInfo ? data.streamInfo.playSessionId : null;
        };

        self.getPlayerInfo = function () {

            var player = self._currentPlayer;

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
                if (self._currentPlayer && self._currentPlayer.isLocalPlayer) {
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

        self.trySetActivePlayer = function (player, targetInfo) {

            if (player === 'localplayer' || player.name === 'localplayer') {
                if (self._currentPlayer && self._currentPlayer.isLocalPlayer) {
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

            events.trigger(self, 'pairing');

            promise.then(function () {

                events.trigger(self, 'paired');

                setCurrentPlayerInternal(player, targetInfo);
            }, function () {

                events.trigger(self, 'pairerror');

                if (currentPairingId === targetInfo.id) {
                    currentPairingId = null;
                }
            });
        };

        self.getTargets = function () {

            var promises = players.filter(displayPlayerIndividually).map(getPlayerTargets);

            return Promise.all(promises).then(function (responses) {

                return connectionManager.currentApiClient().getCurrentUser().then(function (user) {

                    var targets = [];

                    targets.push({
                        name: globalize.translate('sharedcomponents#HeaderMyDevice'),
                        id: 'localplayer',
                        playerName: 'localplayer',
                        playableMediaTypes: ['Audio', 'Video', 'Game', 'Photo', 'Book'],
                        isLocalPlayer: true,
                        supportedCommands: self.getSupportedCommands({
                            isLocalPlayer: true
                        }),
                        user: user
                    });

                    for (var i = 0; i < responses.length; i++) {

                        var subTargets = responses[i];

                        for (var j = 0; j < subTargets.length; j++) {

                            targets.push(subTargets[j]);
                        }
                    }

                    targets = targets.sort(sortPlayerTargets);

                    return targets;
                });
            });
        };

        function getCurrentSubtitleStream(player) {

            if (!player) {
                throw new Error('player cannot be null');
            }

            var index = getPlayerData(player).subtitleStreamIndex;

            if (index == null || index === -1) {
                return null;
            }

            return getSubtitleStream(player, index);
        }

        function getSubtitleStream(player, index) {
            return self.subtitleTracks(player).filter(function (s) {
                return s.Type === 'Subtitle' && s.Index === index;
            })[0];
        }

        self.getPlaylist = function (player) {

            player = player || self._currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {

                if (player.getPlaylistSync) {
                    return Promise.resolve(player.getPlaylistSync());
                }

                return player.getPlaylist();
            }

            return Promise.resolve(self._playQueueManager.getPlaylist());
        };

        function removeCurrentPlayer(player) {
            var previousPlayer = self._currentPlayer;

            if (!previousPlayer || player.id === previousPlayer.id) {
                setCurrentPlayerInternal(null);
            }
        }

        function setCurrentPlayerInternal(player, targetInfo) {

            var previousPlayer = self._currentPlayer;
            var previousTargetInfo = currentTargetInfo;

            if (player && !targetInfo && player.isLocalPlayer) {
                targetInfo = createTarget(self, player);
            }

            if (player && !targetInfo) {
                throw new Error('targetInfo cannot be null');
            }

            currentPairingId = null;
            self._currentPlayer = player;
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

            triggerPlayerChange(self, player, targetInfo, previousPlayer, previousTargetInfo);
        }

        self.isPlaying = function (player) {

            player = player || self._currentPlayer;

            if (player) {
                if (player.isPlaying) {
                    return player.isPlaying();
                }
            }

            return player != null && player.currentSrc() != null;
        };

        self.isPlayingMediaType = function (mediaType, player) {
            player = player || self._currentPlayer;

            if (player) {
                if (player.isPlaying) {
                    return player.isPlaying(mediaType);
                }
            }

            if (self.isPlaying(player)) {
                var playerData = getPlayerData(player);

                return playerData.streamInfo.mediaType === mediaType;
            }

            return false;
        };

        self.isPlayingLocally = function (mediaTypes, player) {

            player = player || self._currentPlayer;

            if (!player || !player.isLocalPlayer) {
                return false;
            }

            return mediaTypes.filter(function (mediaType) {

                return self.isPlayingMediaType(mediaType, player);

            }).length > 0;
        };

        self.isPlayingVideo = function (player) {
            return self.isPlayingMediaType('Video', player);
        };

        self.isPlayingAudio = function (player) {
            return self.isPlayingMediaType('Audio', player);
        };

        self.getPlayers = function () {

            return players;
        };

        function getDefaultPlayOptions() {
            return {
                fullscreen: true
            };
        }

        self.canPlay = function (item) {

            var itemType = item.Type;

            if (itemType === "PhotoAlbum" || itemType === "MusicGenre" || itemType === "Season" || itemType === "Series" || itemType === "BoxSet" || itemType === "MusicAlbum" || itemType === "MusicArtist" || itemType === "Playlist") {
                return true;
            }

            if (item.LocationType === "Virtual") {
                if (itemType !== "Program") {
                    return false;
                }
            }

            if (itemType === "Program") {

                if (!item.EndDate || !item.StartDate) {
                    return false;
                }

                if (new Date().getTime() > datetime.parseISO8601Date(item.EndDate).getTime() || new Date().getTime() < datetime.parseISO8601Date(item.StartDate).getTime()) {
                    return false;
                }
            }

            //var mediaType = item.MediaType;
            return getPlayer(item, getDefaultPlayOptions()) != null;
        };

        self.toggleAspectRatio = function (player) {

            player = player || self._currentPlayer;

            if (player) {
                var current = self.getAspectRatio(player);

                var supported = self.getSupportedAspectRatios(player);

                var index = -1;
                for (var i = 0, length = supported.length; i < length; i++) {
                    if (supported[i].id === current) {
                        index = i;
                        break;
                    }
                }

                index++;
                if (index >= supported.length) {
                    index = 0;
                }

                self.setAspectRatio(supported[index].id, player);
            }
        };

        self.setAspectRatio = function (val, player) {

            player = player || self._currentPlayer;

            if (player && player.setAspectRatio) {

                player.setAspectRatio(val);
            }
        };

        self.getSupportedAspectRatios = function (player) {

            player = player || self._currentPlayer;

            if (player && player.getSupportedAspectRatios) {
                return player.getSupportedAspectRatios();
            }

            return [];
        };

        self.getAspectRatio = function (player) {

            player = player || self._currentPlayer;

            if (player && player.getAspectRatio) {
                return player.getAspectRatio();
            }
        };

        var brightnessOsdLoaded;
        self.setBrightness = function (val, player) {

            player = player || self._currentPlayer;

            if (player) {

                if (!brightnessOsdLoaded) {
                    brightnessOsdLoaded = true;
                    // TODO: Have this trigger an event instead to get the osd out of here
                    require(['brightnessOsd']);
                }
                player.setBrightness(val);
            }
        };

        self.getBrightness = function (player) {

            player = player || self._currentPlayer;

            if (player) {
                return player.getBrightness();
            }
        };

        self.setVolume = function (val, player) {

            player = player || self._currentPlayer;

            if (player) {
                player.setVolume(val);
            }
        };

        self.getVolume = function (player) {

            player = player || self._currentPlayer;

            if (player) {
                return player.getVolume();
            }
        };

        self.volumeUp = function (player) {

            player = player || self._currentPlayer;

            if (player) {
                player.volumeUp();
            }
        };

        self.volumeDown = function (player) {

            player = player || self._currentPlayer;

            if (player) {
                player.volumeDown();
            }
        };

        self.changeAudioStream = function (player) {

            player = player || self._currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.changeAudioStream();
            }

            if (!player) {
                return;
            }

            var currentMediaSource = self.currentMediaSource(player);
            var mediaStreams = [];
            var i, length;
            for (i = 0, length = currentMediaSource.MediaStreams.length; i < length; i++) {
                if (currentMediaSource.MediaStreams[i].Type === 'Audio') {
                    mediaStreams.push(currentMediaSource.MediaStreams[i]);
                }
            }

            // Nothing to change
            if (mediaStreams.length <= 1) {
                return;
            }

            var currentStreamIndex = self.getAudioStreamIndex(player);
            var indexInList = -1;
            for (i = 0, length = mediaStreams.length; i < length; i++) {
                if (mediaStreams[i].Index === currentStreamIndex) {
                    indexInList = i;
                    break;
                }
            }

            var nextIndex = indexInList + 1;
            if (nextIndex >= mediaStreams.length) {
                nextIndex = 0;
            }

            nextIndex = nextIndex === -1 ? -1 : mediaStreams[nextIndex].Index;

            self.setAudioStreamIndex(nextIndex, player);
        };

        self.changeSubtitleStream = function (player) {

            player = player || self._currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.changeSubtitleStream();
            }

            if (!player) {
                return;
            }

            var currentMediaSource = self.currentMediaSource(player);
            var mediaStreams = [];
            var i, length;
            for (i = 0, length = currentMediaSource.MediaStreams.length; i < length; i++) {
                if (currentMediaSource.MediaStreams[i].Type === 'Subtitle') {
                    mediaStreams.push(currentMediaSource.MediaStreams[i]);
                }
            }

            // No known streams, nothing to change
            if (!mediaStreams.length) {
                return;
            }

            var currentStreamIndex = self.getSubtitleStreamIndex(player);
            var indexInList = -1;
            for (i = 0, length = mediaStreams.length; i < length; i++) {
                if (mediaStreams[i].Index === currentStreamIndex) {
                    indexInList = i;
                    break;
                }
            }

            var nextIndex = indexInList + 1;
            if (nextIndex >= mediaStreams.length) {
                nextIndex = -1;
            }

            nextIndex = nextIndex === -1 ? -1 : mediaStreams[nextIndex].Index;

            self.setSubtitleStreamIndex(nextIndex, player);
        };

        self.getAudioStreamIndex = function (player) {

            player = player || self._currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.getAudioStreamIndex();
            }

            return getPlayerData(player).audioStreamIndex;
        };

        function isAudioStreamSupported(mediaSource, index, deviceProfile) {

            var mediaStream;
            var i, length;
            var mediaStreams = mediaSource.MediaStreams;

            for (i = 0, length = mediaStreams.length; i < length; i++) {
                if (mediaStreams[i].Type === 'Audio' && mediaStreams[i].Index === index) {
                    mediaStream = mediaStreams[i];
                    break;
                }
            }

            if (!mediaStream) {
                return false;
            }

            var codec = (mediaStream.Codec || '').toLowerCase();

            if (!codec) {
                return false;
            }

            var profiles = deviceProfile.DirectPlayProfiles || [];

            return profiles.filter(function (p) {

                if (p.Type === 'Video') {

                    if (!p.AudioCodec) {
                        return true;
                    }

                    // This is an exclusion filter
                    if (p.AudioCodec.indexOf('-') === 0) {
                        return p.AudioCodec.toLowerCase().indexOf(codec) === -1;
                    }

                    return p.AudioCodec.toLowerCase().indexOf(codec) !== -1;
                }

                return false;

            }).length > 0;
        }

        self.setAudioStreamIndex = function (index, player) {

            player = player || self._currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.setAudioStreamIndex(index);
            }

            if (self.playMethod(player) === 'Transcode' || !player.canSetAudioStreamIndex()) {

                changeStream(player, getCurrentTicks(player), { AudioStreamIndex: index });
                getPlayerData(player).audioStreamIndex = index;

            } else {

                // See if the player supports the track without transcoding
                player.getDeviceProfile(self.currentItem(player)).then(function (profile) {

                    if (isAudioStreamSupported(self.currentMediaSource(player), index, profile)) {
                        player.setAudioStreamIndex(index);
                        getPlayerData(player).audioStreamIndex = index;
                    }
                    else {
                        changeStream(player, getCurrentTicks(player), { AudioStreamIndex: index });
                        getPlayerData(player).audioStreamIndex = index;
                    }
                });
            }
        };

        function getSavedMaxStreamingBitrate(apiClient, mediaType) {

            if (!apiClient) {
                // This should hopefully never happen
                apiClient = connectionManager.currentApiClient();
            }

            var endpointInfo = apiClient.getSavedEndpointInfo() || {};

            return appSettings.maxStreamingBitrate(endpointInfo.IsInNetwork, mediaType);
        }

        self.getMaxStreamingBitrate = function (player) {

            player = player || self._currentPlayer;
            if (player && player.getMaxStreamingBitrate) {
                return player.getMaxStreamingBitrate();
            }

            var playerData = getPlayerData(player);

            if (playerData.maxStreamingBitrate) {
                return playerData.maxStreamingBitrate;
            }

            var mediaType = playerData.streamInfo ? playerData.streamInfo.mediaType : null;
            var currentItem = self.currentItem(player);

            var apiClient = currentItem ? connectionManager.getApiClient(currentItem.ServerId) : connectionManager.currentApiClient();
            return getSavedMaxStreamingBitrate(apiClient, mediaType);
        };

        self.enableAutomaticBitrateDetection = function (player) {

            player = player || self._currentPlayer;
            if (player && player.enableAutomaticBitrateDetection) {
                return player.enableAutomaticBitrateDetection();
            }

            var playerData = getPlayerData(player);
            var mediaType = playerData.streamInfo ? playerData.streamInfo.mediaType : null;
            var currentItem = self.currentItem(player);

            var apiClient = currentItem ? connectionManager.getApiClient(currentItem.ServerId) : connectionManager.currentApiClient();
            var endpointInfo = apiClient.getSavedEndpointInfo() || {};

            return appSettings.enableAutomaticBitrateDetection(endpointInfo.IsInNetwork, mediaType);
        };

        self.setMaxStreamingBitrate = function (options, player) {

            player = player || self._currentPlayer;
            if (player && player.setMaxStreamingBitrate) {
                return player.setMaxStreamingBitrate(options);
            }

            var apiClient = connectionManager.getApiClient(self.currentItem(player).ServerId);

            apiClient.getEndpointInfo().then(function (endpointInfo) {

                var playerData = getPlayerData(player);
                var mediaType = playerData.streamInfo ? playerData.streamInfo.mediaType : null;

                var promise;
                if (options.enableAutomaticBitrateDetection) {
                    appSettings.enableAutomaticBitrateDetection(endpointInfo.IsInNetwork, mediaType, true);
                    promise = apiClient.detectBitrate(true);
                } else {
                    appSettings.enableAutomaticBitrateDetection(endpointInfo.IsInNetwork, mediaType, false);
                    promise = Promise.resolve(options.maxBitrate);
                }

                promise.then(function (bitrate) {

                    appSettings.maxStreamingBitrate(endpointInfo.IsInNetwork, mediaType, bitrate);

                    changeStream(player, getCurrentTicks(player), {
                        MaxStreamingBitrate: bitrate
                    });
                });
            });
        };

        self.isFullscreen = function (player) {

            player = player || self._currentPlayer;
            if (!player.isLocalPlayer || player.isFullscreen) {
                return player.isFullscreen();
            }

            return fullscreenManager.isFullScreen();
        };

        self.toggleFullscreen = function (player) {

            player = player || self._currentPlayer;
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
            player = player || self._currentPlayer;
            return player.togglePictureInPicture();
        };

        self.getSubtitleStreamIndex = function (player) {

            player = player || self._currentPlayer;

            if (player && !enableLocalPlaylistManagement(player)) {
                return player.getSubtitleStreamIndex();
            }

            if (!player) {
                throw new Error('player cannot be null');
            }

            return getPlayerData(player).subtitleStreamIndex;
        };

        function getDeliveryMethod(subtitleStream) {

            // This will be null for internal subs for local items
            if (subtitleStream.DeliveryMethod) {
                return subtitleStream.DeliveryMethod;
            }

            return subtitleStream.IsExternal ? 'External' : 'Embed';
        }

        self.setSubtitleStreamIndex = function (index, player) {

            player = player || self._currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.setSubtitleStreamIndex(index);
            }

            var currentStream = getCurrentSubtitleStream(player);

            var newStream = getSubtitleStream(player, index);

            if (!currentStream && !newStream) {
                return;
            }

            var selectedTrackElementIndex = -1;

            var currentPlayMethod = self.playMethod(player);

            if (currentStream && !newStream) {

                if (getDeliveryMethod(currentStream) === 'Encode' || (getDeliveryMethod(currentStream) === 'Embed' && currentPlayMethod === 'Transcode')) {

                    // Need to change the transcoded stream to remove subs
                    changeStream(player, getCurrentTicks(player), { SubtitleStreamIndex: -1 });
                }
            }
            else if (!currentStream && newStream) {

                if (getDeliveryMethod(newStream) === 'External') {
                    selectedTrackElementIndex = index;
                } else if (getDeliveryMethod(newStream) === 'Embed' && currentPlayMethod !== 'Transcode') {
                    selectedTrackElementIndex = index;
                } else {

                    // Need to change the transcoded stream to add subs
                    changeStream(player, getCurrentTicks(player), { SubtitleStreamIndex: index });
                }
            }
            else if (currentStream && newStream) {

                // Switching tracks
                // We can handle this clientside if the new track is external or the new track is embedded and we're not transcoding
                if (getDeliveryMethod(newStream) === 'External' || (getDeliveryMethod(newStream) === 'Embed' && currentPlayMethod !== 'Transcode')) {
                    selectedTrackElementIndex = index;

                    // But in order to handle this client side, if the previous track is being added via transcoding, we'll have to remove it
                    if (getDeliveryMethod(currentStream) !== 'External' && getDeliveryMethod(currentStream) !== 'Embed') {
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

        self.seek = function (ticks, player) {

            ticks = Math.max(0, ticks);

            player = player || self._currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {

                if (player.isLocalPlayer) {
                    return player.seek((ticks || 0) / 10000);
                } else {
                    return player.seek(ticks);
                }
            }

            changeStream(player, ticks);
        };

        self.seekRelative = function (offsetTicks, player) {

            player = player || self._currentPlayer;
            if (player && !enableLocalPlaylistManagement(player) && player.seekRelative) {

                if (player.isLocalPlayer) {
                    return player.seekRelative((ticks || 0) / 10000);
                } else {
                    return player.seekRelative(ticks);
                }
            }

            var ticks = getCurrentTicks(player) + offsetTicks;
            return this.seek(ticks, player);
        };

        // Returns true if the player can seek using native client-side seeking functions
        function canPlayerSeek(player) {

            if (!player) {
                throw new Error('player cannot be null');
            }

            var playerData = getPlayerData(player);

            var currentSrc = (playerData.streamInfo.url || '').toLowerCase();

            if (currentSrc.indexOf('.m3u8') !== -1) {
                return true;
            }

            if (player.seekable) {
                return player.seekable();
            }

            var isPlayMethodTranscode = self.playMethod(player) === 'Transcode';

            if (isPlayMethodTranscode) {
                return false;
            }

            return player.duration();
        }

        function changeStream(player, ticks, params) {

            if (canPlayerSeek(player) && params == null) {

                player.currentTime(parseInt(ticks / 10000));
                return;
            }

            params = params || {};

            var liveStreamId = getPlayerData(player).streamInfo.liveStreamId;
            var lastMediaInfoQuery = getPlayerData(player).streamInfo.lastMediaInfoQuery;

            var playSessionId = self.playSessionId(player);

            var currentItem = self.currentItem(player);

            player.getDeviceProfile(currentItem, {

                isRetry: params.EnableDirectPlay === false

            }).then(function (deviceProfile) {

                var audioStreamIndex = params.AudioStreamIndex == null ? getPlayerData(player).audioStreamIndex : params.AudioStreamIndex;
                var subtitleStreamIndex = params.SubtitleStreamIndex == null ? getPlayerData(player).subtitleStreamIndex : params.SubtitleStreamIndex;

                var currentMediaSource = self.currentMediaSource(player);
                var apiClient = connectionManager.getApiClient(currentItem.ServerId);

                if (ticks) {
                    ticks = parseInt(ticks);
                }

                var maxBitrate = params.MaxStreamingBitrate || self.getMaxStreamingBitrate(player);

                var currentPlayOptions = currentItem.playOptions || {};

                getPlaybackInfo(player, apiClient, currentItem, deviceProfile, maxBitrate, ticks, true, currentMediaSource.Id, audioStreamIndex, subtitleStreamIndex, liveStreamId, params.EnableDirectPlay, params.EnableDirectStream, params.AllowVideoStreamCopy, params.AllowAudioStreamCopy).then(function (result) {

                    if (validatePlaybackInfoResult(self, result)) {

                        currentMediaSource = result.MediaSources[0];

                        var streamInfo = createStreamInfo(apiClient, currentItem.MediaType, currentItem, currentMediaSource, ticks);
                        streamInfo.fullscreen = currentPlayOptions.fullscreen;
                        streamInfo.lastMediaInfoQuery = lastMediaInfoQuery;

                        if (!streamInfo.url) {
                            showPlaybackInfoErrorMessage(self, 'NoCompatibleStream', true);
                            return;
                        }

                        getPlayerData(player).subtitleStreamIndex = subtitleStreamIndex;
                        getPlayerData(player).audioStreamIndex = audioStreamIndex;
                        getPlayerData(player).maxStreamingBitrate = maxBitrate;

                        changeStreamToUrl(apiClient, player, playSessionId, streamInfo);
                    }
                });
            });
        }

        function changeStreamToUrl(apiClient, player, playSessionId, streamInfo, newPositionTicks) {

            var playerData = getPlayerData(player);

            playerData.isChangingStream = true;

            if (playerData.streamInfo && playSessionId) {

                apiClient.stopActiveEncodings(playSessionId).then(function () {

                    // Stop the first transcoding afterwards because the player may still send requests to the original url
                    var afterSetSrc = function () {

                        apiClient.stopActiveEncodings(playSessionId);
                    };
                    setSrcIntoPlayer(apiClient, player, streamInfo).then(afterSetSrc, afterSetSrc);
                });

            } else {
                setSrcIntoPlayer(apiClient, player, streamInfo);
            }
        }

        function setSrcIntoPlayer(apiClient, player, streamInfo) {

            return player.play(streamInfo).then(function () {

                var playerData = getPlayerData(player);

                playerData.isChangingStream = false;
                playerData.streamInfo = streamInfo;
                streamInfo.started = true;
                streamInfo.ended = false;

                sendProgressUpdate(player, 'timeupdate');
            }, function (e) {

                var playerData = getPlayerData(player);
                playerData.isChangingStream = false;

                onPlaybackError.call(player, e, {
                    type: 'mediadecodeerror',
                    streamInfo: streamInfo
                });
            });
        }

        function translateItemsForPlayback(items, options) {

            var firstItem = items[0];
            var promise;

            var serverId = firstItem.ServerId;

            var queryOptions = options.queryOptions || {};

            if (firstItem.Type === "Program") {

                promise = getItemsForPlayback(serverId, {
                    Ids: firstItem.ChannelId,
                });
            }
            else if (firstItem.Type === "Playlist") {

                promise = getItemsForPlayback(serverId, {
                    ParentId: firstItem.Id,
                    SortBy: options.shuffle ? 'Random' : null
                });
            }
            else if (firstItem.Type === "MusicArtist") {

                promise = getItemsForPlayback(serverId, {
                    ArtistIds: firstItem.Id,
                    Filters: "IsNotFolder",
                    Recursive: true,
                    SortBy: options.shuffle ? 'Random' : 'SortName',
                    MediaTypes: "Audio"
                });

            }
            else if (firstItem.MediaType === "Photo") {

                promise = getItemsForPlayback(serverId, {
                    ParentId: firstItem.ParentId,
                    Filters: "IsNotFolder",
                    // Setting this to true may cause some incorrect sorting
                    Recursive: false,
                    SortBy: options.shuffle ? 'Random' : 'SortName',
                    MediaTypes: "Photo,Video",
                    Limit: 500

                }).then(function (result) {

                    var items = result.Items;

                    var index = items.map(function (i) {
                        return i.Id;

                    }).indexOf(firstItem.Id);

                    if (index === -1) {
                        index = 0;
                    }

                    options.startIndex = index;

                    return Promise.resolve(result);

                });
            }
            else if (firstItem.Type === "PhotoAlbum") {

                promise = getItemsForPlayback(serverId, {
                    ParentId: firstItem.Id,
                    Filters: "IsNotFolder",
                    // Setting this to true may cause some incorrect sorting
                    Recursive: false,
                    SortBy: options.shuffle ? 'Random' : 'SortName',
                    MediaTypes: "Photo,Video",
                    Limit: 1000

                });
            }
            else if (firstItem.Type === "MusicGenre") {

                promise = getItemsForPlayback(serverId, {
                    GenreIds: firstItem.Id,
                    Filters: "IsNotFolder",
                    Recursive: true,
                    SortBy: options.shuffle ? 'Random' : 'SortName',
                    MediaTypes: "Audio"
                });
            }
            else if (firstItem.IsFolder) {

                promise = getItemsForPlayback(serverId, mergePlaybackQueries({

                    ParentId: firstItem.Id,
                    Filters: "IsNotFolder",
                    Recursive: true,
                    // These are pre-sorted
                    SortBy: options.shuffle ? 'Random' : (['BoxSet'].indexOf(firstItem.Type) === -1 ? 'SortName' : null),
                    MediaTypes: "Audio,Video"

                }, queryOptions));
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
                            Fields: "Chapters"

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

        self.play = function (options) {

            normalizePlayOptions(options);

            if (self._currentPlayer) {
                if (options.enableRemotePlayers === false && !self._currentPlayer.isLocalPlayer) {
                    return Promise.reject();
                }

                if (!self._currentPlayer.isLocalPlayer) {
                    return self._currentPlayer.play(options);
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

        self.getPlayerState = function (player, item, mediaSource) {

            player = player || self._currentPlayer;

            if (!player) {
                throw new Error('player cannot be null');
            }

            if (!enableLocalPlaylistManagement(player) && player.getPlayerState) {
                return player.getPlayerState();
            }

            item = item || self.currentItem(player);
            mediaSource = mediaSource || self.currentMediaSource(player);

            var state = {
                PlayState: {}
            };

            if (player) {

                state.PlayState.VolumeLevel = player.getVolume();
                state.PlayState.IsMuted = player.isMuted();
                state.PlayState.IsPaused = player.paused();
                state.PlayState.RepeatMode = self.getRepeatMode(player);
                state.PlayState.MaxStreamingBitrate = self.getMaxStreamingBitrate(player);

                state.PlayState.PositionTicks = getCurrentTicks(player);
                state.PlayState.PlaybackStartTimeTicks = self.playbackStartTime(player);

                state.PlayState.SubtitleStreamIndex = self.getSubtitleStreamIndex(player);
                state.PlayState.AudioStreamIndex = self.getAudioStreamIndex(player);
                state.PlayState.BufferedRanges = self.getBufferedRanges(player);

                state.PlayState.PlayMethod = self.playMethod(player);

                if (mediaSource) {
                    state.PlayState.LiveStreamId = mediaSource.LiveStreamId;
                }
                state.PlayState.PlaySessionId = self.playSessionId(player);
                state.PlayState.PlaylistItemId = self.getCurrentPlaylistItemId(player);
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

            return state;
        };

        self.duration = function (player) {

            player = player || self._currentPlayer;

            if (player && !enableLocalPlaylistManagement(player) && !player.isLocalPlayer) {
                return player.duration();
            }

            if (!player) {
                throw new Error('player cannot be null');
            }

            var mediaSource = self.currentMediaSource(player);

            if (mediaSource && mediaSource.RunTimeTicks) {
                return mediaSource.RunTimeTicks;
            }

            var playerDuration = player.duration();

            if (playerDuration) {
                playerDuration *= 10000;
            }

            return playerDuration;
        };

        function getCurrentTicks(player) {

            if (!player) {
                throw new Error('player cannot be null');
            }

            var playerTime = Math.floor(10000 * (player || self._currentPlayer).currentTime());

            var streamInfo = getPlayerData(player).streamInfo;
            if (streamInfo) {
                playerTime += getPlayerData(player).streamInfo.transcodingOffsetTicks || 0;
            }

            return playerTime;
        }

        // Only used internally
        self.getCurrentTicks = getCurrentTicks;

        function playPhotos(items, options, user) {

            var playStartIndex = options.startIndex || 0;
            var player = getPlayer(items[playStartIndex], options);

            loading.hide();

            options.items = items;

            return player.play(options);
        }

        function playWithIntros(items, options, user) {

            var playStartIndex = options.startIndex || 0;
            var firstItem = items[playStartIndex];

            // If index was bad, reset it
            if (!firstItem) {
                playStartIndex = 0;
                firstItem = items[playStartIndex];
            }

            // If it's still null then there's nothing to play
            if (!firstItem) {
                showPlaybackInfoErrorMessage(self, 'NoCompatibleStream', false);
                return Promise.reject();
            }

            if (firstItem.MediaType === "Photo") {

                return playPhotos(items, options, user);
            }

            var apiClient = connectionManager.getApiClient(firstItem.ServerId);

            return getIntros(firstItem, apiClient, options).then(function (introsResult) {

                var introItems = introsResult.Items;
                var introPlayOptions;

                firstItem.playOptions = truncatePlayOptions(options);

                if (introItems.length) {

                    introPlayOptions = {
                        fullscreen: firstItem.playOptions.fullscreen
                    };

                } else {
                    introPlayOptions = firstItem.playOptions;
                }

                items = introItems.concat(items);

                // Needed by players that manage their own playlist
                introPlayOptions.items = items;
                introPlayOptions.startIndex = playStartIndex;

                return playInternal(items[playStartIndex], introPlayOptions, function () {

                    self._playQueueManager.setPlaylist(items);

                    setPlaylistState(items[playStartIndex].PlaylistItemId, playStartIndex);
                    loading.hide();
                });
            });
        }

        // Set playlist state. Using a method allows for overloading in derived player implementations
        function setPlaylistState(playlistItemId, index) {

            if (!isNaN(index)) {
                self._playQueueManager.setPlaylistState(playlistItemId, index);
            }
        }

        function playInternal(item, playOptions, onPlaybackStartedFn) {

            if (item.IsPlaceHolder) {
                loading.hide();
                showPlaybackInfoErrorMessage(self, 'PlaceHolder', true);
                return Promise.reject();
            }

            // Normalize defaults to simplfy checks throughout the process
            normalizePlayOptions(playOptions);

            if (playOptions.isFirstItem) {
                playOptions.isFirstItem = false;
            } else {
                playOptions.isFirstItem = true;
            }

            return runInterceptors(item, playOptions).then(function () {

                if (playOptions.fullscreen) {
                    loading.show();
                }

                // TODO: This should be the media type requested, not the original media type
                var mediaType = item.MediaType;

                var onBitrateDetectionFailure = function () {
                    return playAfterBitrateDetect(getSavedMaxStreamingBitrate(connectionManager.getApiClient(item.ServerId), mediaType), item, playOptions, onPlaybackStartedFn);
                };

                if (!isServerItem(item) || itemHelper.isLocalItem(item)) {
                    return onBitrateDetectionFailure();
                }

                var apiClient = connectionManager.getApiClient(item.ServerId);
                apiClient.getEndpointInfo().then(function (endpointInfo) {

                    if ((mediaType === 'Video' || mediaType === 'Audio') && appSettings.enableAutomaticBitrateDetection(endpointInfo.IsInNetwork, mediaType)) {

                        return apiClient.detectBitrate().then(function (bitrate) {

                            appSettings.maxStreamingBitrate(endpointInfo.IsInNetwork, mediaType, bitrate);

                            return playAfterBitrateDetect(bitrate, item, playOptions, onPlaybackStartedFn);

                        }, onBitrateDetectionFailure);

                    } else {

                        onBitrateDetectionFailure();
                    }

                }, onBitrateDetectionFailure);

            }, onInterceptorRejection);
        }

        function onInterceptorRejection() {
            var player = self._currentPlayer;

            if (player) {
                destroyPlayer(player);
                removeCurrentPlayer(player);
            }

            events.trigger(self, 'playbackcancelled');

            return Promise.reject();
        }

        function destroyPlayer(player) {
            player.destroy();
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

        function sendPlaybackListToPlayer(player, items, deviceProfile, maxBitrate, apiClient, startPositionTicks, mediaSourceId, audioStreamIndex, subtitleStreamIndex, startIndex) {

            return setStreamUrls(items, deviceProfile, maxBitrate, apiClient, startPositionTicks).then(function () {

                loading.hide();

                return player.play({
                    items: items,
                    startPositionTicks: startPositionTicks || 0,
                    mediaSourceId: mediaSourceId,
                    audioStreamIndex: audioStreamIndex,
                    subtitleStreamIndex: subtitleStreamIndex,
                    startIndex: startIndex
                });
            });
        }

        function playAfterBitrateDetect(maxBitrate, item, playOptions, onPlaybackStartedFn) {

            var startPosition = playOptions.startPositionTicks;

            var player = getPlayer(item, playOptions);
            var activePlayer = self._currentPlayer;

            var promise;

            if (activePlayer) {

                // TODO: if changing players within the same playlist, this will cause nextItem to be null
                self._playNextAfterEnded = false;
                promise = onPlaybackChanging(activePlayer, player, item);
            } else {
                promise = Promise.resolve();
            }

            if (!isServerItem(item) || item.MediaType === 'Game' || item.MediaType === 'Book') {
                return promise.then(function () {
                    var streamInfo = createStreamInfoFromUrlItem(item);
                    streamInfo.fullscreen = playOptions.fullscreen;
                    getPlayerData(player).isChangingStream = false;
                    return player.play(streamInfo).then(function () {
                        loading.hide();
                        onPlaybackStartedFn();
                        onPlaybackStarted(player, playOptions, streamInfo);
                    }, function () {
                        // TODO: show error message
                        self.stop(player);
                    });
                });
            }

            return Promise.all([promise, player.getDeviceProfile(item)]).then(function (responses) {

                var deviceProfile = responses[1];

                var apiClient = connectionManager.getApiClient(item.ServerId);

                var mediaSourceId = playOptions.mediaSourceId;
                var audioStreamIndex = playOptions.audioStreamIndex;
                var subtitleStreamIndex = playOptions.subtitleStreamIndex;

                if (player && !enableLocalPlaylistManagement(player)) {

                    return sendPlaybackListToPlayer(player, playOptions.items, deviceProfile, maxBitrate, apiClient, startPosition, mediaSourceId, audioStreamIndex, subtitleStreamIndex, playOptions.startIndex);
                }

                // this reference was only needed by sendPlaybackListToPlayer
                playOptions.items = null;

                return getPlaybackMediaSource(player, apiClient, deviceProfile, maxBitrate, item, startPosition, mediaSourceId, audioStreamIndex, subtitleStreamIndex).then(function (mediaSource) {

                    var streamInfo = createStreamInfo(apiClient, item.MediaType, item, mediaSource, startPosition);

                    streamInfo.fullscreen = playOptions.fullscreen;

                    getPlayerData(player).isChangingStream = false;
                    getPlayerData(player).maxStreamingBitrate = maxBitrate;

                    return player.play(streamInfo).then(function () {
                        loading.hide();
                        onPlaybackStartedFn();
                        onPlaybackStarted(player, playOptions, streamInfo, mediaSource);
                    }, function (err) {

                        // TODO: Improve this because it will report playback start on a failure
                        onPlaybackStartedFn();
                        onPlaybackStarted(player, playOptions, streamInfo, mediaSource);
                        setTimeout(function () {
                            onPlaybackError.call(player, err, {
                                type: 'mediadecodeerror',
                                streamInfo: streamInfo
                            });
                        }, 100);
                    });
                });
            });
        }

        self.getPlaybackInfo = function (item, options) {

            options = options || {};
            var startPosition = options.startPositionTicks || 0;
            var mediaType = options.mediaType || item.MediaType;
            var player = getPlayer(item, options);
            var apiClient = connectionManager.getApiClient(item.ServerId);

            // Call this just to ensure the value is recorded, it is needed with getSavedMaxStreamingBitrate
            return apiClient.getEndpointInfo().then(function () {

                var maxBitrate = getSavedMaxStreamingBitrate(connectionManager.getApiClient(item.ServerId), mediaType);

                return player.getDeviceProfile(item).then(function (deviceProfile) {

                    return getPlaybackMediaSource(player, apiClient, deviceProfile, maxBitrate, item, startPosition, options.mediaSourceId, options.audioStreamIndex, options.subtitleStreamIndex).then(function (mediaSource) {

                        return createStreamInfo(apiClient, item.MediaType, item, mediaSource, startPosition);
                    });
                });
            });
        };

        self.getPlaybackMediaSources = function (item, options) {

            options = options || {};
            var startPosition = options.startPositionTicks || 0;
            var mediaType = options.mediaType || item.MediaType;
            // TODO: Remove the true forceLocalPlayer hack
            var player = getPlayer(item, options, true);
            var apiClient = connectionManager.getApiClient(item.ServerId);

            // Call this just to ensure the value is recorded, it is needed with getSavedMaxStreamingBitrate
            return apiClient.getEndpointInfo().then(function () {

                var maxBitrate = getSavedMaxStreamingBitrate(connectionManager.getApiClient(item.ServerId), mediaType);

                return player.getDeviceProfile(item).then(function (deviceProfile) {

                    return getPlaybackInfo(player, apiClient, item, deviceProfile, maxBitrate, startPosition, false, null, null, null, null).then(function (playbackInfoResult) {

                        return playbackInfoResult.MediaSources;
                    });
                });
            });

        };

        function createStreamInfo(apiClient, type, item, mediaSource, startPosition) {

            var mediaUrl;
            var contentType;
            var transcodingOffsetTicks = 0;
            var playerStartPositionTicks = startPosition;
            var liveStreamId = mediaSource.LiveStreamId;

            var playMethod = 'Transcode';

            var mediaSourceContainer = (mediaSource.Container || '').toLowerCase();
            var directOptions;

            if (type === 'Video' || type === 'Audio') {

                contentType = getMimeType(type.toLowerCase(), mediaSourceContainer);

                if (mediaSource.enableDirectPlay) {
                    mediaUrl = mediaSource.Path;

                    playMethod = 'DirectPlay';

                }

                else if (mediaSource.StreamUrl) {

                    // Only used for audio
                    playMethod = 'Transcode';
                    mediaUrl = mediaSource.StreamUrl;
                }

                else if (mediaSource.SupportsDirectStream) {

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
                    }

                    var prefix = type === 'Video' ? 'Videos' : 'Audio';
                    mediaUrl = apiClient.getUrl(prefix + '/' + item.Id + '/stream.' + mediaSourceContainer, directOptions);

                    playMethod = 'DirectStream';

                } else if (mediaSource.SupportsTranscoding) {

                    mediaUrl = apiClient.getUrl(mediaSource.TranscodingUrl);

                    if (mediaSource.TranscodingSubProtocol === 'hls') {

                        contentType = 'application/x-mpegURL';

                    } else {

                        playerStartPositionTicks = null;
                        contentType = getMimeType(type.toLowerCase(), mediaSource.TranscodingContainer);

                        if (mediaUrl.toLowerCase().indexOf('copytimestamps=true') === -1) {
                            transcodingOffsetTicks = startPosition || 0;
                        }
                    }
                }

            } else {

                // All other media types
                mediaUrl = mediaSource.Path;
                playMethod = 'DirectPlay';
            }

            // Fallback (used for offline items)
            if (!mediaUrl && mediaSource.SupportsDirectPlay) {
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
                textTracks: getTextTracks(apiClient, item, mediaSource),
                // TODO: Deprecate
                tracks: getTextTracks(apiClient, item, mediaSource),
                mediaType: type,
                liveStreamId: liveStreamId,
                playSessionId: getParam('playSessionId', mediaUrl),
                title: item.Name
            };

            var backdropUrl = backdropImageUrl(apiClient, item, {});
            if (backdropUrl) {
                resultInfo.backdropUrl = backdropUrl;
            }

            return resultInfo;
        }

        function getTextTracks(apiClient, item, mediaSource) {

            var subtitleStreams = mediaSource.MediaStreams.filter(function (s) {
                return s.Type === 'Subtitle';
            });

            var textStreams = subtitleStreams.filter(function (s) {
                return s.DeliveryMethod === 'External';
            });

            var tracks = [];

            for (var i = 0, length = textStreams.length; i < length; i++) {

                var textStream = textStreams[i];
                var textStreamUrl;

                if (itemHelper.isLocalItem(item)) {
                    textStreamUrl = textStream.Path;
                } else {
                    textStreamUrl = !textStream.IsExternalUrl ? apiClient.getUrl(textStream.DeliveryUrl) : textStream.DeliveryUrl;
                }

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

        function getPlaybackMediaSource(player, apiClient, deviceProfile, maxBitrate, item, startPosition, mediaSourceId, audioStreamIndex, subtitleStreamIndex) {

            return getPlaybackInfo(player, apiClient, item, deviceProfile, maxBitrate, startPosition, true, mediaSourceId, audioStreamIndex, subtitleStreamIndex, null).then(function (playbackInfoResult) {

                if (validatePlaybackInfoResult(self, playbackInfoResult)) {

                    return getOptimalMediaSource(apiClient, item, playbackInfoResult.MediaSources).then(function (mediaSource) {
                        if (mediaSource) {

                            if (mediaSource.RequiresOpening && !mediaSource.LiveStreamId) {

                                return getLiveStream(player, apiClient, item, playbackInfoResult.PlaySessionId, deviceProfile, maxBitrate, startPosition, mediaSource, null, null).then(function (openLiveStreamResult) {

                                    return supportsDirectPlay(apiClient, item, openLiveStreamResult.MediaSource).then(function (result) {

                                        openLiveStreamResult.MediaSource.enableDirectPlay = result;
                                        return openLiveStreamResult.MediaSource;
                                    });

                                });

                            } else {
                                return mediaSource;
                            }
                        } else {
                            showPlaybackInfoErrorMessage(self, 'NoCompatibleStream');
                            return Promise.reject();
                        }
                    });
                } else {
                    return Promise.reject();
                }
            });
        }

        function getPlayer(item, playOptions, forceLocalPlayers) {

            var serverItem = isServerItem(item);
            return getAutomaticPlayers(self, forceLocalPlayers).filter(function (p) {

                if (p.canPlayMediaType(item.MediaType)) {

                    if (serverItem) {
                        if (p.canPlayItem) {
                            return p.canPlayItem(item, playOptions);
                        }
                        return true;
                    }

                    else if (item.Url && p.canPlayUrl) {
                        return p.canPlayUrl(item.Url);
                    }
                }

                return false;

            })[0];
        }

        self.setCurrentPlaylistItem = function (playlistItemId, player) {

            player = player || self._currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.setCurrentPlaylistItem(playlistItemId);
            }

            var newItem;
            var newItemIndex;
            var playlist = self._playQueueManager.getPlaylist();

            for (var i = 0, length = playlist.length; i < length; i++) {
                if (playlist[i].PlaylistItemId === playlistItemId) {
                    newItem = playlist[i];
                    newItemIndex = i;
                    break;
                }
            }

            if (newItem) {

                var newItemPlayOptions = newItem.playOptions || {};

                playInternal(newItem, newItemPlayOptions, function () {
                    setPlaylistState(newItem.PlaylistItemId, newItemIndex);
                });
            }
        };

        self.removeFromPlaylist = function (playlistItemIds, player) {

            if (!playlistItemIds) {
                throw new Error('Invalid playlistItemIds');
            }

            player = player || self._currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.removeFromPlaylist(playlistItemIds);
            }

            var removeResult = self._playQueueManager.removeFromPlaylist(playlistItemIds);

            if (removeResult.result === 'empty') {
                return self.stop(player);
            }

            var isCurrentIndex = removeResult.isCurrentIndex;

            events.trigger(player, 'playlistitemremove', [
                {
                    playlistItemIds: playlistItemIds
                }]);

            if (isCurrentIndex) {

                return self.setCurrentPlaylistItem(self._playQueueManager.getPlaylist()[0].PlaylistItemId, player);
            }

            return Promise.resolve();
        };

        self.movePlaylistItem = function (playlistItemId, newIndex, player) {

            player = player || self._currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.movePlaylistItem(playlistItemId, newIndex);
            }

            var moveResult = self._playQueueManager.movePlaylistItem(playlistItemId, newIndex);

            if (moveResult.result === 'noop') {
                return;
            }

            events.trigger(player, 'playlistitemmove', [
                {
                    playlistItemId: moveResult.playlistItemId,
                    newIndex: moveResult.newIndex
                }]);
        };

        self.getCurrentPlaylistIndex = function (player) {

            player = player || self._currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.getCurrentPlaylistIndex();
            }

            return self._playQueueManager.getCurrentPlaylistIndex();
        };

        self.getCurrentPlaylistItemId = function (player) {

            player = player || self._currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.getCurrentPlaylistItemId();
            }

            return self._playQueueManager.getCurrentPlaylistItemId();
        };

        self.channelUp = function (player) {

            player = player || self._currentPlayer;
            return self.nextTrack(player);
        };

        self.channelDown = function (player) {

            player = player || self._currentPlayer;
            return self.previousTrack(player);
        };

        self.nextTrack = function (player) {

            player = player || self._currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.nextTrack();
            }

            var newItemInfo = self._playQueueManager.getNextItemInfo();

            if (newItemInfo) {

                console.log('playing next track');

                var newItemPlayOptions = newItemInfo.item.playOptions || {};

                playInternal(newItemInfo.item, newItemPlayOptions, function () {
                    setPlaylistState(newItemInfo.item.PlaylistItemId, newItemInfo.index);
                });
            }
        };

        self.previousTrack = function (player) {

            player = player || self._currentPlayer;
            if (player && !enableLocalPlaylistManagement(player)) {
                return player.previousTrack();
            }

            var newIndex = self.getCurrentPlaylistIndex(player) - 1;
            if (newIndex >= 0) {

                var playlist = self._playQueueManager.getPlaylist();
                var newItem = playlist[newIndex];

                if (newItem) {

                    var newItemPlayOptions = newItem.playOptions || {};
                    newItemPlayOptions.startPositionTicks = 0;

                    playInternal(newItem, newItemPlayOptions, function () {
                        setPlaylistState(newItem.PlaylistItemId, newIndex);
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

            player = player || self._currentPlayer;

            if (!player) {
                return self.play(options);
            }

            if (options.items) {

                return translateItemsForPlayback(options.items, options).then(function (items) {

                    // TODO: Handle options.startIndex for photos
                    queueAll(items, mode, player);

                });

            } else {

                if (!options.serverId) {
                    throw new Error('serverId required!');
                }

                return getItemsForPlayback(options.serverId, {

                    Ids: options.ids.join(',')

                }).then(function (result) {

                    return translateItemsForPlayback(result.Items, options).then(function (items) {

                        // TODO: Handle options.startIndex for photos
                        queueAll(items, mode, player);

                    });
                });
            }
        }

        function queueAll(items, mode, player) {

            if (!items.length) {
                return;
            }

            if (!player.isLocalPlayer) {
                if (mode === 'next') {
                    player.queueNext({
                        items: items
                    });
                } else {
                    player.queue({
                        items: items
                    });
                }
                return;
            }

            var queueDirectToPlayer = player && !enableLocalPlaylistManagement(player);

            if (queueDirectToPlayer) {

                var apiClient = connectionManager.getApiClient(items[0].ServerId);

                player.getDeviceProfile(items[0]).then(function (profile) {

                    setStreamUrls(items, profile, self.getMaxStreamingBitrate(player), apiClient, 0).then(function () {

                        if (mode === 'next') {
                            player.queueNext(items);
                        } else {
                            player.queue(items);
                        }
                    });
                });

                return;
            }

            if (mode === 'next') {
                self._playQueueManager.queueNext(items);
            } else {
                self._playQueueManager.queue(items);
            }
        }

        function onPlayerProgressInterval() {
            var player = this;
            sendProgressUpdate(player, 'timeupdate');
        }

        function startPlaybackProgressTimer(player) {

            stopPlaybackProgressTimer(player);

            player._progressInterval = setInterval(onPlayerProgressInterval.bind(player), 10000);
        }

        function stopPlaybackProgressTimer(player) {

            if (player._progressInterval) {

                clearInterval(player._progressInterval);
                player._progressInterval = null;
            }
        }

        function onPlaybackStarted(player, playOptions, streamInfo, mediaSource) {

            if (!player) {
                throw new Error('player cannot be null');
            }

            setCurrentPlayerInternal(player);

            var playerData = getPlayerData(player);

            playerData.streamInfo = streamInfo;

            streamInfo.playbackStartTimeTicks = new Date().getTime() * 10000;

            if (mediaSource) {
                playerData.audioStreamIndex = mediaSource.DefaultAudioStreamIndex;
                playerData.subtitleStreamIndex = mediaSource.DefaultSubtitleStreamIndex;
            } else {
                playerData.audioStreamIndex = null;
                playerData.subtitleStreamIndex = null;
            }

            self._playNextAfterEnded = true;
            var isFirstItem = playOptions.isFirstItem;
            var fullscreen = playOptions.fullscreen;

            var state = self.getPlayerState(player, streamInfo.item, streamInfo.mediaSource);

            reportPlayback(self, state, player, true, state.NowPlayingItem.ServerId, 'reportPlaybackStart');

            state.IsFirstItem = isFirstItem;
            state.IsFullscreen = fullscreen;
            events.trigger(player, 'playbackstart', [state]);
            events.trigger(self, 'playbackstart', [player, state]);

            // only used internally as a safeguard to avoid reporting other events to the server before playback start
            streamInfo.started = true;

            startPlaybackProgressTimer(player);
        }

        function onPlaybackStartedFromSelfManagingPlayer(e, item, mediaSource) {

            var player = this;
            setCurrentPlayerInternal(player);

            var playOptions = item.playOptions || {};
            var isFirstItem = playOptions.isFirstItem;
            var fullscreen = playOptions.fullscreen;

            playOptions.isFirstItem = false;

            var playerData = getPlayerData(player);
            playerData.streamInfo = {};

            var streamInfo = playerData.streamInfo;
            streamInfo.playbackStartTimeTicks = new Date().getTime() * 10000;

            var state = self.getPlayerState(player, item, mediaSource);

            reportPlayback(self, state, player, true, state.NowPlayingItem.ServerId, 'reportPlaybackStart');

            state.IsFirstItem = isFirstItem;
            state.IsFullscreen = fullscreen;
            events.trigger(player, 'playbackstart', [state]);
            events.trigger(self, 'playbackstart', [player, state]);

            // only used internally as a safeguard to avoid reporting other events to the server before playback start
            streamInfo.started = true;

            startPlaybackProgressTimer(player);
        }

        function onPlaybackStoppedFromSelfManagingPlayer(e, playerStopInfo) {

            var player = this;

            stopPlaybackProgressTimer(player);
            var state = self.getPlayerState(player, playerStopInfo.item, playerStopInfo.mediaSource);

            var nextItem = playerStopInfo.nextItem;
            var nextMediaType = playerStopInfo.nextMediaType;

            var playbackStopInfo = {
                player: player,
                state: state,
                nextItem: (nextItem ? nextItem.item : null),
                nextMediaType: nextMediaType
            };

            state.NextMediaType = nextMediaType;

            var streamInfo = getPlayerData(player).streamInfo;

            // only used internally as a safeguard to avoid reporting other events to the server after playback stopped
            streamInfo.ended = true;

            if (isServerItem(playerStopInfo.item)) {

                state.PlayState.PositionTicks = (playerStopInfo.positionMs || 0) * 10000;

                reportPlayback(self, state, player, true, playerStopInfo.item.ServerId, 'reportPlaybackStopped');
            }

            state.NextItem = playbackStopInfo.nextItem;

            events.trigger(player, 'playbackstop', [state]);
            events.trigger(self, 'playbackstop', [playbackStopInfo]);

            var nextItemPlayOptions = nextItem ? (nextItem.item.playOptions || getDefaultPlayOptions()) : getDefaultPlayOptions();
            var newPlayer = nextItem ? getPlayer(nextItem.item, nextItemPlayOptions) : null;

            if (newPlayer !== player) {
                destroyPlayer(player);
                removeCurrentPlayer(player);
            }
        }

        function enablePlaybackRetryWithTranscoding(streamInfo, errorType, currentlyPreventsVideoStreamCopy, currentlyPreventsAudioStreamCopy) {

            // mediadecodeerror, medianotsupported, network, servererror

            if (streamInfo.mediaSource.SupportsTranscoding && (!currentlyPreventsVideoStreamCopy || !currentlyPreventsAudioStreamCopy)) {

                return true;
            }

            return false;
        }

        function onPlaybackError(e, error) {

            var player = this;
            error = error || {};

            // network
            // mediadecodeerror
            // medianotsupported
            var errorType = error.type;

            console.log('playbackmanager playback error type: ' + (errorType || ''));

            var streamInfo = error.streamInfo || getPlayerData(player).streamInfo;

            if (streamInfo) {

                var currentlyPreventsVideoStreamCopy = streamInfo.url.toLowerCase().indexOf('allowvideostreamcopy=false') !== -1;
                var currentlyPreventsAudioStreamCopy = streamInfo.url.toLowerCase().indexOf('allowaudiostreamcopy=false') !== -1;

                // Auto switch to transcoding
                if (enablePlaybackRetryWithTranscoding(streamInfo, errorType, currentlyPreventsVideoStreamCopy, currentlyPreventsAudioStreamCopy)) {

                    var startTime = getCurrentTicks(player) || streamInfo.playerStartPositionTicks;

                    changeStream(player, startTime, {

                        // force transcoding
                        EnableDirectPlay: false,
                        EnableDirectStream: false,
                        AllowVideoStreamCopy: false,
                        AllowAudioStreamCopy: currentlyPreventsAudioStreamCopy || currentlyPreventsVideoStreamCopy ? false : null

                    }, true);

                    return;
                }
            }

            var displayErrorCode = 'NoCompatibleStream';
            onPlaybackStopped.call(player, e, displayErrorCode);
        }

        function onPlaybackStopped(e, displayErrorCode) {

            var player = this;

            if (getPlayerData(player).isChangingStream) {
                return;
            }

            stopPlaybackProgressTimer(player);

            // User clicked stop or content ended
            var state = self.getPlayerState(player);
            var streamInfo = getPlayerData(player).streamInfo;

            var nextItem = self._playNextAfterEnded ? self._playQueueManager.getNextItemInfo() : null;

            var nextMediaType = (nextItem ? nextItem.item.MediaType : null);

            var playbackStopInfo = {
                player: player,
                state: state,
                nextItem: (nextItem ? nextItem.item : null),
                nextMediaType: nextMediaType
            };

            state.NextMediaType = nextMediaType;

            if (isServerItem(streamInfo.item)) {

                if (player.supportsProgress === false && state.PlayState && !state.PlayState.PositionTicks) {
                    state.PlayState.PositionTicks = streamInfo.item.RunTimeTicks;
                }

                // only used internally as a safeguard to avoid reporting other events to the server after playback stopped
                streamInfo.ended = true;

                reportPlayback(self, state, player, true, streamInfo.item.ServerId, 'reportPlaybackStopped');
            }

            state.NextItem = playbackStopInfo.nextItem;

            if (!nextItem) {
                self._playQueueManager.reset();
            }

            events.trigger(player, 'playbackstop', [state]);
            events.trigger(self, 'playbackstop', [playbackStopInfo]);

            var nextItemPlayOptions = nextItem ? (nextItem.item.playOptions || getDefaultPlayOptions()) : getDefaultPlayOptions();
            var newPlayer = nextItem ? getPlayer(nextItem.item, nextItemPlayOptions) : null;

            if (newPlayer !== player) {
                destroyPlayer(player);
                removeCurrentPlayer(player);
            }

            if (displayErrorCode && typeof (displayErrorCode) === 'string') {
                showPlaybackInfoErrorMessage(self, displayErrorCode, nextItem);
            }
            else if (nextItem) {
                self.nextTrack();
            }
        }

        function onPlaybackChanging(activePlayer, newPlayer, newItem) {

            var state = self.getPlayerState(activePlayer);

            var serverId = self.currentItem(activePlayer).ServerId;

            // User started playing something new while existing content is playing
            var promise;

            stopPlaybackProgressTimer(activePlayer);
            unbindStopped(activePlayer);

            if (activePlayer === newPlayer) {

                // If we're staying with the same player, stop it
                promise = activePlayer.stop(false);

            } else {

                // If we're switching players, tear down the current one
                promise = activePlayer.stop(true);
            }

            return promise.then(function () {

                bindStopped(activePlayer);

                if (enableLocalPlaylistManagement(activePlayer)) {
                    reportPlayback(self, state, activePlayer, true, serverId, 'reportPlaybackStopped');
                }

                events.trigger(self, 'playbackstop', [{
                    player: activePlayer,
                    state: state,
                    nextItem: newItem,
                    nextMediaType: newItem.MediaType
                }]);
            });
        }

        function bindStopped(player) {

            if (enableLocalPlaylistManagement(player)) {
                events.off(player, 'stopped', onPlaybackStopped);
                events.on(player, 'stopped', onPlaybackStopped);
            }
        }

        function onPlaybackTimeUpdate(e) {
            var player = this;
            sendProgressUpdate(player, 'timeupdate');
        }

        function onPlaybackPause(e) {
            var player = this;
            sendProgressUpdate(player, 'pause');
        }

        function onPlaybackUnpause(e) {
            var player = this;
            sendProgressUpdate(player, 'unpause');
        }

        function onPlaybackVolumeChange(e) {
            var player = this;
            sendProgressUpdate(player, 'volumechange');
        }

        function onRepeatModeChange(e) {
            var player = this;
            sendProgressUpdate(player, 'repeatmodechange');
        }

        function onPlaylistItemMove(e) {
            var player = this;
            sendProgressUpdate(player, 'playlistitemmove', true);
        }

        function onPlaylistItemRemove(e) {
            var player = this;
            sendProgressUpdate(player, 'playlistitemremove', true);
        }

        function onPlaylistItemAdd(e) {
            var player = this;
            sendProgressUpdate(player, 'playlistitemadd', true);
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
                events.on(player, 'timeupdate', onPlaybackTimeUpdate);
                events.on(player, 'pause', onPlaybackPause);
                events.on(player, 'unpause', onPlaybackUnpause);
                events.on(player, 'volumechange', onPlaybackVolumeChange);
                events.on(player, 'repeatmodechange', onRepeatModeChange);
                events.on(player, 'playlistitemmove', onPlaylistItemMove);
                events.on(player, 'playlistitemremove', onPlaylistItemRemove);
                events.on(player, 'playlistitemadd', onPlaylistItemAdd);
            } else if (player.isLocalPlayer) {

                events.on(player, 'itemstarted', onPlaybackStartedFromSelfManagingPlayer);
                events.on(player, 'itemstopped', onPlaybackStoppedFromSelfManagingPlayer);
                events.on(player, 'timeupdate', onPlaybackTimeUpdate);
                events.on(player, 'pause', onPlaybackPause);
                events.on(player, 'unpause', onPlaybackUnpause);
                events.on(player, 'volumechange', onPlaybackVolumeChange);
                events.on(player, 'repeatmodechange', onRepeatModeChange);
                events.on(player, 'playlistitemmove', onPlaylistItemMove);
                events.on(player, 'playlistitemremove', onPlaylistItemRemove);
                events.on(player, 'playlistitemadd', onPlaylistItemAdd);
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

        function sendProgressUpdate(player, progressEventName, reportPlaylist) {

            if (!player) {
                throw new Error('player cannot be null');
            }

            var state = self.getPlayerState(player);

            if (state.NowPlayingItem) {
                var serverId = state.NowPlayingItem.ServerId;

                var streamInfo = getPlayerData(player).streamInfo;

                if (streamInfo && streamInfo.started && !streamInfo.ended) {
                    reportPlayback(self, state, player, reportPlaylist, serverId, 'reportPlaybackProgress', progressEventName);
                }

                if (streamInfo && streamInfo.liveStreamId) {

                    if (new Date().getTime() - (streamInfo.lastMediaInfoQuery || 0) >= 600000) {
                        getLiveStreamMediaInfo(player, streamInfo, self.currentMediaSource(player), streamInfo.liveStreamId, serverId);
                    }
                }
            }
        }

        function getLiveStreamMediaInfo(player, streamInfo, mediaSource, liveStreamId, serverId) {

            console.log('getLiveStreamMediaInfo');

            streamInfo.lastMediaInfoQuery = new Date().getTime();

            var apiClient = connectionManager.getApiClient(serverId);

            if (!apiClient.isMinServerVersion('3.2.70.7')) {
                return;
            }

            connectionManager.getApiClient(serverId).getLiveStreamMediaInfo(liveStreamId).then(function (info) {

                mediaSource.MediaStreams = info.MediaStreams;
                events.trigger(player, 'mediastreamschange');

            }, function () {

            });
        }

        self.onAppClose = function () {

            var player = this._currentPlayer;

            // Try to report playback stopped before the app closes
            if (player && this.isPlaying(player)) {
                this._playNextAfterEnded = false;
                onPlaybackStopped.call(player);
            }
        };

        self.playbackStartTime = function (player) {

            player = player || this._currentPlayer;
            if (player && !enableLocalPlaylistManagement(player) && !player.isLocalPlayer) {
                return player.playbackStartTime();
            }

            var streamInfo = getPlayerData(player).streamInfo;
            return streamInfo ? streamInfo.playbackStartTimeTicks : null;
        };

        if (apphost.supports('remotecontrol')) {

            require(['serverNotifications'], function (serverNotifications) {
                events.on(serverNotifications, 'ServerShuttingDown', self.setDefaultPlayerActive.bind(self));
                events.on(serverNotifications, 'ServerRestarting', self.setDefaultPlayerActive.bind(self));
            });
        }
    }

    PlaybackManager.prototype.getCurrentPlayer = function () {
        return this._currentPlayer;
    };

    PlaybackManager.prototype.currentTime = function (player) {

        player = player || this._currentPlayer;
        if (player && !enableLocalPlaylistManagement(player) && !player.isLocalPlayer) {
            return player.currentTime();
        }

        return this.getCurrentTicks(player);
    };

    PlaybackManager.prototype.nextItem = function (player) {

        player = player || this._currentPlayer;

        if (player && !enableLocalPlaylistManagement(player)) {
            return player.nextItem();
        }

        var nextItem = this._playQueueManager.getNextItemInfo();

        if (!nextItem || !nextItem.item) {
            return Promise.reject();
        }

        var apiClient = connectionManager.getApiClient(nextItem.item.ServerId);
        return apiClient.getItem(apiClient.getCurrentUserId(), nextItem.item.Id);
    };

    PlaybackManager.prototype.canQueue = function (item) {

        if (item.Type === 'MusicAlbum' || item.Type === 'MusicArtist' || item.Type === 'MusicGenre') {
            return this.canQueueMediaType('Audio');
        }
        return this.canQueueMediaType(item.MediaType);
    };

    PlaybackManager.prototype.canQueueMediaType = function (mediaType) {

        if (this._currentPlayer) {
            return this._currentPlayer.canPlayMediaType(mediaType);
        }

        return false;
    };

    PlaybackManager.prototype.isMuted = function (player) {

        player = player || this._currentPlayer;

        if (player) {
            return player.isMuted();
        }

        return false;
    };

    PlaybackManager.prototype.setMute = function (mute, player) {

        player = player || this._currentPlayer;

        if (player) {
            player.setMute(mute);
        }
    };

    PlaybackManager.prototype.toggleMute = function (mute, player) {

        player = player || this._currentPlayer;
        if (player) {

            if (player.toggleMute) {
                player.toggleMute();
            } else {
                player.setMute(!player.isMuted());
            }
        }
    };

    PlaybackManager.prototype.toggleDisplayMirroring = function () {
        this.enableDisplayMirroring(!this.enableDisplayMirroring());
    };

    PlaybackManager.prototype.enableDisplayMirroring = function (enabled) {

        if (enabled != null) {

            var val = enabled ? '1' : '0';
            appSettings.set('displaymirror', val);
            return;
        }

        return (appSettings.get('displaymirror') || '') !== '0';
    };

    PlaybackManager.prototype.nextChapter = function (player) {

        player = player || this._currentPlayer;
        var item = this.currentItem(player);

        var ticks = this.getCurrentTicks(player);

        var nextChapter = (item.Chapters || []).filter(function (i) {

            return i.StartPositionTicks > ticks;

        })[0];

        if (nextChapter) {
            this.seek(nextChapter.StartPositionTicks, player);
        } else {
            this.nextTrack(player);
        }
    };

    PlaybackManager.prototype.previousChapter = function (player) {

        player = player || this._currentPlayer;
        var item = this.currentItem(player);

        var ticks = this.getCurrentTicks(player);

        // Go back 10 seconds
        ticks -= 100000000;

        // If there's no previous track, then at least rewind to beginning
        if (this.getCurrentPlaylistIndex(player) === 0) {
            ticks = Math.max(ticks, 0);
        }

        var previousChapters = (item.Chapters || []).filter(function (i) {

            return i.StartPositionTicks <= ticks;
        });

        if (previousChapters.length) {
            this.seek(previousChapters[previousChapters.length - 1].StartPositionTicks, player);
        } else {
            this.previousTrack(player);
        }
    };

    PlaybackManager.prototype.fastForward = function (player) {

        player = player || this._currentPlayer;

        if (player.fastForward != null) {
            player.fastForward(userSettings.skipForwardLength());
            return;
        }

        // Go back 15 seconds
        var offsetTicks = userSettings.skipForwardLength() * 10000;

        this.seekRelative(offsetTicks, player);
    };

    PlaybackManager.prototype.rewind = function (player) {

        player = player || this._currentPlayer;

        if (player.rewind != null) {
            player.rewind(userSettings.skipBackLength());
            return;
        }

        // Go back 15 seconds
        var offsetTicks = 0 - (userSettings.skipBackLength() * 10000);

        this.seekRelative(offsetTicks, player);
    };

    PlaybackManager.prototype.seekPercent = function (percent, player) {

        player = player || this._currentPlayer;

        var ticks = this.duration(player) || 0;

        percent /= 100;
        ticks *= percent;
        this.seek(parseInt(ticks), player);
    };

    PlaybackManager.prototype.playTrailers = function (item) {

        var player = this._currentPlayer;

        if (player && player.playTrailers) {
            return player.playTrailers(item);
        }

        var apiClient = connectionManager.getApiClient(item.ServerId);

        var instance = this;

        if (item.LocalTrailerCount) {
            return apiClient.getLocalTrailers(apiClient.getCurrentUserId(), item.Id).then(function (result) {
                return instance.play({
                    items: result
                });
            });
        } else {
            var remoteTrailers = item.RemoteTrailers || [];

            if (!remoteTrailers.length) {
                return Promise.reject();
            }

            return this.play({
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

    PlaybackManager.prototype.getSubtitleUrl = function (textStream, serverId) {

        var apiClient = connectionManager.getApiClient(serverId);
        var textStreamUrl = !textStream.IsExternalUrl ? apiClient.getUrl(textStream.DeliveryUrl) : textStream.DeliveryUrl;
        return textStreamUrl;
    };

    PlaybackManager.prototype.stop = function (player) {

        player = player || this._currentPlayer;

        if (player) {

            if (enableLocalPlaylistManagement(player)) {
                this._playNextAfterEnded = false;
            }

            // TODO: remove second param
            return player.stop(true, true);
        }

        return Promise.resolve();
    };

    PlaybackManager.prototype.getBufferedRanges = function (player) {

        player = player || this._currentPlayer;

        if (player) {

            if (player.getBufferedRanges) {
                return player.getBufferedRanges();
            }
        }

        return [];
    };

    PlaybackManager.prototype.playPause = function (player) {

        player = player || this._currentPlayer;

        if (player) {

            if (player.playPause) {
                return player.playPause();
            }

            if (player.paused()) {
                return this.unpause(player);
            } else {
                return this.pause(player);
            }
        }
    };

    PlaybackManager.prototype.paused = function (player) {

        player = player || this._currentPlayer;

        if (player) {
            return player.paused();
        }
    };

    PlaybackManager.prototype.pause = function (player) {
        player = player || this._currentPlayer;

        if (player) {
            player.pause();
        }
    };

    PlaybackManager.prototype.unpause = function (player) {
        player = player || this._currentPlayer;

        if (player) {
            player.unpause();
        }
    };

    PlaybackManager.prototype.instantMix = function (item, player) {

        player = player || this._currentPlayer;
        if (player && player.instantMix) {
            return player.instantMix(item);
        }

        var apiClient = connectionManager.getApiClient(item.ServerId);

        var options = {};
        options.UserId = apiClient.getCurrentUserId();
        options.Limit = 200;

        var instance = this;

        apiClient.getInstantMixFromItem(item.Id, options).then(function (result) {
            instance.play({
                items: result.Items
            });
        });
    };

    PlaybackManager.prototype.shuffle = function (shuffleItem, player, queryOptions) {

        player = player || this._currentPlayer;
        if (player && player.shuffle) {
            return player.shuffle(shuffleItem);
        }

        return this.play({ items: [shuffleItem], shuffle: true });
    };

    PlaybackManager.prototype.audioTracks = function (player) {

        player = player || this._currentPlayer;
        if (player.audioTracks) {
            var result = player.audioTracks();
            if (result) {
                return result;
            }
        }

        var mediaSource = this.currentMediaSource(player);

        var mediaStreams = (mediaSource || {}).MediaStreams || [];
        return mediaStreams.filter(function (s) {
            return s.Type === 'Audio';
        });
    };

    PlaybackManager.prototype.subtitleTracks = function (player) {

        player = player || this._currentPlayer;
        if (player.subtitleTracks) {
            var result = player.subtitleTracks();
            if (result) {
                return result;
            }
        }

        var mediaSource = this.currentMediaSource(player);

        var mediaStreams = (mediaSource || {}).MediaStreams || [];
        return mediaStreams.filter(function (s) {
            return s.Type === 'Subtitle';
        });
    };

    PlaybackManager.prototype.getSupportedCommands = function (player) {

        player = player || this._currentPlayer || { isLocalPlayer: true };

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
                "SetRepeatMode",
                "PlayMediaSource",
                "PlayTrailers"
            ];

            if (apphost.supports('fullscreenchange')) {
                list.push('ToggleFullscreen');
            }

            if (player.supports) {
                if (player.supports('PictureInPicture')) {
                    list.push('PictureInPicture');
                }
                if (player.supports('SetBrightness')) {
                    list.push('SetBrightness');
                }
                if (player.supports('SetAspectRatio')) {
                    list.push('SetAspectRatio');
                }
            }

            return list;
        }

        var info = this.getPlayerInfo();
        return info ? info.supportedCommands : [];
    };

    PlaybackManager.prototype.setRepeatMode = function (value, player) {

        player = player || this._currentPlayer;
        if (player && !enableLocalPlaylistManagement(player)) {
            return player.setRepeatMode(value);
        }

        this._playQueueManager.setRepeatMode(value);
        events.trigger(player, 'repeatmodechange');
    };

    PlaybackManager.prototype.getRepeatMode = function (player) {

        player = player || this._currentPlayer;
        if (player && !enableLocalPlaylistManagement(player)) {
            return player.getRepeatMode();
        }

        return this._playQueueManager.getRepeatMode();
    };

    PlaybackManager.prototype.trySetActiveDeviceName = function (name) {

        name = normalizeName(name);

        var instance = this;
        instance.getTargets().then(function (result) {

            var target = result.filter(function (p) {
                return normalizeName(p.name) === name;
            })[0];

            if (target) {
                instance.trySetActivePlayer(target.playerName, target);
            }

        });
    };

    PlaybackManager.prototype.displayContent = function (options, player) {
        player = player || this._currentPlayer;
        if (player && player.displayContent) {
            player.displayContent(options);
        }
    };

    PlaybackManager.prototype.beginPlayerUpdates = function (player) {
        if (player.beginPlayerUpdates) {
            player.beginPlayerUpdates();
        }
    };

    PlaybackManager.prototype.endPlayerUpdates = function (player) {
        if (player.endPlayerUpdates) {
            player.endPlayerUpdates();
        }
    };

    PlaybackManager.prototype.setDefaultPlayerActive = function () {

        this.setActivePlayer('localplayer');
    };

    PlaybackManager.prototype.removeActivePlayer = function (name) {

        var playerInfo = this.getPlayerInfo();
        if (playerInfo) {
            if (playerInfo.name === name) {
                this.setDefaultPlayerActive();
            }
        }
    };

    PlaybackManager.prototype.removeActiveTarget = function (id) {

        var playerInfo = this.getPlayerInfo();
        if (playerInfo) {
            if (playerInfo.id === id) {
                this.setDefaultPlayerActive();
            }
        }
    };

    PlaybackManager.prototype.sendCommand = function (cmd, player) {

        // Full list
        // https://github.com/MediaBrowser/MediaBrowser/blob/master/MediaBrowser.Model/Session/GeneralCommand.cs#L23
        console.log('MediaController received command: ' + cmd.Name);
        switch (cmd.Name) {

            case 'SetRepeatMode':
                this.setRepeatMode(cmd.Arguments.RepeatMode, player);
                break;
            case 'VolumeUp':
                this.volumeUp(player);
                break;
            case 'VolumeDown':
                this.volumeDown(player);
                break;
            case 'Mute':
                this.setMute(true, player);
                break;
            case 'Unmute':
                this.setMute(false, player);
                break;
            case 'ToggleMute':
                this.toggleMute(player);
                break;
            case 'SetVolume':
                this.setVolume(cmd.Arguments.Volume, player);
                break;
            case 'SetAspectRatio':
                this.setAspectRatio(cmd.Arguments.AspectRatio, player);
                break;
            case 'SetBrightness':
                this.setBrightness(cmd.Arguments.Brightness, player);
                break;
            case 'SetAudioStreamIndex':
                this.setAudioStreamIndex(parseInt(cmd.Arguments.Index), player);
                break;
            case 'SetSubtitleStreamIndex':
                this.setSubtitleStreamIndex(parseInt(cmd.Arguments.Index), player);
                break;
            case 'SetMaxStreamingBitrate':
                // todo
                //this.setMaxStreamingBitrate(parseInt(cmd.Arguments.Bitrate), player);
                break;
            case 'ToggleFullscreen':
                this.toggleFullscreen(player);
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

    return new PlaybackManager();
});