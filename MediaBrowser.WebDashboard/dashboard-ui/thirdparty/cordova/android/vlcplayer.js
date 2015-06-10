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
                AndroidVlcPlayer.setPosition(val);
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

        self.pause = function () {
            AndroidVlcPlayer.pause();
        };

        self.unpause = function () {
            AndroidVlcPlayer.unpause();
        };

        self.volume = function (val) {
            if (playerState) {
                if (val != null) {
                    AndroidVlcPlayer.setVolume(val);
                    return;
                }

                return playerState.volume;
            }
        };

        self.setCurrentSrc = function (val) {

            if (!val) {
                self.destroy();
            }

            if (type == 'audio') {
                AndroidVlcPlayer.playAudioVlc(val);
            } else {
                AndroidVlcPlayer.playVideoVlc(val);
            }
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

            AndroidVlcPlayer.destroy();
            playerState = {};
        };

        self.setPoster = function (url) {
        };
    }

    window.AudioRenderer = vlcRenderer;

})();