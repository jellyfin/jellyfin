define(['playbackManager'], function (playbackManager) {
    "use strict";

    return function () {

        var self = this;

        self.name = 'Playback validation';
        self.type = 'preplayintercept';
        self.id = 'playbackvalidation';
        self.order = -1;

        self.intercept = function (options) {

            // Don't care about video backdrops, or theme music or any kind of non-fullscreen playback
            if (!options.fullscreen) {
                return Promise.resolve();
            }

            return validatePlayback(options);
        };

        function validatePlayback(options) {
            return new Promise(function (resolve, reject) {

                require(["registrationServices"], function (registrationServices) {
                    registrationServices.validateFeature('playback', options).then(function (result) {

                        if (result && result.enableTimeLimit) {
                            startAutoStopTimer();
                        }
                        resolve();
                    });
                });
            });
        }

        var autoStopTimeout;
        var lockedTimeLimitMs = 63000;

        function startAutoStopTimer() {
            stopAutoStopTimer();
            autoStopTimeout = setTimeout(onAutoStopTimeout, lockedTimeLimitMs);
        }

        function onAutoStopTimeout() {
            stopAutoStopTimer();
            playbackManager.stop();
        }

        function stopAutoStopTimer() {

            var timeout = autoStopTimeout;
            if (timeout) {
                clearTimeout(timeout);
                autoStopTimeout = null;
            }
        }
    };
});