define(['appSettings', 'userSettings', 'playbackManager', 'connectionManager', 'globalize', 'events', 'require', 'castSenderApiLoader'], function (appSettings, userSettings, playbackManager, connectionManager, globalize, events, require, castSenderApiLoader) {
    'use strict';

    // Based on https://github.com/googlecast/CastVideos-chrome/blob/master/CastVideos.js
    var currentResolve;
    var currentReject;

    var PlayerName = 'Chromecast';

    function sendConnectionResult(isOk) {

        var resolve = currentResolve;
        var reject = currentReject;

        currentResolve = null;
        currentReject = null;

        if (isOk) {
            if (resolve) {
                resolve();
            }
        } else {
            if (reject) {
                reject();
            } else {
                playbackManager.removeActivePlayer(PlayerName);
            }
        }
    }

    /**
     * Constants of states for Chromecast device 
     **/
    var DEVICE_STATE = {
        'IDLE': 0,
        'ACTIVE': 1,
        'WARNING': 2,
        'ERROR': 3
    };

    /**
     * Constants of states for CastPlayer 
     **/
    var PLAYER_STATE = {
        'IDLE': 'IDLE',
        'LOADING': 'LOADING',
        'LOADED': 'LOADED',
        'PLAYING': 'PLAYING',
        'PAUSED': 'PAUSED',
        'STOPPED': 'STOPPED',
        'SEEKING': 'SEEKING',
        'ERROR': 'ERROR'
    };

    var applicationID = "2D4B1DA3";

    // This is the beta version used for testing new changes

    //applicationID = '27C4EB5B';

    var messageNamespace = 'urn:x-cast:com.connectsdk';

    var CastPlayer = function () {

        /* device variables */
        // @type {DEVICE_STATE} A state for device
        this.deviceState = DEVICE_STATE.IDLE;

        /* Cast player variables */
        // @type {Object} a chrome.cast.media.Media object
        this.currentMediaSession = null;

        // @type {string} a chrome.cast.Session object
        this.session = null;
        // @type {PLAYER_STATE} A state for Cast media player
        this.castPlayerState = PLAYER_STATE.IDLE;

        this.hasReceivers = false;

        // bind once - commit 2ebffc2271da0bc5e8b13821586aee2a2e3c7753
        this.errorHandler = this.onError.bind(this);
        this.mediaStatusUpdateHandler = this.onMediaStatusUpdate.bind(this);

        this.initializeCastPlayer();
    };

    /**
     * Initialize Cast media player 
     * Initializes the API. Note that either successCallback and errorCallback will be
     * invoked once the API has finished initialization. The sessionListener and 
     * receiverListener may be invoked at any time afterwards, and possibly more than once. 
     */
    CastPlayer.prototype.initializeCastPlayer = function () {

        var chrome = window.chrome;

        if (!chrome) {
            return;
        }

        if (!chrome.cast || !chrome.cast.isAvailable) {

            setTimeout(this.initializeCastPlayer.bind(this), 1000);
            return;
        }

        // request session
        var sessionRequest = new chrome.cast.SessionRequest(applicationID);
        var apiConfig = new chrome.cast.ApiConfig(sessionRequest,
            this.sessionListener.bind(this),
            this.receiverListener.bind(this),
            "origin_scoped");

        console.log('chromecast.initialize');

        chrome.cast.initialize(apiConfig, this.onInitSuccess.bind(this), this.errorHandler);

    };

    /**
     * Callback function for init success 
     */
    CastPlayer.prototype.onInitSuccess = function () {
        this.isInitialized = true;
        console.log("chromecast init success");
    };

    /**
     * Generic error callback function 
     */
    CastPlayer.prototype.onError = function () {
        console.log("chromecast error");
    };

    /**
     * @param {!Object} e A new session
     * This handles auto-join when a page is reloaded
     * When active session is detected, playback will automatically
     * join existing session and occur in Cast mode and media
     * status gets synced up with current media of the session 
     */
    CastPlayer.prototype.sessionListener = function (e) {

        this.session = e;
        if (this.session) {

            //console.log('sessionListener ' + JSON.stringify(e));

            if (this.session.media[0]) {
                this.onMediaDiscovered('activeSession', this.session.media[0]);
            }

            this.onSessionConnected(e);
        }
    };

    function alertText(text, title) {
        require(['alert'], function (alert) {
            alert({
                text: text,
                title: title
            });
        });
    }

    CastPlayer.prototype.messageListener = function (namespace, message) {

        if (typeof (message) === 'string') {
            message = JSON.parse(message);
        }

        if (message.type === 'playbackerror') {

            var errorCode = message.data;

            setTimeout(function () {
                alertText(globalize.translate('MessagePlaybackError' + errorCode), globalize.translate('HeaderPlaybackError'));
            }, 300);

        }
        else if (message.type === 'connectionerror') {

            setTimeout(function () {
                alertText(globalize.translate('MessageChromecastConnectionError'), globalize.translate('HeaderError'));
            }, 300);

        }
        else if (message.type) {
            events.trigger(this, message.type, [message.data]);
        }
    };

    /**
     * @param {string} e Receiver availability
     * This indicates availability of receivers but
     * does not provide a list of device IDs
     */
    CastPlayer.prototype.receiverListener = function (e) {

        if (e === 'available') {
            //console.log("chromecast receiver found");
            this.hasReceivers = true;
        }
        else {
            //console.log("chromecast receiver list empty");
            this.hasReceivers = false;
        }
    };

    /**
     * session update listener
     */
    CastPlayer.prototype.sessionUpdateListener = function (isAlive) {

        //console.log('sessionUpdateListener alive: ' + isAlive);

        if (isAlive) {
        }
        else {
            this.session = null;
            this.deviceState = DEVICE_STATE.IDLE;
            this.castPlayerState = PLAYER_STATE.IDLE;

            //console.log('sessionUpdateListener: setting currentMediaSession to null');
            this.currentMediaSession = null;

            sendConnectionResult(false);
        }
    };

    /**
     * Requests that a receiver application session be created or joined. By default, the SessionRequest
     * passed to the API at initialization time is used; this may be overridden by passing a different
     * session request in opt_sessionRequest. 
     */
    CastPlayer.prototype.launchApp = function () {
        //console.log("chromecast launching app...");
        chrome.cast.requestSession(this.onRequestSessionSuccess.bind(this), this.onLaunchError.bind(this));
    };

    /**
     * Callback function for request session success 
     * @param {Object} e A chrome.cast.Session object
     */
    CastPlayer.prototype.onRequestSessionSuccess = function (e) {

        //console.log("chromecast session success: " + e.sessionId);
        this.onSessionConnected(e);
    };

    CastPlayer.prototype.onSessionConnected = function (session) {

        this.session = session;

        this.deviceState = DEVICE_STATE.ACTIVE;

        this.session.addMessageListener(messageNamespace, this.messageListener.bind(this));
        this.session.addMediaListener(this.sessionMediaListener.bind(this));
        this.session.addUpdateListener(this.sessionUpdateListener.bind(this));

        events.trigger(this, 'connect');

        this.sendMessage({
            options: {},
            command: 'Identify'
        });
    };

    /**
     * session update listener
     */
    CastPlayer.prototype.sessionMediaListener = function (e) {

        //console.log('sessionMediaListener');
        this.currentMediaSession = e;
        this.currentMediaSession.addUpdateListener(this.mediaStatusUpdateHandler);
    };

    /**
     * Callback function for launch error
     */
    CastPlayer.prototype.onLaunchError = function () {
        //console.log("chromecast launch error");
        this.deviceState = DEVICE_STATE.ERROR;

        sendConnectionResult(false);
    };

    /**
     * Stops the running receiver application associated with the session.
     */
    CastPlayer.prototype.stopApp = function () {

        if (this.session) {
            this.session.stop(this.onStopAppSuccess.bind(this, 'Session stopped'),
                this.errorHandler);
        }

    };

    /**
     * Callback function for stop app success 
     */
    CastPlayer.prototype.onStopAppSuccess = function (message) {
        //console.log(message);
        this.deviceState = DEVICE_STATE.IDLE;
        this.castPlayerState = PLAYER_STATE.IDLE;

        //console.log('onStopAppSuccess: setting currentMediaSession to null');
        this.currentMediaSession = null;
    };

    /**
     * Loads media into a running receiver application
     * @param {Number} mediaIndex An index number to indicate current media content
     */
    CastPlayer.prototype.loadMedia = function (options, command) {

        if (!this.session) {
            //console.log("no session");
            return Promise.reject();
        }

        // Convert the items to smaller stubs to send the minimal amount of information
        options.items = options.items.map(function (i) {

            return {
                Id: i.Id,
                ServerId: i.ServerId,
                Name: i.Name,
                Type: i.Type,
                MediaType: i.MediaType,
                IsFolder: i.IsFolder
            };
        });

        return this.sendMessage({
            options: options,
            command: command
        });
    };

    CastPlayer.prototype.sendMessage = function (message) {

        var player = this;

        var receiverName = null;

        var session = player.session;

        if (session && session.receiver && session.receiver.friendlyName) {
            receiverName = session.receiver.friendlyName;
        }

        var apiClient;
        if (message.options && message.options.ServerId) {
            apiClient = connectionManager.getApiClient(message.options.ServerId);
        } else if (message.options && message.options.items && message.options.items.length) {
            apiClient = connectionManager.getApiClient(message.options.items[0].ServerId);
        } else {
            apiClient = connectionManager.currentApiClient();
        }

        message = Object.assign(message, {
            userId: apiClient.getCurrentUserId(),
            deviceId: apiClient.deviceId(),
            accessToken: apiClient.accessToken(),
            serverAddress: apiClient.serverAddress(),
            serverId: apiClient.serverId(),
            serverVersion: apiClient.serverVersion(),
            receiverName: receiverName
        });

        var bitrateSetting = appSettings.maxChromecastBitrate();
        if (bitrateSetting) {
            message.maxBitrate = bitrateSetting;
        }

        if (message.options && message.options.items) {
            message.subtitleAppearance = userSettings.getSubtitleAppearanceSettings();
            message.subtitleBurnIn = appSettings.get('subtitleburnin') || '';
        }

        return new Promise(function (resolve, reject) {

            require(['chromecastHelper'], function (chromecastHelper) {

                chromecastHelper.getServerAddress(apiClient).then(function (serverAddress) {
                    message.serverAddress = serverAddress;
                    player.sendMessageInternal(message).then(resolve, reject);

                }, reject);
            });
        });
    };

    CastPlayer.prototype.sendMessageInternal = function (message) {

        message = JSON.stringify(message);
        //console.log(message);

        this.session.sendMessage(messageNamespace, message, this.onPlayCommandSuccess.bind(this), this.errorHandler);
        return Promise.resolve();
    };

    CastPlayer.prototype.onPlayCommandSuccess = function () {
        //console.log('Message was sent to receiver ok.');
    };

    /**
     * Callback function for loadMedia success
     * @param {Object} mediaSession A new media object.
     */
    CastPlayer.prototype.onMediaDiscovered = function (how, mediaSession) {

        //console.log("chromecast new media session ID:" + mediaSession.mediaSessionId + ' (' + how + ')');
        this.currentMediaSession = mediaSession;

        if (how === 'loadMedia') {
            this.castPlayerState = PLAYER_STATE.PLAYING;
        }

        if (how === 'activeSession') {
            this.castPlayerState = mediaSession.playerState;
        }

        this.currentMediaSession.addUpdateListener(this.mediaStatusUpdateHandler);
    };

    /**
     * Callback function for media status update from receiver
     * @param {!Boolean} e true/false
     */
    CastPlayer.prototype.onMediaStatusUpdate = function (e) {

        if (e === false) {
            this.castPlayerState = PLAYER_STATE.IDLE;
        }
        //console.log("chromecast updating media: " + e);
    };

    /**
     * Set media volume in Cast mode
     * @param {Boolean} mute A boolean  
     */
    CastPlayer.prototype.setReceiverVolume = function (mute, vol) {

        if (!this.currentMediaSession) {
            //console.log('this.currentMediaSession is null');
            return;
        }

        if (!mute) {

            this.session.setReceiverVolumeLevel((vol || 1),
                this.mediaCommandSuccessCallback.bind(this),
                this.errorHandler);
        }
        else {
            this.session.setReceiverMuted(true,
                this.mediaCommandSuccessCallback.bind(this),
                this.errorHandler);
        }
    };

    /**
     * Mute CC
     */
    CastPlayer.prototype.mute = function () {
        this.setReceiverVolume(true);
    };

    /**
     * Callback function for media command success 
     */
    CastPlayer.prototype.mediaCommandSuccessCallback = function (info, e) {
        //console.log(info);
    };

    function normalizeImages(state) {

        if (state && state.NowPlayingItem) {

            var item = state.NowPlayingItem;

            if (!item.ImageTags || !item.ImageTags.Primary) {
                if (item.PrimaryImageTag) {
                    item.ImageTags = item.ImageTags || {};
                    item.ImageTags.Primary = item.PrimaryImageTag;
                }
            }
            if (item.BackdropImageTag && item.BackdropItemId === item.Id) {
                item.BackdropImageTags = [item.BackdropImageTag];
            }
            if (item.BackdropImageTag && item.BackdropItemId !== item.Id) {
                item.ParentBackdropImageTags = [item.BackdropImageTag];
                item.ParentBackdropItemId = item.BackdropItemId;
            }
        }
    }

    function getItemsForPlayback(apiClient, query) {

        var userId = apiClient.getCurrentUserId();

        if (query.Ids && query.Ids.split(',').length === 1) {
            return apiClient.getItem(userId, query.Ids.split(',')).then(function (item) {
                return {
                    Items: [item],
                    TotalRecordCount: 1
                };
            });
        }
        else {

            query.Limit = query.Limit || 100;
            query.ExcludeLocationTypes = "Virtual";
            query.EnableTotalRecordCount = false;

            return apiClient.getItems(userId, query);
        }
    }

    function bindEventForRelay(instance, eventName) {

        events.on(instance._castPlayer, eventName, function (e, data) {

            //console.log('cc: ' + eventName);
            var state = instance.getPlayerStateInternal(data);

            events.trigger(instance, eventName, [state]);
        });
    }

    function initializeChromecast() {

        var instance = this;
        instance._castPlayer = new CastPlayer();

        // To allow the native android app to override
        document.dispatchEvent(new CustomEvent("chromecastloaded", {
            detail: {
                player: instance
            }
        }));

        events.on(instance._castPlayer, "connect", function (e) {

            if (currentResolve) {
                sendConnectionResult(true);
            } else {
                playbackManager.setActivePlayer(PlayerName, instance.getCurrentTargetInfo());
            }

            console.log('cc: connect');
            // Reset this so that statechange will fire
            instance.lastPlayerData = null;
        });

        events.on(instance._castPlayer, "playbackstart", function (e, data) {

            console.log('cc: playbackstart');

            instance._castPlayer.initializeCastPlayer();

            var state = instance.getPlayerStateInternal(data);
            events.trigger(instance, "playbackstart", [state]);
        });

        events.on(instance._castPlayer, "playbackstop", function (e, data) {

            console.log('cc: playbackstop');
            var state = instance.getPlayerStateInternal(data);

            events.trigger(instance, "playbackstop", [state]);

            // Reset this so the next query doesn't make it appear like content is playing.
            instance.lastPlayerData = {};
        });

        events.on(instance._castPlayer, "playbackprogress", function (e, data) {

            //console.log('cc: positionchange');
            var state = instance.getPlayerStateInternal(data);

            events.trigger(instance, "timeupdate", [state]);
        });

        bindEventForRelay(instance, 'timeupdate');
        bindEventForRelay(instance, 'pause');
        bindEventForRelay(instance, 'unpause');
        bindEventForRelay(instance, 'volumechange');
        bindEventForRelay(instance, 'repeatmodechange');

        events.on(instance._castPlayer, "playstatechange", function (e, data) {

            //console.log('cc: playstatechange');
            var state = instance.getPlayerStateInternal(data);

            events.trigger(instance, "pause", [state]);
        });
    }

    function ChromecastPlayer() {

        // playbackManager needs this
        this.name = PlayerName;
        this.type = 'mediaplayer';
        this.id = 'chromecast';
        this.isLocalPlayer = false;
        this.lastPlayerData = {};

        castSenderApiLoader.load().then(initializeChromecast.bind(this));
    }

    ChromecastPlayer.prototype.tryPair = function (target) {

        var castPlayer = this._castPlayer;

        if (castPlayer.deviceState !== DEVICE_STATE.ACTIVE && castPlayer.isInitialized) {

            return new Promise(function (resolve, reject) {
                currentResolve = resolve;
                currentReject = reject;

                castPlayer.launchApp();
            });
        } else {

            currentResolve = null;
            currentReject = null;

            return Promise.reject();
        }
    };

    ChromecastPlayer.prototype.getTargets = function () {

        var targets = [];

        if (this._castPlayer && this._castPlayer.hasReceivers) {
            targets.push(this.getCurrentTargetInfo());
        }

        return Promise.resolve(targets);
    };

    // This is a privately used method
    ChromecastPlayer.prototype.getCurrentTargetInfo = function () {

        var appName = null;

        var castPlayer = this._castPlayer;

        if (castPlayer.session && castPlayer.session.receiver && castPlayer.session.receiver.friendlyName) {
            appName = castPlayer.session.receiver.friendlyName;
        }

        return {
            name: PlayerName,
            id: PlayerName,
            playerName: PlayerName,
            playableMediaTypes: ["Audio", "Video"],
            isLocalPlayer: false,
            appName: PlayerName,
            deviceName: appName,
            supportedCommands: [
                "VolumeUp",
                "VolumeDown",
                "Mute",
                "Unmute",
                "ToggleMute",
                "SetVolume",
                "SetAudioStreamIndex",
                "SetSubtitleStreamIndex",
                "DisplayContent",
                "SetRepeatMode",
                "EndSession",
                "PlayMediaSource",
                "PlayTrailers"
            ]
        };
    };

    ChromecastPlayer.prototype.getPlayerStateInternal = function (data) {

        var triggerStateChange = false;
        if (data && !this.lastPlayerData) {
            triggerStateChange = true;
        }

        data = data || this.lastPlayerData;
        this.lastPlayerData = data;

        normalizeImages(data);

        //console.log(JSON.stringify(data));

        if (triggerStateChange) {
            events.trigger(this, "statechange", [data]);
        }

        return data;
    };

    ChromecastPlayer.prototype.playWithCommand = function (options, command) {

        if (!options.items) {
            var apiClient = connectionManager.getApiClient(options.serverId);
            var instance = this;

            return apiClient.getItem(apiClient.getCurrentUserId(), options.ids[0]).then(function (item) {

                options.items = [item];
                return instance.playWithCommand(options, command);
            });
        }

        return this._castPlayer.loadMedia(options, command);
    };

    ChromecastPlayer.prototype.seek = function (position) {

        position = parseInt(position);

        position = position / 10000000;

        this._castPlayer.sendMessage({
            options: {
                position: position
            },
            command: 'Seek'
        });
    };

    ChromecastPlayer.prototype.setAudioStreamIndex = function (index) {
        this._castPlayer.sendMessage({
            options: {
                index: index
            },
            command: 'SetAudioStreamIndex'
        });
    };

    ChromecastPlayer.prototype.setSubtitleStreamIndex = function (index) {
        this._castPlayer.sendMessage({
            options: {
                index: index
            },
            command: 'SetSubtitleStreamIndex'
        });
    };

    ChromecastPlayer.prototype.setMaxStreamingBitrate = function (options) {

        this._castPlayer.sendMessage({
            options: options,
            command: 'SetMaxStreamingBitrate'
        });
    };

    ChromecastPlayer.prototype.isFullscreen = function () {
        var state = this.lastPlayerData || {};
        state = state.PlayState || {};
        return state.IsFullscreen;
    };

    ChromecastPlayer.prototype.nextTrack = function () {
        this._castPlayer.sendMessage({
            options: {},
            command: 'NextTrack'
        });
    };

    ChromecastPlayer.prototype.previousTrack = function () {
        this._castPlayer.sendMessage({
            options: {},
            command: 'PreviousTrack'
        });
    };

    ChromecastPlayer.prototype.volumeDown = function () {

        this._castPlayer.sendMessage({
            options: {},
            command: 'VolumeDown'
        });
    };

    ChromecastPlayer.prototype.endSession = function () {

        var instance = this;

        this.stop().then(function () {
            setTimeout(function () {
                instance._castPlayer.stopApp();
            }, 1000);
        });
    };

    ChromecastPlayer.prototype.volumeUp = function () {

        this._castPlayer.sendMessage({
            options: {},
            command: 'VolumeUp'
        });
    };

    ChromecastPlayer.prototype.setVolume = function (vol) {

        vol = Math.min(vol, 100);
        vol = Math.max(vol, 0);

        this._castPlayer.sendMessage({
            options: {
                volume: vol
            },
            command: 'SetVolume'
        });
    };

    ChromecastPlayer.prototype.unpause = function () {
        this._castPlayer.sendMessage({
            options: {},
            command: 'Unpause'
        });
    };

    ChromecastPlayer.prototype.playPause = function () {
        this._castPlayer.sendMessage({
            options: {},
            command: 'PlayPause'
        });
    };

    ChromecastPlayer.prototype.pause = function () {
        this._castPlayer.sendMessage({
            options: {},
            command: 'Pause'
        });
    };

    ChromecastPlayer.prototype.stop = function () {
        return this._castPlayer.sendMessage({
            options: {},
            command: 'Stop'
        });
    };

    ChromecastPlayer.prototype.displayContent = function (options) {

        this._castPlayer.sendMessage({
            options: options,
            command: 'DisplayContent'
        });
    };

    ChromecastPlayer.prototype.setMute = function (isMuted) {

        var castPlayer = this._castPlayer;

        if (isMuted) {
            castPlayer.sendMessage({
                options: {},
                command: 'Mute'
            });
        } else {
            castPlayer.sendMessage({
                options: {},
                command: 'Unmute'
            });
        }
    };

    ChromecastPlayer.prototype.getRepeatMode = function () {
        var state = this.lastPlayerData || {};
        state = state.PlayState || {};
        return state.RepeatMode;
    };

    ChromecastPlayer.prototype.playTrailers = function (item) {

        this._castPlayer.sendMessage({
            options: {
                ItemId: item.Id,
                ServerId: item.ServerId
            },
            command: 'PlayTrailers'
        });
    };

    ChromecastPlayer.prototype.setRepeatMode = function (mode) {
        this._castPlayer.sendMessage({
            options: {
                RepeatMode: mode
            },
            command: 'SetRepeatMode'
        });
    };

    ChromecastPlayer.prototype.toggleMute = function () {

        this._castPlayer.sendMessage({
            options: {},
            command: 'ToggleMute'
        });
    };

    ChromecastPlayer.prototype.audioTracks = function () {
        var state = this.lastPlayerData || {};
        state = state.NowPlayingItem || {};
        var streams = state.MediaStreams || [];
        return streams.filter(function (s) {
            return s.Type === 'Audio';
        });
    };

    ChromecastPlayer.prototype.getAudioStreamIndex = function () {
        var state = this.lastPlayerData || {};
        state = state.PlayState || {};
        return state.AudioStreamIndex;
    };

    ChromecastPlayer.prototype.subtitleTracks = function () {
        var state = this.lastPlayerData || {};
        state = state.NowPlayingItem || {};
        var streams = state.MediaStreams || [];
        return streams.filter(function (s) {
            return s.Type === 'Subtitle';
        });
    };

    ChromecastPlayer.prototype.getSubtitleStreamIndex = function () {
        var state = this.lastPlayerData || {};
        state = state.PlayState || {};
        return state.SubtitleStreamIndex;
    };

    ChromecastPlayer.prototype.getMaxStreamingBitrate = function () {
        var state = this.lastPlayerData || {};
        state = state.PlayState || {};
        return state.MaxStreamingBitrate;
    };

    ChromecastPlayer.prototype.getVolume = function () {

        var state = this.lastPlayerData || {};
        state = state.PlayState || {};

        return state.VolumeLevel == null ? 100 : state.VolumeLevel;
    };

    ChromecastPlayer.prototype.isPlaying = function () {
        var state = this.lastPlayerData || {};
        return state.NowPlayingItem != null;
    };

    ChromecastPlayer.prototype.isPlayingVideo = function () {
        var state = this.lastPlayerData || {};
        state = state.NowPlayingItem || {};
        return state.MediaType === 'Video';
    };

    ChromecastPlayer.prototype.isPlayingAudio = function () {
        var state = this.lastPlayerData || {};
        state = state.NowPlayingItem || {};
        return state.MediaType === 'Audio';
    };

    ChromecastPlayer.prototype.currentTime = function (val) {

        if (val != null) {
            return this.seek(val);
        }

        var state = this.lastPlayerData || {};
        state = state.PlayState || {};
        return state.PositionTicks;
    };

    ChromecastPlayer.prototype.duration = function () {
        var state = this.lastPlayerData || {};
        state = state.NowPlayingItem || {};
        return state.RunTimeTicks;
    };

    ChromecastPlayer.prototype.getBufferedRanges = function () {
        var state = this.lastPlayerData || {};
        state = state.PlayState || {};
        return state.BufferedRanges || [];
    };

    ChromecastPlayer.prototype.paused = function () {
        var state = this.lastPlayerData || {};
        state = state.PlayState || {};

        return state.IsPaused;
    };

    ChromecastPlayer.prototype.isMuted = function () {
        var state = this.lastPlayerData || {};
        state = state.PlayState || {};

        return state.IsMuted;
    };

    ChromecastPlayer.prototype.shuffle = function (item) {

        var apiClient = connectionManager.getApiClient(item.ServerId);
        var userId = apiClient.getCurrentUserId();

        var instance = this;

        apiClient.getItem(userId, item.Id).then(function (item) {

            instance.playWithCommand({

                items: [item]

            }, 'Shuffle');

        });

    };

    ChromecastPlayer.prototype.instantMix = function (item) {

        var apiClient = connectionManager.getApiClient(item.ServerId);
        var userId = apiClient.getCurrentUserId();

        var instance = this;

        apiClient.getItem(userId, item.Id).then(function (item) {

            instance.playWithCommand({

                items: [item]

            }, 'InstantMix');

        });

    };

    ChromecastPlayer.prototype.canPlayMediaType = function (mediaType) {

        mediaType = (mediaType || '').toLowerCase();
        return mediaType === 'audio' || mediaType === 'video';
    };

    ChromecastPlayer.prototype.canQueueMediaType = function (mediaType) {
        return this.canPlayMediaType(mediaType);
    };

    ChromecastPlayer.prototype.queue = function (options) {
        this.playWithCommand(options, 'PlayLast');
    };

    ChromecastPlayer.prototype.queueNext = function (options) {
        this.playWithCommand(options, 'PlayNext');
    };

    ChromecastPlayer.prototype.play = function (options) {

        if (options.items) {

            return this.playWithCommand(options, 'PlayNow');

        } else {

            if (!options.serverId) {
                throw new Error('serverId required!');
            }

            var instance = this;
            var apiClient = connectionManager.getApiClient(options.serverId);

            return getItemsForPlayback(apiClient, {

                Ids: options.ids.join(',')

            }).then(function (result) {

                options.items = result.Items;
                return instance.playWithCommand(options, 'PlayNow');

            });
        }
    };

    ChromecastPlayer.prototype.toggleFullscreen = function () {
        // not supported
    };

    ChromecastPlayer.prototype.beginPlayerUpdates = function () {
        // Setup polling here
    };

    ChromecastPlayer.prototype.endPlayerUpdates = function () {
        // Stop polling here
    };

    ChromecastPlayer.prototype.getPlaylist = function () {
        return Promise.resolve([]);
    };

    ChromecastPlayer.prototype.getCurrentPlaylistItemId = function () {
    };

    ChromecastPlayer.prototype.setCurrentPlaylistItem = function (playlistItemId) {
        return Promise.resolve();
    };

    ChromecastPlayer.prototype.removeFromPlaylist = function (playlistItemIds) {
        return Promise.resolve();
    };

    ChromecastPlayer.prototype.getPlayerState = function () {

        return this.getPlayerStateInternal() || {};
    };

    return ChromecastPlayer;
});