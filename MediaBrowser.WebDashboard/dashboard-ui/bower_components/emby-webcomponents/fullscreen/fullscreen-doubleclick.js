define(['dom', 'fullscreenManager'], function (dom, fullscreenManager) {

    dom.addEventListener(window, 'dblclick', function () {

        if (fullscreenManager.isFullScreen()) {
            fullscreenManager.exitFullscreen();
        } else {
            fullscreenManager.requestFullscreen();
        }

    }, {
        passive: true
    });
});