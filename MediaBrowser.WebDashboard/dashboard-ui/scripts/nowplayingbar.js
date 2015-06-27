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
        html += '<div class="nowPlayingBar" style="display:none;">';

        html += '<div class="nowPlayingImage"></div>';
        html += '<div class="nowPlayingText"></div>';

        // The onclicks are needed due to the return false above
        html += '<paper-icon-button icon="tablet-android" onclick="Dashboard.navigate(\'nowplaying.html\', false);" class="mediaButton remoteControlButton"></paper-icon-button>';
        html += '<paper-icon-button icon="view-list" onclick="Dashboard.navigate(\'nowplaying.html?tab=Playlist\', false);" class="mediaButton playlistButton"></paper-icon-button>';

        html += '<paper-icon-button icon="skip-previous" class="previousTrackButton mediaButton"></paper-icon-button>';

        html += '<paper-icon-button icon="play-arrow" class="mediaButton unpauseButton"></paper-icon-button>';
        html += '<paper-icon-button icon="pause" class="mediaButton pauseButton"></paper-icon-button>';

        html += '<paper-icon-button icon="stop" class="stopButton mediaButton"></paper-icon-button>';

        html += '<paper-icon-button icon="skip-next" class="nextTrackButton mediaButton"></paper-icon-button>';

        html += '<div id="mediaElement"></div>';

        html += '<div class="positionSliderContainer sliderContainer">';
        html += '<input type="range" class="mediaSlider positionSlider slider" step=".001" min="0" max="100" value="0" style="display:none;" data-mini="true" data-theme="a" data-highlight="true" />';
        html += '</div>';

        html += '<div class="currentTime"></div>';
        html += '<paper-icon-button icon="volume-up" class="muteButton mediaButton"></paper-icon-button>';
        html += '<paper-icon-button icon="volume-off" class="unmuteButton mediaButton"></paper-icon-button>';

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

        if (state.NowPlayingItem.Id) {
            nameHtml = '<a style="color:inherit;text-decoration:none;" href="' + LibraryBrowser.getHref(state.NowPlayingItem) + '">' + nameHtml + '</a>';
        }

        nowPlayingTextElement.html(nameHtml);

        var url;
        var imgHeight = 60;

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

        var imgHtml = '<img src="' + url + '" />';

        if (state.NowPlayingItem.Id) {
            imgHtml = '<a href="' + LibraryBrowser.getHref(state.NowPlayingItem) + '">' + imgHtml + '</a>';
        }
        nowPlayingImageElement.html(imgHtml);

        if (nowPlayingItem.Id) {
            ApiClient.getItem(Dashboard.getCurrentUserId(), nowPlayingItem.Id).done(function (item) {
                nowPlayingUserData.html(LibraryBrowser.getUserDataIconsHtml(item, false));
            });
        }
    }

    function onPlaybackStart(e, state) {

        Logger.log('nowplaying event: ' + e.type);

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

        Logger.log('nowplaying event: ' + e.type);
        var player = this;

        player.endPlayerUpdates();

        hideNowPlayingBar();
    }

    function onStateChanged(e, state) {

        //Logger.log('nowplaying event: ' + e.type);
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