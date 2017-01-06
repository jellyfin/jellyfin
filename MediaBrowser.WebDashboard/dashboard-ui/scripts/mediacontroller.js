define(['appSettings', 'events'], function (appSettings, events) {
    'use strict';

    function mediaController() {

        var self = this;
        var currentPlayer;

        self.currentPlaylistIndex = function (i) {

            if (i == null) {
                // TODO: Get this implemented in all of the players
                return currentPlayer.currentPlaylistIndex ? currentPlayer.currentPlaylistIndex() : -1;
            }

            currentPlayer.currentPlaylistIndex(i);
        };

        self.removeFromPlaylist = function (i) {
            currentPlayer.removeFromPlaylist(i);
        };

        self.playlist = function () {
            return currentPlayer.playlist || [];
        };
    }
});