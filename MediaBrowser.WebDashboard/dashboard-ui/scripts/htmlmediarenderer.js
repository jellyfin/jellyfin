(function () {

    var supportsTextTracks;

    function htmlMediaRenderer(type) {

        var mediaElement;
        var self = this;

        function onEnded() {
            $(self).trigger('ended');
        }

        function onTimeUpdate() {
            $(self).trigger('timeupdate');
        }

        function onVolumeChange() {
            $(self).trigger('volumechange');
        }

        function onOneAudioPlaying() {
            $('.mediaPlayerAudioContainer').hide();
        }

        function onPlaying() {
            $(self).trigger('playing');
        }

        function onPlay() {
            $(self).trigger('play');
        }

        function onPause() {
            $(self).trigger('pause');
        }

        function onClick() {
            $(self).trigger('click');
        }

        function onDblClick() {
            $(self).trigger('dblclick');
        }

        function onError() {

            var errorCode = this.error ? this.error.code : '';
            console.log('Media element error code: ' + errorCode);

            $(self).trigger('error');
        }

        function onLoadedMetadata() {

            // The IE video player won't autoplay without this
            if ($.browser.msie) {
                this.play();
            }
        }

        function onOneVideoPlaying() {

            var requiresNativeControls = !MediaPlayer.enableCustomVideoControls();

            if (requiresNativeControls) {
                $(this).attr('controls', 'controls');
            }

            var currentSrc = (this.currentSrc || '').toLowerCase();

            var parts = currentSrc.split('#');

            if (parts.length > 1) {

                parts = parts[parts.length - 1].split('=');

                if (parts.length == 2) {

                    var startPositionInSeekParam = parseFloat(parts[1]);

                    // Appending #t=xxx to the query string doesn't seem to work with HLS
                    if (startPositionInSeekParam && currentSrc.indexOf('.m3u8') != -1) {
                        var element = this;
                        setTimeout(function () {
                            element.currentTime = startPositionInSeekParam;
                        }, 3000);
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
                    html += '<div class="mediaPlayerAudioContainer"><div class="mediaPlayerAudioContainerInner">';;
                } else {
                    html += '<div class="mediaPlayerAudioContainer" style="display:none;"><div class="mediaPlayerAudioContainerInner">';;
                }

                html += '<audio class="mediaPlayerAudio" crossorigin="anonymous" controls>';
                html += '</audio></div></div>';

                $(document.body).append(html);

                elem = $('.mediaPlayerAudio');
            }

            return $(elem)
	            .on('timeupdate', onTimeUpdate)
	            .on('ended', onEnded)
	            .on('volumechange', onVolumeChange)
	            .one('playing', onOneAudioPlaying)
	            .on('play', onPlay)
	            .on('pause', onPause)
	            .on('playing', onPlaying)
	            .on('error', onError)[0];
        }

        function createVideoElement() {

            var elem = $('.itemVideo');

            return $(elem)
            	.one('.loadedmetadata')
            	.one('playing', onOneVideoPlaying)
	            .on('timeupdate', onTimeUpdate)
	            .on('ended', onEnded)
	            .on('volumechange', onVolumeChange)
	            .on('play', onPlay)
	            .on('pause', onPause)
	            .on('playing', onPlaying)
	            .on('click', onClick)
	            .on('dblclick', onDblClick)
	            .on('error', onError)[0];
        }

        self.currentTime = function (val) {

            if (mediaElement) {
                if (val != null) {
                    mediaElement.currentTime = val;
                    return;
                }

                return mediaElement.currentTime;
            }
        };

        self.duration = function (val) {

            if (mediaElement) {
                return mediaElement.duration;
            }

            return null;
        };

        self.stop = function () {
            if (mediaElement) {
                mediaElement.pause();
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

        self.setCurrentSrc = function (val) {

            var elem = mediaElement;

            if (!elem) {
                return;
            }

            if (!val) {
                elem.src = null;
                elem.src = "";

                // When the browser regains focus it may start auto-playing the last video
                if ($.browser.safari) {
                    elem.src = 'files/dummy.mp4';
                    elem.play();
                }

                return;
            }

            elem.src = val;

            if (elem.tagName.toLowerCase() == 'audio') {
                elem.play();
            }
            else {

                $(elem).one("loadedmetadata.mediaplayerevent", onLoadedMetadata);
            }
        };

        self.currentSrc = function () {
            if (mediaElement) {
                return mediaElement.currentSrc;
            }
        };

        self.paused = function () {

            if (mediaElement) {
                return mediaElement.paused;
            }

            return false;
        };

        self.destroy = function () {

            self.setCurrentSrc(null);

            var elem = mediaElement;

            if (elem) {

                $(elem).off();

                if (elem.tagName.toLowerCase() != 'audio') {
                    $(elem).remove();
                }
            }
        };

        self.setPoster = function (url) {
            var elem = mediaElement;

            if (elem) {
                elem.poster = url;
            }
        };

        self.supportsTextTracks = function () {

            if (supportsTextTracks == null) {
                supportsTextTracks = document.createElement('video').textTracks != null;
            }

            // For now, until ready
            return supportsTextTracks;
        };

        self.setCurrentTrackElement = function (trackIndex) {

            console.log('Setting new text track index to: ' + trackIndex);

            var allTracks = mediaElement.textTracks; // get list of tracks

            var modes = ['disabled', 'showing', 'hidden'];

            for (var i = 0; i < allTracks.length; i++) {

                var mode;

                if (trackIndex == i) {
                    mode = 1; // show this track
                } else {
                    mode = 0; // hide all other tracks
                }

                console.log('Setting track ' + i + ' mode to: ' + mode);

                // Safari uses integers for the mode property
                // http://www.jwplayer.com/html5/scripting/
                var useNumericMode = false;

                if (!isNaN(allTracks[i].mode)) {
                    useNumericMode = true;
                }

                if (useNumericMode) {
                    allTracks[i].mode = mode;
                } else {
                    allTracks[i].mode = modes[mode];
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

                var currentSrc = this.src;

                currentSrc = replaceQueryString(currentSrc, 'startPositionTicks', startPositionTicks);

                this.src = currentSrc;

            });
        };

        if (type == 'audio') {
            mediaElement = createAudioElement();
        }
        else {
            mediaElement = createVideoElement();
        }
    }

    if (!window.AudioRenderer) {
        window.AudioRenderer = htmlMediaRenderer;
    }

    if (!window.VideoRenderer) {
        window.VideoRenderer = htmlMediaRenderer;
    }

})();