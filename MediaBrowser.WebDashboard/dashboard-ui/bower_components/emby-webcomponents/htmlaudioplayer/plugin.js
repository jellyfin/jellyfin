define(['events', 'browser', 'require', 'apphost', 'appSettings', 'htmlMediaHelper'], function (events, browser, require, appHost, appSettings, htmlMediaHelper) {
    "use strict";

    function getDefaultProfile() {

        return new Promise(function (resolve, reject) {

            require(['browserdeviceprofile'], function (profileBuilder) {

                resolve(profileBuilder({}));
            });
        });
    }

    var fadeTimeout;
    function fade(instance, elem, startingVolume) {

        instance._isFadingOut = true;

        // Need to record the starting volume on each pass rather than querying elem.volume
        // This is due to iOS safari not allowing volume changes and always returning the system volume value

        var newVolume = Math.max(0, startingVolume - 0.15);
        console.log('fading volume to ' + newVolume);
        elem.volume = newVolume;

        if (newVolume <= 0) {

            instance._isFadingOut = false;
            return Promise.resolve();
        }

        return new Promise(function (resolve, reject) {

            cancelFadeTimeout();

            fadeTimeout = setTimeout(function () {

                fade(instance, elem, newVolume).then(resolve, reject);
            }, 100);
        });
    }

    function cancelFadeTimeout() {
        var timeout = fadeTimeout;
        if (timeout) {
            clearTimeout(timeout);
            fadeTimeout = null;
        }
    }

    function supportsFade() {

        if (browser.tv) {
            // Not working on tizen. 
            // We could possibly enable on other tv's, but all smart tv browsers tend to be pretty primitive
            return false;
        }

        return true;
    }

    function requireHlsPlayer(callback) {
        require(['hlsjs'], function (hls) {
            window.Hls = hls;
            callback();
        });
    }

    function enableHlsPlayer(url, item, mediaSource, mediaType) {

        if (!htmlMediaHelper.enableHlsJsPlayer(mediaSource.RunTimeTicks, mediaType)) {

            return Promise.reject();
        }

        if (url.indexOf('.m3u8') !== -1) {
            return Promise.resolve();
        }

        // issue head request to get content type
        return new Promise(function (resolve, reject) {

            require(['fetchHelper'], function (fetchHelper) {
                fetchHelper.ajax({
                    url: url,
                    type: 'HEAD'
                }).then(function (response) {

                    var contentType = (response.headers.get('Content-Type') || '').toLowerCase();
                    if (contentType === 'application/x-mpegurl') {
                        resolve();
                    } else {
                        reject();
                    }

                }, reject);
            });
        });
    }

    function HtmlAudioPlayer() {

        var self = this;

        self.name = 'Html Audio Player';
        self.type = 'mediaplayer';
        self.id = 'htmlaudioplayer';

        // Let any players created by plugins take priority
        self.priority = 1;

        self.play = function (options) {

            self._started = false;
            self._timeUpdated = false;

            self._currentTime = null;

            var elem = createMediaElement(options);

            return setCurrentSrc(elem, options);
        };

        function setCurrentSrc(elem, options) {

            elem.removeEventListener('error', onError);

            unBindEvents(elem);
            bindEvents(elem);

            var val = options.url;
            console.log('playing url: ' + val);

            // Convert to seconds
            var seconds = (options.playerStartPositionTicks || 0) / 10000000;
            if (seconds) {
                val += '#t=' + seconds;
            }

            htmlMediaHelper.destroyHlsPlayer(self);

            self._currentPlayOptions = options;

            var crossOrigin = htmlMediaHelper.getCrossOriginValue(options.mediaSource);
            if (crossOrigin) {
                elem.crossOrigin = crossOrigin;
            }

            return enableHlsPlayer(val, options.item, options.mediaSource, 'Audio').then(function () {

                return new Promise(function (resolve, reject) {

                    requireHlsPlayer(function () {
                        var hls = new Hls({
                            manifestLoadingTimeOut: 20000
                            //appendErrorMaxRetry: 6,
                            //debug: true
                        });
                        hls.loadSource(val);
                        hls.attachMedia(elem);

                        htmlMediaHelper.bindEventsToHlsPlayer(self, hls, elem, onError, resolve, reject);

                        self._hlsPlayer = hls;

                        self._currentSrc = val;
                    });
                });


            }, function () {

                elem.autoplay = true;

                return htmlMediaHelper.applySrc(elem, val, options).then(function () {

                    self._currentSrc = val;

                    return htmlMediaHelper.playWithPromise(elem, onError);
                });
            });
        }

        function bindEvents(elem) {
            elem.addEventListener('timeupdate', onTimeUpdate);
            elem.addEventListener('ended', onEnded);
            elem.addEventListener('volumechange', onVolumeChange);
            elem.addEventListener('pause', onPause);
            elem.addEventListener('playing', onPlaying);
            elem.addEventListener('play', onPlay);
        }

        function unBindEvents(elem) {
            elem.removeEventListener('timeupdate', onTimeUpdate);
            elem.removeEventListener('ended', onEnded);
            elem.removeEventListener('volumechange', onVolumeChange);
            elem.removeEventListener('pause', onPause);
            elem.removeEventListener('playing', onPlaying);
            elem.removeEventListener('play', onPlay);
        }

        self.stop = function (destroyPlayer) {

            cancelFadeTimeout();

            var elem = self._mediaElement;
            var src = self._currentSrc;

            if (elem && src) {

                if (!destroyPlayer || !supportsFade()) {

                    elem.pause();

                    htmlMediaHelper.onEndedInternal(self, elem, onError);

                    if (destroyPlayer) {
                        self.destroy();
                    }
                    return Promise.resolve();
                }

                var originalVolume = elem.volume;

                return fade(self, elem, elem.volume).then(function () {

                    elem.pause();
                    elem.volume = originalVolume;

                    htmlMediaHelper.onEndedInternal(self, elem, onError);

                    if (destroyPlayer) {
                        self.destroy();
                    }
                });
            }
            return Promise.resolve();
        };

        self.destroy = function () {
            unBindEvents(self._mediaElement);
        };

        function createMediaElement() {

            var elem = self._mediaElement;

            if (elem) {
                return elem;
            }

            elem = document.querySelector('.mediaPlayerAudio');

            if (!elem) {
                elem = document.createElement('audio');
                elem.classList.add('mediaPlayerAudio');
                elem.classList.add('hide');

                document.body.appendChild(elem);
            }

            elem.volume = htmlMediaHelper.getSavedVolume();

            self._mediaElement = elem;

            return elem;
        }

        function onEnded() {

            htmlMediaHelper.onEndedInternal(self, this, onError);
        }

        function onTimeUpdate() {

            // Get the player position + the transcoding offset
            var time = this.currentTime;

            // Don't trigger events after user stop
            if (!self._isFadingOut) {
                self._currentTime = time;
                events.trigger(self, 'timeupdate');
            }
        }

        function onVolumeChange() {

            if (!self._isFadingOut) {
                htmlMediaHelper.saveVolume(this.volume);
                events.trigger(self, 'volumechange');
            }
        }

        function onPlaying(e) {

            if (!self._started) {
                self._started = true;
                this.removeAttribute('controls');

                htmlMediaHelper.seekOnPlaybackStart(self, e.target, self._currentPlayOptions.playerStartPositionTicks);
            }
            events.trigger(self, 'playing');
        }

        function onPlay(e) {

            events.trigger(self, 'unpause');
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
    }

    HtmlAudioPlayer.prototype.currentSrc = function () {
        return this._currentSrc;
    };

    HtmlAudioPlayer.prototype.canPlayMediaType = function (mediaType) {

        return (mediaType || '').toLowerCase() === 'audio';
    };

    HtmlAudioPlayer.prototype.getDeviceProfile = function (item) {

        if (appHost.getDeviceProfile) {
            return appHost.getDeviceProfile(item);
        }

        return getDefaultProfile();
    };

    // Save this for when playback stops, because querying the time at that point might return 0
    HtmlAudioPlayer.prototype.currentTime = function (val) {

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

    HtmlAudioPlayer.prototype.duration = function (val) {

        var mediaElement = this._mediaElement;
        if (mediaElement) {
            var duration = mediaElement.duration;
            if (htmlMediaHelper.isValidDuration(duration)) {
                return duration * 1000;
            }
        }

        return null;
    };

    HtmlAudioPlayer.prototype.seekable = function () {
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

    HtmlAudioPlayer.prototype.getBufferedRanges = function () {
        var mediaElement = this._mediaElement;
        if (mediaElement) {

            return htmlMediaHelper.getBufferedRanges(this, mediaElement);
        }

        return [];
    };

    HtmlAudioPlayer.prototype.pause = function () {
        var mediaElement = this._mediaElement;
        if (mediaElement) {
            mediaElement.pause();
        }
    };

    // This is a retry after error
    HtmlAudioPlayer.prototype.resume = function () {
        var mediaElement = this._mediaElement;
        if (mediaElement) {
            mediaElement.play();
        }
    };

    HtmlAudioPlayer.prototype.unpause = function () {
        var mediaElement = this._mediaElement;
        if (mediaElement) {
            mediaElement.play();
        }
    };

    HtmlAudioPlayer.prototype.paused = function () {

        var mediaElement = this._mediaElement;
        if (mediaElement) {
            return mediaElement.paused;
        }

        return false;
    };

    HtmlAudioPlayer.prototype.setVolume = function (val) {
        var mediaElement = this._mediaElement;
        if (mediaElement) {
            mediaElement.volume = val / 100;
        }
    };

    HtmlAudioPlayer.prototype.getVolume = function () {
        var mediaElement = this._mediaElement;
        if (mediaElement) {

            return Math.min(Math.round(mediaElement.volume * 100), 100);
        }
    };

    HtmlAudioPlayer.prototype.volumeUp = function () {
        this.setVolume(Math.min(this.getVolume() + 2, 100));
    };

    HtmlAudioPlayer.prototype.volumeDown = function () {
        this.setVolume(Math.max(this.getVolume() - 2, 0));
    };

    HtmlAudioPlayer.prototype.setMute = function (mute) {

        var mediaElement = this._mediaElement;
        if (mediaElement) {
            mediaElement.muted = mute;
        }
    };

    HtmlAudioPlayer.prototype.isMuted = function () {
        var mediaElement = this._mediaElement;
        if (mediaElement) {
            return mediaElement.muted;
        }
        return false;
    };

    HtmlAudioPlayer.prototype.destroy = function () {

    };

    return HtmlAudioPlayer;
});