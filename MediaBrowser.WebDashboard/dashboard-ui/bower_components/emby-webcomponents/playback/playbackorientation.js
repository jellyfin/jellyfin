define(["playbackManager", "layoutManager", "events"], function(playbackManager, layoutManager, events) {
    "use strict";

    function onOrientationChangeSuccess() {
        orientationLocked = !0
    }

    function onOrientationChangeError(err) {
        orientationLocked = !1, console.log("error locking orientation: " + err)
    }
    var orientationLocked;
    events.on(playbackManager, "playbackstart", function(e, player, state) {
        if (player.isLocalPlayer && !player.isExternalPlayer && playbackManager.isPlayingVideo(player) && layoutManager.mobile) {
            var lockOrientation = screen.lockOrientation || screen.mozLockOrientation || screen.msLockOrientation || screen.orientation && screen.orientation.lock;
            if (lockOrientation) try {
                var promise = lockOrientation("landscape");
                promise.then ? promise.then(onOrientationChangeSuccess, onOrientationChangeError) : orientationLocked = promise
            } catch (err) {
                onOrientationChangeError(err)
            }
        }
    }), events.on(playbackManager, "playbackstop", function(e, playbackStopInfo) {
        if (orientationLocked && !playbackStopInfo.nextMediaType) {
            var unlockOrientation = screen.unlockOrientation || screen.mozUnlockOrientation || screen.msUnlockOrientation || screen.orientation && screen.orientation.unlock;
            if (unlockOrientation) {
                try {
                    unlockOrientation()
                } catch (err) {
                    console.log("error unlocking orientation: " + err)
                }
                orientationLocked = !1
            }
        }
    })
});