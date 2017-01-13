define(['browser', 'pluginManager', 'events', 'apphost', 'loading', 'playbackManager', 'embyRouter', 'appSettings', 'connectionManager'], function (browser, pluginManager, events, appHost, loading, playbackManager, embyRouter, appSettings, connectionManager) {
    "use strict";

    return function () {

        var self = this;

        self.name = 'Html Video Player';
        self.type = 'mediaplayer';
        self.id = 'htmlvideoplayer';

        // Let any players created by plugins take priority
        self.priority = 1;

        var mediaElement;
        var videoDialog;
        var currentSrc;
        var started = false;
        var hlsPlayer;

        var winJsPlaybackItem;
        var currentPlayOptions;

        var subtitleTrackIndexToSetOnPlaying;

        var currentSubtitlesElement;
        var currentTrackEvents;
        var lastCustomTrackMs = 0;
        var currentClock;
        var currentAssRenderer;
        var customTrackIndex = -1;

        self.canPlayMediaType = function (mediaType) {

            return (mediaType || '').toLowerCase() === 'video';
        };

        function getSavedVolume() {
            return appSettings.get("volume") || 1;
        }

        function saveVolume(value) {
            if (value) {
                appSettings.set("volume", value);
            }
        }

        function getBaseProfileOptions(item) {

            var disableHlsVideoAudioCodecs = [];
            if (!canPlayNativeHls() || (browser.edge && !item.RunTimeTicks)) {

                // this does not work with hls.js + edge, but seems to be fine in other browsers
                if (browser.edge) {
                    disableHlsVideoAudioCodecs.push('mp3');
                }

                // hls.js does not support this
                disableHlsVideoAudioCodecs.push('ac3');
            }

            var enableMkvProgressive = (item.RunTimeTicks && browser.edgeUwp) ? true : false;

            return {
                enableMkvProgressive: enableMkvProgressive,
                disableHlsVideoAudioCodecs: disableHlsVideoAudioCodecs
            };
        }

        function getDeviceProfileForWindowsUwp(item) {

            return new Promise(function (resolve, reject) {

                require(['browserdeviceprofile', 'environments/windows-uwp/mediacaps'], function (profileBuilder, uwpMediaCaps) {

                    var profileOptions = getBaseProfileOptions(item);
                    profileOptions.supportsDts = uwpMediaCaps.supportsDTS();
                    profileOptions.supportsTrueHd = uwpMediaCaps.supportsDolby();
                    profileOptions.audioChannels = uwpMediaCaps.getAudioChannels();

                    resolve(profileBuilder(profileOptions));
                });
            });
        }

        self.getDeviceProfile = function (item) {

            if (window.Windows) {
                return getDeviceProfileForWindowsUwp(item);
            }

            return new Promise(function (resolve, reject) {

                require(['browserdeviceprofile'], function (profileBuilder) {

                    var profile = profileBuilder(getBaseProfileOptions(item));

                    if (!browser.edge && !browser.msie) {
                        // libjass not working here
                        profile.SubtitleProfiles.push({
                            Format: 'ass',
                            Method: 'External'
                        });
                        profile.SubtitleProfiles.push({
                            Format: 'ssa',
                            Method: 'External'
                        });
                    }

                    resolve(profile);
                });
            });
        };

        self.currentSrc = function () {
            return currentSrc;
        };

        function updateVideoUrl(streamInfo) {

            var isHls = streamInfo.url.toLowerCase().indexOf('.m3u8') !== -1;

            var mediaSource = streamInfo.mediaSource;
            var item = streamInfo.item;

            // Huge hack alert. Safari doesn't seem to like if the segments aren't available right away when playback starts
            // This will start the transcoding process before actually feeding the video url into the player
            // Edit: Also seeing stalls from hls.js
            if (mediaSource && item && !mediaSource.RunTimeTicks && isHls && (browser.iOS || browser.osx)) {

                var hlsPlaylistUrl = streamInfo.url.replace('master.m3u8', 'live.m3u8');

                loading.show();

                console.log('prefetching hls playlist: ' + hlsPlaylistUrl);

                return connectionManager.getApiClient(item.ServerId).ajax({

                    type: 'GET',
                    url: hlsPlaylistUrl

                }).then(function () {

                    console.log('completed prefetching hls playlist: ' + hlsPlaylistUrl);

                    loading.hide();
                    streamInfo.url = hlsPlaylistUrl;

                    return Promise.resolve();

                }, function () {

                    console.log('error prefetching hls playlist: ' + hlsPlaylistUrl);

                    loading.hide();
                    return Promise.resolve();
                });

            } else {
                return Promise.resolve();
            }
        }

        self.play = function (options) {

            started = false;
            _currentTime = null;

            return createMediaElement(options).then(function (elem) {

                return updateVideoUrl(options, options.mediaSource).then(function () {
                    return setCurrentSrc(elem, options);
                });
            });
        };

        var supportedFeatures;
        function getSupportedFeatures() {

            var list = [];

            var video = document.createElement('video');
            //if (video.webkitSupportsPresentationMode && video.webkitSupportsPresentationMode('picture-in-picture') && typeof video.webkitSetPresentationMode === "function") {
            //    list.push('pictureinpicture');
            //}
            if (browser.ipad) {

                // Unfortunately this creates a false positive on devices where its' not actually supported
                if (navigator.userAgent.toLowerCase().indexOf('os 9') === -1) {
                    if (video.webkitSupportsPresentationMode && video.webkitSupportsPresentationMode && typeof video.webkitSetPresentationMode === "function") {
                        list.push('pictureinpicture');
                    }
                }
            }

            return list;
        }

        self.supports = function (feature) {

            if (!supportedFeatures) {
                supportedFeatures = getSupportedFeatures();
            }

            return supportedFeatures.indexOf(feature) !== -1;
        };

        self.togglePictureInPicture = function () {
            return self.setPictureInPictureEnabled(!self.isPictureInPictureEnabled());
        };

        self.setPictureInPictureEnabled = function (isEnabled) {

            var video = mediaElement;
            if (video) {
                if (video.webkitSupportsPresentationMode && typeof video.webkitSetPresentationMode === "function") {
                    video.webkitSetPresentationMode(isEnabled ? "picture-in-picture" : "inline");
                }
            }
        };

        self.isPictureInPictureEnabled = function (isEnabled) {

            var video = mediaElement;
            if (video) {
                return video.webkitPresentationMode === "picture-in-picture";
            }

            return false;
        };

        function getCrossOriginValue(mediaSource) {

            return 'anonymous';
        }

        function requireHlsPlayer(callback) {
            require(['hlsjs'], function (hls) {
                window.Hls = hls;
                callback();
            });
        }

        function setCurrentSrc(elem, options) {

            //if (!elem) {
            //    currentSrc = null;
            //    resolve();
            //    return;
            //}

            //if (!options) {
            //    currentSrc = null;
            //    elem.src = null;
            //    elem.src = "";

            //    // When the browser regains focus it may start auto-playing the last video
            //    //if ($.browser.safari) {
            //    //    elem.src = 'files/dummy.mp4';
            //    //    elem.play();
            //    //}

            //    resolve();
            //    return;
            //}

            var val = options.url;

            console.log('playing url: ' + val);

            //if (AppInfo.isNativeApp && $.browser.safari) {
            //    val = val.replace('file://', '');
            //}

            // Convert to seconds
            var seconds = (options.playerStartPositionTicks || 0) / 10000000;
            if (seconds) {
                val += '#t=' + seconds;
            }

            destroyHlsPlayer();

            var tracks = getMediaStreamTextTracks(options.mediaSource);

            var currentTrackIndex = -1;
            for (var i = 0, length = tracks.length; i < length; i++) {
                if (tracks[i].Index === options.mediaSource.DefaultSubtitleStreamIndex) {
                    currentTrackIndex = tracks[i].Index;
                    break;
                }
            }
            subtitleTrackIndexToSetOnPlaying = currentTrackIndex;

            currentPlayOptions = options;

            elem.crossOrigin = getCrossOriginValue(options.mediaSource);

            if (enableHlsPlayer(val, options.item, options.mediaSource)) {

                setTracks(elem, tracks, options.mediaSource, options.item.ServerId);

                return new Promise(function (resolve, reject) {

                    requireHlsPlayer(function () {
                        var hls = new Hls({
                            manifestLoadingTimeOut: 20000
                            //appendErrorMaxRetry: 6,
                            //debug: true
                        });
                        hls.loadSource(val);
                        hls.attachMedia(elem);
                        hls.on(Hls.Events.MANIFEST_PARSED, function () {
                            playWithPromise(elem).then(resolve, reject);
                        });

                        hls.on(Hls.Events.ERROR, function (event, data) {

                            console.log('HLS Error: Type: ' + data.type + ' Details: ' + (data.details || '') + ' Fatal: ' + (data.fatal || false));

                            if (data.fatal) {
                                switch (data.type) {
                                    case Hls.ErrorTypes.NETWORK_ERROR:
                                        // try to recover network error
                                        console.log("fatal network error encountered, try to recover");
                                        hls.startLoad();
                                        break;
                                    case Hls.ErrorTypes.MEDIA_ERROR:
                                        console.log("fatal media error encountered, try to recover");
                                        handleMediaError();
                                        break;
                                    default:
                                        // cannot recover
                                        hls.destroy();
                                        break;
                                }
                            }
                        });

                        hlsPlayer = hls;

                        // This is needed in setCurrentTrackElement
                        currentSrc = val;

                        setCurrentTrackElement(currentTrackIndex);
                    });
                });

            } else {

                elem.autoplay = true;
                var mimeType = options.mimeType;

                // Opera TV guidelines suggest using source elements, so let's do that if we have a valid mimeType
                if (mimeType && browser.operaTv) {

                    if (browser.chrome && mimeType === 'video/x-matroska') {
                        mimeType = 'video/webm';
                    }

                    // Need to do this or we won't be able to restart a new stream
                    if (elem.currentSrc) {
                        elem.src = '';
                        elem.removeAttribute('src');
                    }

                    elem.innerHTML = '<source src="' + val + '" type="' + mimeType + '">' + getTracksHtml(tracks, options.mediaSource, options.item.ServerId);

                    elem.addEventListener('loadedmetadata', onLoadedMetadata);
                } else {
                    applySrc(elem, val);
                    setTracks(elem, tracks, options.mediaSource, options.item.ServerId);
                }

                // This is needed in setCurrentTrackElement
                currentSrc = val;

                setCurrentTrackElement(currentTrackIndex);
                return playWithPromise(elem);
            }
        }

        var recoverDecodingErrorDate, recoverSwapAudioCodecDate;

        function handleMediaError() {

            if (!hlsPlayer) {
                return;
            }

            var now = Date.now();

            if (window.performance && window.performance.now) {
                now = performance.now();
            }

            if (!recoverDecodingErrorDate || (now - recoverDecodingErrorDate) > 3000) {
                recoverDecodingErrorDate = now;
                console.log('try to recover media Error ...');
                hlsPlayer.recoverMediaError();
            } else {
                if (!recoverSwapAudioCodecDate || (now - recoverSwapAudioCodecDate) > 3000) {
                    recoverSwapAudioCodecDate = now;
                    console.log('try to swap Audio Codec and recover media Error ...');
                    hlsPlayer.swapAudioCodec();
                    hlsPlayer.recoverMediaError();
                } else {
                    console.error('cannot recover, last media error recovery failed ...');
                }
            }
        }

        function applySrc(elem, src) {

            if (window.Windows) {

                var playlist = new Windows.Media.Playback.MediaPlaybackList();
                var source1 = Windows.Media.Core.MediaSource.createFromUri(new Windows.Foundation.Uri(src));

                winJsPlaybackItem = new Windows.Media.Playback.MediaPlaybackItem(source1);
                playlist.items.append(winJsPlaybackItem);

                elem.src = URL.createObjectURL(playlist, { oneTimeOnly: true });

            } else {

                elem.src = src;
            }
        }

        function playWithPromise(elem) {

            try {
                var promise = elem.play();
                if (promise && promise.then) {
                    // Chrome now returns a promise
                    return promise.catch(function (e) {

                        var errorName = (e.name || '').toLowerCase();
                        // safari uses aborterror
                        if (errorName === 'notallowederror' ||
                            errorName === 'aborterror') {
                            // swallow this error because the user can still click the play button on the video element
                            return Promise.resolve();
                        }
                        return Promise.reject();
                    });
                } else {
                    return Promise.resolve();
                }
            } catch (err) {
                console.log('error calling video.play: ' + err);
                return Promise.reject();
            }
        }

        self.setSubtitleStreamIndex = function (index) {

            setCurrentTrackElement(index);
        };

        self.canSetAudioStreamIndex = function () {

            if (winJsPlaybackItem) {
                return true;
            }

            return false;
        };

        self.setAudioStreamIndex = function (index) {

            var audioTracks = getMediaStreamAudioTracks(currentPlayOptions.mediaSource);

            var track = audioTracks.filter(function (t) {
                return t.Index === index;
            })[0];

            if (!track) {
                return;
            }

            if (winJsPlaybackItem) {
                var audioIndex = audioTracks.indexOf(track);
                winJsPlaybackItem.audioTracks.selectedIndex = audioIndex;
            }
        };

        // Save this for when playback stops, because querying the time at that point might return 0
        var _currentTime;
        self.currentTime = function (val) {

            if (mediaElement) {
                if (val != null) {
                    mediaElement.currentTime = val / 1000;
                    return;
                }

                if (_currentTime) {
                    return _currentTime * 1000;
                }

                return (mediaElement.currentTime || 0) * 1000;
            }
        };

        self.duration = function (val) {

            if (mediaElement) {
                var duration = mediaElement.duration;
                if (duration && !isNaN(duration) && duration !== Number.POSITIVE_INFINITY && duration !== Number.NEGATIVE_INFINITY) {
                    return duration * 1000;
                }
            }

            return null;
        };

        self.stop = function (destroyPlayer, reportEnded) {

            var elem = mediaElement;
            var src = currentSrc;

            if (elem && src) {

                elem.pause();

                elem.src = '';
                elem.innerHTML = '';
                elem.removeAttribute("src");

                destroyHlsPlayer();

                onEndedInternal(reportEnded);

                if (destroyPlayer) {
                    self.destroy();
                }
            }

            destroyCustomTrack(elem);

            return Promise.resolve();
        };

        self.destroy = function () {

            destroyHlsPlayer();
            embyRouter.setTransparency('none');

            var videoElement = mediaElement;

            if (videoElement) {

                mediaElement = null;

                destroyCustomTrack(videoElement);

                videoElement.removeEventListener('timeupdate', onTimeUpdate);
                videoElement.removeEventListener('ended', onEnded);
                videoElement.removeEventListener('volumechange', onVolumeChange);
                videoElement.removeEventListener('pause', onPause);
                videoElement.removeEventListener('playing', onPlaying);
                videoElement.removeEventListener('error', onError);
                videoElement.removeEventListener('loadedmetadata', onLoadedMetadata);
                videoElement.removeEventListener('click', onClick);
                videoElement.removeEventListener('dblclick', onDblClick);

                videoElement.parentNode.removeChild(videoElement);
            }

            var dlg = videoDialog;
            if (dlg) {

                videoDialog = null;

                dlg.parentNode.removeChild(dlg);
            }
        };

        function destroyHlsPlayer() {
            var player = hlsPlayer;
            if (player) {
                try {
                    player.destroy();
                } catch (err) {
                    console.log(err);
                }

                hlsPlayer = null;
            }
        }

        self.pause = function () {
            if (mediaElement) {
                mediaElement.pause();
            }
        };

        // This is a retry after error
        self.resume = function () {
            if (mediaElement) {
                mediaElement.play();
            }
        };

        self.unpause = function () {
            if (mediaElement) {
                mediaElement.play();
            }
        };

        self.paused = function () {

            if (mediaElement) {
                return mediaElement.paused;
            }

            return false;
        };

        self.setVolume = function (val) {
            if (mediaElement) {
                mediaElement.volume = val / 100;
            }
        };

        self.getVolume = function () {
            if (mediaElement) {
                return mediaElement.volume * 100;
            }
        };

        self.volumeUp = function () {
            self.setVolume(Math.min(self.getVolume() + 2, 100));
        };

        self.volumeDown = function () {
            self.setVolume(Math.max(self.getVolume() - 2, 0));
        };

        self.setMute = function (mute) {

            if (mediaElement) {
                mediaElement.muted = mute;
            }
        };

        self.isMuted = function () {
            if (mediaElement) {
                return mediaElement.muted;
            }
            return false;
        };

        function onEnded() {

            destroyCustomTrack(this);
            onEndedInternal(true);
        }

        function onEndedInternal(triggerEnded) {

            if (self.originalDocumentTitle) {
                document.title = self.originalDocumentTitle;
                self.originalDocumentTitle = null;
            }

            if (triggerEnded) {

                var stopInfo = {
                    src: currentSrc
                };

                events.trigger(self, 'stopped', [stopInfo]);

                _currentTime = null;
            }

            currentSrc = null;
            winJsPlaybackItem = null;
        }

        function onTimeUpdate(e) {

            // Get the player position + the transcoding offset
            var time = this.currentTime;
            _currentTime = time;
            var timeMs = time * 1000;
            timeMs += ((currentPlayOptions.transcodingOffsetTicks || 0) / 10000);
            updateSubtitleText(timeMs);

            events.trigger(self, 'timeupdate');
        }

        function onVolumeChange() {

            saveVolume(this.volume);
            events.trigger(self, 'volumechange');
        }

        function onNavigatedToOsd() {

            videoDialog.classList.remove('videoPlayerContainer-withBackdrop');
            videoDialog.classList.remove('videoPlayerContainer-onTop');
        }

        function onPlaying(e) {

            if (!started) {
                started = true;
                this.removeAttribute('controls');

                if (currentPlayOptions.title) {
                    self.originalDocumentTitle = document.title;
                    document.title = currentPlayOptions.title;
                } else {
                    self.originalDocumentTitle = null;
                }

                setCurrentTrackElement(subtitleTrackIndexToSetOnPlaying);

                seekOnPlaybackStart(e.target);

                if (currentPlayOptions.fullscreen) {

                    embyRouter.showVideoOsd().then(onNavigatedToOsd);

                } else {
                    embyRouter.setTransparency('backdrop');
                    videoDialog.classList.remove('videoPlayerContainer-withBackdrop');
                    videoDialog.classList.remove('videoPlayerContainer-onTop');
                }

                loading.hide();
            }
            events.trigger(self, 'playing');
        }

        function seekOnPlaybackStart(element) {

            var seconds = (currentPlayOptions.playerStartPositionTicks || 0) / 10000000;

            if (seconds) {
                var src = (self.currentSrc() || '').toLowerCase();

                // Appending #t=xxx to the query string doesn't seem to work with HLS
                // For plain video files, not all browsers support it either
                if (!browser.chrome || src.indexOf('.m3u8') !== -1) {

                    var delay = browser.safari ? 2500 : 0;
                    if (delay) {
                        setTimeout(function () {
                            element.currentTime = seconds;
                        }, delay);
                    } else {
                        element.currentTime = seconds;
                    }
                }
            }
        }

        function onClick() {
            events.trigger(self, 'click');
        }

        function onDblClick() {
            events.trigger(self, 'dblclick');
        }

        function onPause() {
            events.trigger(self, 'pause');
        }

        function onError() {

            var errorCode = this.error ? (this.error.code || 0) : 0;
            console.log('Media element error code: ' + errorCode.toString());

            var type;

            switch (errorCode) {
                case 1:
                    // MEDIA_ERR_ABORTED
                    // This will trigger when changing media while something is playing
                    return;
                case 2:
                    // MEDIA_ERR_NETWORK
                    type = 'network';
                    break;
                case 3:
                    // MEDIA_ERR_DECODE
                    handleMediaError();
                    return;
                case 4:
                    // MEDIA_ERR_SRC_NOT_SUPPORTED
                    break;
            }

            destroyCustomTrack(this);

            //events.trigger(self, 'error', [
            //{
            //    type: type
            //}]);
        }

        function onLoadedMetadata(e) {

            var mediaElem = e.target;
            mediaElem.removeEventListener('loadedmetadata', onLoadedMetadata);

            if (!hlsPlayer) {

                try {
                    mediaElem.play();
                } catch (err) {
                    console.log('error calling mediaElement.play: ' + err);
                }
            }
        }

        function enableHlsPlayer(src, item, mediaSource) {

            if (src) {
                if (src.indexOf('.m3u8') === -1) {
                    return false;
                }
            }

            if (window.MediaSource == null) {
                return false;
            }

            if (canPlayNativeHls()) {

                // simple playback should use the native support
                if (mediaSource.RunTimeTicks) {
                    //if (!browser.edge) {
                    return false;
                    //}
                }

                //return false;
            }

            // hls.js is only in beta. needs more testing.
            if (browser.safari && !browser.osx) {
                return false;
            }

            return true;
        }

        function canPlayNativeHls() {
            var media = document.createElement('video');

            if (media.canPlayType('application/x-mpegURL').replace(/no/, '') ||
                media.canPlayType('application/vnd.apple.mpegURL').replace(/no/, '')) {
                return true;
            }

            return false;
        }

        function setTracks(elem, tracks, mediaSource, serverId) {

            elem.innerHTML = getTracksHtml(tracks, mediaSource, serverId);
        }

        function getTextTrackUrl(track, serverId) {
            return playbackManager.getSubtitleUrl(track, serverId);
        }

        function getTracksHtml(tracks, mediaSource, serverId) {
            return tracks.map(function (t) {

                var defaultAttribute = mediaSource.DefaultSubtitleStreamIndex === t.Index ? ' default' : '';

                var language = t.Language || 'und';
                var label = t.Language || 'und';
                return '<track id="textTrack' + t.Index + '" label="' + label + '" kind="subtitles" src="' + getTextTrackUrl(t, serverId) + '" srclang="' + language + '"' + defaultAttribute + '></track>';

            }).join('');
        }

        var _supportsTextTracks;

        function supportsTextTracks() {

            if (_supportsTextTracks == null) {
                _supportsTextTracks = document.createElement('video').textTracks != null;
            }

            // For now, until ready
            return _supportsTextTracks;
        }

        function enableNativeTrackSupport(track) {

            if (browser.firefox) {
                if ((currentSrc || '').toLowerCase().indexOf('.m3u8') !== -1) {
                    return false;
                }
            }

            if (browser.ps4) {
                return false;
            }

            if (track) {
                var format = (track.Codec || '').toLowerCase();
                if (format === 'ssa' || format === 'ass') {
                    // libjass is needed here
                    return false;
                }
            }

            return true;
        }

        function destroyCustomTrack(videoElement, isPlaying) {

            window.removeEventListener('resize', onVideoResize);
            window.removeEventListener('orientationchange', onVideoResize);

            var videoSubtitlesElem = document.querySelector('.videoSubtitles');
            if (videoSubtitlesElem) {
                videoSubtitlesElem.parentNode.removeChild(videoSubtitlesElem);
            }

            if (isPlaying) {

                var allTracks = mediaElement.textTracks; // get list of tracks
                for (var i = 0; i < allTracks.length; i++) {

                    var currentTrack = allTracks[i];

                    if (currentTrack.label.indexOf('manualTrack') !== -1) {
                        currentTrack.mode = 'disabled';
                    }
                }
            }

            customTrackIndex = -1;
            currentSubtitlesElement = null;
            currentTrackEvents = null;
            currentClock = null;

            var renderer = currentAssRenderer;
            if (renderer) {
                renderer.setEnabled(false);
            }
            currentAssRenderer = null;
        }

        function fetchSubtitles(track, serverId) {

            return new Promise(function (resolve, reject) {

                require(['fetchHelper'], function (fetchHelper) {
                    fetchHelper.ajax({
                        url: getTextTrackUrl(track, serverId).replace('.vtt', '.js'),
                        type: 'GET',
                        dataType: 'json'
                    }).then(resolve, reject);
                });
            });
        }

        function setTrackForCustomDisplay(videoElement, track) {

            if (!track) {
                destroyCustomTrack(videoElement, true);
                return;
            }

            // if already playing thids track, skip
            if (customTrackIndex === track.Index) {
                return;
            }

            var serverId = currentPlayOptions.item.ServerId;

            destroyCustomTrack(videoElement, true);
            customTrackIndex = track.Index;
            renderTracksEvents(videoElement, track, serverId);
            lastCustomTrackMs = 0;
        }

        function renderWithLibjass(videoElement, track, serverId) {

            var rendererSettings = {};

            require(['libjass'], function (libjass) {

                libjass.ASS.fromUrl(getTextTrackUrl(track, serverId)).then(function (ass) {

                    var clock = new libjass.renderers.ManualClock();
                    currentClock = clock;

                    // Create a DefaultRenderer using the video element and the ASS object
                    var renderer = new libjass.renderers.WebRenderer(ass, clock, videoElement.parentNode, rendererSettings);

                    currentAssRenderer = renderer;

                    renderer.addEventListener("ready", function () {
                        try {
                            renderer.resize(videoElement.offsetWidth, videoElement.offsetHeight, 0, 0);
                            window.removeEventListener('resize', onVideoResize);
                            window.addEventListener('resize', onVideoResize);
                            window.removeEventListener('orientationchange', onVideoResize);
                            window.addEventListener('orientationchange', onVideoResize);
                            //clock.pause();
                        } catch (ex) {
                        }
                    });
                });
            });
        }

        function onVideoResize() {
            var renderer = currentAssRenderer;
            if (renderer) {
                var videoElement = mediaElement;
                var width = videoElement.offsetWidth;
                var height = videoElement.offsetHeight;
                console.log('videoElement resized: ' + width + 'x' + height);
                renderer.resize(width, height, 0, 0);
            }
        }

        function renderTracksEvents(videoElement, track, serverId) {

            var format = (track.Codec || '').toLowerCase();
            if (format === 'ssa' || format === 'ass') {
                // libjass is needed here
                renderWithLibjass(videoElement, track, serverId);
                return;
            }

            var trackElement = null;
            var expectedId = 'manualTrack' + track.Index;

            var allTracks = videoElement.textTracks; // get list of tracks
            for (var i = 0; i < allTracks.length; i++) {

                var currentTrack = allTracks[i];

                if (currentTrack.label === expectedId) {
                    trackElement = currentTrack;
                    break;
                } else {
                    currentTrack.mode = 'disabled';
                }
            }

            if (!trackElement) {
                trackElement = videoElement.addTextTrack('subtitles', 'manualTrack' + track.Index, track.Language || 'und');

                // download the track json
                fetchSubtitles(track, serverId).then(function (data) {

                    // show in ui
                    console.log('downloaded ' + data.TrackEvents.length + ' track events');
                    // add some cues to show the text
                    // in safari, the cues need to be added before setting the track mode to showing
                    data.TrackEvents.forEach(function (trackEvent) {

                        var trackCueObject = window.VTTCue || window.TextTrackCue;
                        var cue = new trackCueObject(trackEvent.StartPositionTicks / 10000000, trackEvent.EndPositionTicks / 10000000, trackEvent.Text.replace(/\\N/gi, '\n'));

                        trackElement.addCue(cue);
                    });
                    trackElement.mode = 'showing';
                });
            } else {
                trackElement.mode = 'showing';
            }
        }

        function updateSubtitleText(timeMs) {

            var clock = currentClock;
            if (clock) {
                clock.seek(timeMs / 1000);
            }

            var trackEvents = currentTrackEvents;
            if (!trackEvents) {
                return;
            }

            if (!currentSubtitlesElement) {
                var videoSubtitlesElem = document.querySelector('.videoSubtitles');
                if (!videoSubtitlesElem) {
                    videoSubtitlesElem = document.createElement('div');
                    videoSubtitlesElem.classList.add('videoSubtitles');
                    videoSubtitlesElem.innerHTML = '<div class="videoSubtitlesInner"></div>';
                    videoDialog.appendChild(videoSubtitlesElem);
                }
                currentSubtitlesElement = videoSubtitlesElem.querySelector('.videoSubtitlesInner');
            }

            if (lastCustomTrackMs > 0) {
                if (Math.abs(lastCustomTrackMs - timeMs) < 500) {
                    return;
                }
            }

            lastCustomTrackMs = new Date().getTime();

            var positionTicks = timeMs * 10000;
            for (var i = 0, length = trackEvents.length; i < length; i++) {

                var caption = trackEvents[i];
                if (positionTicks >= caption.StartPositionTicks && positionTicks <= caption.EndPositionTicks) {
                    currentSubtitlesElement.innerHTML = caption.Text;
                    currentSubtitlesElement.classList.remove('hide');
                    return;
                }
            }

            currentSubtitlesElement.innerHTML = '';
            currentSubtitlesElement.classList.add('hide');
        }

        function getMediaStreamAudioTracks(mediaSource) {

            return mediaSource.MediaStreams.filter(function (s) {
                return s.Type === 'Audio';
            });
        }

        function getMediaStreamTextTracks(mediaSource) {

            return mediaSource.MediaStreams.filter(function (s) {
                return s.Type === 'Subtitle' && s.DeliveryMethod === 'External';
            });
        }

        function setCurrentTrackElement(streamIndex) {

            console.log('Setting new text track index to: ' + streamIndex);

            var mediaStreamTextTracks = getMediaStreamTextTracks(currentPlayOptions.mediaSource);

            var track = streamIndex === -1 ? null : mediaStreamTextTracks.filter(function (t) {
                return t.Index === streamIndex;
            })[0];

            if (enableNativeTrackSupport(track)) {

                setTrackForCustomDisplay(mediaElement, null);
            } else {
                setTrackForCustomDisplay(mediaElement, track);

                // null these out to disable the player's native display (handled below)
                streamIndex = -1;
                track = null;
            }

            var expectedId = 'textTrack' + streamIndex;
            var trackIndex = streamIndex === -1 || !track ? -1 : mediaStreamTextTracks.indexOf(track);
            var modes = ['disabled', 'showing', 'hidden'];

            var allTracks = mediaElement.textTracks; // get list of tracks
            for (var i = 0; i < allTracks.length; i++) {

                var currentTrack = allTracks[i];

                console.log('currentTrack id: ' + currentTrack.id);

                var mode;

                console.log('expectedId: ' + expectedId + '--currentTrack.Id:' + currentTrack.id);

                // IE doesn't support track id
                if (browser.msie || browser.edge) {
                    if (trackIndex === i) {
                        mode = 1; // show this track
                    } else {
                        mode = 0; // hide all other tracks
                    }
                } else {

                    if (currentTrack.label.indexOf('manualTrack') !== -1) {
                        continue;
                    }
                    if (currentTrack.id === expectedId) {
                        mode = 1; // show this track
                    } else {
                        mode = 0; // hide all other tracks
                    }
                }

                console.log('Setting track ' + i + ' mode to: ' + mode);

                // Safari uses integers for the mode property
                // http://www.jwplayer.com/html5/scripting/
                // edit: not anymore
                var useNumericMode = false;

                if (!isNaN(currentTrack.mode)) {
                    //useNumericMode = true;
                }

                if (useNumericMode) {
                    currentTrack.mode = mode;
                } else {
                    currentTrack.mode = modes[mode];
                }
            }
        }

        function updateTextStreamUrls(startPositionTicks) {

            if (!supportsTextTracks()) {
                return;
            }

            var allTracks = mediaElement.textTracks; // get list of tracks
            var i;
            var track;

            for (i = 0; i < allTracks.length; i++) {

                track = allTracks[i];

                // This throws an error in IE, but is fine in chrome
                // In IE it's not necessary anyway because changing the src seems to be enough
                try {
                    while (track.cues.length) {
                        track.removeCue(track.cues[0]);
                    }
                } catch (e) {
                    console.log('Error removing cue from textTrack');
                }
            }

            var tracks = mediaElement.querySelectorAll('track');
            for (i = 0; i < tracks.length; i++) {

                track = tracks[i];

                track.src = replaceQueryString(track.src, 'startPositionTicks', startPositionTicks);
            }
        }

        function zoomIn(elem, iterations) {
            var keyframes = [
                { transform: 'scale3d(.2, .2, .2)  ', opacity: '.6', offset: 0 },
                { transform: 'none', opacity: '1', offset: 1 }
            ];

            var timing = { duration: 240, iterations: iterations };
            return elem.animate(keyframes, timing);
        }

        function createMediaElement(options) {

            if (browser.tv || browser.noAnimation || browser.iOS) {
                // too slow
                // also on iOS, the backdrop image doesn't look right
                options.backdropUrl = null;
            }
            return new Promise(function (resolve, reject) {

                var dlg = document.querySelector('.videoPlayerContainer');

                if (!dlg) {

                    require(['css!' + pluginManager.mapPath(self, 'style.css')], function () {

                        loading.show();

                        var dlg = document.createElement('div');

                        dlg.classList.add('videoPlayerContainer');

                        if (options.backdropUrl) {

                            dlg.classList.add('videoPlayerContainer-withBackdrop');
                            dlg.style.backgroundImage = "url('" + options.backdropUrl + "')";
                        }

                        if (options.fullscreen) {
                            dlg.classList.add('videoPlayerContainer-onTop');
                        }

                        // playsinline new for iOS 10
                        // https://developer.apple.com/library/content/releasenotes/General/WhatsNewInSafari/Articles/Safari_10_0.html

                        var html = '';
                        // Can't autoplay in these browsers so we need to use the full controls, at least until playback starts
                        if (!appHost.supports('htmlvideoautoplay')) {
                            html += '<video class="htmlvideoplayer" preload="metadata" autoplay="autoplay" controls="controls" webkit-playsinline playsinline>';
                        } else {

                            // Chrome 35 won't play with preload none
                            html += '<video class="htmlvideoplayer" preload="metadata" autoplay="autoplay" webkit-playsinline playsinline>';
                        }

                        html += '</video>';

                        dlg.innerHTML = html;
                        var videoElement = dlg.querySelector('video');

                        videoElement.volume = getSavedVolume();
                        videoElement.addEventListener('timeupdate', onTimeUpdate);
                        videoElement.addEventListener('ended', onEnded);
                        videoElement.addEventListener('volumechange', onVolumeChange);
                        videoElement.addEventListener('pause', onPause);
                        videoElement.addEventListener('playing', onPlaying);
                        videoElement.addEventListener('error', onError);
                        videoElement.addEventListener('click', onClick);
                        videoElement.addEventListener('dblclick', onDblClick);

                        document.body.insertBefore(dlg, document.body.firstChild);
                        videoDialog = dlg;
                        mediaElement = videoElement;

                        // don't animate on smart tv's, too slow
                        if (options.fullscreen && dlg.animate && !browser.slow) {
                            zoomIn(dlg, 1).onfinish = function () {
                                resolve(videoElement);
                            };
                        } else {
                            resolve(videoElement);
                        }

                    });

                } else {

                    if (options.backdropUrl) {

                        dlg.classList.add('videoPlayerContainer-withBackdrop');
                        dlg.style.backgroundImage = "url('" + options.backdropUrl + "')";
                    }

                    resolve(dlg.querySelector('video'));
                }
            });
        }
    };
});