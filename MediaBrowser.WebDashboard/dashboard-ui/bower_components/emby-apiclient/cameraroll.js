define([], function () {
    'use strict';

    function CameraRoll() {

    }

    CameraRoll.prototype.getFiles = function () {

        return Promise.resolve([]);
    };

    return new CameraRoll();

});