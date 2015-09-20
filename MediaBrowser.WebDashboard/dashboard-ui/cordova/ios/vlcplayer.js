(function () {

    function vlcRenderer(options) {

        var self = this;

        // Need to use this to fire events because the iOS vlc callbacks always go to the first instance
        window.AudioRenderer.Current = self;

        self.enableProgressReporting = options.type == 'audio';

        function onEnded() {
            Events.trigger(window.AudioRenderer.Current, 'ended');
        }

        function onTimeUpdate() {
            Events.trigger(window.AudioRenderer.Current, 'timeupdate');
        }

        function onVolumeChange() {
            Events.trigger(window.AudioRenderer.Current, 'volumechange');
        }

        function onPlaying() {
            Events.trigger(window.AudioRenderer.Current, 'playing');
        }

        function onPlay() {
            Events.trigger(window.AudioRenderer.Current, 'play');
        }

        function onPause() {
            Events.trigger(window.AudioRenderer.Current, 'pause');
        }

        function onClick() {
            Events.trigger(window.AudioRenderer.Current, 'click');
        }

        function onDblClick() {
            Events.trigger(window.AudioRenderer.Current, 'dblclick');
        }

        function onError() {

            var errorCode = this.error ? this.error.code : '';
            Logger.log('Media element error code: ' + errorCode);

            Events.trigger(window.AudioRenderer.Current, 'error');
        }

        self.playerState = {};

        self.currentTime = function (val) {

            if (val != null) {
                window.audioplayer.seekto(function () {
                    Logger.log('set currentTime succeeded!');
                }, function () {
                    Logger.log('set currentTime failed!');

                }, val / 1000);
                return;
            }

            return self.playerState.currentTime;
        };

        self.duration = function (val) {

            // TODO
            // This value doesn't seem to be getting reported properly
            // Right now it's only used to determine if the player can seek, so for now we can mock it
            return 1;
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
            if (val != null) {
                // TODO
                return;
            }

            return self.playerState.volume;
        };

        self.setCurrentSrc = function (streamInfo, item, mediaSource, tracks) {

            if (!streamInfo) {
                return;
            }

            var val = streamInfo.url;
            var tIndex = val.indexOf('#t=');
            var startPosMs = 0;

            if (tIndex != -1) {
                startPosMs = val.substring(tIndex + 3);
                startPosMs = parseFloat(startPosMs) * 1000;
                val = val.split('#')[0];
            }

            if (options.type == 'audio') {

                //AndroidVlcPlayer.playAudioVlc(val, JSON.stringify(item), JSON.stringify(mediaSource), options.poster);
                var artist = item.ArtistItems && item.ArtistItems.length ? item.ArtistItems[0].Name : null;

                var metadata = {};

                if (item.Name) {
                    metadata.title = item.Name;
                }
                if (artist) {
                    metadata.artist = artist;
                }
                if (item.Overview) {
                    metadata.description = item.Overview;
                }
                if (options.poster) {
                    metadata.image = {
                        url: options.poster
                    };
                    metadata.imageThumbnail = {
                        url: options.poster
                    };
                }

                window.audioplayer.playstream(successHandler, function () {

                    Logger.log('playstream failed!');
                    //onError();
                },
                                              {
                                                  ios: val
                                              },
                                              // metadata used for iOS lock screen, Android 'Now Playing' notification
                                              metadata
                                              );

            } else {

            }

            AudioRenderer.Current.playerState.currentSrc = val;
            reportEvent('playing', {});
        };

        self.currentSrc = function () {
            return self.playerState.currentSrc;
        };

        self.paused = function () {

            return self.playerState.paused;
        };

        self.cleanup = function (destroyRenderer) {

            self.playerState = {};
        };

        function reportEvent(eventName, result) {

            var duration = result.duration || 0;
            var position = result.progress || 0;

            Logger.log('eventName: ' + eventName + '. position: ' + position);

            var state = AudioRenderer.Current.playerState;

            state.duration = duration;
            state.currentTime = position;
            state.paused = result.state == 3 || eventName == 'paused';
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

        function errorHandler() {
            onError();
        }

        self.init = function () {

            var deferred = DeferredBuilder.Deferred();

            window.audioplayer.configure(successHandler, errorHandler);

            setTimeout(function () {
                deferred.resolve();
            }, 500);

            return deferred.promise();
        };
    }

    window.AudioRenderer = function (options) {
        options = options || {};
        options.type = 'audio';

        return new vlcRenderer(options);
    };

})();