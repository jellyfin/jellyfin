(function () {

    // Handle the volume down button
    //
    function onVolumeDownKeyDown() {

        MediaController.volumeDown();
    }

    // Handle the volume up button
    //
    function onVolumeUpKeyDown() {

        MediaController.volumeUp();
    }

    $(MediaController).on('playerchange', function (e, newPlayer, newTarget) {

        document.removeEventListener("volumedownbutton", onVolumeDownKeyDown, false);
        document.removeEventListener("volumeupbutton", onVolumeUpKeyDown, false);

        if (!newPlayer.localPlayer) {
            document.addEventListener("volumedownbutton", onVolumeDownKeyDown, false);
            document.addEventListener("volumeupbutton", onVolumeUpKeyDown, false);
        }
    });

})();