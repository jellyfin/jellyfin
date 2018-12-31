define(['events', 'dom'], function (events, dom) {
    'use strict';

    function fullscreenManager() {

    }

    fullscreenManager.prototype.requestFullscreen = function (element) {

        element = element || document.documentElement;

        if (element.requestFullscreen) {
            element.requestFullscreen();
            return;
        } else if (element.mozRequestFullScreen) {
            element.mozRequestFullScreen();
            return;
        } else if (element.webkitRequestFullscreen) {
            element.webkitRequestFullscreen();
            return;
        } else if (element.msRequestFullscreen) {
            element.msRequestFullscreen();
            return;
        }

        // Hack - This is only available for video elements in ios safari
        if (element.tagName !== 'VIDEO') {
            element = document.querySelector('video') || element;
        }
        if (element.webkitEnterFullscreen) {
            element.webkitEnterFullscreen();
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
        } else if (document.msExitFullscreen) {
            document.msExitFullscreen();
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