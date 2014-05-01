(function (window, document, $, setTimeout, clearTimeout) {

    var currentPlayer;
    var lastPlayerState;
    var isPositionSliderActive;

    function bindEvents(page) {

        $('.radioTabButton', page).on('change', function () {

            var elem = $('.' + this.value, page);
            elem.siblings('.tabContent').hide();

            elem.show();
        });

        $('.btnCommand,.btnToggleFullscreen', page).on('click', function () {

            MediaController.sendCommand({
                Name: this.getAttribute('data-command')
                
            }, currentPlayer);
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

        $('.positionSlider', page).on('slidestart', function () {

            isPositionSliderActive = true;

        }).on('slidestop', function () {

            isPositionSliderActive = false;

            if (currentPlayer && lastPlayerState) {

                var newPercent = parseFloat(this.value);
                var newPositionTicks = (newPercent / 100) * lastPlayerState.NowPlayingItem.RunTimeTicks;

                currentPlayer.seek(Math.floor(newPositionTicks));
            }
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

        onStateChanged.call(player, e, {});
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

        $('.btnAudioTracks', page).buttonEnabled(false);
        $('.btnSubtitles', page).buttonEnabled(false);
        $('.btnChapters', page).buttonEnabled(false);

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

        if (!isPositionSliderActive) {

            var positionSlider = $('.positionSlider', page);

            if (item && item.RunTimeTicks) {

                var pct = playState.PositionTicks / item.RunTimeTicks;
                pct *= 100;

                positionSlider.val(pct);

            } else {

                positionSlider.val(0);
            }
            
            if (playState.CanSeek) {
                positionSlider.slider("enable");
            } else {
                positionSlider.slider("disable");
            }

            positionSlider.slider('refresh');
        }

        if (playState.PositionTicks == null) {
            $('.positionTime', page).html('--:--');
        } else {
            $('.positionTime', page).html(Dashboard.getDisplayTime(playState.PositionTicks));
        }

        if (item && item.RunTimeTicks != null) {
            $('.runtime', page).html(Dashboard.getDisplayTime(item.RunTimeTicks));
        } else {
            $('.runtime', page).html('--:--');
        }

        if (item && item.MediaType == 'Video') {
            $('.videoButton', page).css('visibility', 'visible');
        } else {
            $('.videoButton', page).css('visibility', 'hidden');
        }
        
        updateNowPlayingInfo(page, state);
    }

    var currentImgUrl;
    function updateNowPlayingInfo(page, state) {

        var item = state.NowPlayingItem;

        $('.itemName', page).html(item ? MediaPlayer.getNowPlayingNameHtml(state) : '');

        var url;

        if (!item) {
        }
        else if (item.PrimaryImageTag) {

            url = ApiClient.getImageUrl(item.PrimaryImageItemId, {
                type: "Primary",
                height: 600,
                tag: item.PrimaryImageTag
            });
        }
        else if (item.BackdropImageTag) {

            url = ApiClient.getImageUrl(item.BackdropItemId, {
                type: "Backdrop",
                height: 600,
                tag: item.BackdropImageTag,
                index: 0
            });

        } else if (item.ThumbImageTag) {

            url = ApiClient.getImageUrl(item.ThumbImageItemId, {
                type: "Thumb",
                height: 600,
                tag: item.ThumbImageTag
            });
        }

        if (url == currentImgUrl) {
            return;
        }

        setImageUrl(page, url);
    }
    
    function setImageUrl(page, url) {
        currentImgUrl = url;

        $('.nowPlayingPageImage', page).html(url ? '<img src="' + url + '" />' : '');
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

            if (state.NowPlayingItem) {
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

        lastPlayerState = null;
    });

})(window, document, jQuery, setTimeout, clearTimeout);