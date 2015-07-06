(function () {

    function vlcRenderer(options) {

        var self = this;

        function onEnded() {
            Events.trigger(self, 'ended');
        }

        function onTimeUpdate() {
            Events.trigger(self, 'timeupdate');
        }

        function onVolumeChange() {
            Events.trigger(self, 'volumechange');
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

        function onError() {

            var errorCode = this.error ? this.error.code : '';
            Logger.log('Media element error code: ' + errorCode);

            Events.trigger(self, 'error');
        }

        var playerState = {};

        self.currentTime = function (val) {

            if (val != null) {
                AndroidVlcPlayer.sendVlcCommand("setposition", val.toString());
                return;
            }

            return playerState.currentTime;
        };

        self.duration = function (val) {

            if (playerState) {
                return playerState.duration;
            }

            return null;
        };

        self.stop = function () {
            AndroidVlcPlayer.sendVlcCommand("stop", null);
        };

        self.pause = function () {
            AndroidVlcPlayer.sendVlcCommand("pause", null);
        };

        self.unpause = function () {
            AndroidVlcPlayer.sendVlcCommand("unpause", null);
        };

        self.volume = function (val) {
            if (playerState) {
                if (val != null) {
                    AndroidVlcPlayer.sendVlcCommand("setvolume", (val * 100).toString());
                    return;
                }

                return playerState.volume;
            }
        };

        self.setCurrentSrc = function (val, tracks, item, mediaSource) {

            if (!val) {
                self.destroy();
                return;
            }

            if (options.type == 'audio') {

                AndroidVlcPlayer.playAudioVlc(val, JSON.stringify(item), JSON.stringify(mediaSource), posterUrl);
            } else {
                AndroidVlcPlayer.playVideoVlc(val, JSON.stringify(item), JSON.stringify(mediaSource), posterUrl);
            }

            playerState.currentSrc = val;
        };

        self.currentSrc = function () {
            if (playerState) {
                return playerState.currentSrc;
            }
        };

        self.paused = function () {

            if (playerState) {
                return playerState.paused;
            }

            return false;
        };

        self.cleanup = function (destroyRenderer) {

            if (destroyRenderer !== false) {
                AndroidVlcPlayer.destroyVlc();
            }

            playerState = {};
        };

        self.enableCustomVideoControls = function () {

            return false;
        };

        var posterUrl;
        self.setPoster = function (url) {
            posterUrl = url;
        };

        self.report = function (eventName, duration, position, isPaused, volume) {

            var state = playerState;

            state.duration = duration;
            state.currentTime = position;
            state.paused = isPaused;
            state.volume = (volume || 0) / 100;

            if (eventName == 'playbackstop') {
                onEnded();
            }
            else if (eventName == 'volumechange') {
                onVolumeChange();
            }
            else if (eventName == 'positionchange') {
                onTimeUpdate();
            }
            else if (eventName == 'paused') {
                onPause();
            }
            else if (eventName == 'unpaused') {
                onPlaying();
            }
            else if (eventName == 'playing') {
                onPlaying();
            }
        };

        self.init = function () {

            var deferred = DeferredBuilder.Deferred();
            deferred.resolve();
            return deferred.promise();
        };

        window.AudioRenderer.Current = self;
        window.VideoRenderer.Current = self;
    }

    window.AudioRenderer = function (options) {
        options = options || {};
        options.type = 'audio';

        return new vlcRenderer(options);
    };

    window.VideoRenderer = function (options) {
        options = options || {};
        options.type = 'video';

        return new vlcRenderer(options);
    };

})();