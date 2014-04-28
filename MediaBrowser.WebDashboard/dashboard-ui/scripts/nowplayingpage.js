(function (window, document, $, setTimeout, clearTimeout) {

    var currentPlayer;
    var lastPlayerState;

    function bindEvents(page) {

        $('.radioTabButton', page).on('change', function () {

            var elem = $('.' + this.value, page);
            elem.siblings('.tabContent').hide();

            elem.show();
        });

        $('.btnCommand,.btnToggleFullscreen', page).on('click', function () {

            currentPlayer.sendCommand({
                Name: this.getAttribute('data-command')
            });
        });

        $('.btnStop', page).on('click', function () {

            currentPlayer.stop();
        });

        $('.btnPlay', page).on('click', function () {

            currentPlayer.unpause();
        });

        $('.btnPause', page).on('click', function () {

            currentPlayer.pause();
        });

        $('.btnNextTrack', page).on('click', function () {

            currentPlayer.nextTrack();
        });

        $('.btnPreviousTrack', page).on('click', function () {

            currentPlayer.previousTrack();
        });
    }

    function onPlaybackStart(e, state) {

        var player = this;

        player.beginPlayerUpdates();

        onStateChanged.call(player, e, state);
    }

    function onPlaybackStopped(e, state) {

        var player = this;

        player.endPlayerUpdates();

        onStateChanged.call(player, e, state);
    }

    function onStateChanged(e, state) {

        updatePlayerState($.mobile.activePage, state);
    }

    function showButton(button) {
        button.removeClass('hide');
    }

    function hideButton(button) {
        button.addClass('hide');
    }

    function updatePlayerState(page, state) {

        lastPlayerState = state;

        var item = state.NowPlayingItem;

        var playerInfo = MediaController.getPlayerInfo();

        var supportedCommands = playerInfo.supportedCommands;

        $('.btnToggleFullscreen', page).buttonEnabled(item && item.MediaType == 'Video' && supportedCommands.indexOf('ToggleFullscreen') != -1);

        $('.btnAudioTracks', page).buttonEnabled(item != null);
        $('.btnSubtitles', page).buttonEnabled(item != null);
        $('.btnChapters', page).buttonEnabled(item != null);

        $('.btnStop', page).buttonEnabled(item != null);
        $('.btnNextTrack', page).buttonEnabled(item != null);
        $('.btnPreviousTrack', page).buttonEnabled(item != null);
        
        var btnPause = $('.btnPause', page).buttonEnabled(item != null);
        var btnPlay = $('.btnPlay', page).buttonEnabled(item != null);
        
        var playState = state.PlayState || {};

        if (playState.IsPaused) {

            hideButton(btnPause);
            showButton(btnPlay);

        } else {

            showButton(btnPause);
            hideButton(btnPlay);
        }

    }

    function updateSupportedCommands(page, commands) {

        $('.btnCommand', page).each(function () {

            $(this).buttonEnabled(commands.indexOf(this.getAttribute('data-command')) != -1);

        });
    }

    function releaseCurrentPlayer() {

        if (currentPlayer) {

            $(currentPlayer).off('.nowplayingpage');
            currentPlayer.endPlayerUpdates();
            currentPlayer = null;
        }
    }

    function bindToPlayer(page, player) {

        releaseCurrentPlayer();

        currentPlayer = player;

        player.getPlayerState().done(function (state) {

            if (state.itemName) {
                player.beginPlayerUpdates();
            }

            onStateChanged.call(player, { type: 'init' }, state);
        });

        $(player).on('playbackstart.nowplayingpage', onPlaybackStart)
            .on('playbackstop.nowplayingpage', onPlaybackStopped)
            .on('volumechange.nowplayingpage', onStateChanged)
            .on('playstatechange.nowplayingpage', onStateChanged)
            .on('positionchange.nowplayingpage', onStateChanged);

        var playerInfo = MediaController.getPlayerInfo();

        var supportedCommands = playerInfo.supportedCommands;

        updateSupportedCommands(page, supportedCommands);
    }

    $(document).on('pageinit', "#nowPlayingPage", function () {

        var page = this;

        bindEvents(page);

        $(function () {

            $(MediaController).on('playerchange.nowplayingpage', function () {

                bindToPlayer(page, MediaController.getCurrentPlayer());
            });

            bindToPlayer(page, MediaController.getCurrentPlayer());

        });

    }).on('pageshow', "#nowPlayingPage", function () {

        var page = this;

        $('.radioTabButton', page).checked(false).checkboxradio('refresh');
        $('.radioTabButton:first', page).checked(true).checkboxradio('refresh').trigger('change');

    }).on('pagehide', "#nowPlayingPage", function () {

        releaseCurrentPlayer();

        $(MediaController).off('playerchange.nowplayingpage');

        lastPlayerState = null;
    });

})(window, document, jQuery, setTimeout, clearTimeout);