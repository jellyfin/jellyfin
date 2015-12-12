(function (window, chrome, console) {

    // Based on https://github.com/googlecast/CastVideos-chrome/blob/master/CastVideos.js

    /**
     * Constants of states for Chromecast device 
     **/
    var DEVICE_STATE = {
        'IDLE': 0,
        'ACTIVE': 1,
        'WARNING': 2,
        'ERROR': 3,
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

    var PlayerName = 'Chromecast';

    var applicationID = "2D4B1DA3";
    var messageNamespace = 'urn:x-cast:com.connectsdk';

    //var applicationID = "F4EB2E8E";
    //var messageNamespace = 'urn:x-cast:com.google.cast.mediabrowser.v3';

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
          this.receiverListener.bind(this));

        Logger.log('chromecast.initialize');

        chrome.cast.initialize(apiConfig, this.onInitSuccess.bind(this), this.errorHandler);

    };

    /**
     * Callback function for init success 
     */
    CastPlayer.prototype.onInitSuccess = function () {
        this.isInitialized = true;
        Logger.log("chromecast init success");
    };

    /**
     * Generic error callback function 
     */
    CastPlayer.prototype.onError = function () {
        Logger.log("chromecast error");
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

            Logger.log('sessionListener ' + JSON.stringify(e));

            if (this.session.media[0]) {
                this.onMediaDiscovered('activeSession', this.session.media[0]);
            }

            this.onSessionConnected(e);
        }
    };

    CastPlayer.prototype.messageListener = function (namespace, message) {

        message = JSON.parse(message);

        if (message.type == 'playbackerror') {

            var errorCode = message.data;

            setTimeout(function () {
                Dashboard.alert({
                    message: Globalize.translate('MessagePlaybackError' + errorCode),
                    title: Globalize.translate('HeaderPlaybackError')
                });
            }, 300);

        }
        else if (message.type == 'connectionerror') {

            setTimeout(function () {
                Dashboard.alert({
                    message: Globalize.translate('MessageChromecastConnectionError'),
                    title: Globalize.translate('HeaderError')
                });
            }, 300);

        }
        else if (message.type && message.type.indexOf('playback') == 0) {
            Events.trigger(this, message.type, [message.data]);

        }
    };

    /**
     * @param {string} e Receiver availability
     * This indicates availability of receivers but
     * does not provide a list of device IDs
     */
    CastPlayer.prototype.receiverListener = function (e) {

        if (e === 'available') {
            Logger.log("chromecast receiver found");
            this.hasReceivers = true;
        }
        else {
            Logger.log("chromecast receiver list empty");
            this.hasReceivers = false;
        }
    };

    /**
     * session update listener
     */
    CastPlayer.prototype.sessionUpdateListener = function (isAlive) {

        Logger.log('sessionUpdateListener alive: ' + isAlive);

        if (isAlive) {
        }
        else {
            this.session = null;
            this.deviceState = DEVICE_STATE.IDLE;
            this.castPlayerState = PLAYER_STATE.IDLE;

            Logger.log('sessionUpdateListener: setting currentMediaSession to null');
            this.currentMediaSession = null;

            MediaController.removeActivePlayer(PlayerName);
        }
    };

    /**
     * Requests that a receiver application session be created or joined. By default, the SessionRequest
     * passed to the API at initialization time is used; this may be overridden by passing a different
     * session request in opt_sessionRequest. 
     */
    CastPlayer.prototype.launchApp = function () {
        Logger.log("chromecast launching app...");
        chrome.cast.requestSession(this.onRequestSessionSuccess.bind(this), this.onLaunchError.bind(this));
    };

    /**
     * Callback function for request session success 
     * @param {Object} e A chrome.cast.Session object
     */
    CastPlayer.prototype.onRequestSessionSuccess = function (e) {

        Logger.log("chromecast session success: " + e.sessionId);
        this.onSessionConnected(e);
    };

    CastPlayer.prototype.onSessionConnected = function (session) {

        this.session = session;

        this.deviceState = DEVICE_STATE.ACTIVE;

        this.session.addMessageListener(messageNamespace, this.messageListener.bind(this));
        this.session.addMediaListener(this.sessionMediaListener.bind(this));
        this.session.addUpdateListener(this.sessionUpdateListener.bind(this));

        Events.trigger(this, 'connect');

        this.sendMessage({
            options: {},
            command: 'Identify'
        });
    };

    /**
     * session update listener
     */
    CastPlayer.prototype.sessionMediaListener = function (e) {

        Logger.log('sessionMediaListener');
        this.currentMediaSession = e;
        this.currentMediaSession.addUpdateListener(this.mediaStatusUpdateHandler);
    };

    /**
     * Callback function for launch error
     */
    CastPlayer.prototype.onLaunchError = function () {
        Logger.log("chromecast launch error");
        this.deviceState = DEVICE_STATE.ERROR;

        //Dashboard.alert({

        //    title: Globalize.translate("Error"),
        //    message: Globalize.translate("ErrorLaunchingChromecast")

        //});

        MediaController.removeActivePlayer(PlayerName);
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
        Logger.log(message);
        this.deviceState = DEVICE_STATE.IDLE;
        this.castPlayerState = PLAYER_STATE.IDLE;

        Logger.log('onStopAppSuccess: setting currentMediaSession to null');
        this.currentMediaSession = null;
    };

    /**
     * Loads media into a running receiver application
     * @param {Number} mediaIndex An index number to indicate current media content
     */
    CastPlayer.prototype.loadMedia = function (options, command) {

        if (!this.session) {
            Logger.log("no session");
            return;
        }

        // Convert the items to smaller stubs to send the minimal amount of information
        options.items = options.items.map(function (i) {

            return {
                Id: i.Id,
                Name: i.Name,
                Type: i.Type,
                MediaType: i.MediaType,
                IsFolder: i.IsFolder
            };
        });

        this.sendMessage({
            options: options,
            command: command
        });
    };

    var endpointInfo;
    function getEndpointInfo() {

        if (endpointInfo) {

            return new Promise(function (resolve, reject) {

                resolve(endpointInfo);
            });
        }

        return ApiClient.getJSON(ApiClient.getUrl('System/Endpoint')).then(function (info) {

            endpointInfo = info;
            return info;
        });
    }

    CastPlayer.prototype.sendMessage = function (message) {

        var player = this;

        var bitrateSetting = AppSettings.maxChromecastBitrate();

        var receiverName = null;

        if (castPlayer.session && castPlayer.session.receiver && castPlayer.session.receiver.friendlyName) {
            receiverName = castPlayer.session.receiver.friendlyName;
        }

        message = $.extend(message, {
            userId: Dashboard.getCurrentUserId(),
            deviceId: ApiClient.deviceId(),
            accessToken: ApiClient.accessToken(),
            serverAddress: ApiClient.serverAddress(),
            maxBitrate: bitrateSetting,
            receiverName: receiverName,
            supportsAc3: AppSettings.enableChromecastAc3()
        });

        getEndpointInfo().then(function (endpoint) {

            if (endpoint.IsInNetwork) {
                ApiClient.getPublicSystemInfo().then(function (info) {

                    message.serverAddress = info.LocalAddress;
                    player.sendMessageInternal(message);
                });
            } else {
                player.sendMessageInternal(message);
            }
        });
    };

    CastPlayer.prototype.sendMessageInternal = function (message) {

        message = JSON.stringify(message);
        //Logger.log(message);

        this.session.sendMessage(messageNamespace, message, this.onPlayCommandSuccess.bind(this), this.errorHandler);
    };

    CastPlayer.prototype.onPlayCommandSuccess = function () {
        Logger.log('Message was sent to receiver ok.');
    };

    /**
     * Callback function for loadMedia success
     * @param {Object} mediaSession A new media object.
     */
    CastPlayer.prototype.onMediaDiscovered = function (how, mediaSession) {

        Logger.log("chromecast new media session ID:" + mediaSession.mediaSessionId + ' (' + how + ')');
        this.currentMediaSession = mediaSession;

        if (how == 'loadMedia') {
            this.castPlayerState = PLAYER_STATE.PLAYING;
        }

        if (how == 'activeSession') {
            this.castPlayerState = mediaSession.playerState;
        }

        this.currentMediaSession.addUpdateListener(this.mediaStatusUpdateHandler);
    };

    /**
     * Callback function for media status update from receiver
     * @param {!Boolean} e true/false
     */
    CastPlayer.prototype.onMediaStatusUpdate = function (e) {

        if (e == false) {
            this.castPlayerState = PLAYER_STATE.IDLE;
        }
        Logger.log("chromecast updating media: " + e);
    };

    /**
     * Set media volume in Cast mode
     * @param {Boolean} mute A boolean  
     */
    CastPlayer.prototype.setReceiverVolume = function (mute, vol) {

        if (!this.currentMediaSession) {
            Logger.log('this.currentMediaSession is null');
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
        Logger.log(info);
    };

    // Create Cast Player
    var castPlayer;

    function chromecastPlayer() {

        var self = this;

        // MediaController needs this
        self.name = PlayerName;

        self.getItemsForPlayback = function (query) {

            var userId = Dashboard.getCurrentUserId();

            if (query.Ids && query.Ids.split(',').length == 1) {
                return new Promise(function (resolve, reject) {

                    ApiClient.getItem(userId, query.Ids.split(',')).then(function (item) {
                        resolve({
                            Items: [item],
                            TotalRecordCount: 1
                        });
                    });
                });
            }
            else {

                query.Limit = query.Limit || 100;
                query.ExcludeLocationTypes = "Virtual";

                return ApiClient.getItems(userId, query);
            }
        };

        $(castPlayer).on("connect", function (e) {

            MediaController.setActivePlayer(PlayerName, self.getCurrentTargetInfo());

            Logger.log('cc: connect');
            // Reset this so the next query doesn't make it appear like content is playing.
            self.lastPlayerData = {};
        });

        $(castPlayer).on("playbackstart", function (e, data) {

            Logger.log('cc: playbackstart');

            castPlayer.initializeCastPlayer();

            var state = self.getPlayerStateInternal(data);
            Events.trigger(self, "playbackstart", [state]);
        });

        $(castPlayer).on("playbackstop", function (e, data) {

            Logger.log('cc: playbackstop');
            var state = self.getPlayerStateInternal(data);

            Events.trigger(self, "playbackstop", [state]);

            // Reset this so the next query doesn't make it appear like content is playing.
            self.lastPlayerData = {};
        });

        $(castPlayer).on("playbackprogress", function (e, data) {

            Logger.log('cc: positionchange');
            var state = self.getPlayerStateInternal(data);

            Events.trigger(self, "positionchange", [state]);
        });

        self.play = function (options) {

            Dashboard.getCurrentUser().then(function (user) {

                if (options.items) {

                    self.playWithCommand(options, 'PlayNow');

                } else {

                    self.getItemsForPlayback({

                        Ids: options.ids.join(',')

                    }).then(function (result) {

                        options.items = result.Items;
                        self.playWithCommand(options, 'PlayNow');

                    });
                }

            });

        };

        self.playWithCommand = function (options, command) {

            if (!options.items) {
                ApiClient.getItem(Dashboard.getCurrentUserId(), options.ids[0]).then(function (item) {

                    options.items = [item];
                    self.playWithCommand(options, command);
                });

                return;
            }

            castPlayer.loadMedia(options, command);
        };

        self.unpause = function () {
            castPlayer.sendMessage({
                options: {},
                command: 'Unpause'
            });
        };

        self.pause = function () {
            castPlayer.sendMessage({
                options: {},
                command: 'Pause'
            });
        };

        self.shuffle = function (id) {

            var userId = Dashboard.getCurrentUserId();

            ApiClient.getItem(userId, id).then(function (item) {

                self.playWithCommand({

                    items: [item]

                }, 'Shuffle');

            });

        };

        self.instantMix = function (id) {

            var userId = Dashboard.getCurrentUserId();

            ApiClient.getItem(userId, id).then(function (item) {

                self.playWithCommand({

                    items: [item]

                }, 'InstantMix');

            });

        };

        self.canQueueMediaType = function (mediaType) {
            return mediaType == "Audio";
        };

        self.queue = function (options) {
            self.playWithCommnd(options, 'PlayLast');
        };

        self.queueNext = function (options) {
            self.playWithCommand(options, 'PlayNext');
        };

        self.stop = function () {
            castPlayer.sendMessage({
                options: {},
                command: 'Stop'
            });
        };

        self.displayContent = function (options) {

            castPlayer.sendMessage({
                options: options,
                command: 'DisplayContent'
            });
        };

        self.mute = function () {
            castPlayer.sendMessage({
                options: {},
                command: 'Mute'
            });
            //castPlayer.mute();
        };

        self.unMute = function () {
            self.setVolume(getCurrentVolume() + 2);
        };

        self.setRepeatMode = function (mode) {
            castPlayer.sendMessage({
                options: {
                    RepeatMode: mode
                },
                command: 'SetRepeatMode'
            });
        };

        self.toggleMute = function () {

            var state = self.lastPlayerData || {};
            state = state.PlayState || {};

            if (state.IsMuted) {
                self.unMute();
            } else {
                self.mute();
            }
        };

        self.getTargets = function () {

            var targets = [];

            if (castPlayer.hasReceivers) {
                targets.push(self.getCurrentTargetInfo());
            }

            return targets;

        };

        self.getCurrentTargetInfo = function () {

            var appName = null;

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
                supportedCommands: ["VolumeUp",
                                    "VolumeDown",
                                    "Mute",
                                    "Unmute",
                                    "ToggleMute",
                                    "SetVolume",
                                    "SetAudioStreamIndex",
                                    "SetSubtitleStreamIndex",
                                    "DisplayContent",
                                    "SetRepeatMode",
                                    "EndSession"]
            };
        };

        self.seek = function (position) {

            position = parseInt(position);

            position = position / 10000000;

            castPlayer.sendMessage({
                options: {
                    position: position
                },
                command: 'Seek'
            });
        };

        self.setAudioStreamIndex = function (index) {
            castPlayer.sendMessage({
                options: {
                    index: index
                },
                command: 'SetAudioStreamIndex'
            });
        };

        self.setSubtitleStreamIndex = function (index) {
            castPlayer.sendMessage({
                options: {
                    index: index
                },
                command: 'SetSubtitleStreamIndex'
            });
        };

        self.nextTrack = function () {
            castPlayer.sendMessage({
                options: {},
                command: 'NextTrack'
            });
        };

        self.previousTrack = function () {
            castPlayer.sendMessage({
                options: {},
                command: 'PreviousTrack'
            });
        };

        self.beginPlayerUpdates = function () {
            // Setup polling here
        };

        self.endPlayerUpdates = function () {
            // Stop polling here
        };

        function getCurrentVolume() {
            var state = self.lastPlayerData || {};
            state = state.PlayState || {};

            return state.VolumeLevel == null ? 100 : state.VolumeLevel;
        }

        self.volumeDown = function () {

            castPlayer.sendMessage({
                options: {},
                command: 'VolumeDown'
            });
        };

        self.endSession = function () {

            self.stop();
            setTimeout(function () {
                castPlayer.stopApp();
            }, 1000);
        };

        self.volumeUp = function () {

            castPlayer.sendMessage({
                options: {},
                command: 'VolumeUp'
            });
        };

        self.setVolume = function (vol) {

            vol = Math.min(vol, 100);
            vol = Math.max(vol, 0);

            //castPlayer.setReceiverVolume(false, (vol / 100));
            castPlayer.sendMessage({
                options: {
                    volume: vol
                },
                command: 'SetVolume'
            });
        };

        self.getPlayerState = function () {

            return new Promise(function (resolve, reject) {

                var result = self.getPlayerStateInternal();
                resolve(result);
            });
        };

        self.lastPlayerData = {};

        self.getPlayerStateInternal = function (data) {

            data = data || self.lastPlayerData;
            self.lastPlayerData = data;

            Logger.log(JSON.stringify(data));
            return data;
        };

        self.tryPair = function (target) {

            return new Promise(function (resolve, reject) {
                resolve();
            });
        };
    }

    function initializeChromecast() {

        castPlayer = new CastPlayer();

        MediaController.registerPlayer(new chromecastPlayer());

        $(MediaController).on('playerchange', function (e, newPlayer, newTarget) {
            if (newPlayer.name == PlayerName) {
                if (castPlayer.deviceState != DEVICE_STATE.ACTIVE && castPlayer.isInitialized) {
                    castPlayer.launchApp();
                }
            }
        });
    }

    requirejs(["https://www.gstatic.com/cv/js/sender/v1/cast_sender.js"], initializeChromecast);

})(window, window.chrome, console);