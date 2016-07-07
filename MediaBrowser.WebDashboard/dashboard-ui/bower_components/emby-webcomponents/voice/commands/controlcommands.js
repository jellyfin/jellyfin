define(['playbackManager'], function (playbackManager) {

    function setActiveDevice(name) {
        return function () {
            playbackManager.trySetActiveDeviceName(name);
        };
    }

    return function (result) {

        if (result.properties.devicename) {
            return setActiveDevice(result.properties.devicename);
        }
        return;
    }
});