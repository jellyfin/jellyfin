define(['playbackManager', 'layoutManager', 'events'], function (playbackManager, layoutManager, events) {
    "use strict";

    var orientationLocked;

    function onOrientationChangeSuccess() {
        orientationLocked = true;
    }

    function onOrientationChangeError(err) {
        orientationLocked = false;
        console.log('error locking orientation: ' + err);
    }

    events.on(playbackManager, 'playbackstart', function (e, player, state) {

        var isLocalVideo = player.isLocalPlayer && !player.isExternalPlayer && playbackManager.isPlayingVideo(player);

        if (isLocalVideo && layoutManager.mobile) {
            var lockOrientation = screen.lockOrientation || screen.mozLockOrientation || screen.msLockOrientation || (screen.orientation && screen.orientation.lock);

            if (lockOrientation) {

                try {
                    var promise = lockOrientation('landscape');
                    if (promise.then) {
                        promise.then(onOrientationChangeSuccess, onOrientationChangeError);
                    } else {
                        // returns a boolean
                        orientationLocked = promise;
                    }
                }
                catch (err) {
                    onOrientationChangeError(err);
                }
            }
        }
    });

    events.on(playbackManager, 'playbackstop', function (e, playbackStopInfo) {

        if (orientationLocked && !playbackStopInfo.nextMediaType) {

            var unlockOrientation = screen.unlockOrientation || screen.mozUnlockOrientation || screen.msUnlockOrientation || (screen.orientation && screen.orientation.unlock);

            if (unlockOrientation) {
                try {
                    unlockOrientation();
                }
                catch (err) {
                    console.log('error unlocking orientation: ' + err);
                }
                orientationLocked = false;
            }
        }
    });
});