(function () {

    function vlcRenderer(type) {

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

        self.setCurrentSrc = function (val) {

            if (!val) {
                self.destroy();
                return;
            }

            if (type == 'audio') {
                AndroidVlcPlayer.playAudioVlc(val);
            } else {
                AndroidVlcPlayer.playVideoVlc(val);
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

        self.destroy = function () {

            AndroidVlcPlayer.destroyVlc();
            playerState = {};
        };

        self.setPoster = function (url) {
        };

        self.report = function (eventName, duration, position, isPaused, volume) {

            var state = playerState;
            console.log('Vlc: ' + eventName + ' - ' + position + ' - ' + duration);
            state.duration = duration;
            state.currentTime = position;
            state.isPaused = isPaused;
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

        window.AudioRenderer.Current = self;
    }

    window.AudioRenderer = vlcRenderer;

})();