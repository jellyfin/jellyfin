(function () {

    var supportsTextTracks;
    var hlsPlayer;
    var requiresSettingStartTimeOnStart;
    var subtitleTrackIndexToSetOnPlaying;
    var currentTrackList;

    function htmlMediaRenderer(options) {

        var mediaElement;
        var self = this;

        function onEnded() {
            destroyCustomTrack();
            Events.trigger(self, 'ended');
        }

        function onTimeUpdate() {

            //if (isViblastStarted) {

            //    // This is a workaround for viblast not stopping playback at the end
            //    var time = this.currentTime;
            //    var duration = this.duration;

            //    if (duration) {
            //        if (time >= (duration - 1)) {

            //            //onEnded();
            //            return;
            //        }
            //    }
            //}
            updateSubtitleText(this.currentTime * 1000);

            Events.trigger(self, 'timeupdate');
        }

        function onVolumeChange() {
            Events.trigger(self, 'volumechange');
        }

        function onOneAudioPlaying(e) {

            var elem = e.target;
            elem.removeEventListener('playing', onOneAudioPlaying);
            $('.mediaPlayerAudioContainer').hide();
        }

        function onPlaying() {
            Events.trigger(self, 'playing');
        }

        function onPlay() {
            Events.trigger(self, 'play');
        }

        function onPause() {
            Events.trigger(self, 'pause');
        }

        function onClick() {
            Events.trigger(self, 'click');
        }

        function onDblClick() {
            Events.trigger(self, 'dblclick');
        }

        function onError(e) {

            destroyCustomTrack();

            var elem = e.target;
            var errorCode = elem.error ? elem.error.code : '';
            console.log('Media element error code: ' + errorCode);

            Events.trigger(self, 'error');
        }

        function onLoadedMetadata(e) {

            var elem = e.target;

            elem.removeEventListener('loadedmetadata', onLoadedMetadata);

            if (!hlsPlayer) {
                elem.play();
            }
        }

        function requireHlsPlayer(callback) {
            require(['hlsjs'], function (hls) {
                window.Hls = hls;
                callback();
            });
        }

        function getStartTime(url) {

            var src = url;

            var parts = src.split('#');

            if (parts.length > 1) {

                parts = parts[parts.length - 1].split('=');

                if (parts.length == 2) {

                    return parseFloat(parts[1]);
                }
            }

            return 0;
        }

        function onOneVideoPlaying(e) {

            var element = e.target;
            element.removeEventListener('playing', onOneVideoPlaying);

            self.setCurrentTrackElement(subtitleTrackIndexToSetOnPlaying);

            var requiresNativeControls = !self.enableCustomVideoControls();

            if (requiresNativeControls) {
                $(element).attr('controls', 'controls');
            }

            if (requiresSettingStartTimeOnStart) {
                var src = (self.currentSrc() || '').toLowerCase();

                var startPositionInSeekParam = getStartTime(src);

                // Appending #t=xxx to the query string doesn't seem to work with HLS
                if (startPositionInSeekParam && src.indexOf('.m3u8') != -1) {

                    var delay = browserInfo.safari ? 2500 : 0;
                    if (delay) {
                        setTimeout(function () {
                            element.currentTime = startPositionInSeekParam;
                        }, delay);
                    } else {
                        element.currentTime = startPositionInSeekParam;
                    }
                }
            }
        }

        function createAudioElement() {

            var elem = $('.mediaPlayerAudio');

            if (!elem.length) {
                var html = '';

                var requiresControls = !MediaPlayer.canAutoPlayAudio();

                if (requiresControls) {
                    html += '<div class="mediaPlayerAudioContainer" style="position: fixed;top: 40%;text-align: center;left: 0;right: 0;z-index:999999;"><div class="mediaPlayerAudioContainerInner">';;
                } else {
                    html += '<div class="mediaPlayerAudioContainer" style="display:none;padding: 1em;background: #222;"><div class="mediaPlayerAudioContainerInner">';;
                }

                html += '<audio class="mediaPlayerAudio" controls>';
                html += '</audio></div></div>';

                $(document.body).append(html);

                elem = $('.mediaPlayerAudio');
            }

            elem = elem[0];

            elem.addEventListener('playing', onOneAudioPlaying);
            elem.addEventListener('timeupdate', onTimeUpdate);
            elem.addEventListener('ended', onEnded);
            elem.addEventListener('volumechange', onVolumeChange);
            elem.addEventListener('error', onError);
            elem.addEventListener('pause', onPause);
            elem.addEventListener('play', onPlay);
            elem.addEventListener('playing', onPlaying);

            return elem;
        }

        function enableHlsPlayer(src) {

            if (src) {
                if (src.indexOf('.m3u8') == -1) {
                    return false;
                }
            }

            return MediaPlayer.canPlayHls() && !MediaPlayer.canPlayNativeHls();
        }

        function getCrossOriginValue(mediaSource) {

            return 'anonymous';
        }

        function createVideoElement() {

            var html = '';

            var requiresNativeControls = !self.enableCustomVideoControls();

            // Safari often displays the poster under the video and it doesn't look good
            var poster = !browserInfo.safari && options.poster ? (' poster="' + options.poster + '"') : '';

            // Can't autoplay in these browsers so we need to use the full controls
            if (requiresNativeControls && AppInfo.isNativeApp && browserInfo.android) {
                html += '<video class="itemVideo" id="itemVideo" preload="metadata" autoplay="autoplay"' + poster + ' webkit-playsinline>';
            }
            else if (requiresNativeControls) {
                html += '<video class="itemVideo" id="itemVideo" preload="metadata" autoplay="autoplay"' + poster + ' controls="controls" webkit-playsinline>';
            }
            else {

                // Chrome 35 won't play with preload none
                html += '<video class="itemVideo" id="itemVideo" preload="metadata" autoplay="autoplay"' + poster + ' webkit-playsinline>';
            }

            html += '</video>';

            var elem = $('#videoElement', '#videoPlayer').prepend(html);

            var itemVideo = $('.itemVideo', elem)[0];

            itemVideo.addEventListener('loadedmetadata', onLoadedMetadata);

            itemVideo.addEventListener('timeupdate', onTimeUpdate);
            itemVideo.addEventListener('ended', onEnded);
            itemVideo.addEventListener('volumechange', onVolumeChange);

            itemVideo.addEventListener('play', onPlay);
            itemVideo.addEventListener('pause', onPause);
            itemVideo.addEventListener('playing', onPlaying);

            itemVideo.addEventListener('click', onClick);
            itemVideo.addEventListener('dblclick', onDblClick);
            itemVideo.addEventListener('error', onError);

            return itemVideo;
        }

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
                return mediaElement.duration;
            }

            return null;
        };

        self.stop = function () {

            destroyCustomTrack();

            if (mediaElement) {
                mediaElement.pause();

                if (hlsPlayer) {
                    _currentTime = mediaElement.currentTime;

                    // Sometimes this fails
                    try {
                        hlsPlayer.destroy();
                    }
                    catch (err) {
                        console.log(err);
                    }

                    hlsPlayer = null;
                }
            }
        };

        self.pause = function () {
            if (mediaElement) {
                mediaElement.pause();
            }
        };

        self.unpause = function () {
            if (mediaElement) {
                mediaElement.play();
            }
        };

        self.volume = function (val) {
            if (mediaElement) {
                if (val != null) {
                    mediaElement.volume = val;
                    return;
                }

                return mediaElement.volume;
            }
        };

        var currentSrc;
        self.setCurrentSrc = function (streamInfo, item, mediaSource, tracks) {

            var elem = mediaElement;

            if (!elem) {
                currentSrc = null;
                return;
            }

            if (!streamInfo) {
                currentSrc = null;
                elem.src = null;
                elem.src = "";

                // When the browser regains focus it may start auto-playing the last video
                if (browserInfo.safari) {
                    elem.src = 'files/dummy.mp4';
                    elem.play();
                }

                return;
            }

            elem.crossOrigin = getCrossOriginValue(mediaSource);
            var val = streamInfo.url;

            if (AppInfo.isNativeApp && browserInfo.safari) {
                val = val.replace('file://', '');
            }

            requiresSettingStartTimeOnStart = false;
            var startTime = getStartTime(val);
            var playNow = false;

            if (elem.tagName.toLowerCase() == 'audio') {

                elem.src = val;
                playNow = true;

            }
            else {

                elem.removeEventListener('playing', onOneVideoPlaying);
                elem.addEventListener('playing', onOneVideoPlaying);

                if (hlsPlayer) {
                    hlsPlayer.destroy();
                    hlsPlayer = null;
                }

                if (startTime) {

                    //try {
                    //    elem.currentTime = startTime;
                    //} catch (err) {
                    //    // IE will throw an invalid state exception when trying to set currentTime before starting playback
                    //}
                    //requiresSettingStartTimeOnStart = elem.currentTime == 0;
                    requiresSettingStartTimeOnStart = true;
                }

                tracks = tracks || [];
                currentTrackList = tracks;

                var currentTrackIndex = -1;
                for (var i = 0, length = tracks.length; i < length; i++) {
                    if (tracks[i].isDefault) {
                        currentTrackIndex = tracks[i].index;
                        break;
                    }
                }
                subtitleTrackIndexToSetOnPlaying = currentTrackIndex;

                if (enableHlsPlayer(val)) {

                    setTracks(elem, tracks);

                    var hls = new Hls();
                    hls.loadSource(val);
                    hls.attachMedia(elem);
                    hls.on(Hls.Events.MANIFEST_PARSED, function () {
                        elem.play();
                    });
                    hlsPlayer = hls;

                } else {

                    elem.src = val;
                    elem.autoplay = true;

                    setTracks(elem, tracks);

                    elem.addEventListener("loadedmetadata", onLoadedMetadata);
                    playNow = true;
                }

                self.setCurrentTrackElement(currentTrackIndex);
            }

            currentSrc = val;

            if (playNow) {
                elem.play();
            }
        };

        function setTracks(elem, tracks) {

            var html = tracks.map(function (t) {

                var defaultAttribute = t.isDefault ? ' default' : '';

                var label = t.language || 'und';
                return '<track id="textTrack' + t.index + '" label="' + label + '" kind="subtitles" src="' + t.url + '" srclang="' + t.language + '"' + defaultAttribute + '></track>';

            }).join('');

            elem.innerHTML = html;
        }

        self.currentSrc = function () {
            if (mediaElement) {
                // We have to use this cached value because viblast will muck with the url
                return currentSrc;
                //return mediaElement.currentSrc;
            }
        };

        self.paused = function () {

            if (mediaElement) {
                return mediaElement.paused;
            }

            return false;
        };

        self.cleanup = function (destroyRenderer) {

            self.setCurrentSrc(null);
            _currentTime = null;

            var elem = mediaElement;

            if (elem) {

                if (elem.tagName == 'AUDIO') {

                    elem.removeEventListener('timeupdate', onTimeUpdate);
                    elem.removeEventListener('ended', onEnded);
                    elem.removeEventListener('volumechange', onVolumeChange);
                    elem.removeEventListener('playing', onOneAudioPlaying);
                    elem.removeEventListener('play', onPlay);
                    elem.removeEventListener('pause', onPause);
                    elem.removeEventListener('playing', onPlaying);
                    elem.removeEventListener('error', onError);

                } else {

                    elem.removeEventListener('loadedmetadata', onLoadedMetadata);
                    elem.removeEventListener('playing', onOneVideoPlaying);
                    elem.removeEventListener('timeupdate', onTimeUpdate);
                    elem.removeEventListener('ended', onEnded);
                    elem.removeEventListener('volumechange', onVolumeChange);
                    elem.removeEventListener('play', onPlay);
                    elem.removeEventListener('pause', onPause);
                    elem.removeEventListener('playing', onPlaying);
                    elem.removeEventListener('click', onClick);
                    elem.removeEventListener('dblclick', onDblClick);
                    elem.removeEventListener('error', onError);
                }

                if (elem.tagName.toLowerCase() != 'audio') {
                    $(elem).remove();
                }
            }
        };

        self.supportsTextTracks = function () {

            if (supportsTextTracks == null) {
                supportsTextTracks = document.createElement('video').textTracks != null;
            }

            // For now, until ready
            return supportsTextTracks;
        };

        function enableNativeTrackSupport(track) {

            if (browserInfo.safari) {
                return false;
            }

            return true;
        }

        function destroyCustomTrack() {

            var videoSubtitlesElem = document.querySelector('.videoSubtitles');
            if (videoSubtitlesElem) {
                videoSubtitlesElem.parentNode.removeChild(videoSubtitlesElem);
            }

            currentSubtitlesElement = null;
            currentTrackEvents = null;
            customTrackIndex = -1;
        }

        function fetchSubtitles(track) {

            return ApiClient.ajax({
                url: track.url.replace('.vtt', '.js'),
                type: 'GET',
                dataType: 'json'
            });
        }

        function setTrackForCustomDisplay(track) {

            if (!track) {
                destroyCustomTrack();
                return;
            }

            // if already playing this track, skip
            if (customTrackIndex == track.index) {
                return;
            }

            destroyCustomTrack();
            customTrackIndex = track.index;

            // download the track json
            fetchSubtitles(track).then(function (data) {

                // show in ui
                console.log('downloaded ' + data.TrackEvents.length + ' track events');
                currentTrackEvents = data.TrackEvents;
            });
        }

        var currentSubtitlesElement;
        var currentTrackEvents;
        var customTrackIndex = -1;
        var lastCustomTrackMs = 0;
        function updateSubtitleText(timeMs) {

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
                    document.body.appendChild(videoSubtitlesElem);
                }
                currentSubtitlesElement = videoSubtitlesElem.querySelector('.videoSubtitlesInner');
            }

            if (lastCustomTrackMs > 0) {
                if (Math.abs(lastCustomTrackMs - timeMs) < 500) {
                    return;
                }
            }

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

        self.setCurrentTrackElement = function (streamIndex) {

            console.log('Setting new text track index to: ' + streamIndex);

            var track = streamIndex == -1 ? null : currentTrackList.filter(function (t) {
                return t.index == streamIndex;
            })[0];

            if (enableNativeTrackSupport(track)) {

                setTrackForCustomDisplay(null);
            } else {
                setTrackForCustomDisplay(track);

                // null these out to disable the player's native display (handled below)
                streamIndex = -1;
                track = null;
            }

            var expectedId = 'textTrack' + streamIndex;
            var trackIndex = streamIndex == -1 || !track ? -1 : currentTrackList.indexOf(track);
            var modes = ['disabled', 'showing', 'hidden'];

            var allTracks = mediaElement.textTracks; // get list of tracks
            for (var i = 0; i < allTracks.length; i++) {

                var currentTrack = allTracks[i];

                console.log('currentTrack id: ' + currentTrack.id);

                var mode;

                console.log('expectedId: ' + expectedId + '--currentTrack.Id:' + currentTrack.id);

                // IE doesn't support track id
                if (browserInfo.msie) {
                    if (trackIndex == i) {
                        mode = 1; // show this track
                    } else {
                        mode = 0; // hide all other tracks
                    }
                } else {
                    if (currentTrack.id == expectedId) {
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
        };

        self.updateTextStreamUrls = function (startPositionTicks) {

            if (!self.supportsTextTracks()) {
                return;
            }

            var allTracks = mediaElement.textTracks; // get list of tracks

            for (var i = 0; i < allTracks.length; i++) {

                var track = allTracks[i];

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

            $('track', mediaElement).each(function () {

                this.src = replaceQueryString(this.src, 'startPositionTicks', startPositionTicks);

            });
        };

        self.enableCustomVideoControls = function () {

            if (AppInfo.isNativeApp && browserInfo.safari) {

                if (navigator.userAgent.toLowerCase().indexOf('iphone') != -1) {
                    return true;
                }

                // Need to disable it in order to support picture in picture
                return false;
            }

            return self.canAutoPlayVideo();
        };

        self.canAutoPlayVideo = function () {

            if (AppInfo.isNativeApp) {
                return true;
            }

            if (browserInfo.mobile) {
                return false;
            }

            return true;
        };

        self.init = function () {

            return new Promise(function (resolve, reject) {

                if (options.type == 'video' && enableHlsPlayer()) {

                    requireHlsPlayer(resolve);

                } else {
                    resolve();
                }
            });
        };

        if (options.type == 'audio') {
            mediaElement = createAudioElement();
        }
        else {
            mediaElement = createVideoElement();
        }
    }

    if (!window.AudioRenderer) {
        window.AudioRenderer = function (options) {
            options = options || {};
            options.type = 'audio';

            return new htmlMediaRenderer(options);
        };
    }

    if (!window.VideoRenderer) {
        window.VideoRenderer = function (options) {
            options = options || {};
            options.type = 'video';

            return new htmlMediaRenderer(options);
        };
    }

})();