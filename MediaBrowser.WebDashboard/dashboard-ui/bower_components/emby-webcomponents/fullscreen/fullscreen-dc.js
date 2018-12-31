define(['dom', 'fullscreenManager'], function (dom, fullscreenManager) {
    'use strict';

    function isTargetValid(target) {

        if (dom.parentWithTag(target, ['BUTTON', 'INPUT', 'TEXTAREA'])) {
            return false;
        }

        return true;
    }

    dom.addEventListener(window, 'dblclick', function (e) {

        if (isTargetValid(e.target)) {
            if (fullscreenManager.isFullScreen()) {
                fullscreenManager.exitFullscreen();
            } else {
                fullscreenManager.requestFullscreen();
            }
        }

    }, {
        passive: true
    });
});