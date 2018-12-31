define(['browser', 'require', 'events', 'apphost', 'loading', 'dom', 'playbackManager', 'appRouter', 'appSettings', 'connectionManager', 'htmlMediaHelper', 'itemHelper'], function (browser, require, events, appHost, loading, dom, playbackManager, appRouter, appSettings, connectionManager, htmlMediaHelper, itemHelper) {
    "use strict";

    var mediaManager;

    function tryRemoveElement(elem) {
        var parentNode = elem.parentNode;
        if (parentNode) {

            // Seeing crashes in edge webview
            try {
                parentNode.removeChild(elem);
            } catch (err) {
                console.log('Error removing dialog element: ' + err);
            }
        }
    }

    var _supportsTextTracks;
    function supportsTextTracks() {

        if (_supportsTextTracks == null) {
            _supportsTextTracks = document.createElement('video').textTracks != null;
        }

        // For now, until ready
        return _supportsTextTracks;
    }

    function enableNativeTrackSupport(currentSrc, track) {

        if (track) {
            if (track.DeliveryMethod === 'Embed') {
                return true;
            }
        }

        if (browser.firefox) {
            if ((currentSrc || '').toLowerCase().indexOf('.m3u8') !== -1) {
                return false;
            }
        }

        // subs getting blocked due to CORS
        if (browser.chromecast) {
            if ((currentSrc || '').toLowerCase().indexOf('.m3u8') !== -1) {
                return false;
            }
        }

        if (browser.ps4) {
            return false;
        }

        if (browser.web0s) {
            return false;
        }

        // Edge is randomly not rendering subtitles
        if (browser.edge) {
            return false;
        }

        if (browser.iOS) {
            // works in the browser but not the native app
            if ((browser.iosVersion || 10) < 10) {
                return false;
            }
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

    function requireHlsPlayer(callback) {
        require(['hlsjs'], function (hls) {
            window.Hls = hls;
            callback();
        });
    }

    function getMediaStreamAudioTracks(mediaSource) {

        return mediaSource.MediaStreams.filter(function (s) {
            return s.Type === 'Audio';
        });
    }

    function getMediaStreamTextTracks(mediaSource) {

        return mediaSource.MediaStreams.filter(function (s) {
            return s.Type === 'Subtitle';
        });
    }

    function zoomIn(elem) {

        return new Promise(function (resolve, reject) {

            var duration = 240;
            elem.style.animation = 'htmlvideoplayer-zoomin ' + duration + 'ms ease-in normal';
            dom.addEventListener(elem, dom.whichAnimationEvent(), resolve, {
                once: true
            });
        });
    }

    function normalizeTrackEventText(text) {
        return text.replace(/\\N/gi, '\n');
    }

    function setTracks(elem, tracks, item, mediaSource) {

        elem.innerHTML = getTracksHtml(tracks, item, mediaSource);
    }

    function getTextTrackUrl(track, item, format) {

        if (itemHelper.isLocalItem(item) && track.Path) {
            return track.Path;
        }

        var url = playbackManager.getSubtitleUrl(track, item.ServerId);
        if (format) {
            url = url.replace('.vtt', format);
        }

        return url;
    }

    function getTracksHtml(tracks, item, mediaSource) {
        return tracks.map(function (t) {

            if (t.DeliveryMethod !== 'External') {
                return '';
            }

            var defaultAttribute = mediaSource.DefaultSubtitleStreamIndex === t.Index ? ' default' : '';

            var language = t.Language || 'und';
            var label = t.Language || 'und';
            return '<track id="textTrack' + t.Index + '" label="' + label + '" kind="subtitles" src="' + getTextTrackUrl(t, item) + '" srclang="' + language + '"' + defaultAttribute + '></track>';

        }).join('');
    }

    function getDefaultProfile() {

        return new Promise(function (resolve, reject) {

            require(['browserdeviceprofile'], function (profileBuilder) {

                resolve(profileBuilder({}));
            });
        });
    }

    function HtmlVideoPlayer() {

        if (browser.edgeUwp) {
            this.name = 'Windows Video Player';
        } else {
            this.name = 'Html Video Player';
        }

        this.type = 'mediaplayer';
        this.id = 'htmlvideoplayer';

        // Let any players created by plugins take priority
        this.priority = 1;

        var videoDialog;

        var winJsPlaybackItem;

        var subtitleTrackIndexToSetOnPlaying;
        var audioTrackIndexToSetOnPlaying;

        var lastCustomTrackMs = 0;
        var currentClock;
        var currentAssRenderer;
        var customTrackIndex = -1;

        var videoSubtitlesElem;
        var currentTrackEvents;

        var self = this;

        self.currentSrc = function () {
            return self._currentSrc;
        };

        function updateVideoUrl(streamInfo) {

            var isHls = streamInfo.url.toLowerCase().indexOf('.m3u8') !== -1;

            var mediaSource = streamInfo.mediaSource;
            var item = streamInfo.item;

            // Huge hack alert. Safari doesn't seem to like if the segments aren't available right away when playback starts
            // This will start the transcoding process before actually feeding the video url into the player
            // Edit: Also seeing stalls from hls.js
            if (mediaSource && item && !mediaSource.RunTimeTicks && isHls && streamInfo.playMethod === 'Transcode' && (browser.iOS || browser.osx)) {

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

            if (browser.msie) {
                if (options.playMethod === 'Transcode' && !window.MediaSource) {
                    alert('Playback of this content is not supported in Internet Explorer. For a better experience, try a modern browser such as Microsoft Edge, Google Chrome, Firefox or Opera.');
                    return Promise.reject();
                }
            }

            self._started = false;
            self._timeUpdated = false;

            self._currentTime = null;

            return createMediaElement(options).then(function (elem) {

                return updateVideoUrl(options, options.mediaSource).then(function () {
                    return setCurrentSrc(elem, options);
                });
            });
        };

        function setSrcWithFlvJs(instance, elem, options, url) {

            return new Promise(function (resolve, reject) {

                require(['flvjs'], function (flvjs) {

                    var flvPlayer = flvjs.createPlayer({
                        type: 'flv',
                        url: url
                    },
                        {
                            seekType: 'range',
                            lazyLoad: false
                        });

                    flvPlayer.attachMediaElement(elem);
                    flvPlayer.load();

                    flvPlayer.play().then(resolve, reject);
                    instance._flvPlayer = flvPlayer;

                    // This is needed in setCurrentTrackElement
                    self._currentSrc = url;
                });
            });
        }

        function setSrcWithHlsJs(instance, elem, options, url) {

            return new Promise(function (resolve, reject) {

                requireHlsPlayer(function () {

                    var hls = new Hls({
                        manifestLoadingTimeOut: 20000
                        //appendErrorMaxRetry: 6,
                        //debug: true
                    });
                    hls.loadSource(url);
                    hls.attachMedia(elem);

                    htmlMediaHelper.bindEventsToHlsPlayer(self, hls, elem, onError, resolve, reject);

                    self._hlsPlayer = hls;

                    // This is needed in setCurrentTrackElement
                    self._currentSrc = url;
                });
            });
        }

        function onShakaError(event) {

            var error = event.detail;
            console.error('Error code', error.code, 'object', error);
        }

        function setSrcWithShakaPlayer(instance, elem, options, url) {

            return new Promise(function (resolve, reject) {

                require(['shaka'], function () {

                    var player = new shaka.Player(elem);

                    //player.configure({
                    //    abr: {
                    //        enabled: false
                    //    },
                    //    streaming: {

                    //        failureCallback: function () {
                    //            alert(2);
                    //        }
                    //    }
                    //});

                    //shaka.log.setLevel(6);

                    // Listen for error events.
                    player.addEventListener('error', onShakaError);

                    // Try to load a manifest.
                    // This is an asynchronous process.
                    player.load(url).then(function () {

                        // This runs if the asynchronous load is successful.
                        resolve();

                    }, reject);

                    self._shakaPlayer = player;

                    // This is needed in setCurrentTrackElement
                    self._currentSrc = url;
                });
            });
        }

        function setCurrentSrcChromecast(instance, elem, options, url) {

            elem.autoplay = true;

            var lrd = new cast.receiver.MediaManager.LoadRequestData();
            lrd.currentTime = (options.playerStartPositionTicks || 0) / 10000000;
            lrd.autoplay = true;
            lrd.media = new cast.receiver.media.MediaInformation();

            lrd.media.contentId = url;
            lrd.media.contentType = options.mimeType;
            lrd.media.streamType = cast.receiver.media.StreamType.OTHER;
            lrd.media.customData = options;

            console.log('loading media url into mediaManager');

            try {
                mediaManager.load(lrd);
                // This is needed in setCurrentTrackElement
                self._currentSrc = url;

                return Promise.resolve();
            } catch (err) {

                console.log('mediaManager error: ' + err);
                return Promise.reject();
            }
        }

        // Adapted from : https://github.com/googlecast/CastReferencePlayer/blob/master/player.js
        function onMediaManagerLoadMedia(event) {

            if (self._castPlayer) {
                self._castPlayer.unload();    // Must unload before starting again.
            }
            self._castPlayer = null;

            var data = event.data;

            var media = event.data.media || {};
            var url = media.contentId;
            var contentType = media.contentType.toLowerCase();
            var options = media.customData;

            var protocol;
            var ext = 'm3u8';

            var mediaElement = self._mediaElement;

            var host = new cast.player.api.Host({
                'url': url,
                'mediaElement': mediaElement
            });

            if (ext === 'm3u8' ||
                contentType === 'application/x-mpegurl' ||
                contentType === 'application/vnd.apple.mpegurl') {
                protocol = cast.player.api.CreateHlsStreamingProtocol(host);
            } else if (ext === 'mpd' ||
                contentType === 'application/dash+xml') {
                protocol = cast.player.api.CreateDashStreamingProtocol(host);
            } else if (url.indexOf('.ism') > -1 ||
                contentType === 'application/vnd.ms-sstr+xml') {
                protocol = cast.player.api.CreateSmoothStreamingProtocol(host);
            }

            console.log('loading playback url: ' + url);
            console.log('contentType: ' + contentType);

            host.onError = function (errorCode) {
                console.log("Fatal Error - " + errorCode);
            };

            mediaElement.autoplay = false;

            self._castPlayer = new cast.player.api.Player(host);

            self._castPlayer.load(protocol, data.currentTime || 0);

            self._castPlayer.playWhenHaveEnoughData();
        }

        function initMediaManager() {

            mediaManager.defaultOnLoad = mediaManager.onLoad.bind(mediaManager);
            mediaManager.onLoad = onMediaManagerLoadMedia.bind(self);

            //mediaManager.defaultOnPlay = mediaManager.onPlay.bind(mediaManager);
            //mediaManager.onPlay = function (event) {
            //    // TODO ???
            //    mediaManager.defaultOnPlay(event);
            //};

            mediaManager.defaultOnStop = mediaManager.onStop.bind(mediaManager);
            mediaManager.onStop = function (event) {
                playbackManager.stop();
                mediaManager.defaultOnStop(event);
            };
        }

        function setCurrentSrc(elem, options) {

            elem.removeEventListener('error', onError);

            var val = options.url;
            console.log('playing url: ' + val);

            // Convert to seconds
            var seconds = (options.playerStartPositionTicks || 0) / 10000000;
            if (seconds) {
                val += '#t=' + seconds;
            }

            htmlMediaHelper.destroyHlsPlayer(self);
            htmlMediaHelper.destroyFlvPlayer(self);
            htmlMediaHelper.destroyCastPlayer(self);

            var tracks = getMediaStreamTextTracks(options.mediaSource);

            subtitleTrackIndexToSetOnPlaying = options.mediaSource.DefaultSubtitleStreamIndex == null ? -1 : options.mediaSource.DefaultSubtitleStreamIndex;
            if (subtitleTrackIndexToSetOnPlaying != null && subtitleTrackIndexToSetOnPlaying >= 0) {
                var initialSubtitleStream = options.mediaSource.MediaStreams[subtitleTrackIndexToSetOnPlaying];
                if (!initialSubtitleStream || initialSubtitleStream.DeliveryMethod === 'Encode') {
                    subtitleTrackIndexToSetOnPlaying = -1;
                }
            }

            audioTrackIndexToSetOnPlaying = options.playMethod === 'Transcode' ? null : options.mediaSource.DefaultAudioStreamIndex;

            self._currentPlayOptions = options;

            var crossOrigin = htmlMediaHelper.getCrossOriginValue(options.mediaSource);
            if (crossOrigin) {
                elem.crossOrigin = crossOrigin;
            }

            /*if (htmlMediaHelper.enableHlsShakaPlayer(options.item, options.mediaSource, 'Video') && val.indexOf('.m3u8') !== -1) {

                setTracks(elem, tracks, options.item, options.mediaSource);

                return setSrcWithShakaPlayer(self, elem, options, val);

            } else*/ if (browser.chromecast && val.indexOf('.m3u8') !== -1 && options.mediaSource.RunTimeTicks) {

                setTracks(elem, tracks, options.item, options.mediaSource);
                return setCurrentSrcChromecast(self, elem, options, val);
            }

            else if (htmlMediaHelper.enableHlsJsPlayer(options.mediaSource.RunTimeTicks, 'Video') && val.indexOf('.m3u8') !== -1) {

                setTracks(elem, tracks, options.item, options.mediaSource);

                return setSrcWithHlsJs(self, elem, options, val);

            } else if (options.playMethod !== 'Transcode' && options.mediaSource.Container === 'flv') {

                setTracks(elem, tracks, options.item, options.mediaSource);

                return setSrcWithFlvJs(self, elem, options, val);

            } else {

                elem.autoplay = true;

                return htmlMediaHelper.applySrc(elem, val, options).then(function () {

                    setTracks(elem, tracks, options.item, options.mediaSource);

                    self._currentSrc = val;

                    return htmlMediaHelper.playWithPromise(elem, onError);
                });
            }
        }

        self.setSubtitleStreamIndex = function (index) {

            setCurrentTrackElement(index);
        };

        function isAudioStreamSupported(stream, deviceProfile) {

            var codec = (stream.Codec || '').toLowerCase();

            if (!codec) {
                return true;
            }

            if (!deviceProfile) {
                // This should never happen
                return true;
            }

            var profiles = deviceProfile.DirectPlayProfiles || [];

            return profiles.filter(function (p) {


                if (p.Type === 'Video') {

                    if (!p.AudioCodec) {
                        return true;
                    }

                    return p.AudioCodec.toLowerCase().indexOf(codec) !== -1;
                }

                return false;

            }).length > 0;
        }

        function getSupportedAudioStreams() {
            var profile = self._lastProfile;

            return getMediaStreamAudioTracks(self._currentPlayOptions.mediaSource).filter(function (stream) {
                return isAudioStreamSupported(stream, profile);
            });
        }

        self.setAudioStreamIndex = function (index) {

            var streams = getSupportedAudioStreams();

            if (streams.length < 2) {
                // If there's only one supported stream then trust that the player will handle it on it's own
                return;
            }

            var audioIndex = -1;
            var i, length, stream;

            for (i = 0, length = streams.length; i < length; i++) {
                stream = streams[i];

                audioIndex++;

                if (stream.Index === index) {
                    break;
                }
            }

            if (audioIndex === -1) {
                return;
            }

            var elem = self._mediaElement;
            if (!elem) {
                return;
            }

            // https://msdn.microsoft.com/en-us/library/hh772507(v=vs.85).aspx

            var elemAudioTracks = elem.audioTracks || [];
            console.log('found ' + elemAudioTracks.length + ' audio tracks');

            for (i = 0, length = elemAudioTracks.length; i < length; i++) {

                if (audioIndex === i) {
                    console.log('setting audio track ' + i + ' to enabled');
                    elemAudioTracks[i].enabled = true;
                } else {
                    console.log('setting audio track ' + i + ' to disabled');
                    elemAudioTracks[i].enabled = false;
                }
            }

            setTimeout(function () {
                elem.currentTime = elem.currentTime;
            }, 100);
        };

        self.stop = function (destroyPlayer) {

            var elem = self._mediaElement;
            var src = self._currentSrc;

            if (elem) {

                if (src) {
                    elem.pause();
                }

                htmlMediaHelper.onEndedInternal(self, elem, onError);

                if (destroyPlayer) {
                    self.destroy();
                }
            }

            destroyCustomTrack(elem);

            return Promise.resolve();
        };

        self.destroy = function () {

            htmlMediaHelper.destroyHlsPlayer(self);
            htmlMediaHelper.destroyFlvPlayer(self);

            appRouter.setTransparency('none');

            var videoElement = self._mediaElement;

            if (videoElement) {

                self._mediaElement = null;

                destroyCustomTrack(videoElement);

                videoElement.removeEventListener('timeupdate', onTimeUpdate);
                videoElement.removeEventListener('ended', onEnded);
                videoElement.removeEventListener('volumechange', onVolumeChange);
                videoElement.removeEventListener('pause', onPause);
                videoElement.removeEventListener('playing', onPlaying);
                videoElement.removeEventListener('play', onPlay);
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

        function onEnded() {

            destroyCustomTrack(this);
            htmlMediaHelper.onEndedInternal(self, this, onError);
        }

        function onTimeUpdate(e) {

            // Get the player position + the transcoding offset
            var time = this.currentTime;

            if (time && !self._timeUpdated) {
                self._timeUpdated = true;
                ensureValidVideo(this);
            }

            self._currentTime = time;

            var currentPlayOptions = self._currentPlayOptions;
            // Not sure yet how this is coming up null since we never null it out, but it is causing app crashes
            if (currentPlayOptions) {
                var timeMs = time * 1000;
                timeMs += ((currentPlayOptions.transcodingOffsetTicks || 0) / 10000);
                updateSubtitleText(timeMs);
            }

            events.trigger(self, 'timeupdate');
        }

        function onVolumeChange() {

            htmlMediaHelper.saveVolume(this.volume);
            events.trigger(self, 'volumechange');
        }

        function onNavigatedToOsd() {

            var dlg = videoDialog;
            if (dlg) {
                dlg.classList.remove('videoPlayerContainer-withBackdrop');
                dlg.classList.remove('videoPlayerContainer-onTop');

                onStartedAndNavigatedToOsd();
            }
        }
        function onStartedAndNavigatedToOsd() {

            // If this causes a failure during navigation we end up in an awkward UI state
            setCurrentTrackElement(subtitleTrackIndexToSetOnPlaying);

            if (audioTrackIndexToSetOnPlaying != null && self.canSetAudioStreamIndex()) {
                self.setAudioStreamIndex(audioTrackIndexToSetOnPlaying);
            }
        }

        function onPlaying(e) {

            if (!self._started) {
                self._started = true;
                this.removeAttribute('controls');

                loading.hide();

                htmlMediaHelper.seekOnPlaybackStart(self, e.target, self._currentPlayOptions.playerStartPositionTicks);

                if (self._currentPlayOptions.fullscreen) {

                    appRouter.showVideoOsd().then(onNavigatedToOsd);

                } else {
                    appRouter.setTransparency('backdrop');
                    videoDialog.classList.remove('videoPlayerContainer-withBackdrop');
                    videoDialog.classList.remove('videoPlayerContainer-onTop');

                    onStartedAndNavigatedToOsd();
                }
            }
            events.trigger(self, 'playing');
        }

        function onPlay(e) {

            events.trigger(self, 'unpause');
        }

        function ensureValidVideo(elem) {
            if (elem !== self._mediaElement) {
                return;
            }

            if (elem.videoWidth === 0 && elem.videoHeight === 0) {

                var mediaSource = (self._currentPlayOptions || {}).mediaSource;

                // Only trigger this if there is media info
                // Avoid triggering in situations where it might not actually have a video stream (audio only live tv channel)
                if (!mediaSource || mediaSource.RunTimeTicks) {
                    htmlMediaHelper.onErrorInternal(self, 'mediadecodeerror');
                    return;
                }
            }

            //if (elem.audioTracks && !elem.audioTracks.length) {
            //    htmlMediaHelper.onErrorInternal(self, 'mediadecodeerror');
            //}
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
            var errorMessage = this.error ? (this.error.message || '') : '';
            console.log('Media element error: ' + errorCode.toString() + ' ' + errorMessage);

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
                    if (self._hlsPlayer) {
                        htmlMediaHelper.handleHlsJsMediaError(self);
                        return;
                    } else {
                        type = 'mediadecodeerror';
                    }
                    break;
                case 4:
                    // MEDIA_ERR_SRC_NOT_SUPPORTED
                    type = 'medianotsupported';
                    break;
                default:
                    // seeing cases where Edge is firing error events with no error code
                    // example is start playing something, then immediately change src to something else
                    return;
            }

            htmlMediaHelper.onErrorInternal(self, type);
        }

        function destroyCustomTrack(videoElement) {

            if (self._resizeObserver) {
                self._resizeObserver.disconnect();
                self._resizeObserver = null;
            }

            if (videoSubtitlesElem) {
                var subtitlesContainer = videoSubtitlesElem.parentNode;
                if (subtitlesContainer) {
                    tryRemoveElement(subtitlesContainer);
                }
                videoSubtitlesElem = null;
            }

            currentTrackEvents = null;

            if (videoElement) {
                var allTracks = videoElement.textTracks || []; // get list of tracks
                for (var i = 0; i < allTracks.length; i++) {

                    var currentTrack = allTracks[i];

                    if (currentTrack.label.indexOf('manualTrack') !== -1) {
                        currentTrack.mode = 'disabled';
                    }
                }
            }

            customTrackIndex = -1;
            currentClock = null;
            self._currentAspectRatio = null;

            var renderer = currentAssRenderer;
            if (renderer) {
                renderer.setEnabled(false);
            }
            currentAssRenderer = null;
        }

        self.destroyCustomTrack = destroyCustomTrack;

        function fetchSubtitlesUwp(track, item) {

            return Windows.Storage.StorageFile.getFileFromPathAsync(track.Path).then(function (storageFile) {

                return Windows.Storage.FileIO.readTextAsync(storageFile).then(function (text) {
                    return JSON.parse(text);
                });
            });

        }

        function fetchSubtitles(track, item) {

            if (window.Windows && itemHelper.isLocalItem(item)) {
                return fetchSubtitlesUwp(track, item);
            }

            return new Promise(function (resolve, reject) {

                var xhr = new XMLHttpRequest();

                var url = getTextTrackUrl(track, item, '.js');

                xhr.open('GET', url, true);

                xhr.onload = function (e) {
                    resolve(JSON.parse(this.response));
                };

                xhr.onerror = reject;

                xhr.send();
            });
        }

        function setTrackForCustomDisplay(videoElement, track) {

            if (!track) {
                destroyCustomTrack(videoElement);
                return;
            }

            // if already playing thids track, skip
            if (customTrackIndex === track.Index) {
                return;
            }

            var item = self._currentPlayOptions.item;

            destroyCustomTrack(videoElement);
            customTrackIndex = track.Index;
            renderTracksEvents(videoElement, track, item);
            lastCustomTrackMs = 0;
        }

        function renderWithLibjass(videoElement, track, item) {

            var rendererSettings = {};

            if (browser.ps4) {
                // Text outlines are not rendering very well
                rendererSettings.enableSvg = false;
            }
            else if (browser.edge || browser.msie) {
                // svg not rendering at all
                rendererSettings.enableSvg = false;
            }

            // probably safer to just disable everywhere
            rendererSettings.enableSvg = false;

            require(['libjass', 'ResizeObserver'], function (libjass, ResizeObserver) {

                libjass.ASS.fromUrl(getTextTrackUrl(track, item)).then(function (ass) {

                    var clock = new libjass.renderers.ManualClock();
                    currentClock = clock;

                    // Create a DefaultRenderer using the video element and the ASS object
                    var renderer = new libjass.renderers.WebRenderer(ass, clock, videoElement.parentNode, rendererSettings);

                    currentAssRenderer = renderer;

                    renderer.addEventListener("ready", function () {
                        try {
                            renderer.resize(videoElement.offsetWidth, videoElement.offsetHeight, 0, 0);

                            if (!self._resizeObserver) {
                                self._resizeObserver = new ResizeObserver(onVideoResize, {});
                                self._resizeObserver.observe(videoElement);
                            }
                            //clock.pause();
                        } catch (ex) {
                            //alert(ex);
                        }
                    });
                }, function () {
                    htmlMediaHelper.onErrorInternal(self, 'mediadecodeerror');
                });
            });
        }

        function onVideoResize() {

            if (browser.iOS) {

                // with wkwebview, the new sizes will be delayed for about 500ms
                setTimeout(resetVideoRendererSize, 500);
            } else {
                resetVideoRendererSize();
            }
        }

        function resetVideoRendererSize() {
            var renderer = currentAssRenderer;
            if (renderer) {
                var videoElement = self._mediaElement;
                var width = videoElement.offsetWidth;
                var height = videoElement.offsetHeight;
                console.log('videoElement resized: ' + width + 'x' + height);
                renderer.resize(width, height, 0, 0);
            }
        }

        function requiresCustomSubtitlesElement() {

            // after a system update, ps4 isn't showing anything when creating a track element dynamically
            // going to have to do it ourselves
            if (browser.ps4) {
                return true;
            }

            // This is unfortunate, but we're unable to remove the textTrack that gets added via addTextTrack
            if (browser.firefox || browser.web0s) {
                return true;
            }

            if (browser.edge) {
                return true;
            }

            if (browser.iOS) {
                var userAgent = navigator.userAgent.toLowerCase();
                // works in the browser but not the native app
                if ((userAgent.indexOf('os 9') !== -1 || userAgent.indexOf('os 8') !== -1) && userAgent.indexOf('safari') === -1) {
                    return true;
                }
            }

            return false;
        }

        function renderSubtitlesWithCustomElement(videoElement, track, item) {

            fetchSubtitles(track, item).then(function (data) {
                if (!videoSubtitlesElem) {
                    var subtitlesContainer = document.createElement('div');
                    subtitlesContainer.classList.add('videoSubtitles');
                    subtitlesContainer.innerHTML = '<div class="videoSubtitlesInner"></div>';
                    videoSubtitlesElem = subtitlesContainer.querySelector('.videoSubtitlesInner');
                    setSubtitleAppearance(subtitlesContainer, videoSubtitlesElem);
                    videoElement.parentNode.appendChild(subtitlesContainer);
                    currentTrackEvents = data.TrackEvents;
                }
            });
        }

        function setSubtitleAppearance(elem, innerElem) {

            require(['userSettings', 'subtitleAppearanceHelper'], function (userSettings, subtitleAppearanceHelper) {

                subtitleAppearanceHelper.applyStyles({

                    text: innerElem,
                    window: elem

                }, userSettings.getSubtitleAppearanceSettings());
            });
        }

        function getCueCss(appearance, selector) {

            var html = selector + '::cue {';

            html += appearance.text.map(function (s) {

                return s.name + ':' + s.value + '!important;';

            }).join('');

            html += '}';

            return html;
        }

        function setCueAppearance() {

            require(['userSettings', 'subtitleAppearanceHelper'], function (userSettings, subtitleAppearanceHelper) {

                var elementId = self.id + '-cuestyle';

                var styleElem = document.querySelector('#' + elementId);
                if (!styleElem) {
                    styleElem = document.createElement('style');
                    styleElem.id = elementId;
                    styleElem.type = 'text/css';
                    document.getElementsByTagName('head')[0].appendChild(styleElem);
                }

                styleElem.innerHTML = getCueCss(subtitleAppearanceHelper.getStyles(userSettings.getSubtitleAppearanceSettings(), true), '.htmlvideoplayer');
            });
        }

        function renderTracksEvents(videoElement, track, item) {

            if (!itemHelper.isLocalItem(item) || track.IsExternal) {
                var format = (track.Codec || '').toLowerCase();
                if (format === 'ssa' || format === 'ass') {
                    // libjass is needed here
                    renderWithLibjass(videoElement, track, item);
                    return;
                }

                if (requiresCustomSubtitlesElement()) {
                    renderSubtitlesWithCustomElement(videoElement, track, item);
                    return;
                }
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
                fetchSubtitles(track, item).then(function (data) {

                    // show in ui
                    console.log('downloaded ' + data.TrackEvents.length + ' track events');
                    // add some cues to show the text
                    // in safari, the cues need to be added before setting the track mode to showing
                    data.TrackEvents.forEach(function (trackEvent) {

                        var trackCueObject = window.VTTCue || window.TextTrackCue;
                        var cue = new trackCueObject(trackEvent.StartPositionTicks / 10000000, trackEvent.EndPositionTicks / 10000000, normalizeTrackEventText(trackEvent.Text));

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
                try {
                    clock.seek(timeMs / 1000);
                } catch (err) {
                    console.log('Error in libjass: ' + err);
                }
                return;
            }

            var trackEvents = currentTrackEvents;
            var subtitleTextElement = videoSubtitlesElem;

            if (trackEvents && subtitleTextElement) {
                var ticks = timeMs * 10000;
                var selectedTrackEvent;
                for (var i = 0; i < trackEvents.length; i++) {

                    var currentTrackEvent = trackEvents[i];
                    if (currentTrackEvent.StartPositionTicks <= ticks && currentTrackEvent.EndPositionTicks >= ticks) {
                        selectedTrackEvent = currentTrackEvent;
                        break;
                    }
                }

                if (selectedTrackEvent && selectedTrackEvent.Text) {

                    subtitleTextElement.innerHTML = normalizeTrackEventText(selectedTrackEvent.Text);
                    subtitleTextElement.classList.remove('hide');

                } else {
                    subtitleTextElement.classList.add('hide');
                }
            }
        }

        function setCurrentTrackElement(streamIndex) {

            console.log('Setting new text track index to: ' + streamIndex);

            var mediaStreamTextTracks = getMediaStreamTextTracks(self._currentPlayOptions.mediaSource);

            var track = streamIndex === -1 ? null : mediaStreamTextTracks.filter(function (t) {
                return t.Index === streamIndex;
            })[0];

            if (enableNativeTrackSupport(self._currentSrc, track)) {

                setTrackForCustomDisplay(self._mediaElement, null);

                if (streamIndex !== -1) {
                    setCueAppearance();
                }

            } else {
                setTrackForCustomDisplay(self._mediaElement, track);

                // null these out to disable the player's native display (handled below)
                streamIndex = -1;
                track = null;
            }

            var expectedId = 'textTrack' + streamIndex;
            var trackIndex = streamIndex === -1 || !track ? -1 : mediaStreamTextTracks.indexOf(track);
            var modes = ['disabled', 'showing', 'hidden'];

            var allTracks = self._mediaElement.textTracks; // get list of tracks
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

                currentTrack.mode = modes[mode];
            }
        }

        function updateTextStreamUrls(startPositionTicks) {

            if (!supportsTextTracks()) {
                return;
            }

            var allTracks = self._mediaElement.textTracks; // get list of tracks
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

            var tracks = self._mediaElement.querySelectorAll('track');
            for (i = 0; i < tracks.length; i++) {

                track = tracks[i];

                track.src = replaceQueryString(track.src, 'startPositionTicks', startPositionTicks);
            }
        }

        function createMediaElement(options) {

            if (browser.tv || browser.iOS || browser.mobile) {
                // too slow
                // also on iOS, the backdrop image doesn't look right
                // on android mobile, it works, but can be slow to have the video surface fully cover the backdrop
                options.backdropUrl = null;
            }
            return new Promise(function (resolve, reject) {

                var dlg = document.querySelector('.videoPlayerContainer');

                if (!dlg) {

                    require(['css!./style'], function () {

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

                        var cssClass = 'htmlvideoplayer';

                        if (!browser.chromecast) {
                            cssClass += ' htmlvideoplayer-moveupsubtitles';
                        }

                        // Can't autoplay in these browsers so we need to use the full controls, at least until playback starts
                        if (!appHost.supports('htmlvideoautoplay')) {
                            html += '<video class="' + cssClass + '" preload="metadata" autoplay="autoplay" controls="controls" webkit-playsinline playsinline>';
                        } else {

                            // Chrome 35 won't play with preload none
                            html += '<video class="' + cssClass + '" preload="metadata" autoplay="autoplay" webkit-playsinline playsinline>';
                        }

                        html += '</video>';

                        dlg.innerHTML = html;
                        var videoElement = dlg.querySelector('video');

                        videoElement.volume = htmlMediaHelper.getSavedVolume();
                        videoElement.addEventListener('timeupdate', onTimeUpdate);
                        videoElement.addEventListener('ended', onEnded);
                        videoElement.addEventListener('volumechange', onVolumeChange);
                        videoElement.addEventListener('pause', onPause);
                        videoElement.addEventListener('playing', onPlaying);
                        videoElement.addEventListener('play', onPlay);
                        videoElement.addEventListener('click', onClick);
                        videoElement.addEventListener('dblclick', onDblClick);

                        document.body.insertBefore(dlg, document.body.firstChild);
                        videoDialog = dlg;
                        self._mediaElement = videoElement;

                        if (mediaManager) {

                            if (!mediaManager.embyInit) {
                                initMediaManager();
                                mediaManager.embyInit = true;
                            }
                            mediaManager.setMediaElement(videoElement);
                        }

                        // don't animate on smart tv's, too slow
                        if (options.fullscreen && browser.supportsCssAnimation() && !browser.slow) {
                            zoomIn(dlg).then(function () {
                                resolve(videoElement);
                            });
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
    }

    HtmlVideoPlayer.prototype.canPlayMediaType = function (mediaType) {

        return (mediaType || '').toLowerCase() === 'video';
    };

    HtmlVideoPlayer.prototype.supportsPlayMethod = function (playMethod, item) {

        if (appHost.supportsPlayMethod) {
            return appHost.supportsPlayMethod(playMethod, item);
        }

        return true;
    };

    HtmlVideoPlayer.prototype.getDeviceProfile = function (item, options) {

        var instance = this;
        return getDeviceProfileInternal(item, options).then(function (profile) {
            instance._lastProfile = profile;
            return profile;
        });
    };

    function getDeviceProfileInternal(item, options) {
        if (appHost.getDeviceProfile) {
            return appHost.getDeviceProfile(item, options);
        }

        return getDefaultProfile();
    }

    var supportedFeatures;
    function getSupportedFeatures() {

        var list = [];

        var video = document.createElement('video');
        //if (video.webkitSupportsPresentationMode && video.webkitSupportsPresentationMode('picture-in-picture') && typeof video.webkitSetPresentationMode === "function") {
        //    list.push('PictureInPicture');
        //}
        if (document.pictureInPictureEnabled) {
            list.push('PictureInPicture');
        }
        else if (browser.ipad) {

            // Unfortunately this creates a false positive on devices where its' not actually supported
            if (navigator.userAgent.toLowerCase().indexOf('os 9') === -1) {
                if (video.webkitSupportsPresentationMode && video.webkitSupportsPresentationMode && typeof video.webkitSetPresentationMode === "function") {
                    list.push('PictureInPicture');
                }
            }
        } else if (window.Windows) {

            if (Windows.UI.ViewManagement.ApplicationView.getForCurrentView().isViewModeSupported(Windows.UI.ViewManagement.ApplicationViewMode.compactOverlay)) {
                list.push('PictureInPicture');
            }
        }

        list.push('SetBrightness');

        return list;
    }

    HtmlVideoPlayer.prototype.supports = function (feature) {

        if (!supportedFeatures) {
            supportedFeatures = getSupportedFeatures();
        }

        return supportedFeatures.indexOf(feature) !== -1;
    };

    // Save this for when playback stops, because querying the time at that point might return 0
    HtmlVideoPlayer.prototype.currentTime = function (val) {

        var mediaElement = this._mediaElement;
        if (mediaElement) {
            if (val != null) {
                mediaElement.currentTime = val / 1000;
                return;
            }

            var currentTime = this._currentTime;
            if (currentTime) {
                return currentTime * 1000;
            }

            return (mediaElement.currentTime || 0) * 1000;
        }
    };

    HtmlVideoPlayer.prototype.duration = function (val) {

        var mediaElement = this._mediaElement;
        if (mediaElement) {
            var duration = mediaElement.duration;
            if (htmlMediaHelper.isValidDuration(duration)) {
                return duration * 1000;
            }
        }

        return null;
    };

    HtmlVideoPlayer.prototype.canSetAudioStreamIndex = function (index) {

        if (browser.tizen || browser.orsay) {
            return true;
        }

        var video = this._mediaElement;
        if (video) {
            if (video.audioTracks) {
                return true;
            }
        }

        return false;
    };

    function onPictureInPictureError(err) {
        console.log('Picture in picture error: ' + err.toString());
    }

    HtmlVideoPlayer.prototype.setPictureInPictureEnabled = function (isEnabled) {

        var video = this._mediaElement;

        if (document.pictureInPictureEnabled) {
            if (video) {
                if (isEnabled) {
                    video.requestPictureInPicture().catch(onPictureInPictureError);
                } else {
                    document.exitPictureInPicture().catch(onPictureInPictureError);
                }
            }
        }
        else if (window.Windows) {

            this.isPip = isEnabled;
            if (isEnabled) {
                Windows.UI.ViewManagement.ApplicationView.getForCurrentView().tryEnterViewModeAsync(Windows.UI.ViewManagement.ApplicationViewMode.compactOverlay);
            }
            else {
                Windows.UI.ViewManagement.ApplicationView.getForCurrentView().tryEnterViewModeAsync(Windows.UI.ViewManagement.ApplicationViewMode.default);
            }
        }
        else {
            if (video) {
                if (video.webkitSupportsPresentationMode && typeof video.webkitSetPresentationMode === "function") {
                    video.webkitSetPresentationMode(isEnabled ? "picture-in-picture" : "inline");
                }
            }
        }
    };

    HtmlVideoPlayer.prototype.isPictureInPictureEnabled = function () {

        if (document.pictureInPictureEnabled) {
            return document.pictureInPictureElement ? true : false;
        }
        else if (window.Windows) {
            return this.isPip || false;
        }
        else {
            var video = this._mediaElement;
            if (video) {
                return video.webkitPresentationMode === "picture-in-picture";
            }
        }

        return false;
    };

    HtmlVideoPlayer.prototype.setBrightness = function (val) {

        var elem = this._mediaElement;

        if (elem) {

            val = Math.max(0, val);
            val = Math.min(100, val);

            var rawValue = val;
            rawValue = Math.max(20, rawValue);

            var cssValue = rawValue >= 100 ? 'none' : (rawValue / 100);
            elem.style['-webkit-filter'] = 'brightness(' + cssValue + ');';
            elem.style.filter = 'brightness(' + cssValue + ')';
            elem.brightnessValue = val;
            events.trigger(this, 'brightnesschange');
        }
    };

    HtmlVideoPlayer.prototype.getBrightness = function () {

        var elem = this._mediaElement;

        if (elem) {
            var val = elem.brightnessValue;
            return val == null ? 100 : val;
        }
    };

    HtmlVideoPlayer.prototype.seekable = function () {
        var mediaElement = this._mediaElement;
        if (mediaElement) {

            var seekable = mediaElement.seekable;
            if (seekable && seekable.length) {

                var start = seekable.start(0);
                var end = seekable.end(0);

                if (!htmlMediaHelper.isValidDuration(start)) {
                    start = 0;
                }
                if (!htmlMediaHelper.isValidDuration(end)) {
                    end = 0;
                }

                return (end - start) > 0;
            }

            return false;
        }
    };

    HtmlVideoPlayer.prototype.pause = function () {
        var mediaElement = this._mediaElement;
        if (mediaElement) {
            mediaElement.pause();
        }
    };

    // This is a retry after error
    HtmlVideoPlayer.prototype.resume = function () {
        var mediaElement = this._mediaElement;
        if (mediaElement) {
            mediaElement.play();
        }
    };

    HtmlVideoPlayer.prototype.unpause = function () {
        var mediaElement = this._mediaElement;
        if (mediaElement) {
            mediaElement.play();
        }
    };

    HtmlVideoPlayer.prototype.paused = function () {

        var mediaElement = this._mediaElement;
        if (mediaElement) {
            return mediaElement.paused;
        }

        return false;
    };

    HtmlVideoPlayer.prototype.setVolume = function (val) {
        var mediaElement = this._mediaElement;
        if (mediaElement) {
            mediaElement.volume = val / 100;
        }
    };

    HtmlVideoPlayer.prototype.getVolume = function () {
        var mediaElement = this._mediaElement;
        if (mediaElement) {

            return Math.min(Math.round(mediaElement.volume * 100), 100);
        }
    };

    HtmlVideoPlayer.prototype.volumeUp = function () {
        this.setVolume(Math.min(this.getVolume() + 2, 100));
    };

    HtmlVideoPlayer.prototype.volumeDown = function () {
        this.setVolume(Math.max(this.getVolume() - 2, 0));
    };

    HtmlVideoPlayer.prototype.setMute = function (mute) {

        var mediaElement = this._mediaElement;
        if (mediaElement) {
            mediaElement.muted = mute;
        }
    };

    HtmlVideoPlayer.prototype.isMuted = function () {
        var mediaElement = this._mediaElement;
        if (mediaElement) {
            return mediaElement.muted;
        }
        return false;
    };

    HtmlVideoPlayer.prototype.setAspectRatio = function (val) {

    };

    HtmlVideoPlayer.prototype.getAspectRatio = function () {

        return this._currentAspectRatio;
    };

    HtmlVideoPlayer.prototype.getSupportedAspectRatios = function () {

        return [];
    };

    HtmlVideoPlayer.prototype.togglePictureInPicture = function () {
        return this.setPictureInPictureEnabled(!this.isPictureInPictureEnabled());
    };

    HtmlVideoPlayer.prototype.getBufferedRanges = function () {
        var mediaElement = this._mediaElement;
        if (mediaElement) {

            return htmlMediaHelper.getBufferedRanges(this, mediaElement);
        }

        return [];
    };

    HtmlVideoPlayer.prototype.getStats = function () {

        var mediaElement = this._mediaElement;
        var playOptions = this._currentPlayOptions || [];

        var categories = [];

        if (!mediaElement) {
            return Promise.resolve({
                categories: categories
            });
        }

        var mediaCategory = {
            stats: [],
            type: 'media'
        };
        categories.push(mediaCategory);

        if (playOptions.url) {
            //  create an anchor element (note: no need to append this element to the document)
            var link = document.createElement('a');
            //  set href to any path
            link.setAttribute('href', playOptions.url);
            var protocol = (link.protocol || '').replace(':', '');

            if (protocol) {
                mediaCategory.stats.push({
                    label: 'Protocol:',
                    value: protocol
                });
            }

            link = null;
        }

        if (this._hlsPlayer || this._shakaPlayer) {
            mediaCategory.stats.push({
                label: 'Stream type:',
                value: 'HLS'
            });
        } else {
            mediaCategory.stats.push({
                label: 'Stream type:',
                value: 'Video'
            });
        }

        var videoCategory = {
            stats: [],
            type: 'video'
        };
        categories.push(videoCategory);

        var rect = mediaElement.getBoundingClientRect ? mediaElement.getBoundingClientRect() : {};
        var height = rect.height;
        var width = rect.width;

        // Don't show player dimensions on smart TVs because the app UI could be lower resolution than the video and this causes users to think there is a problem
        if (width && height && !browser.tv) {
            videoCategory.stats.push({
                label: 'Player dimensions:',
                value: width + 'x' + height
            });
        }

        height = mediaElement.videoHeight;
        width = mediaElement.videoWidth;

        if (width && height) {
            videoCategory.stats.push({
                label: 'Video resolution:',
                value: width + 'x' + height
            });
        }

        if (mediaElement.getVideoPlaybackQuality) {
            var playbackQuality = mediaElement.getVideoPlaybackQuality();

            var droppedVideoFrames = playbackQuality.droppedVideoFrames || 0;
            videoCategory.stats.push({
                label: 'Dropped frames:',
                value: droppedVideoFrames
            });

            var corruptedVideoFrames = playbackQuality.corruptedVideoFrames || 0;
            videoCategory.stats.push({
                label: 'Corrupted frames:',
                value: corruptedVideoFrames
            });
        }

        var audioCategory = {
            stats: [],
            type: 'audio'
        };
        categories.push(audioCategory);

        var sinkId = mediaElement.sinkId;
        if (sinkId) {
            audioCategory.stats.push({
                label: 'Sink Id:',
                value: sinkId
            });
        }

        return Promise.resolve({
            categories: categories
        });
    };

    if (browser.chromecast) {
        mediaManager = new cast.receiver.MediaManager(document.createElement('video'));
    }

    return HtmlVideoPlayer;
});