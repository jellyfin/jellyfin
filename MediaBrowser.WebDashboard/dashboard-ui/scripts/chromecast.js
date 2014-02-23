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

    var CastPlayer = function () {

        /* device variables */
        // @type {DEVICE_STATE} A state for device
        this.deviceState = DEVICE_STATE.IDLE;

        /* Cast player variables */
        // @type {Object} a chrome.cast.media.Media object
        this.currentMediaSession = null;
        // @type {Number} volume
        this.currentVolume = 0.5;
        // @type {Boolean} A flag for autoplay after load
        this.autoplay = true;
        // @type {string} a chrome.cast.Session object
        this.session = null;
        // @type {PLAYER_STATE} A state for Cast media player
        this.castPlayerState = PLAYER_STATE.IDLE;

        /* Local player variables */
        // @type {PLAYER_STATE} A state for local media player
        this.localPlayerState = PLAYER_STATE.IDLE;
        // @type {HTMLElement} local player
        this.localPlayer = null;
        // @type {Boolean} Fullscreen mode on/off
        this.fullscreen = false;

        /* Current media variables */
        // @type {Boolean} Audio on and off
        this.audio = true;
        // @type {Number} A number for current media index
        this.currentMediaIndex = 0;
        // @type {Number} A number for current media time
        this.currentMediaTime = 0;
        // @type {Number} A number for current media duration
        this.currentMediaDuration = -1;
        // @type {Timer} A timer for tracking progress of media
        this.timer = null;
        // @type {Boolean} A boolean to stop timer update of progress when triggered by media status event 
        this.progressFlag = true;
        // @type {Number} A number in milliseconds for minimal progress update
        this.timerStep = 1000;

        /* media contents from JSON */
        this.mediaContents = null;

        this.initializeCastPlayer();
    };

    /**
     * Initialize Cast media player 
     * Initializes the API. Note that either successCallback and errorCallback will be
     * invoked once the API has finished initialization. The sessionListener and 
     * receiverListener may be invoked at any time afterwards, and possibly more than once. 
     */
    CastPlayer.prototype.initializeCastPlayer = function () {

        if (!chrome.cast || !chrome.cast.isAvailable) {
            return;
        }

        var applicationID = 'AE4DA10A';

        // request session
        var sessionRequest = new chrome.cast.SessionRequest(applicationID);
        var apiConfig = new chrome.cast.ApiConfig(sessionRequest,
          this.sessionListener.bind(this),
          this.receiverListener.bind(this));

        chrome.cast.initialize(apiConfig, this.onInitSuccess.bind(this), this.onError.bind(this));
    };

    /**
     * Callback function for init success 
     */
    CastPlayer.prototype.onInitSuccess = function () {
        console.log("init success");
        this.updateMediaControlUI();
    };

    /**
     * Generic error callback function 
     */
    CastPlayer.prototype.onError = function () {
        console.log("error");
    };

    /**
     * @param {!Object} e A new session
     * This handles auto-join when a page is reloaded
     * When active session is detected, playback will automatically
     * join existing session and occur in Cast mode and media
     * status gets synced up with current media of the session 
     */
    CastPlayer.prototype.sessionListener = function(e) {
        this.session = e;
        if (this.session) {
            this.deviceState = DEVICE_STATE.ACTIVE;
            if (this.session.media[0]) {
                this.onMediaDiscovered('activeSession', this.session.media[0]);
            } else {
                this.loadMedia(this.currentMediaIndex);
            }
        }
    };

    /**
     * @param {string} e Receiver availability
     * This indicates availability of receivers but
     * does not provide a list of device IDs
     */
    CastPlayer.prototype.receiverListener = function (e) {
        if (e === 'available') {
            console.log("receiver found");
        }
        else {
            console.log("receiver list empty");
        }
    };

    /**
     * Requests that a receiver application session be created or joined. By default, the SessionRequest
     * passed to the API at initialization time is used; this may be overridden by passing a different
     * session request in opt_sessionRequest. 
     */
    CastPlayer.prototype.launchApp = function () {
        console.log("launching app...");
        chrome.cast.requestSession(this.onRequestSessionSuccess.bind(this), this.onLaunchError.bind(this));
        if (this.timer) {
            clearInterval(this.timer);
        }
    };

    /**
     * Callback function for request session success 
     * @param {Object} e A chrome.cast.Session object
     */
    CastPlayer.prototype.onRequestSessionSuccess = function (e) {
        console.log("session success: " + e.sessionId);
        this.session = e;
        this.deviceState = DEVICE_STATE.ACTIVE;
        this.updateMediaControlUI();
        this.loadMedia(this.currentMediaIndex);
    };

    /**
     * Callback function for launch error
     */
    CastPlayer.prototype.onLaunchError = function () {
        console.log("launch error");
        this.deviceState = DEVICE_STATE.ERROR;
    };

    /**
     * Stops the running receiver application associated with the session.
     */
    CastPlayer.prototype.stopApp = function () {
        this.session.stop(this.onStopAppSuccess.bind(this, 'Session stopped'),
            this.onError.bind(this));

    };

    /**
     * Callback function for stop app success 
     */
    CastPlayer.prototype.onStopAppSuccess = function (message) {
        console.log(message);
        this.deviceState = DEVICE_STATE.IDLE;
        this.castPlayerState = PLAYER_STATE.IDLE;
        this.currentMediaSession = null;
        clearInterval(this.timer);
        this.updateDisplayMessage();

        // continue to play media locally
        console.log("current time: " + this.currentMediaTime);
        this.playMediaLocally(this.currentMediaTime);
        this.updateMediaControlUI();
    };

    /**
     * Loads media into a running receiver application
     * @param {Number} mediaIndex An index number to indicate current media content
     */
    CastPlayer.prototype.loadMedia = function (mediaIndex) {
        //if (!this.session) {
        //    console.log("no session");
        //    return;
        //}
        //console.log("loading..." + this.mediaContents[mediaIndex]['title']);
        //var mediaInfo = new chrome.cast.media.MediaInfo(this.mediaContents[mediaIndex]['sources'][0]);
        //mediaInfo.contentType = 'video/mp4';
        //var request = new chrome.cast.media.LoadRequest(mediaInfo);
        //request.autoplay = this.autoplay;
        //if (this.localPlayerState == PLAYER_STATE.PLAYING) {
        //    request.currentTime = this.localPlayer.currentTime;
        //}
        //else {
        //    request.currentTime = 0;
        //}
        //var payload = {
        //    "title:": this.mediaContents[0]['title'],
        //    "thumb": this.mediaContents[0]['thumb']
        //};

        //var json = {
        //    "payload": payload
        //};

        //request.customData = json;

        //this.castPlayerState = PLAYER_STATE.LOADING;
        //this.session.loadMedia(request,
        //  this.onMediaDiscovered.bind(this, 'loadMedia'),
        //  this.onLoadMediaError.bind(this));

        //document.getElementById("media_title").innerHTML = this.mediaContents[this.currentMediaIndex]['title'];
        //document.getElementById("media_subtitle").innerHTML = this.mediaContents[this.currentMediaIndex]['subtitle'];
        //document.getElementById("media_desc").innerHTML = this.mediaContents[this.currentMediaIndex]['description'];

    };

    /**
     * Callback function for loadMedia success
     * @param {Object} mediaSession A new media object.
     */
    CastPlayer.prototype.onMediaDiscovered = function (how, mediaSession) {
        //console.log("new media session ID:" + mediaSession.mediaSessionId + ' (' + how + ')');
        //this.currentMediaSession = mediaSession;
        //if (how == 'loadMedia') {
        //    if (this.autoplay) {
        //        this.castPlayerState = PLAYER_STATE.PLAYING;
        //    }
        //    else {
        //        this.castPlayerState = PLAYER_STATE.LOADED;
        //    }
        //}

        //if (how == 'activeSession') {
        //    this.castPlayerState = this.session.media[0].playerState;
        //    this.currentMediaTime = this.session.media[0].currentTime;
        //}

        //if (this.castPlayerState == PLAYER_STATE.PLAYING) {
        //    // start progress timer
        //    this.startProgressTimer(this.incrementMediaTime);
        //}

        //this.currentMediaSession.addUpdateListener(this.onMediaStatusUpdate.bind(this));

        //this.currentMediaDuration = this.currentMediaSession.media.duration;
        //var duration = this.currentMediaDuration;
        //var hr = parseInt(duration / 3600);
        //duration -= hr * 3600;
        //var min = parseInt(duration / 60);
        //var sec = parseInt(duration % 60);
        //if (hr > 0) {
        //    duration = hr + ":" + min + ":" + sec;
        //}
        //else {
        //    if (min > 0) {
        //        duration = min + ":" + sec;
        //    }
        //    else {
        //        duration = sec;
        //    }
        //}
        //document.getElementById("duration").innerHTML = duration;

        //if (this.localPlayerState == PLAYER_STATE.PLAYING) {
        //    this.localPlayerState == PLAYER_STATE.STOPPED;
        //    var vi = document.getElementById('video_image')
        //    vi.style.display = 'block';
        //    this.localPlayer.style.display = 'none';
        //    // start progress timer
        //    this.startProgressTimer(this.incrementMediaTime);
        //}
        //// update UIs
        //this.updateMediaControlUI();
        //this.updateDisplayMessage();
    };

    window.CastPlayer = CastPlayer;

    $(function() {

        var castPlayer = new CastPlayer();

    });

})(window, window.chrome, console);