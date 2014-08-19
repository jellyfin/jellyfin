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

    var messageNamespace = 'urn:x-cast:com.google.cast.mediabrowser.v3';

    var cPlayer = {
        deviceState: DEVICE_STATE.IDLE
    };

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

        // v1 Id AE4DA10A
        // v2 Id 472F0435
        // v3 Id 69C59853
        // default receiver chrome.cast.media.DEFAULT_MEDIA_RECEIVER_APP_ID

        var applicationID = "69C59853";

        // request session
        var sessionRequest = new chrome.cast.SessionRequest(applicationID);
        var apiConfig = new chrome.cast.ApiConfig(sessionRequest,
          this.sessionListener.bind(this),
          this.receiverListener.bind(this));

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

        message = JSON.parse(message);

        if (message.type && message.type.indexOf('playback') == 0) {
            $(this).trigger(message.type, [message.data]);

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

        $(this).trigger('connect');

        MediaController.setActivePlayer(PlayerName);

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
        this.session.stop(this.onStopAppSuccess.bind(this, 'Session stopped'),
            this.errorHandler);

    };

    /**
     * Callback function for stop app success 
     */
    CastPlayer.prototype.onStopAppSuccess = function (message) {
        console.log(message);
        this.deviceState = DEVICE_STATE.IDLE;
        this.castPlayerState = PLAYER_STATE.IDLE;
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

    var endpointInfo;
    function getEndpointInfo() {

        if (endpointInfo) {

            var deferred = $.Deferred();
            deferred.resolveWith(null, [endpointInfo]);
            return deferred.promise();
        }

        return ApiClient.getJSON(ApiClient.getUrl('System/Endpoint')).done(function (info) {

            endpointInfo = info;
        });
    }

    CastPlayer.prototype.sendMessage = function (message) {

        var player = this;

        message = $.extend(message, {
            userId: Dashboard.getCurrentUserId(),
            deviceId: ApiClient.deviceId(),
            accessToken: ApiClient.accessToken(),
            serverAddress: ApiClient.serverAddress()
        });

        getEndpointInfo().done(function (endpoint) {

            if (endpoint.IsLocal || endpoint.IsInNetwork) {
                ApiClient.getSystemInfo().done(function (info) {

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
     * Callback function when media load returns error 
     */
    CastPlayer.prototype.onLoadMediaError = function (e) {
        console.log("chromecast media error");
        this.castPlayerState = PLAYER_STATE.IDLE;
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
     * Play media in Cast mode 
     */
    CastPlayer.prototype.playMedia = function () {

        if (!this.currentMediaSession) {
            return;
        }

        this.currentMediaSession.play(null, this.mediaCommandSuccessCallback.bind(this, "playing started for " + this.currentMediaSession.sessionId), this.errorHandler);
        //this.currentMediaSession.addUpdateListener(this.mediaStatusUpdateHandler);
    };

    /**
     * Pause media playback in Cast mode  
     */
    CastPlayer.prototype.pauseMedia = function () {

        if (!this.currentMediaSession) {
            return;
        }

        this.currentMediaSession.pause(null, this.mediaCommandSuccessCallback.bind(this, "paused " + this.currentMediaSession.sessionId), this.errorHandler);
    };

    /**
     * Stop CC playback 
     */
    CastPlayer.prototype.stopMedia = function () {

        if (!this.currentMediaSession) {
            return;
        }

        this.currentMediaSession.stop(null,
          this.mediaCommandSuccessCallback.bind(this, "stopped " + this.currentMediaSession.sessionId),
          this.errorHandler);
        this.castPlayerState = PLAYER_STATE.STOPPED;
    };

    /**
     * Set media volume in Cast mode
     * @param {Boolean} mute A boolean  
     */
    CastPlayer.prototype.setReceiverVolume = function (mute, vol) {

        if (!this.currentMediaSession) {
            return;
        }

        if (!mute) {

            this.session.setReceiverVolumeLevel(vol || 1,
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
     * Toggle mute CC
     */
    CastPlayer.prototype.toggleMute = function () {
        if (this.audio == true) {
            this.mute();
        }
        else {
            this.unMute();
        }
    };

    /**
     * Mute CC
     */
    CastPlayer.prototype.mute = function () {
        this.audio = false;
        this.setReceiverVolume(true);
    };

    /**
     * Unmute CC
     */
    CastPlayer.prototype.unMute = function () {
        this.audio = true;
        this.setReceiverVolume(false);
    };


    /**
     * media seek function in either Cast or local mode
     * @param {Event} e An event object from seek 
     */
    CastPlayer.prototype.seekMedia = function (event) {

        var pos = parseInt(event);

        var curr = pos / 10000000;

        if (!this.currentMediaSession) {
            return;
        }

        var request = new chrome.cast.media.SeekRequest();
        request.currentTime = curr;

        this.currentMediaSession.seek(request,
          this.onSeekSuccess.bind(this, 'media seek done'),
          this.errorHandler);

        this.castPlayerState = PLAYER_STATE.SEEKING;
    };

    /**
     * Callback function for seek success
     * @param {String} info A string that describe seek event
     */
    CastPlayer.prototype.onSeekSuccess = function (info) {
        console.log(info);
        this.castPlayerState = PLAYER_STATE.PLAYING;
    };

    /**
     * Callback function for media command success 
     */
    CastPlayer.prototype.mediaCommandSuccessCallback = function (info, e) {
        console.log(info);
    };

    // Create Cast Player
    var castPlayer = new CastPlayer();

    function chromecastPlayer() {

        var self = this;

        // MediaController needs this
        self.name = PlayerName;

        self.getItemsForPlayback = function (query) {

            var userId = Dashboard.getCurrentUserId();

            query.Limit = query.Limit || 100;
            query.ExcludeLocationTypes = "Virtual";

            return ApiClient.getItems(userId, query);
        };

        $(castPlayer).on("connect", function (e) {

            console.log('cc: connect');
            // Reset this so the next query doesn't make it appear like content is playing.
            self.lastPlayerData = {};
        });

        $(castPlayer).on("playbackstart", function (e, data) {

            console.log('cc: playbackstart');

            castPlayer.initializeCastPlayer();

            var state = self.getPlayerStateInternal(data);
            $(self).trigger("playbackstart", [state]);
        });

        $(castPlayer).on("playbackstop", function (e, data) {

            console.log('cc: playbackstop');
            var state = self.getPlayerStateInternal(data);

            $(self).trigger("playbackstop", [state]);

            // Reset this so the next query doesn't make it appear like content is playing.
            self.lastPlayerData = {};
        });

        $(castPlayer).on("playbackprogress", function (e, data) {

            console.log('cc: positionchange');
            var state = self.getPlayerStateInternal(data);

            $(self).trigger("positionchange", [state]);
        });

        self.play = function (options) {

            Dashboard.getCurrentUser().done(function (user) {

                if (options.items) {

                    self.playWithCommand(options, 'PlayNow');

                } else {

                    self.getItemsForPlayback({

                        Ids: options.ids.join(',')

                    }).done(function (result) {

                        options.items = result.Items;
                        self.playWithCommand(options, 'PlayNow');

                    });
                }

            });

        };

        self.playWithCommand = function (options, command) {

            if (!options.items) {
                ApiClient.getItem(Dashboard.getCurrentUserId(), options.ids[0]).done(function (item) {

                    options.items = [item];
                    self.playWithCommand(options, command);
                });

                return;
            }

            castPlayer.loadMedia(options, command);
        };

        self.unpause = function () {
            castPlayer.playMedia();
        };

        self.pause = function () {
            castPlayer.pauseMedia();
        };

        self.shuffle = function (id) {

            var userId = Dashboard.getCurrentUserId();

            ApiClient.getItem(userId, id).done(function (item) {

                self.playWithCommand({

                    items: [item]

                }, 'Shuffle');

            });

        };

        self.instantMix = function (id) {

            var userId = Dashboard.getCurrentUserId();

            ApiClient.getItem(userId, id).done(function (item) {

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
            castPlayer.stopMedia();
        };

        self.displayContent = function (options) {

            castPlayer.sendMessage({
                options: options,
                command: 'DisplayContent'
            });
        };

        self.mute = function () {
            castPlayer.mute();
        };

        self.unMute = function () {
            castPlayer.unMute();
        };

        self.toggleMute = function () {
            castPlayer.toggleMute();
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
                                    "SetVolume"]
            };
        };

        self.seek = function (position) {
            castPlayer.seekMedia(position);
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

            self.setVolume(getCurrentVolume() - 2);
        };

        self.volumeUp = function () {

            self.setVolume(getCurrentVolume() + 2);
        };

        self.setVolume = function (vol) {

            vol = Math.min(vol, 100);
            vol = Math.max(vol, 0);

            castPlayer.setReceiverVolume(false, (vol / 100));
        };

        self.getPlayerState = function () {

            var deferred = $.Deferred();

            var result = self.getPlayerStateInternal();

            deferred.resolveWith(null, [result]);

            return deferred.promise();
        };

        self.lastPlayerData = {};

        self.getPlayerStateInternal = function (data) {

            data = data || self.lastPlayerData;
            self.lastPlayerData = data;

            console.log(JSON.stringify(data));
            return data;
        };
    }

    MediaController.registerPlayer(new chromecastPlayer());

    $(MediaController).on('playerchange', function () {

        if (MediaController.getPlayerInfo().name == PlayerName) {
            if (castPlayer.deviceState != DEVICE_STATE.ACTIVE && castPlayer.isInitialized) {
                castPlayer.launchApp();
            }
        }
    });

})(window, window.chrome, console);