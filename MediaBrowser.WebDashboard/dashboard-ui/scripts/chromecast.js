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

    var CastPlayer = function () {

        /* device variables */
        // @type {DEVICE_STATE} A state for device
        this.deviceState = DEVICE_STATE.IDLE;

        /* Cast player variables */
        // @type {Object} a chrome.cast.media.Media object
        this.currentMediaSession = null;
        // @type {Number} volume
        this.currentVolume = 1;

        // @type {string} a chrome.cast.Session object
        this.session = null;
        // @type {PLAYER_STATE} A state for Cast media player
        this.castPlayerState = PLAYER_STATE.IDLE;

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

        this.hasReceivers = false;

        this.currentMediaOffset = 0;

        // Progress bar element id
        this.progressBar = "positionSlider";

        // Timec display element id
        this.duration = "currentTime";

        // Playback display element id
        this.playback = "playTime";

        // bind once - commit 2ebffc2271da0bc5e8b13821586aee2a2e3c7753
        this.errorHandler = this.onError.bind(this);
        this.incrementMediaTimeHandler = this.incrementMediaTime.bind(this);
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
        var applicationID = '472F0435';

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
            this.deviceState = DEVICE_STATE.ACTIVE;
            MediaController.setActivePlayer(PlayerName);
            if (this.session.media[0]) {
                this.onMediaDiscovered('activeSession', this.session.media[0]);
            }

            this.session.addUpdateListener(this.sessionUpdateListener.bind(this));
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
        if (!isAlive) {
            this.session = null;
            this.deviceState = DEVICE_STATE.IDLE;
            this.castPlayerState = PLAYER_STATE.IDLE;
            this.currentMediaSession = null;
            clearInterval(this.timer);

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
        if (this.timer) {
            clearInterval(this.timer);
        }
    };

    /**
     * Callback function for request session success 
     * @param {Object} e A chrome.cast.Session object
     */
    CastPlayer.prototype.onRequestSessionSuccess = function (e) {
        console.log("chromecast session success: " + e.sessionId);
        this.session = e;
        this.deviceState = DEVICE_STATE.ACTIVE;
        this.session.addUpdateListener(this.sessionUpdateListener.bind(this));
    };

    /**
     * Callback function for launch error
     */
    CastPlayer.prototype.onLaunchError = function () {
        console.log("chromecast launch error");
        this.deviceState = DEVICE_STATE.ERROR;

        Dashboard.alert({

            title: Globalize.translate("Error"),
            message: Globalize.translate("ErrorLaunchingChromecast")

        });

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
        clearInterval(this.timer);
    };

    /**
     * Loads media into a running receiver application
     * @param {Number} mediaIndex An index number to indicate current media content
     */
    CastPlayer.prototype.loadMedia = function (userId, options, command) {

        if (!this.session) {
            console.log("no session");
            return;
        }

        //this.currentMediaOffset = startTimeTicks || 0;

        //var maxBitrate = 12000000;
        //var mediaInfo = getMediaSourceInfo(user, item, maxBitrate, mediaSourceId, audioStreamIndex, subtitleStreamIndex);

        //var streamUrl = getStreamUrl(item, mediaInfo, startTimeTicks, maxBitrate);

        //var castMediaInfo = new chrome.cast.media.MediaInfo(streamUrl);

        //castMediaInfo.customData = getCustomData(item, mediaInfo.mediaSource.Id, startTimeTicks);
        //castMediaInfo.metadata = getMetadata(item);

        //if (mediaInfo.streamContainer == 'm3u8') {
        //    castMediaInfo.contentType = 'application/x-mpegURL';
        //} else {
        //    castMediaInfo.contentType = item.MediaType.toLowerCase() + '/' + mediaInfo.streamContainer.toLowerCase();
        //}

        //castMediaInfo.streamType = mediaInfo.isStatic ? chrome.cast.media.StreamType.BUFFERED : chrome.cast.media.StreamType.LIVE;

        //var request = new chrome.cast.media.LoadRequest(castMediaInfo);
        //request.autoplay = true;
        //request.currentTime = startTimeTicks ? startTimeTicks / 10000000 : 0;

        //this.castPlayerState = PLAYER_STATE.LOADING;
        //this.session.loadMedia(request,
        //  this.onMediaDiscovered.bind(this, 'loadMedia'),
        //  this.onLoadMediaError.bind(this));
    };

    /**
     * Callback function for loadMedia success
     * @param {Object} mediaSession A new media object.
     */
    CastPlayer.prototype.onMediaDiscovered = function (how, mediaSession) {

        console.log("chromecast new media session ID:" + mediaSession.mediaSessionId + ' (' + how + ')');
        this.currentMediaSession = mediaSession;
        this.currentMediaTime = mediaSession.currentTime;

        if (how == 'loadMedia') {
            this.castPlayerState = PLAYER_STATE.PLAYING;
            clearInterval(this.timer);
            this.startProgressTimer();
        }

        if (how == 'activeSession') {
            this.castPlayerState = mediaSession.playerState;
        }

        if (this.castPlayerState == PLAYER_STATE.PLAYING) {
            // start progress timer
            this.startProgressTimer();
        }

        this.currentMediaSession.addUpdateListener(this.mediaStatusUpdateHandler);
        this.currentMediaDuration = mediaSession.media.duration * 10000000;
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
            this.currentMediaTime = 0;
            this.castPlayerState = PLAYER_STATE.IDLE;
        }
        console.log("chromecast updating media");
        this.updateProgressBarByTimer();
    };

    /**
     * Helper function
     * Increment media current position by 1 second 
     */
    CastPlayer.prototype.incrementMediaTime = function () {
        if (this.castPlayerState == PLAYER_STATE.PLAYING) {
            if (this.currentMediaTime < this.currentMediaDuration) {
                this.currentMediaTime += 1;
                this.updateProgressBarByTimer();
            }
            else {
                this.currentMediaTime = 0;
                clearInterval(this.timer);
            }
        }
    };

    /**
     * Play media in Cast mode 
     */
    CastPlayer.prototype.playMedia = function () {

        if (!this.currentMediaSession) {
            return;
        }

        switch (this.castPlayerState) {
            case PLAYER_STATE.LOADED:
            case PLAYER_STATE.PAUSED:
                this.currentMediaSession.play(null,
                  this.mediaCommandSuccessCallback.bind(this, "playing started for " + this.currentMediaSession.sessionId),
                  this.errorHandler);
                this.currentMediaSession.addUpdateListener(this.mediaStatusUpdateHandler);
                this.castPlayerState = PLAYER_STATE.PLAYING;
                // start progress timer
                clearInterval(this.timer);
                this.startProgressTimer();
                break;
            case PLAYER_STATE.IDLE:
            case PLAYER_STATE.LOADING:
            case PLAYER_STATE.STOPPED:
                this.loadMedia();
                this.currentMediaSession.addUpdateListener(this.mediaStatusUpdateHandler);
                this.castPlayerState = PLAYER_STATE.PLAYING;
                break;
            default:
                break;
        }
    };

    /**
     * Pause media playback in Cast mode  
     */
    CastPlayer.prototype.pauseMedia = function () {

        if (!this.currentMediaSession) {
            return;
        }

        if (this.castPlayerState == PLAYER_STATE.PLAYING) {
            this.castPlayerState = PLAYER_STATE.PAUSED;
            this.currentMediaSession.pause(null,
              this.mediaCommandSuccessCallback.bind(this, "paused " + this.currentMediaSession.sessionId),
              this.errorHandler);
            clearInterval(this.timer);
        }
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
        clearInterval(this.timer);
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
            this.currentVolume = vol || 1;
            this.session.setReceiverVolumeLevel(this.currentVolume,
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

        if (this.castPlayerState != PLAYER_STATE.PLAYING && this.castPlayerState != PLAYER_STATE.PAUSED) {
            return;
        }

        this.currentMediaTime = curr;
        console.log('Seeking ' + this.currentMediaSession.sessionId + ':' +
          this.currentMediaSession.mediaSessionId + ' to ' + curr);
        var request = new chrome.cast.media.SeekRequest();
        request.currentTime = this.currentMediaTime;
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

    /**
     * Update progress bar when there is a media status update
     * @param {Object} e An media status update object 
     */
    CastPlayer.prototype.updateProgressBar = function (e) {
        if (e.idleReason == 'FINISHED' && e.playerState == 'IDLE') {
            clearInterval(this.timer);
            this.castPlayerState = PLAYER_STATE.STOPPED;
            if (e.idleReason == 'FINISHED') {
                $(this).trigger("/playback/complete", e);
            }
        }
        else {
            var p = Number(e.currentTime / this.currentMediaSession.media.duration + 1).toFixed(3);
            this.progressFlag = false;
            setTimeout(this.setProgressFlag.bind(this), 1000); // don't update progress in 1 second
        }
    };

    /**
     * Set progressFlag with a timeout of 1 second to avoid UI update
     * until a media status update from receiver 
     */
    CastPlayer.prototype.setProgressFlag = function () {
        this.progressFlag = true;
    };

    /**
     * Update progress bar based on timer  
     */
    CastPlayer.prototype.updateProgressBarByTimer = function () {
        if (!this.currentMediaTime) {
            this.currentMediaDuration = this.session.media[0].currentTime;
        }

        if (!this.currentMediaDuration) {
            this.currentMediaDuration = this.session.media[0].media.customData.runTimeTicks;
        }

        var pp = 0;
        if (this.currentMediaDuration > 0) {
            pp = Number(this.currentMediaTime / this.currentMediaDuration).toFixed(3);
        }

        if (this.progressFlag) {
            // don't update progress if it's been updated on media status update event
            $(this).trigger("/playback/update",
            [{
                positionTicks: this.currentMediaTime * 10000000,
                runtimeTicks: this.currentMediaDuration
            }]);
        }

        if (pp > 100 || this.castPlayerState == PLAYER_STATE.IDLE) {
            clearInterval(this.timer);
            this.deviceState = DEVICE_STATE.IDLE;
            this.castPlayerState = PLAYER_STATE.IDLE;
            $(this).trigger("/playback/complete", true);
        }
    };

    /**
    * @param {function} A callback function for the fucntion to start timer 
    */
    CastPlayer.prototype.startProgressTimer = function () {
        if (this.timer) {
            clearInterval(this.timer);
            this.timer = null;
        }

        // start progress timer
        this.timer = setInterval(this.incrementMediaTimeHandler, this.timerStep);
    };

    var castPlayer = new CastPlayer();

    function getCodecLimits() {

        return {

            maxVideoAudioChannels: 6,
            maxAudioChannels: 2,
            maxVideoLevel: 41,
            maxWidth: 1920,
            maxHeight: 1080,
            maxSampleRate: 44100

        };
    }

    function canDirectStream(mediaType, mediaSource, maxBitrate) {

        // If bitrate is unknown don't direct stream
        if (!mediaSource.Bitrate || mediaSource.Bitrate > maxBitrate) {
            return false;
        }

        var codecLimits = getCodecLimits();

        if (mediaType == "Audio") {

            return ['mp3', 'aac'].indexOf(mediaSource.Container || '') != -1;
        }
        else if (mediaType == "Video") {

            var videoStream = mediaSource.MediaStreams.filter(function (s) {

                return s.Type == 'Video';

            })[0];

            if (!videoStream) {
                return false;
            }

            if (['high', 'main', 'baseline'].indexOf((videoStream.Profile || '').toLowerCase()) == -1) {
                return false;
            }

            if (!videoStream.Level || videoStream.Level > codecLimits.maxVideoLevel) {
                return false;
            }

            if (!videoStream.Width || videoStream.Width > codecLimits.maxWidth) {
                return false;
            }

            if (!videoStream.Height || videoStream.Height > codecLimits.maxHeight) {
                return false;
            }

            return ['mp4'].indexOf(mediaSource.Container || '') != -1;
        }

        throw new Error('Unrecognized MediaType');
    }

    function canPlayAudioStreamDirect(audioStream, isVideo) {

        var audioCodec = (audioStream.Codec || '').toLowerCase().replace('-', '');

        if (audioCodec.indexOf('aac') == -1 &&
            audioCodec.indexOf('mp3') == -1 &&
            audioCodec.indexOf('mpeg') == -1) {

            return false;
        }

        var codecLimits = getCodecLimits();

        var maxChannels = isVideo ? codecLimits.maxVideoAudioChannels : codecLimits.maxAudioChannels;

        if (!audioStream.Channels || audioStream.Channels > maxChannels) {
            return false;
        }

        if (!audioStream.SampleRate || audioStream.SampleRate > codecLimits.maxSampleRate) {
            return false;
        }

        return true;
    }

    function isSupportedCodec(mediaType, mediaSource) {

        if (mediaType == "Audio") {
            return false;
        }
        else if (mediaType == "Video") {

            return mediaSource.MediaStreams.filter(function (m) {

                return m.Type == "Video" && (m.Codec || '').toLowerCase() == 'h264';

            }).length > 0;
        }

        throw new Error('Unrecognized MediaType');
    }

    function getStreamByIndex(streams, type, index) {
        return streams.filter(function (s) {

            return s.Type == type && s.Index == index;

        })[0];
    }

    function getDefaultAudioStream(mediaStreams, user) {

        // Find all audio streams
        var audioStreams = mediaStreams.filter(function (stream) {
            return stream.Type == "Audio";

        }).sort(function (a, b) {

            var av = a.IsDefault ? 0 : 1;
            var bv = b.IsDefault ? 0 : 1;

            return av - bv;
        });

        if (user.Configuration.AudioLanguagePreference) {

            for (var i = 0, length = audioStreams.length; i < length; i++) {
                var mediaStream = audioStreams[i];

                if (mediaStream.Language == user.Configuration.AudioLanguagePreference) {
                    return mediaStream.Index;
                }

            }
        }

        // Just use the first audio stream
        return audioStreams[0];
    }

    function getMediaSourceInfo(user, item, maxBitrate, mediaSourceId, audioStreamIndex, subtitleStreamIndex) {

        var sources = item.MediaSources || [];

        // If a specific stream was requested, filter the list
        if (mediaSourceId) {
            sources = sources.filter(function (m) {

                return m.Id == mediaSourceId;

            });
        }

        // Find first one that can be direct streamed
        var source = sources.filter(function (m) {

            var audioStreams = m.MediaStreams.filter(function (s) {
                return s.Type == 'Audio';
            });

            var audioStream = mediaSourceId == m.Id && audioStreamIndex != null ? getStreamByIndex(audioStreams, 'Audio', audioStreamIndex) : getDefaultAudioStream(audioStreams, user);

            if (!audioStream || !canPlayAudioStreamDirect(audioStream, item.MediaType == 'Video')) {
                return false;
            }

            var subtitleStream = mediaSourceId == m.Id && subtitleStreamIndex != null ? getStreamByIndex(m.MediaStreams, 'Subtitle', subtitleStreamIndex) : null;

            if (subtitleStream) {
                return false;
            }

            return canDirectStream(item.MediaType, m, maxBitrate, audioStream);

        })[0];

        if (source) {
            return {
                mediaSource: source,
                isStatic: true,
                streamContainer: source.Container
            };
        }

        // Find first one with supported codec
        source = sources.filter(function (m) {

            return isSupportedCodec(item.MediaType, m);

        })[0];

        // Default to first one
        return {
            mediaSource: source || sources[0],
            isStatic: false,
            streamContainer: item.MediaType == 'Audio' ? 'mp3' : 'm3u8'
        };
    }

    function getCustomData(item, mediaSourceId, startTimeTicks) {

        return {

            serverAddress: ApiClient.serverAddress(),
            itemId: item.Id,
            userId: Dashboard.getCurrentUserId(),
            deviceName: ApiClient.deviceName(),
            //deviceId: ApiClient.deviceId(),
            startTimeTicks: startTimeTicks || 0,
            runTimeTicks: item.RunTimeTicks
        };

    }

    function getMetadata(item) {

        var metadata = {};

        if (item.Type == 'Episode') {
            metadata = new chrome.cast.media.TvShowMediaMetadata();
            metadata.type = chrome.cast.media.MetadataType.TV_SHOW;

            metadata.episodeTitle = item.Name;

            if (item.PremiereDate) {
                metadata.originalAirdate = parseISO8601Date(item.PremiereDate).toISOString();
            }

            metadata.seriesTitle = item.SeriesName;

            if (item.IndexNumber != null) {
                metadata.episode = metadata.episodeNumber = item.IndexNumber;
            }

            if (item.ParentIndexNumber != null) {
                metadata.season = metadata.seasonNumber = item.ParentIndexNumber;
            }
        }

        else if (item.Type == 'Photo') {
            metadata = new chrome.cast.media.PhotoMediaMetadata();
            metadata.type = chrome.cast.media.MetadataType.PHOTO;

            if (item.PremiereDate) {
                metadata.creationDateTime = parseISO8601Date(item.PremiereDate).toISOString();
            }
        }

        else if (item.MediaType == 'Audio') {
            metadata = new chrome.cast.media.MusicTrackMediaMetadata();
            metadata.type = chrome.cast.media.MetadataType.MUSIC_TRACK;

            if (item.ProductionYear) {
                metadata.releaseYear = item.ProductionYear;
            }

            if (item.PremiereDate) {
                metadata.releaseDate = parseISO8601Date(item.PremiereDate).toISOString();
            }

            metadata.songName = item.Name;
            metadata.artist = item.Artists & item.Artists.length ? item.Artists[0] : '';
            metadata.albumArtist = item.AlbumArtist;

            if (item.IndexNumber != null) {
                metadata.trackNumber = item.IndexNumber;
            }

            if (item.ParentIndexNumber != null) {
                metadata.discNumber = item.ParentIndexNumber;
            }

            var composer = (item.People || []).filter(function (p) {
                return p.PersonType == 'Type';
            })[0];

            if (composer) {
                metadata.composer = composer.Name;
            }
        }

        else if (item.MediaType == 'Movie') {
            metadata = new chrome.cast.media.MovieMediaMetadata();
            metadata.type = chrome.cast.media.MetadataType.MOVIE;

            if (item.ProductionYear) {
                metadata.releaseYear = item.ProductionYear;
            }

            if (item.PremiereDate) {
                metadata.releaseDate = parseISO8601Date(item.PremiereDate).toISOString();
            }
        }

        else {
            metadata = new chrome.cast.media.GenericMediaMetadata();
            metadata.type = chrome.cast.media.MetadataType.GENERIC;

            if (item.ProductionYear) {
                metadata.releaseYear = item.ProductionYear;
            }

            if (item.PremiereDate) {
                metadata.releaseDate = parseISO8601Date(item.PremiereDate).toISOString();
            }
        }

        metadata.title = item.Name;

        if (item.Studios && item.Studios.length) {
            metadata.Studio = item.Studios[0];
        }

        return metadata;
    }

    function getStreamUrl(item, mediaSourceInfo, startTimeTicks, maxBitrate) {

        var url;

        var codecLimits = getCodecLimits();

        if (item.MediaType == 'Audio') {

            url = ApiClient.serverAddress() + '/mediabrowser/audio/' + item.Id + '/stream.' + mediaSourceInfo.streamContainer + '?';
            url += '&static=' + mediaSourceInfo.isStatic.toString();
            url += '&maxaudiochannels=' + codecLimits.maxAudioChannels;

            if (startTimeTicks) {
                url += '&startTimeTicks=' + startTimeTicks.toString();
            }

            if (maxBitrate) {
                url += '&audiobitrate=' + Math.min(maxBitrate, 320000).toString();
            }

            url += '&audiosamplerate=' + codecLimits.maxSampleRate;
            url += '&mediasourceid=' + mediaSourceInfo.mediaSource.Id;

            return url;

        }
        else if (item.MediaType == 'Video') {

            url = ApiClient.serverAddress() + '/mediabrowser/videos/' + item.Id + '/stream.' + mediaSourceInfo.streamContainer + '?';
            url += 'static=' + mediaSourceInfo.isStatic.toString();
            url += '&maxaudiochannels=' + codecLimits.maxVideoAudioChannels;

            if (startTimeTicks) {
                url += '&startTimeTicks=' + startTimeTicks.toString();
            }

            if (maxBitrate) {

                var audioRate = 768000;
                url += '&audiobitrate=' + audioRate.toString();
                url += '&videobitrate=' + (maxBitrate - audioRate).toString();
            }

            url += '&profile=high';
            url += '&level=' + codecLimits.maxVideoLevel;

            url += '&maxwidth=' + codecLimits.maxWidth;
            url += '&maxheight=' + codecLimits.maxHeight;

            url += '&videoCodec=h264';
            url += '&audioCodec=aac,mp3';

            url += '&audiosamplerate=' + codecLimits.maxSampleRate;
            url += '&mediasourceid=' + mediaSourceInfo.mediaSource.Id;

            return url;
        }

        throw new Error('Unrecognized MediaType');
    }

    function chromecastPlayer() {

        var self = this;

        var getItemFields = "MediaSources,Chapters";

        self.name = PlayerName;

        self.isPaused = false;

        self.isMuted = false;

        self.positionTicks = 0;

        self.runtimeTicks = 0;

        $(castPlayer).on("/playback/complete", function (e) {

            var state = self.getPlayerStateInternal();

            $(self).trigger("playbackstop", [state]);

        });

        $(castPlayer).on("/playback/update", function (e, data) {

            self.positionTicks = data.positionTicks;
            self.runtimeTicks = data.runtimeTicks;

            var state = self.getPlayerStateInternal();

            $(self).trigger("positionchange", [state]);
        });

        self.play = function (options) {
            castPlayer.loadMedia(Dashboard.getCurrentUserId(), options, 'PlayNow');
        };

        self.unpause = function () {
            self.isPaused = !self.isPaused;
            castPlayer.playMedia();
        };

        self.pause = function () {
            self.isPaused = true;
            castPlayer.pauseMedia();
        };

        self.shuffle = function (id) {
            var userId = Dashboard.getCurrentUserId();
            ApiClient.getItem(userId, id).done(function (item) {
                var query = {
                    UserId: userId,
                    Fields: getItemFields,
                    Limit: 50,
                    Filters: "IsNotFolder",
                    Recursive: true,
                    SortBy: "Random"
                };

                if (item.IsFolder) {
                    query.ParentId = id;
                }
                else if (item.Type == "MusicArtist") {
                    query.MediaTypes = "Audio";
                    query.Artists = item.Name;
                }
                else if (item.Type == "MusicGenre") {
                    query.MediaTypes = "Audio";
                    query.Genres = item.Name;
                } else {
                    return;
                }

                self.getItemsForPlayback(query).done(function (result) {
                    self.play({ items: result.Items });
                });
            });
        };

        self.instantMix = function (id) {
            var userId = Dashboard.getCurrentUserId();
            ApiClient.getItem(userId, id).done(function (item) {
                var promise;
                var mixLimit = 3;

                if (item.Type == "MusicArtist") {
                    promise = ApiClient.getInstantMixFromArtist(name, {
                        UserId: userId,
                        Fields: getItemFields,
                        Limit: mixLimit
                    });
                }
                else if (item.Type == "MusicGenre") {
                    promise = ApiClient.getInstantMixFromMusicGenre(name, {
                        UserId: userId,
                        Fields: getItemFields,
                        Limit: mixLimit
                    });
                }
                else if (item.Type == "MusicAlbum") {
                    promise = ApiClient.getInstantMixFromAlbum(id, {
                        UserId: userId,
                        Fields: getItemFields,
                        Limit: mixLimit
                    });
                }
                else if (item.Type == "Audio") {
                    promise = ApiClient.getInstantMixFromSong(id, {
                        UserId: userId,
                        Fields: getItemFields,
                        Limit: mixLimit
                    });
                }
                else {
                    return;
                }

                promise.done(function (result) {
                    self.play({ items: result.Items });
                });
            });
        };

        self.canQueueMediaType = function (mediaType) {
            return mediaType == "Audio";
        };

        self.queue = function (options) {
            castPlayer.loadMedia(Dashboard.getCurrentUserId(), options, 'PlayLast');
        };

        self.queueNext = function (options) {
            castPlayer.loadMedia(Dashboard.getCurrentUserId(), options, 'PlayNext');
        };

        self.stop = function () {
            castPlayer.stopMedia();
        };

        self.displayContent = function (options) {

        };

        self.mute = function () {
            self.isMuted = true;
            castPlayer.mute();
        };

        self.unMute = function () {
            self.isMuted = false;
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
                playerName: self.name, // TODO: PlayerName == self.name, so do we need to use either/or?
                playableMediaTypes: ["Audio", "Video"],
                isLocalPlayer: false,
                appName: appName,
                supportedCommands: ["VolumeUp",
                                    "VolumeDown",
                                    "Mute",
                                    "Unmute",
                                    "ToggleMute",
                                    "SetVolume",
                                    "DisplayContent"]
            };
        };

        self.seek = function (position) {
            castPlayer.seekMedia(position);
        };

        self.nextTrack = function () {
        };

        self.previousTrack = function () {
        };

        self.beginPlayerUpdates = function () {
            // Setup polling here
        };

        self.endPlayerUpdates = function () {
            // Stop polling here
        };

        self.volumeDown = function () {
            var vol = castPlayer.volumeLevel - 0.02;
            castPlayer.setReceiverVolume(false, vol / 100);
        };

        self.volumeUp = function () {
            var vol = castPlayer.volumeLevel + 0.02;
            castPlayer.setReceiverVolume(false, vol / 100);
        };

        self.setVolume = function (vol) {
            castPlayer.setReceiverVolume(false, vol / 100);
        };

        self.getPlayerState = function () {

            var deferred = $.Deferred();

            var result = self.getPlayerStateInternal();

            deferred.resolveWith(null, [result]);

            return deferred.promise();
        };

        self.getPlayerStateInternal = function () {

            var state = {
                PlayState: {

                    CanSeek: self.runtimeTicks && self.positionTicks > 0,
                    PositionTicks: self.positionTicks,
                    VolumeLevel: castPlayer.currentVolume * 100,
                    IsPaused: self.isPaused,
                    IsMuted: self.isMuted

                    // TODO: Implement
                    // AudioStreamIndex: null,
                    // SubtitleStreamIndex: null,
                    // PlayMethod: 'DirectStream' or 'Transcode'
                }
            };

            // TODO: Implement
            var isPlaying = false;

            if (isPlaying) {

                //state.PlayState.MediaSourceId = 'xxx';

                state.NowPlayingItem = {

                    RunTimeTicks: self.runtimeTicks,
                    Name: 'Chromecast'
                };

                var nowPlayingItem = state.NowPlayingItem;

                // TODO: Fill in these properties using chromecast mediainfo and/or custom data
                //nowPlayingItem.Id = item.Id;
                //nowPlayingItem.MediaType = item.MediaType;
                //nowPlayingItem.Type = item.Type;
                //nowPlayingItem.Name = item.Name;

                //nowPlayingItem.IndexNumber = item.IndexNumber;
                //nowPlayingItem.IndexNumberEnd = item.IndexNumberEnd;
                //nowPlayingItem.ParentIndexNumber = item.ParentIndexNumber;
                //nowPlayingItem.ProductionYear = item.ProductionYear;
                //nowPlayingItem.PremiereDate = item.PremiereDate;
                //nowPlayingItem.SeriesName = item.SeriesName;
                //nowPlayingItem.Album = item.Album;
                //nowPlayingItem.Artists = item.Artists;

            }

            return state;
        };
    }

    //MediaController.registerPlayer(new chromecastPlayer());

    //$(MediaController).on('playerchange', function () {

    //    if (MediaController.getPlayerInfo().name == PlayerName) {

    //        if (CastPlayer.deviceState != DEVICE_STATE.ACTIVE && CastPlayer.isInitialized) {
    //            CastPlayer.launchApp();
    //        }
    //    }
    //});

})(window, window.chrome, console);