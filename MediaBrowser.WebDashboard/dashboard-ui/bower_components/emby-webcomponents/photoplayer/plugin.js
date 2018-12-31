define(['browser', 'require', 'events', 'apphost', 'loading', 'dom', 'playbackManager', 'appRouter', 'appSettings', 'connectionManager'], function (browser, require, events, appHost, loading, dom, playbackManager, appRouter, appSettings, connectionManager) {
    "use strict";

    function PhotoPlayer() {

        var self = this;

        self.name = 'Photo Player';
        self.type = 'mediaplayer';
        self.id = 'photoplayer';

        // Let any players created by plugins take priority
        self.priority = 1;
    }

    PhotoPlayer.prototype.play = function (options) {

        return new Promise(function (resolve, reject) {

            require(['slideshow'], function (slideshow) {

                var index = options.startIndex || 0;

                var newSlideShow = new slideshow({
                    showTitle: false,
                    cover: false,
                    items: options.items,
                    startIndex: index,
                    interval: 11000,
                    interactive: true
                });

                newSlideShow.show();

                resolve();
            });
        });
    };

    PhotoPlayer.prototype.canPlayMediaType = function (mediaType) {

        return (mediaType || '').toLowerCase() === 'photo';
    };

    return PhotoPlayer;
});