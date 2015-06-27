(function () {

    // Reports media playback to the device for lock screen control

    var currentPlayer;
    var lastPlayerState;

    function updatePlayerState(state) {

        if (!state.NowPlayingItem) {
            hideNowPlayingBar();
            return;
        }

        lastPlayerState = state;

        var playState = state.PlayState || {};

        var nameHtml = MediaController.getNowPlayingNameHtml(state.NowPlayingItem) || '';
        var parts = nameHtml.split('<br/>');

        var artist = parts.length == 1 ? '' : parts[0];
        var title = parts[parts.length - 1];
        var album = state.NowPlayingItem.Album || '';
        var duration = state.NowPlayingItem.RunTimeTicks ? (state.NowPlayingItem.RunTimeTicks / 10000000) : 0;
        var elapsedTime = playState.PositionTicks ? (playState.PositionTicks / 10000000) : 0;

        var url = '';
        var imgHeight = 100;

        var nowPlayingItem = state.NowPlayingItem;

        if (nowPlayingItem.PrimaryImageTag) {

            url = ApiClient.getScaledImageUrl(nowPlayingItem.PrimaryImageItemId, {
                type: "Primary",
                height: imgHeight,
                tag: nowPlayingItem.PrimaryImageTag
            });
        }
        else if (nowPlayingItem.BackdropImageTag) {

            url = ApiClient.getScaledImageUrl(nowPlayingItem.BackdropItemId, {
                type: "Backdrop",
                height: imgHeight,
                tag: nowPlayingItem.BackdropImageTag,
                index: 0
            });

        } else if (nowPlayingItem.ThumbImageTag) {

            url = ApiClient.getScaledImageUrl(nowPlayingItem.ThumbImageItemId, {
                type: "Thumb",
                height: imgHeight,
                tag: nowPlayingItem.ThumbImageTag
            });
        }

        var params = [artist, title, album, url, duration, elapsedTime];
        try {
            window.remoteControls.updateMetas(onUpdateMetasSuccess, onUpdateMetasFail, params);
        } catch (err) {
            onUpdateMetasFail(err);
        }
    }

    function onStateChanged(e, state) {

        updatePlayerState(state);
    }

    function onPlaybackStart(e, state) {

        Logger.log('nowplaying event: ' + e.type);

        var player = this;

        player.beginPlayerUpdates();

        onStateChanged.call(player, e, state);
    }

    function onPlaybackStopped(e, state) {

        Logger.log('nowplaying event: ' + e.type);
        var player = this;

        player.endPlayerUpdates();

        hideNowPlayingBar();
    }

    function releaseCurrentPlayer() {

        if (currentPlayer) {

            $(currentPlayer).off('.cordovaremote');
            currentPlayer.endPlayerUpdates();
            currentPlayer = null;

            hideNowPlayingBar();
        }
    }

    function hideNowPlayingBar() {

        var artist = "";
        var title = "";
        var album = "";
        var image = "";
        var duration = 0;
        var elapsedTime = 0;

        var params = [artist, title, album, image, duration, elapsedTime];

        try {
            window.remoteControls.updateMetas(onUpdateMetasSuccess, onUpdateMetasFail, params);
        } catch (err) {
            onUpdateMetasFail(err);
        }
    }

    function onUpdateMetasSuccess() {

        Logger.log('onUpdateMetasSuccess');
    }

    function onUpdateMetasFail(fail) {

        Logger.log('onUpdateMetasFail: ' + fail);
    }

    function bindToPlayer(player) {

        releaseCurrentPlayer();

        currentPlayer = player;

        if (!player.isLocalPlayer) {
            return;
        }

        Logger.log('binding remotecontrols to MediaPlayer');

        player.getPlayerState().done(function (state) {

            if (state.NowPlayingItem) {
                player.beginPlayerUpdates();
            }

            onStateChanged.call(player, { type: 'init' }, state);
        });

        $(player).on('playbackstart.cordovaremote', onPlaybackStart)
            .on('playbackstop.cordovaremote', onPlaybackStopped)
            .on('playstatechange.cordovaremote', onStateChanged)
            .on('positionchange.cordovaremote', onStateChanged);
    }

    Dashboard.ready(function () {

        Logger.log('binding remotecontrols to MediaController');

        $(MediaController).on('playerchange', function () {

            bindToPlayer(MediaController.getCurrentPlayer());
        });

        bindToPlayer(MediaController.getCurrentPlayer());

    });

})();