(function () {

    function vlcRenderer(options) {

        var self = this;

        self.enableProgressReporting = options.type == 'audio';

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
                window.audioplayer.seekto(function () {
                    Logger.log('set currentTime succeeded!');
                }, function () {
                    Logger.log('set currentTime failed!');

                }, val / 1000);
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
            window.audioplayer.stop(function (result) {
                Logger.log('Stop succeeded!');
                reportEvent('playbackstop', result);

            }, function () {
                Logger.log('Stop failed!');
            });
        };

        self.pause = function () {
            window.audioplayer.pause(function (result) {
                Logger.log('Pause succeeded!');
                reportEvent('sepaused', result);

            }, function () {
                Logger.log('Pause failed!');
            });
        };

        self.unpause = function () {
            window.audioplayer.pause(function (result) {
                Logger.log('Unpause succeeded!');
                reportEvent('playing', result);

            }, function () {
                Logger.log('Unpause failed!');
            });
        };

        self.volume = function (val) {
            if (playerState) {
                if (val != null) {
                    // TODO
                    return;
                }

                return playerState.volume;
            }
        };

        self.setCurrentSrc = function (val, item, mediaSource, tracks) {

            if (!val) {
                return;
            }

            var tIndex = val.indexOf('#t=');
            var startPosMs = 0;

            if (tIndex != -1) {
                startPosMs = val.substring(tIndex + 3);
                startPosMs = parseFloat(startPosMs) * 1000;
                val = val.split('#')[0];
            }

            if (options.type == 'audio') {

                // TODO

                //AndroidVlcPlayer.playAudioVlc(val, JSON.stringify(item), JSON.stringify(mediaSource), options.poster);
                var artist = item.ArtistItems && item.ArtistItems.length ? item.ArtistItems[0].Name : null;
                window.audioplayer.playstream(successHandler, function () {

                    Logger.log('playstream failed!');
                    //onError();
                },
                                              {
                                                  ios: val
                                              },
                                              // metadata used for iOS lock screen, Android 'Now Playing' notification
                                              {
                                                  "title": item.Name,
                                                  "artist": artist,
                                                  "image": {
                                                      "url": options.poster
                                                  },
                                                  "imageThumbnail": {
                                                      "url": options.poster
                                                  },
                                                  "name": item.Name,
                                                  "description": item.Overview
                                              }
                                              );

            } else {

            }

            playerState.currentSrc = val;
            reportEvent('playing', {});
        };

        self.currentSrc = function () {
            return playerState.currentSrc;
        };

        self.paused = function () {

            if (playerState) {
                return playerState.paused;
            }

            return false;
        };

        self.cleanup = function (destroyRenderer) {

            playerState = {};
        };

        self.enableCustomVideoControls = function () {

            return false;
        };

        function reportEvent(eventName, result) {

            var duration = result.duration || 0;
            var position = result.progress || 0;

            Logger.log('eventName: ' + eventName + '. position: ' + position);

            var isPaused = result.state == 3 || eventName == 'paused';

            var state = playerState;

            state.duration = duration;
            state.currentTime = position;
            state.paused = isPaused;
            state.volume = 0;

            if (eventName == 'playbackstop') {
                onEnded();
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
        }

        function successHandler(result) {

            if (!result) {
                return;
            }

            if (result.state == 4 || result.state == 6) {
                reportEvent('playbackstop', result);
            }
            else {

                var eventName = 'positionchange';
                reportEvent(eventName, result);
            }
        }

        self.init = function () {

            var deferred = DeferredBuilder.Deferred();

            window.audioplayer.configure(function () {
                Logger.log('audioplayer.configure success');
                deferred.resolve();
            }, function () {
                Logger.log('audioplayer.configure error');
                deferred.resolve();
            });
            return deferred.promise();
        };
    }

    window.AudioRenderer = function (options) {
        options = options || {};
        options.type = 'audio';

        return new vlcRenderer(options);
    };

})();