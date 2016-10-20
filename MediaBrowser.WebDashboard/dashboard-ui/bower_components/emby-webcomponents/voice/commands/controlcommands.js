define(['playbackManager'], function (playbackManager) {
    'use strict';

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
    };
});