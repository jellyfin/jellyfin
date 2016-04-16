define(['appSettings'], function (appSettings) {

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
                MediaController.removeActivePlayer(PlayerName);
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

            console.log('sessionListener ' + JSON.stringify(e));

            if (this.session.media[0]) {
                this.onMediaDiscovered('activeSession', this.session.media[0]);
            }

            this.onSessionConnected(e);
        }
    };

    CastPlayer.prototype.messageListener = function (namespace, message) {

        if (typeof (message) === 'string') {
            message = JSON.parse(message);
        }

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
        else if (message.type) {
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
            console.log("chromecast receiver found");
            this.hasReceivers = true;
        }
        else {
            console.log("chromecast receiver list empty");
            this.hasReceivers = false;
        }
    };

    /**
     * session update listener
     */
    CastPlayer.prototype.sessionUpdateListener = function (isAlive) {

        console.log('sessionUpdateListener alive: ' + isAlive);

        if (isAlive) {
        }
        else {
            this.session = null;
            this.deviceState = DEVICE_STATE.IDLE;
            this.castPlayerState = PLAYER_STATE.IDLE;

            console.log('sessionUpdateListener: setting currentMediaSession to null');
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
        console.log("chromecast launching app...");
        chrome.cast.requestSession(this.onRequestSessionSuccess.bind(this), this.onLaunchError.bind(this));
    };

    /**
     * Callback function for request session success 
     * @param {Object} e A chrome.cast.Session object
     */
    CastPlayer.prototype.onRequestSessionSuccess = function (e) {

        console.log("chromecast session success: " + e.sessionId);
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

        console.log('sessionMediaListener');
        this.currentMediaSession = e;
        this.currentMediaSession.addUpdateListener(this.mediaStatusUpdateHandler);
    };

    /**
     * Callback function for launch error
     */
    CastPlayer.prototype.onLaunchError = function () {
        console.log("chromecast launch error");
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
        console.log(message);
        this.deviceState = DEVICE_STATE.IDLE;
        this.castPlayerState = PLAYER_STATE.IDLE;

        console.log('onStopAppSuccess: setting currentMediaSession to null');
        this.currentMediaSession = null;
    };

    /**
     * Loads media into a running receiver application
     * @param {Number} mediaIndex An index number to indicate current media content
     */
    CastPlayer.prototype.loadMedia = function (options, command) {

        if (!this.session) {
            console.log("no session");
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

    CastPlayer.prototype.sendMessage = function (message) {

        var player = this;

        var receiverName = null;

        if (castPlayer.session && castPlayer.session.receiver && castPlayer.session.receiver.friendlyName) {
            receiverName = castPlayer.session.receiver.friendlyName;
        }

        message = Object.assign(message, {
            userId: Dashboard.getCurrentUserId(),
            deviceId: ApiClient.deviceId(),
            accessToken: ApiClient.accessToken(),
            serverAddress: ApiClient.serverAddress(),
            receiverName: receiverName
        });

        var bitrateSetting = appSettings.maxChromecastBitrate();
        if (bitrateSetting) {
            message.maxBitrate = bitrateSetting;
        }

        require(['chromecasthelpers'], function (chromecasthelpers) {

            chromecasthelpers.getServerAddress(ApiClient).then(function (serverAddress) {
                message.serverAddress = serverAddress;
                player.sendMessageInternal(message);
            });
        });
    };

    CastPlayer.prototype.sendMessageInternal = function (message) {

        message = JSON.stringify(message);
        //console.log(message);

        this.session.sendMessage(messageNamespace, message, this.onPlayCommandSuccess.bind(this), this.errorHandler);
    };

    CastPlayer.prototype.onPlayCommandSuccess = function () {
        console.log('Message was sent to receiver ok.');
    };

    /**
     * Callback function for loadMedia success
     * @param {Object} mediaSession A new media object.
     */
    CastPlayer.prototype.onMediaDiscovered = function (how, mediaSession) {

        console.log("chromecast new media session ID:" + mediaSession.mediaSessionId + ' (' + how + ')');
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
        console.log("chromecast updating media: " + e);
    };

    /**
     * Set media volume in Cast mode
     * @param {Boolean} mute A boolean  
     */
    CastPlayer.prototype.setReceiverVolume = function (mute, vol) {

        if (!this.currentMediaSession) {
            console.log('this.currentMediaSession is null');
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
        console.log(info);
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
                return ApiClient.getItem(userId, query.Ids.split(',')).then(function (item) {
                    return {
                        Items: [item],
                        TotalRecordCount: 1
                    };
                });
            }
            else {

                query.Limit = query.Limit || 100;
                query.ExcludeLocationTypes = "Virtual";

                return ApiClient.getItems(userId, query);
            }
        };

        Events.on(castPlayer, "connect", function (e) {

            if (currentResolve) {
                sendConnectionResult(true);
            } else {
                MediaController.setActivePlayer(PlayerName, self.getCurrentTargetInfo());
            }

            console.log('cc: connect');
            // Reset this so the next query doesn't make it appear like content is playing.
            self.lastPlayerData = {};
        });

        Events.on(castPlayer, "playbackstart", function (e, data) {

            console.log('cc: playbackstart');

            castPlayer.initializeCastPlayer();

            var state = self.getPlayerStateInternal(data);
            Events.trigger(self, "playbackstart", [state]);
        });

        Events.on(castPlayer, "playbackstop", function (e, data) {

            console.log('cc: playbackstop');
            var state = self.getPlayerStateInternal(data);

            Events.trigger(self, "playbackstop", [state]);

            // Reset this so the next query doesn't make it appear like content is playing.
            self.lastPlayerData = {};
        });

        Events.on(castPlayer, "playbackprogress", function (e, data) {

            console.log('cc: positionchange');
            var state = self.getPlayerStateInternal(data);

            Events.trigger(self, "positionchange", [state]);
        });

        Events.on(castPlayer, "volumechange", function (e, data) {

            console.log('cc: volumechange');
            var state = self.getPlayerStateInternal(data);

            Events.trigger(self, "volumechange", [state]);
        });

        Events.on(castPlayer, "playstatechange", function (e, data) {

            console.log('cc: playstatechange');
            var state = self.getPlayerStateInternal(data);

            Events.trigger(self, "playstatechange", [state]);
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
            self.playWithCommand(options, 'PlayLast');
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

            return Promise.resolve(targets);
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

            var result = self.getPlayerStateInternal();
            return Promise.resolve(result);
        };

        self.lastPlayerData = {};

        self.getPlayerStateInternal = function (data) {

            data = data || self.lastPlayerData;
            self.lastPlayerData = data;

            console.log(JSON.stringify(data));
            return data;
        };

        self.tryPair = function (target) {

            return new Promise(function (resolve, reject) {
                if (castPlayer.deviceState != DEVICE_STATE.ACTIVE && castPlayer.isInitialized) {

                    currentResolve = resolve;
                    currentReject = reject;

                    castPlayer.launchApp();
                } else {

                    currentResolve = null;
                    currentReject = null;

                    reject();
                }
            });
        };
    }

    function initializeChromecast() {

        castPlayer = new CastPlayer();

        var registeredPlayer = new chromecastPlayer();
        MediaController.registerPlayer(registeredPlayer);

        // To allow the native android app to override
        document.dispatchEvent(new CustomEvent("chromecastloaded", {
            detail: {
                player: registeredPlayer
            }
        }));
    }

    var fileref = document.createElement('script');
    fileref.setAttribute("type", "text/javascript");
    fileref.onload = initializeChromecast;
    fileref.setAttribute("src", "https://www.gstatic.com/cv/js/sender/v1/cast_sender.js");
    document.querySelector('head').appendChild(fileref);

});