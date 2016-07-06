define(['playbackManager'], function (playbackManager) {

    return function (result) {
        result.success = true;
        if (result.properties.devicename) {
            playbackManager.trySetActiveDeviceName(result.properties.devicename);
        }
        return;
    }
});