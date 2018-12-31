define(['playbackManager', 'nowPlayingHelper', 'events', 'connectionManager'], function (playbackManager, nowPlayingHelper, events, connectionManager) {
    "use strict";

    // Reports media playback to the device for lock screen control

    var currentPlayer;
    var lastUpdateTime = 0;

    function seriesImageUrl(item, options) {

        if (item.Type !== 'Episode') {
            return null;
        }

        options = options || {};
        options.type = options.type || "Primary";

        if (options.type === 'Primary') {

            if (item.SeriesPrimaryImageTag) {

                options.tag = item.SeriesPrimaryImageTag;

                return connectionManager.getApiClient(item.ServerId).getScaledImageUrl(item.SeriesId, options);
            }
        }

        if (options.type === 'Thumb') {

            if (item.SeriesThumbImageTag) {

                options.tag = item.SeriesThumbImageTag;

                return connectionManager.getApiClient(item.ServerId).getScaledImageUrl(item.SeriesId, options);
            }
            if (item.ParentThumbImageTag) {

                options.tag = item.ParentThumbImageTag;

                return connectionManager.getApiClient(item.ServerId).getScaledImageUrl(item.ParentThumbItemId, options);
            }
        }

        return null;
    }

    function imageUrl(item, options) {

        options = options || {};
        options.type = options.type || "Primary";

        if (item.ImageTags && item.ImageTags[options.type]) {

            options.tag = item.ImageTags[options.type];
            return connectionManager.getApiClient(item.ServerId).getScaledImageUrl(item.Id, options);
        }

        if (item.AlbumId && item.AlbumPrimaryImageTag) {

            options.tag = item.AlbumPrimaryImageTag;
            return connectionManager.getApiClient(item.ServerId).getScaledImageUrl(item.AlbumId, options);
        }

        return null;
    }

    function pushImageUrl(item, height, list) {

        var imageOptions = {
            height: height
        };

        var url = seriesImageUrl(item, imageOptions) || imageUrl(item, imageOptions);
        if (url) {
            list.push({ src: url, sizes: height + 'x' + height });
        }
    }

    function getImageUrls(item) {

        var list = [];

        pushImageUrl(item, 96, list);
        pushImageUrl(item, 128, list);
        pushImageUrl(item, 192, list);
        pushImageUrl(item, 256, list);
        pushImageUrl(item, 384, list);
        pushImageUrl(item, 512, list);

        return list;
    }

    function updatePlayerState(player, state, eventName) {

        var item = state.NowPlayingItem;

        if (!item) {
            hideMediaControls();
            return;
        }

        var playState = state.PlayState || {};

        var parts = nowPlayingHelper.getNowPlayingNames(item);

        var artist = parts.length === 1 ? '' : parts[0].text;
        var title = parts[parts.length - 1].text;

        var isVideo = item.MediaType === 'Video';

        // Switch these two around for video
        if (isVideo && parts.length > 1) {
            var temp = artist;
            artist = title;
            title = temp;
        }

        var albumArtist;

        if (item.AlbumArtists && item.AlbumArtists[0]) {
            albumArtist = item.AlbumArtists[0].Name;
        }

        var album = item.Album || '';
        var itemId = item.Id;

        // Convert to ms
        var duration = parseInt(item.RunTimeTicks ? (item.RunTimeTicks / 10000) : 0);
        var currentTime = parseInt(playState.PositionTicks ? (playState.PositionTicks / 10000) : 0);

        var isPaused = playState.IsPaused || false;
        var canSeek = playState.CanSeek || false;

        navigator.mediaSession.metadata = new MediaMetadata({
            title: title,
            artist: artist,
            album: album,
            artwork: getImageUrls(item),
            albumArtist: albumArtist,
            currentTime: currentTime,
            duration: duration,
            paused: isPaused,
            itemId: itemId,
            mediaType: item.MediaType
        });
    }

    function onGeneralEvent(e) {

        var player = this;
        var state = playbackManager.getPlayerState(player);

        updatePlayerState(player, state, e.type);
    }

    function onStateChanged(e, state) {

        var player = this;
        updatePlayerState(player, state, 'statechange');
    }

    function onPlaybackStart(e, state) {

        var player = this;

        updatePlayerState(player, state, e.type);
    }

    function onPlaybackStopped(e, state) {

        var player = this;

        hideMediaControls();
    }

    function releaseCurrentPlayer() {

        if (currentPlayer) {

            events.off(currentPlayer, 'playbackstart', onPlaybackStart);
            events.off(currentPlayer, 'playbackstop', onPlaybackStopped);
            events.off(currentPlayer, 'unpause', onGeneralEvent);
            events.off(currentPlayer, 'pause', onGeneralEvent);
            events.off(currentPlayer, 'statechange', onStateChanged);
            events.off(currentPlayer, 'timeupdate', onGeneralEvent);

            currentPlayer = null;

            hideMediaControls();
        }
    }

    function hideMediaControls() {
        navigator.mediaSession.metadata = null;
    }

    function bindToPlayer(player) {

        releaseCurrentPlayer();

        if (!player) {
            return;
        }

        currentPlayer = player;

        var state = playbackManager.getPlayerState(player);
        updatePlayerState(player, state, 'init');

        events.on(currentPlayer, 'playbackstart', onPlaybackStart);
        events.on(currentPlayer, 'playbackstop', onPlaybackStopped);
        events.on(currentPlayer, 'unpause', onGeneralEvent);
        events.on(currentPlayer, 'pause', onGeneralEvent);
        events.on(currentPlayer, 'statechange', onStateChanged);
        events.on(currentPlayer, 'timeupdate', onGeneralEvent);
    }

    function execute(name) {
        playbackManager[name](currentPlayer);
    }

    navigator.mediaSession.setActionHandler('previoustrack', function () {
        execute('previousTrack');
    });

    navigator.mediaSession.setActionHandler('nexttrack', function () {
        execute('nextTrack');
    });

    navigator.mediaSession.setActionHandler('play', function () {
        execute('unpause');
    });

    navigator.mediaSession.setActionHandler('pause', function () {
        execute('pause');
    });

    navigator.mediaSession.setActionHandler('seekbackward', function () {
        execute('rewind');
    });

    navigator.mediaSession.setActionHandler('seekforward', function () {
        execute('fastForward');
    });

    events.on(playbackManager, 'playerchange', function () {

        bindToPlayer(playbackManager.getCurrentPlayer());
    });

    bindToPlayer(playbackManager.getCurrentPlayer());
});