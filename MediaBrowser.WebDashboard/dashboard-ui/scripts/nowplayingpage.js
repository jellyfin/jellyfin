(function (window, document, $, setTimeout, clearTimeout) {

    var currentPlayer;

    function bindEvents(page) {

        $('.radioTabButton', page).on('change', function () {

            var elem = $('.' + this.value, page);
            elem.siblings('.tabContent').hide();

            elem.show();
        });

        $('.btnCommand', page).on('click', function () {

            currentPlayer.sendCommand({
                Name: this.getAttribute('data-command')
            });
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
    }

    function onStateChanged(e, state) {

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

    }).on('pageshow', "#nowPlayingPage", function () {

        var page = this;

        $('.radioTabButton', page).checked(false).checkboxradio('refresh');
        $('.radioTabButton:first', page).checked(true).checkboxradio('refresh').trigger('change');

        $(function () {

            $(MediaController).on('playerchange.nowplayingpage', function () {

                bindToPlayer(page, MediaController.getCurrentPlayer());
            });

            bindToPlayer(page, MediaController.getCurrentPlayer());

        });

    }).on('pagehide', "#nowPlayingPage", function () {

        releaseCurrentPlayer();

        $(MediaController).off('playerchange.nowplayingpage');
    });

})(window, document, jQuery, setTimeout, clearTimeout);