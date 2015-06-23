(function (window, document, $, setTimeout, clearTimeout) {

    var currentPlayer;

    var currentTimeElement;
    var nowPlayingImageElement;
    var nowPlayingTextElement;
    var nowPlayingUserData;
    var unmuteButton;
    var muteButton;
    var volumeSlider;
    var volumeSliderContainer;
    var isVolumeSliderActive;
    var unpauseButton;
    var pauseButton;
    var positionSlider;
    var isPositionSliderActive;

    var lastPlayerState;

    function getNowPlayingBarHtml() {

        var html = '';

        // add return false because on iOS clicking the bar often ends up clicking the content underneath. 
        html += '<div class="nowPlayingBar" style="display:none;" onclick="return false;">';

        html += '<div class="nowPlayingImage"></div>';
        html += '<div class="nowPlayingText"></div>';

        // The onclicks are needed due to the return false above
        html += '<a class="mediaButton remoteControlButton imageButton" href="nowplaying.html" onclick="Dashboard.navigate(this.href, false, this.getAttribute(\'data-transition\'));return false;" title="' + Globalize.translate('ButtonRemoteControl') + '"><i class="fa fa-tablet"></i></a>';
        html += '<a class="mediaButton playlistButton imageButton" href="nowplaying.html?tab=Playlist" onclick="Dashboard.navigate(this.href, false, this.getAttribute(\'data-transition\'));return false;" title="' + Globalize.translate('ButtonPlaylist') + '"><i class="fa fa-list"></i></a>';

        html += '<button class="mediaButton previousTrackButton imageButton" title="' + Globalize.translate('ButtonPreviousTrack') + '" type="button" data-role="none"><i class="fa fa-step-backward"></i></button>';

        html += '<button class="mediaButton unpauseButton imageButton" title="' + Globalize.translate('ButtonPlay') + '" type="button" data-role="none"><i class="fa fa-play"></i></button>';
        html += '<button class="mediaButton pauseButton imageButton" title="' + Globalize.translate('ButtonPause') + '" type="button" data-role="none"><i class="fa fa-pause"></i></button>';

        html += '<button class="mediaButton stopButton imageButton" title="' + Globalize.translate('ButtonStop') + '" type="button" data-role="none"><i class="fa fa-stop"></i></button>';

        html += '<button class="mediaButton nextTrackButton imageButton" title="' + Globalize.translate('ButtonNextTrack') + '" type="button" data-role="none"><i class="fa fa-step-forward"></i></button>';

        html += '<div id="mediaElement"></div>';

        html += '<div class="positionSliderContainer sliderContainer">';
        html += '<input type="range" class="mediaSlider positionSlider slider" step=".001" min="0" max="100" value="0" style="display:none;" data-mini="true" data-theme="a" data-highlight="true" />';
        html += '</div>';

        html += '<div class="currentTime"></div>';
        html += '<button class="mediaButton muteButton imageButton" title="' + Globalize.translate('ButtonMute') + '" type="button" data-role="none"><i class="fa fa-volume-up"></i></button>';
        html += '<button class="mediaButton unmuteButton imageButton" title="' + Globalize.translate('ButtonUnmute') + '" type="button" data-role="none"><i class="fa fa-volume-off"></i></button>';

        html += '<div class="volumeSliderContainer sliderContainer">';
        html += '<input type="range" class="mediaSlider volumeSlider slider" step=".05" min="0" max="100" value="0" style="display:none;" data-mini="true" data-theme="a" data-highlight="true" />';
        html += '</div>';

        html += '<div class="nowPlayingBarUserDataButtons">';
        html += '</div>';

        html += '</div>';

        return html;
    }

    function bindEvents(elem) {

        currentTimeElement = $('.currentTime', elem);
        nowPlayingImageElement = $('.nowPlayingImage', elem);
        nowPlayingTextElement = $('.nowPlayingText', elem);
        nowPlayingUserData = $('.nowPlayingBarUserDataButtons', elem);

        $(elem).on('swipeup', function () {
            Dashboard.navigate('nowplaying.html');
        });

        unmuteButton = $('.unmuteButton', elem).on('click', function () {

            if (currentPlayer) {
                currentPlayer.unMute();
            }
        });

        muteButton = $('.muteButton', elem).on('click', function () {

            if (currentPlayer) {
                currentPlayer.mute();
            }
        });

        $('.stopButton', elem).on('click', function () {

            if (currentPlayer) {
                currentPlayer.stop();
            }
        });

        pauseButton = $('.pauseButton', elem).on('click', function () {

            if (currentPlayer) {
                currentPlayer.pause();
            }
        });

        unpauseButton = $('.unpauseButton', elem).on('click', function () {

            if (currentPlayer) {
                currentPlayer.unpause();
            }
        });

        $('.nextTrackButton', elem).on('click', function () {

            if (currentPlayer) {
                currentPlayer.nextTrack();
            }
        });

        $('.previousTrackButton', elem).on('click', function () {

            if (currentPlayer) {
                currentPlayer.previousTrack();
            }
        });

        volumeSlider = $('.volumeSlider', elem).on('slidestart', function () {

            isVolumeSliderActive = true;

        }).on('slidestop', function () {

            isVolumeSliderActive = false;

            if (currentPlayer) {
                currentPlayer.setVolume(this.value);
            }
        });

        volumeSliderContainer = $('.volumeSliderContainer', elem);

        positionSlider = $('.positionSlider', elem).on('slidestart', function () {

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

    function getNowPlayingBar() {

        var elem = $('.nowPlayingBar');

        if (elem.length) {
            return elem;
        }

        elem = $(getNowPlayingBarHtml()).insertBefore('#footerNotifications').trigger('create');

        bindEvents(elem);

        return elem;
    }

    function showButton(button) {
        button.removeClass('hide');
    }

    function hideButton(button) {
        button.addClass('hide');
    }

    function updatePlayerState(state) {

        if (state.NowPlayingItem && !$($.mobile.activePage).hasClass('nowPlayingPage')) {
            showNowPlayingBar();
        } else {
            hideNowPlayingBar();
            return;
        }

        lastPlayerState = state;

        if (!muteButton) {
            getNowPlayingBar();
        }

        var playerInfo = MediaController.getPlayerInfo();

        var playState = state.PlayState || {};

        if (playState.IsPaused) {

            hideButton(pauseButton);
            showButton(unpauseButton);

        } else {

            showButton(pauseButton);
            hideButton(unpauseButton);
        }

        updatePlayerVolumeState(state, playerInfo);

        var nowPlayingItem = state.NowPlayingItem || {};
        if (!isPositionSliderActive) {

            if (nowPlayingItem && nowPlayingItem.RunTimeTicks) {

                var pct = playState.PositionTicks / nowPlayingItem.RunTimeTicks;
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

        var timeText = Dashboard.getDisplayTime(playState.PositionTicks);

        if (nowPlayingItem.RunTimeTicks) {

            timeText += " / " + Dashboard.getDisplayTime(nowPlayingItem.RunTimeTicks);

        }

        currentTimeElement.html(timeText);

        updateNowPlayingInfo(state);
    }

    function updatePlayerVolumeState(state, playerInfo) {

        playerInfo = playerInfo || MediaController.getPlayerInfo();

        if (!muteButton) {
            getNowPlayingBar();
        }

        var playState = state.PlayState || {};
        var supportedCommands = playerInfo.supportedCommands;

        var showMuteButton = true;
        var showUnmuteButton = true;
        var showVolumeSlider = true;

        //if (supportedCommands.indexOf('SetVolume') == -1) {
        //    volumeSlider.prop('disabled', 'disabled');
        //} else {
        //    volumeSlider.prop('disabled', '');
        //}

        if (supportedCommands.indexOf('Mute') == -1) {
            showMuteButton = false;
        }

        if (supportedCommands.indexOf('Unmute') == -1) {
            showUnmuteButton = false;
        }

        if (playState.IsMuted) {

            showMuteButton = false;
        } else {

            showUnmuteButton = false;
        }

        if (supportedCommands.indexOf('SetVolume') == -1) {
            showVolumeSlider = false;
        }

        if (playerInfo.isLocalPlayer && AppInfo.hasPhysicalVolumeButtons) {
            showMuteButton = false;
            showUnmuteButton = false;
            showVolumeSlider = false;
        }

        if (showMuteButton) {
            showButton(muteButton);
        } else {
            hideButton(muteButton);
        }

        if (showUnmuteButton) {
            showButton(unmuteButton);
        } else {
            hideButton(unmuteButton);
        }

        if (showVolumeSlider) {
            volumeSliderContainer.show();
        } else {
            volumeSliderContainer.hide();
        }

        if (!isVolumeSliderActive) {
            volumeSlider.val(playState.VolumeLevel || 0);
        }

        volumeSlider.slider('refresh');
    }

    var currentImgUrl;
    function updateNowPlayingInfo(state) {

        var nameHtml = MediaController.getNowPlayingNameHtml(state.NowPlayingItem) || '';

        if (nameHtml.indexOf('<br/>') != -1) {
            nowPlayingTextElement.addClass('nowPlayingDoubleText');
        } else {
            nowPlayingTextElement.removeClass('nowPlayingDoubleText');
        }

        nowPlayingTextElement.html(nameHtml);

        var url;
        var imgHeight = 50;

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

        else if (nowPlayingItem.Type == "TvChannel" || nowPlayingItem.Type == "Recording") {
            url = "css/images/items/detail/tv.png";
        }
        else if (nowPlayingItem.MediaType == "Audio") {
            url = "css/images/items/detail/audio.png";
        }
        else {
            url = "css/images/items/detail/video.png";
        }

        if (url == currentImgUrl) {
            return;
        }

        currentImgUrl = url;

        nowPlayingImageElement.html('<img src="' + url + '" />');

        if (nowPlayingItem.Id) {
            ApiClient.getItem(Dashboard.getCurrentUserId(), nowPlayingItem.Id).done(function (item) {
                nowPlayingUserData.html(LibraryBrowser.getUserDataIconsHtml(item, false));
            });
        }
    }

    function onPlaybackStart(e, state) {

        console.log('nowplaying event: ' + e.type);

        var player = this;

        player.beginPlayerUpdates();

        onStateChanged.call(player, e, state);
    }

    function showNowPlayingBar() {

        var nowPlayingBar = getNowPlayingBar();

        nowPlayingBar.show();
    }

    function hideNowPlayingBar() {

        // Use a timeout to prevent the bar from hiding and showing quickly
        // in the event of a stop->play command

        // Don't call getNowPlayingBar here because we don't want to end up creating it just to hide it
        $('.nowPlayingBar').hide();
    }

    function onPlaybackStopped(e, state) {

        console.log('nowplaying event: ' + e.type);
        var player = this;

        player.endPlayerUpdates();

        hideNowPlayingBar();
    }

    function onStateChanged(e, state) {

        //console.log('nowplaying event: ' + e.type);
        var player = this;

        if (player.isDefaultPlayer && state.NowPlayingItem && state.NowPlayingItem.MediaType == 'Video') {
            return;
        }

        updatePlayerState(state);
    }

    function releaseCurrentPlayer() {

        if (currentPlayer) {

            $(currentPlayer).off('.nowplayingbar');
            currentPlayer.endPlayerUpdates();
            currentPlayer = null;

            hideNowPlayingBar();
        }
    }

    function onVolumeChanged(e) {

        var player = this;

        player.getPlayerState().done(function (state) {

            if (player.isDefaultPlayer && state.NowPlayingItem && state.NowPlayingItem.MediaType == 'Video') {
                return;
            }

            updatePlayerVolumeState(state);
        });
    }

    function bindToPlayer(player) {

        releaseCurrentPlayer();

        currentPlayer = player;

        player.getPlayerState().done(function (state) {

            if (state.NowPlayingItem) {
                player.beginPlayerUpdates();
            }

            onStateChanged.call(player, { type: 'init' }, state);
        });

        $(player).on('playbackstart.nowplayingbar', onPlaybackStart)
            .on('playbackstop.nowplayingbar', onPlaybackStopped)
            .on('volumechange.nowplayingbar', onVolumeChanged)
            .on('playstatechange.nowplayingbar', onStateChanged)
            .on('positionchange.nowplayingbar', onStateChanged);
    }

    Dashboard.ready(function () {

        $(MediaController).on('playerchange', function () {

            bindToPlayer(MediaController.getCurrentPlayer());
        });

        bindToPlayer(MediaController.getCurrentPlayer());
    });

})(window, document, jQuery, setTimeout, clearTimeout);