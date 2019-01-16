define(["appSettings", "userSettings", "playbackManager", "connectionManager", "globalize", "events", "require", "castSenderApiLoader"], function(appSettings, userSettings, playbackManager, connectionManager, globalize, events, require, castSenderApiLoader) {
    "use strict";

    function sendConnectionResult(isOk) {
        var resolve = currentResolve,
            reject = currentReject;
        currentResolve = null, currentReject = null, isOk ? resolve && resolve() : reject ? reject() : playbackManager.removeActivePlayer(PlayerName)
    }

    function alertText(text, title) {
        require(["alert"], function(alert) {
            alert({
                text: text,
                title: title
            })
        })
    }

    function normalizeImages(state) {
        if (state && state.NowPlayingItem) {
            var item = state.NowPlayingItem;
            item.ImageTags && item.ImageTags.Primary || item.PrimaryImageTag && (item.ImageTags = item.ImageTags || {}, item.ImageTags.Primary = item.PrimaryImageTag), item.BackdropImageTag && item.BackdropItemId === item.Id && (item.BackdropImageTags = [item.BackdropImageTag]), item.BackdropImageTag && item.BackdropItemId !== item.Id && (item.ParentBackdropImageTags = [item.BackdropImageTag], item.ParentBackdropItemId = item.BackdropItemId)
        }
    }

    function getItemsForPlayback(apiClient, query) {
        var userId = apiClient.getCurrentUserId();
        return query.Ids && 1 === query.Ids.split(",").length ? apiClient.getItem(userId, query.Ids.split(",")).then(function(item) {
            return {
                Items: [item],
                TotalRecordCount: 1
            }
        }) : (query.Limit = query.Limit || 100, query.ExcludeLocationTypes = "Virtual", query.EnableTotalRecordCount = !1, apiClient.getItems(userId, query))
    }

    function bindEventForRelay(instance, eventName) {
        events.on(instance._castPlayer, eventName, function(e, data) {
            var state = instance.getPlayerStateInternal(data);
            events.trigger(instance, eventName, [state])
        })
    }

    function initializeChromecast() {
        var instance = this;
        instance._castPlayer = new CastPlayer, document.dispatchEvent(new CustomEvent("chromecastloaded", {
            detail: {
                player: instance
            }
        })), events.on(instance._castPlayer, "connect", function(e) {
            currentResolve ? sendConnectionResult(!0) : playbackManager.setActivePlayer(PlayerName, instance.getCurrentTargetInfo()), console.log("cc: connect"), instance.lastPlayerData = null
        }), events.on(instance._castPlayer, "playbackstart", function(e, data) {
            console.log("cc: playbackstart"), instance._castPlayer.initializeCastPlayer();
            var state = instance.getPlayerStateInternal(data);
            events.trigger(instance, "playbackstart", [state])
        }), events.on(instance._castPlayer, "playbackstop", function(e, data) {
            console.log("cc: playbackstop");
            var state = instance.getPlayerStateInternal(data);
            events.trigger(instance, "playbackstop", [state]), instance.lastPlayerData = {}
        }), events.on(instance._castPlayer, "playbackprogress", function(e, data) {
            var state = instance.getPlayerStateInternal(data);
            events.trigger(instance, "timeupdate", [state])
        }), bindEventForRelay(instance, "timeupdate"), bindEventForRelay(instance, "pause"), bindEventForRelay(instance, "unpause"), bindEventForRelay(instance, "volumechange"), bindEventForRelay(instance, "repeatmodechange"), events.on(instance._castPlayer, "playstatechange", function(e, data) {
            var state = instance.getPlayerStateInternal(data);
            events.trigger(instance, "pause", [state])
        })
    }

    function ChromecastPlayer() {
        this.name = PlayerName, this.type = "mediaplayer", this.id = "chromecast", this.isLocalPlayer = !1, this.lastPlayerData = {}, castSenderApiLoader.load().then(initializeChromecast.bind(this))
    }
    var currentResolve, currentReject, PlayerName = "Chromecast",
        DEVICE_STATE = {
            IDLE: 0,
            ACTIVE: 1,
            WARNING: 2,
            ERROR: 3
        },
        PLAYER_STATE = {
            IDLE: "IDLE",
            LOADING: "LOADING",
            LOADED: "LOADED",
            PLAYING: "PLAYING",
            PAUSED: "PAUSED",
            STOPPED: "STOPPED",
            SEEKING: "SEEKING",
            ERROR: "ERROR"
        },
        CastPlayer = function() {
            this.deviceState = DEVICE_STATE.IDLE, this.currentMediaSession = null, this.session = null, this.castPlayerState = PLAYER_STATE.IDLE, this.hasReceivers = !1, this.errorHandler = this.onError.bind(this), this.mediaStatusUpdateHandler = this.onMediaStatusUpdate.bind(this), this.initializeCastPlayer()
        };
    return CastPlayer.prototype.initializeCastPlayer = function() {
        var chrome = window.chrome;
        if (chrome) {
            if (!chrome.cast || !chrome.cast.isAvailable) return void setTimeout(this.initializeCastPlayer.bind(this), 1e3);
            var sessionRequest = new chrome.cast.SessionRequest("F007D354"),
                apiConfig = new chrome.cast.ApiConfig(sessionRequest, this.sessionListener.bind(this), this.receiverListener.bind(this), "origin_scoped");
            console.log("chromecast.initialize"), chrome.cast.initialize(apiConfig, this.onInitSuccess.bind(this), this.errorHandler)
        }
    }, CastPlayer.prototype.onInitSuccess = function() {
        this.isInitialized = !0, console.log("chromecast init success")
    }, CastPlayer.prototype.onError = function() {
        console.log("chromecast error")
    }, CastPlayer.prototype.sessionListener = function(e) {
        this.session = e, this.session && (this.session.media[0] && this.onMediaDiscovered("activeSession", this.session.media[0]), this.onSessionConnected(e))
    }, CastPlayer.prototype.messageListener = function(namespace, message) {
        if ("string" == typeof message && (message = JSON.parse(message)), "playbackerror" === message.type) {
            var errorCode = message.data;
            setTimeout(function() {
                alertText(globalize.translate("MessagePlaybackError" + errorCode), globalize.translate("HeaderPlaybackError"))
            }, 300)
        } else "connectionerror" === message.type ? setTimeout(function() {
            alertText(globalize.translate("MessageChromecastConnectionError"), globalize.translate("HeaderError"))
        }, 300) : message.type && events.trigger(this, message.type, [message.data])
    }, CastPlayer.prototype.receiverListener = function(e) {
        this.hasReceivers = "available" === e
    }, CastPlayer.prototype.sessionUpdateListener = function(isAlive) {
        isAlive || (this.session = null, this.deviceState = DEVICE_STATE.IDLE, this.castPlayerState = PLAYER_STATE.IDLE, this.currentMediaSession = null, sendConnectionResult(!1))
    }, CastPlayer.prototype.launchApp = function() {
        chrome.cast.requestSession(this.onRequestSessionSuccess.bind(this), this.onLaunchError.bind(this))
    }, CastPlayer.prototype.onRequestSessionSuccess = function(e) {
        this.onSessionConnected(e)
    }, CastPlayer.prototype.onSessionConnected = function(session) {
        this.session = session, this.deviceState = DEVICE_STATE.ACTIVE, this.session.addMessageListener("urn:x-cast:com.connectsdk", this.messageListener.bind(this)), this.session.addMediaListener(this.sessionMediaListener.bind(this)), this.session.addUpdateListener(this.sessionUpdateListener.bind(this)), events.trigger(this, "connect"), this.sendMessage({
            options: {},
            command: "Identify"
        })
    }, CastPlayer.prototype.sessionMediaListener = function(e) {
        this.currentMediaSession = e, this.currentMediaSession.addUpdateListener(this.mediaStatusUpdateHandler)
    }, CastPlayer.prototype.onLaunchError = function() {
        this.deviceState = DEVICE_STATE.ERROR, sendConnectionResult(!1)
    }, CastPlayer.prototype.stopApp = function() {
        this.session && this.session.stop(this.onStopAppSuccess.bind(this, "Session stopped"), this.errorHandler)
    }, CastPlayer.prototype.onStopAppSuccess = function(message) {
        this.deviceState = DEVICE_STATE.IDLE, this.castPlayerState = PLAYER_STATE.IDLE, this.currentMediaSession = null
    }, CastPlayer.prototype.loadMedia = function(options, command) {
        return this.session ? (options.items = options.items.map(function(i) {
            return {
                Id: i.Id,
                ServerId: i.ServerId,
                Name: i.Name,
                Type: i.Type,
                MediaType: i.MediaType,
                IsFolder: i.IsFolder
            }
        }), this.sendMessage({
            options: options,
            command: command
        })) : Promise.reject()
    }, CastPlayer.prototype.sendMessage = function(message) {
        var player = this,
            receiverName = null,
            session = player.session;
        session && session.receiver && session.receiver.friendlyName && (receiverName = session.receiver.friendlyName);
        var apiClient;
        apiClient = message.options && message.options.ServerId ? connectionManager.getApiClient(message.options.ServerId) : message.options && message.options.items && message.options.items.length ? connectionManager.getApiClient(message.options.items[0].ServerId) : connectionManager.currentApiClient(), message = Object.assign(message, {
            userId: apiClient.getCurrentUserId(),
            deviceId: apiClient.deviceId(),
            accessToken: apiClient.accessToken(),
            serverAddress: apiClient.serverAddress(),
            serverId: apiClient.serverId(),
            serverVersion: apiClient.serverVersion(),
            receiverName: receiverName
        });
        var bitrateSetting = appSettings.maxChromecastBitrate();
        return bitrateSetting && (message.maxBitrate = bitrateSetting), message.options && message.options.items && (message.subtitleAppearance = userSettings.getSubtitleAppearanceSettings(), message.subtitleBurnIn = appSettings.get("subtitleburnin") || ""), new Promise(function(resolve, reject) {
            require(["chromecastHelper"], function(chromecastHelper) {
                chromecastHelper.getServerAddress(apiClient).then(function(serverAddress) {
                    message.serverAddress = serverAddress, player.sendMessageInternal(message).then(resolve, reject)
                }, reject)
            })
        })
    }, CastPlayer.prototype.sendMessageInternal = function(message) {
        return message = JSON.stringify(message), this.session.sendMessage("urn:x-cast:com.connectsdk", message, this.onPlayCommandSuccess.bind(this), this.errorHandler), Promise.resolve()
    }, CastPlayer.prototype.onPlayCommandSuccess = function() {}, CastPlayer.prototype.onMediaDiscovered = function(how, mediaSession) {
        this.currentMediaSession = mediaSession, "loadMedia" === how && (this.castPlayerState = PLAYER_STATE.PLAYING), "activeSession" === how && (this.castPlayerState = mediaSession.playerState), this.currentMediaSession.addUpdateListener(this.mediaStatusUpdateHandler)
    }, CastPlayer.prototype.onMediaStatusUpdate = function(e) {
        !1 === e && (this.castPlayerState = PLAYER_STATE.IDLE)
    }, CastPlayer.prototype.setReceiverVolume = function(mute, vol) {
        this.currentMediaSession && (mute ? this.session.setReceiverMuted(!0, this.mediaCommandSuccessCallback.bind(this), this.errorHandler) : this.session.setReceiverVolumeLevel(vol || 1, this.mediaCommandSuccessCallback.bind(this), this.errorHandler))
    }, CastPlayer.prototype.mute = function() {
        this.setReceiverVolume(!0)
    }, CastPlayer.prototype.mediaCommandSuccessCallback = function(info, e) {}, ChromecastPlayer.prototype.tryPair = function(target) {
        var castPlayer = this._castPlayer;
        return castPlayer.deviceState !== DEVICE_STATE.ACTIVE && castPlayer.isInitialized ? new Promise(function(resolve, reject) {
            currentResolve = resolve, currentReject = reject, castPlayer.launchApp()
        }) : (currentResolve = null, currentReject = null, Promise.reject())
    }, ChromecastPlayer.prototype.getTargets = function() {
        var targets = [];
        return this._castPlayer && this._castPlayer.hasReceivers && targets.push(this.getCurrentTargetInfo()), Promise.resolve(targets)
    }, ChromecastPlayer.prototype.getCurrentTargetInfo = function() {
        var appName = null,
            castPlayer = this._castPlayer;
        return castPlayer.session && castPlayer.session.receiver && castPlayer.session.receiver.friendlyName && (appName = castPlayer.session.receiver.friendlyName), {
            name: PlayerName,
            id: PlayerName,
            playerName: PlayerName,
            playableMediaTypes: ["Audio", "Video"],
            isLocalPlayer: !1,
            appName: PlayerName,
            deviceName: appName,
            supportedCommands: ["VolumeUp", "VolumeDown", "Mute", "Unmute", "ToggleMute", "SetVolume", "SetAudioStreamIndex", "SetSubtitleStreamIndex", "DisplayContent", "SetRepeatMode", "EndSession", "PlayMediaSource", "PlayTrailers"]
        }
    }, ChromecastPlayer.prototype.getPlayerStateInternal = function(data) {
        var triggerStateChange = !1;
        return data && !this.lastPlayerData && (triggerStateChange = !0), data = data || this.lastPlayerData, this.lastPlayerData = data, normalizeImages(data), triggerStateChange && events.trigger(this, "statechange", [data]), data
    }, ChromecastPlayer.prototype.playWithCommand = function(options, command) {
        if (!options.items) {
            var apiClient = connectionManager.getApiClient(options.serverId),
                instance = this;
            return apiClient.getItem(apiClient.getCurrentUserId(), options.ids[0]).then(function(item) {
                return options.items = [item], instance.playWithCommand(options, command)
            })
        }
        return this._castPlayer.loadMedia(options, command)
    }, ChromecastPlayer.prototype.seek = function(position) {
        position = parseInt(position), position /= 1e7, this._castPlayer.sendMessage({
            options: {
                position: position
            },
            command: "Seek"
        })
    }, ChromecastPlayer.prototype.setAudioStreamIndex = function(index) {
        this._castPlayer.sendMessage({
            options: {
                index: index
            },
            command: "SetAudioStreamIndex"
        })
    }, ChromecastPlayer.prototype.setSubtitleStreamIndex = function(index) {
        this._castPlayer.sendMessage({
            options: {
                index: index
            },
            command: "SetSubtitleStreamIndex"
        })
    }, ChromecastPlayer.prototype.setMaxStreamingBitrate = function(options) {
        this._castPlayer.sendMessage({
            options: options,
            command: "SetMaxStreamingBitrate"
        })
    }, ChromecastPlayer.prototype.isFullscreen = function() {
        var state = this.lastPlayerData || {};
        return state = state.PlayState || {}, state.IsFullscreen
    }, ChromecastPlayer.prototype.nextTrack = function() {
        this._castPlayer.sendMessage({
            options: {},
            command: "NextTrack"
        })
    }, ChromecastPlayer.prototype.previousTrack = function() {
        this._castPlayer.sendMessage({
            options: {},
            command: "PreviousTrack"
        })
    }, ChromecastPlayer.prototype.volumeDown = function() {
        this._castPlayer.sendMessage({
            options: {},
            command: "VolumeDown"
        })
    }, ChromecastPlayer.prototype.endSession = function() {
        var instance = this;
        this.stop().then(function() {
            setTimeout(function() {
                instance._castPlayer.stopApp()
            }, 1e3)
        })
    }, ChromecastPlayer.prototype.volumeUp = function() {
        this._castPlayer.sendMessage({
            options: {},
            command: "VolumeUp"
        })
    }, ChromecastPlayer.prototype.setVolume = function(vol) {
        vol = Math.min(vol, 100), vol = Math.max(vol, 0), this._castPlayer.sendMessage({
            options: {
                volume: vol
            },
            command: "SetVolume"
        })
    }, ChromecastPlayer.prototype.unpause = function() {
        this._castPlayer.sendMessage({
            options: {},
            command: "Unpause"
        })
    }, ChromecastPlayer.prototype.playPause = function() {
        this._castPlayer.sendMessage({
            options: {},
            command: "PlayPause"
        })
    }, ChromecastPlayer.prototype.pause = function() {
        this._castPlayer.sendMessage({
            options: {},
            command: "Pause"
        })
    }, ChromecastPlayer.prototype.stop = function() {
        return this._castPlayer.sendMessage({
            options: {},
            command: "Stop"
        })
    }, ChromecastPlayer.prototype.displayContent = function(options) {
        this._castPlayer.sendMessage({
            options: options,
            command: "DisplayContent"
        })
    }, ChromecastPlayer.prototype.setMute = function(isMuted) {
        var castPlayer = this._castPlayer;
        isMuted ? castPlayer.sendMessage({
            options: {},
            command: "Mute"
        }) : castPlayer.sendMessage({
            options: {},
            command: "Unmute"
        })
    }, ChromecastPlayer.prototype.getRepeatMode = function() {
        var state = this.lastPlayerData || {};
        return state = state.PlayState || {}, state.RepeatMode
    }, ChromecastPlayer.prototype.playTrailers = function(item) {
        this._castPlayer.sendMessage({
            options: {
                ItemId: item.Id,
                ServerId: item.ServerId
            },
            command: "PlayTrailers"
        })
    }, ChromecastPlayer.prototype.setRepeatMode = function(mode) {
        this._castPlayer.sendMessage({
            options: {
                RepeatMode: mode
            },
            command: "SetRepeatMode"
        })
    }, ChromecastPlayer.prototype.toggleMute = function() {
        this._castPlayer.sendMessage({
            options: {},
            command: "ToggleMute"
        })
    }, ChromecastPlayer.prototype.audioTracks = function() {
        var state = this.lastPlayerData || {};
        return state = state.NowPlayingItem || {}, (state.MediaStreams || []).filter(function(s) {
            return "Audio" === s.Type
        })
    }, ChromecastPlayer.prototype.getAudioStreamIndex = function() {
        var state = this.lastPlayerData || {};
        return state = state.PlayState || {}, state.AudioStreamIndex
    }, ChromecastPlayer.prototype.subtitleTracks = function() {
        var state = this.lastPlayerData || {};
        return state = state.NowPlayingItem || {}, (state.MediaStreams || []).filter(function(s) {
            return "Subtitle" === s.Type
        })
    }, ChromecastPlayer.prototype.getSubtitleStreamIndex = function() {
        var state = this.lastPlayerData || {};
        return state = state.PlayState || {}, state.SubtitleStreamIndex
    }, ChromecastPlayer.prototype.getMaxStreamingBitrate = function() {
        var state = this.lastPlayerData || {};
        return state = state.PlayState || {}, state.MaxStreamingBitrate
    }, ChromecastPlayer.prototype.getVolume = function() {
        var state = this.lastPlayerData || {};
        return state = state.PlayState || {}, null == state.VolumeLevel ? 100 : state.VolumeLevel
    }, ChromecastPlayer.prototype.isPlaying = function() {
        return null != (this.lastPlayerData || {}).NowPlayingItem
    }, ChromecastPlayer.prototype.isPlayingVideo = function() {
        var state = this.lastPlayerData || {};
        return state = state.NowPlayingItem || {}, "Video" === state.MediaType
    }, ChromecastPlayer.prototype.isPlayingAudio = function() {
        var state = this.lastPlayerData || {};
        return state = state.NowPlayingItem || {}, "Audio" === state.MediaType
    }, ChromecastPlayer.prototype.currentTime = function(val) {
        if (null != val) return this.seek(val);
        var state = this.lastPlayerData || {};
        return state = state.PlayState || {}, state.PositionTicks
    }, ChromecastPlayer.prototype.duration = function() {
        var state = this.lastPlayerData || {};
        return state = state.NowPlayingItem || {}, state.RunTimeTicks
    }, ChromecastPlayer.prototype.getBufferedRanges = function() {
        var state = this.lastPlayerData || {};
        return state = state.PlayState || {}, state.BufferedRanges || []
    }, ChromecastPlayer.prototype.paused = function() {
        var state = this.lastPlayerData || {};
        return state = state.PlayState || {}, state.IsPaused
    }, ChromecastPlayer.prototype.isMuted = function() {
        var state = this.lastPlayerData || {};
        return state = state.PlayState || {}, state.IsMuted
    }, ChromecastPlayer.prototype.shuffle = function(item) {
        var apiClient = connectionManager.getApiClient(item.ServerId),
            userId = apiClient.getCurrentUserId(),
            instance = this;
        apiClient.getItem(userId, item.Id).then(function(item) {
            instance.playWithCommand({
                items: [item]
            }, "Shuffle")
        })
    }, ChromecastPlayer.prototype.instantMix = function(item) {
        var apiClient = connectionManager.getApiClient(item.ServerId),
            userId = apiClient.getCurrentUserId(),
            instance = this;
        apiClient.getItem(userId, item.Id).then(function(item) {
            instance.playWithCommand({
                items: [item]
            }, "InstantMix")
        })
    }, ChromecastPlayer.prototype.canPlayMediaType = function(mediaType) {
        return "audio" === (mediaType = (mediaType || "").toLowerCase()) || "video" === mediaType
    }, ChromecastPlayer.prototype.canQueueMediaType = function(mediaType) {
        return this.canPlayMediaType(mediaType)
    }, ChromecastPlayer.prototype.queue = function(options) {
        this.playWithCommand(options, "PlayLast")
    }, ChromecastPlayer.prototype.queueNext = function(options) {
        this.playWithCommand(options, "PlayNext")
    }, ChromecastPlayer.prototype.play = function(options) {
        if (options.items) return this.playWithCommand(options, "PlayNow");
        if (!options.serverId) throw new Error("serverId required!");
        var instance = this;
        return getItemsForPlayback(connectionManager.getApiClient(options.serverId), {
            Ids: options.ids.join(",")
        }).then(function(result) {
            return options.items = result.Items, instance.playWithCommand(options, "PlayNow")
        })
    }, ChromecastPlayer.prototype.toggleFullscreen = function() {}, ChromecastPlayer.prototype.beginPlayerUpdates = function() {}, ChromecastPlayer.prototype.endPlayerUpdates = function() {}, ChromecastPlayer.prototype.getPlaylist = function() {
        return Promise.resolve([])
    }, ChromecastPlayer.prototype.getCurrentPlaylistItemId = function() {}, ChromecastPlayer.prototype.setCurrentPlaylistItem = function(playlistItemId) {
        return Promise.resolve()
    }, ChromecastPlayer.prototype.removeFromPlaylist = function(playlistItemIds) {
        return Promise.resolve()
    }, ChromecastPlayer.prototype.getPlayerState = function() {
        return this.getPlayerStateInternal() || {}
    }, ChromecastPlayer
});
