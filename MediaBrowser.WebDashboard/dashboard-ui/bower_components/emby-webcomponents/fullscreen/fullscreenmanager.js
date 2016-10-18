define([], function () {
    'use strict';

    function fullscreenManager() {

    }

    fullscreenManager.prototype.requestFullscreen = function (element) {

        element = element || document.documentElement;

        if (element.requestFullscreen) {
            element.requestFullscreen();
        } else if (element.mozRequestFullScreen) {
            element.mozRequestFullScreen();
        } else if (element.webkitRequestFullscreen) {
            element.webkitRequestFullscreen();
        } else if (element.msRequestFullscreen) {
            element.msRequestFullscreen();
        }
    };

    fullscreenManager.prototype.exitFullscreen = function () {

        if (document.exitFullscreen) {
            document.exitFullscreen();
        } else if (document.mozCancelFullScreen) {
            document.mozCancelFullScreen();
        } else if (document.webkitExitFullscreen) {
            document.webkitExitFullscreen();
        } else if (document.webkitCancelFullscreen) {
            document.webkitCancelFullscreen();
        }
    };

    fullscreenManager.prototype.isFullScreen = function () {

        return document.fullscreen || document.mozFullScreen || document.webkitIsFullScreen || document.msFullscreenElement ? true : false;
    };

    return new fullscreenManager();
});