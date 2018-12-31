define([], function () {
    'use strict';

    var currentId = 0;
    function addUniquePlaylistItemId(item) {

        if (!item.PlaylistItemId) {

            item.PlaylistItemId = "playlistItem" + currentId;
            currentId++;
        }
    }

    function findPlaylistIndex(playlistItemId, list) {

        for (var i = 0, length = list.length; i < length; i++) {
            if (list[i].PlaylistItemId === playlistItemId) {
                return i;
            }
        }

        return -1;
    }

    function PlayQueueManager() {

        this._playlist = [];
        this._repeatMode = 'RepeatNone';
    }

    PlayQueueManager.prototype.getPlaylist = function () {
        return this._playlist.slice(0);
    };

    PlayQueueManager.prototype.setPlaylist = function (items) {

        items = items.slice(0);

        for (var i = 0, length = items.length; i < length; i++) {

            addUniquePlaylistItemId(items[i]);
        }

        this._currentPlaylistItemId = null;
        this._playlist = items;
        this._repeatMode = 'RepeatNone';
    };

    PlayQueueManager.prototype.queue = function (items) {

        for (var i = 0, length = items.length; i < length; i++) {

            addUniquePlaylistItemId(items[i]);

            this._playlist.push(items[i]);
        }
    };

    function arrayInsertAt(destArray, pos, arrayToInsert) {
        var args = [];
        args.push(pos);                           // where to insert
        args.push(0);                             // nothing to remove
        args = args.concat(arrayToInsert);        // add on array to insert
        destArray.splice.apply(destArray, args);  // splice it in
    }

    PlayQueueManager.prototype.queueNext = function (items) {

        var i, length;

        for (i = 0, length = items.length; i < length; i++) {

            addUniquePlaylistItemId(items[i]);
        }

        var currentIndex = this.getCurrentPlaylistIndex();

        if (currentIndex === -1) {
            currentIndex = this._playlist.length;
        } else {
            currentIndex++;
        }

        arrayInsertAt(this._playlist, currentIndex, items);
    };

    PlayQueueManager.prototype.getCurrentPlaylistIndex = function () {

        return findPlaylistIndex(this.getCurrentPlaylistItemId(), this._playlist);
    };

    PlayQueueManager.prototype.getCurrentItem = function () {

        var index = findPlaylistIndex(this.getCurrentPlaylistItemId(), this._playlist);

        return index === -1 ? null : this._playlist[index];
    };

    PlayQueueManager.prototype.getCurrentPlaylistItemId = function () {
        return this._currentPlaylistItemId;
    };

    PlayQueueManager.prototype.setPlaylistState = function (playlistItemId, playlistIndex) {

        this._currentPlaylistItemId = playlistItemId;
    };

    PlayQueueManager.prototype.setPlaylistIndex = function (playlistIndex) {

        if (playlistIndex < 0) {
            this.setPlaylistState(null);
        } else {
            this.setPlaylistState(this._playlist[playlistIndex].PlaylistItemId);
        }
    };

    PlayQueueManager.prototype.removeFromPlaylist = function (playlistItemIds) {

        var playlist = this.getPlaylist();

        if (playlist.length <= playlistItemIds.length) {
            return {
                result: 'empty'
            };
        }

        var currentPlaylistItemId = this.getCurrentPlaylistItemId();
        var isCurrentIndex = playlistItemIds.indexOf(currentPlaylistItemId) !== -1;

        this._playlist = playlist.filter(function (item) {
            return playlistItemIds.indexOf(item.PlaylistItemId) === -1;
        });

        return {
            result: 'removed',
            isCurrentIndex: isCurrentIndex
        };
    };

    function moveInArray(array, from, to) {
        array.splice(to, 0, array.splice(from, 1)[0]);
    }

    PlayQueueManager.prototype.movePlaylistItem = function (playlistItemId, newIndex) {

        var playlist = this.getPlaylist();

        var oldIndex;
        for (var i = 0, length = playlist.length; i < length; i++) {
            if (playlist[i].PlaylistItemId === playlistItemId) {
                oldIndex = i;
                break;
            }
        }

        if (oldIndex === -1 || oldIndex === newIndex) {
            return {
                result: 'noop'
            };
        }

        if (newIndex >= playlist.length) {
            throw new Error('newIndex out of bounds');
        }

        moveInArray(playlist, oldIndex, newIndex);

        this._playlist = playlist;

        return {
            result: 'moved',
            playlistItemId: playlistItemId,
            newIndex: newIndex
        };
    };

    PlayQueueManager.prototype.reset = function () {

        this._playlist = [];
        this._currentPlaylistItemId = null;
        this._repeatMode = 'RepeatNone';
    };

    PlayQueueManager.prototype.setRepeatMode = function (value) {

        this._repeatMode = value;
    };

    PlayQueueManager.prototype.getRepeatMode = function () {

        return this._repeatMode;
    };

    PlayQueueManager.prototype.getNextItemInfo = function () {

        var newIndex;
        var playlist = this.getPlaylist();
        var playlistLength = playlist.length;

        switch (this.getRepeatMode()) {

            case 'RepeatOne':
                newIndex = this.getCurrentPlaylistIndex();
                break;
            case 'RepeatAll':
                newIndex = this.getCurrentPlaylistIndex() + 1;
                if (newIndex >= playlistLength) {
                    newIndex = 0;
                }
                break;
            default:
                newIndex = this.getCurrentPlaylistIndex() + 1;
                break;
        }

        if (newIndex < 0 || newIndex >= playlistLength) {
            return null;
        }

        var item = playlist[newIndex];

        if (!item) {
            return null;
        }

        return {
            item: item,
            index: newIndex
        };
    };

    return PlayQueueManager;
});