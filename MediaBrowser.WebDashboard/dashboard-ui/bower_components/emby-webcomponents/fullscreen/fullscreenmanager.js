define(['events', 'dom'], function (events, dom) {
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

    var manager = new fullscreenManager();

    function onFullScreenChange() {
        events.trigger(manager, 'fullscreenchange');
    }

    dom.addEventListener(document, 'fullscreenchange', onFullScreenChange, {
        passive: true
    });

    dom.addEventListener(document, 'webkitfullscreenchange', onFullScreenChange, {
        passive: true
    });

    dom.addEventListener(document, 'mozfullscreenchange', onFullScreenChange, {
        passive: true
    });

    return manager;
});