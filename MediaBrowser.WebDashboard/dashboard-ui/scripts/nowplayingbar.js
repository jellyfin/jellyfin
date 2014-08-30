(function (window, document, $, setTimeout, clearTimeout) {

    var currentPlayer;

    var currentTimeElement;
    var nowPlayingImageElement;
    var nowPlayingTextElement;
    var unmuteButton;
    var muteButton;
    var volumeSlider;
    var isVolumeSliderActive;
    var unpauseButton;
    var pauseButton;
    var positionSlider;
    var isPositionSliderActive;

    var lastPlayerState;

    function getNowPlayingBarHtml() {

        var html = '';

        html += '<div class="nowPlayingBar" style="display:none;">';
        html += '<div style="display:inline-block;width:12px;"></div>';

        html += '<a class="mediaButton remoteControlButton" title="' + Globalize.translate('ButtonRemoteControl') + '" href="nowplaying.html" data-role="button" data-icon="remote" data-iconpos="notext" data-inline="true">' + Globalize.translate('ButtonRemoteControl') + '</a>';

        html += '<a id="playlistButton" class="mediaButton playlistButton" href="playlist.html" data-role="button" data-icon="bullets" data-iconpos="notext" data-inline="true" title="' + Globalize.translate('ButtonPlaylist') + '">' + Globalize.translate('ButtonPlaylist') + '</a>';
        html += '<button id="previousTrackButton" class="mediaButton previousTrackButton" title="' + Globalize.translate('ButtonPreviousTrack') + '" type="button" data-icon="previous-track" data-iconpos="notext" data-inline="true">' + Globalize.translate('ButtonPreviousTrack') + '</button>';

        html += '<button id="playButton" class="mediaButton unpauseButton" title="' + Globalize.translate('ButtonPlay') + '" type="button" data-icon="play" data-iconpos="notext" data-inline="true">' + Globalize.translate('ButtonPlay') + '</button>';
        html += '<button id="pauseButton" class="mediaButton pauseButton" title="' + Globalize.translate('ButtonPause') + '" type="button" data-icon="pause" data-iconpos="notext" data-inline="true">' + Globalize.translate('ButtonPause') + '</button>';

        html += '<button id="stopButton" class="mediaButton stopButton" title="' + Globalize.translate('ButtonStop') + '" type="button" data-icon="stop" data-iconpos="notext" data-inline="true">' + Globalize.translate('ButtonStop') + '</button>';
        html += '<button id="nextTrackButton" class="mediaButton nextTrackButton" title="' + Globalize.translate('ButtonNextTrack') + '" type="button" data-icon="next-track" data-iconpos="notext" data-inline="true">' + Globalize.translate('ButtonNextTrack') + '</button>';

        html += '<div id="mediaElement"></div>';

        html += '<div class="positionSliderContainer sliderContainer">';
        html += '<input type="range" class="mediaSlider positionSlider slider" step=".001" min="0" max="100" value="0" style="display:none;" data-mini="true" data-theme="a" data-highlight="true" />';
        html += '</div>';

        html += '<div class="currentTime"></div>';
        html += '<div class="nowPlayingImage"></div>';
        html += '<div class="nowPlayingText"></div>';

        html += '<button id="muteButton" class="mediaButton muteButton" title="' + Globalize.translate('ButtonMute') + '" type="button" data-icon="audio" data-iconpos="notext" data-inline="true">' + Globalize.translate('ButtonMute') + '</button>';
        html += '<button id="unmuteButton" class="mediaButton unmuteButton" title="' + Globalize.translate('ButtonUnmute') + '" type="button" data-icon="volume-off" data-iconpos="notext" data-inline="true">' + Globalize.translate('ButtonUnmute') + '</button>';

        html += '<div class="volumeSliderContainer sliderContainer">';
        html += '<input type="range" class="mediaSlider volumeSlider slider" step=".05" min="0" max="100" value="0" style="display:none;" data-mini="true" data-theme="a" data-highlight="true" />';
        html += '</div>';

        html += '</div>';

        return html;
    }

    function bindEvents(elem) {

        currentTimeElement = $('.currentTime', elem);
        nowPlayingImageElement = $('.nowPlayingImage', elem);
        nowPlayingTextElement = $('.nowPlayingText', elem);

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

        var supportedCommands = playerInfo.supportedCommands;

        if (supportedCommands.indexOf('SetVolume') == -1) {
            volumeSlider.prop('disabled', 'disabled');
        } else {
            volumeSlider.prop('disabled', '');
        }

        if (supportedCommands.indexOf('Mute') == -1) {
            muteButton.prop('disabled', 'disabled');
        } else {
            muteButton.prop('disabled', '');
        }

        if (supportedCommands.indexOf('Unmute') == -1) {
            unmuteButton.prop('disabled', 'disabled');
        } else {
            unmuteButton.prop('disabled', '');
        }

        var playState = state.PlayState || {};
        
        if (playState.IsMuted) {

            hideButton(muteButton);
            showButton(unmuteButton);

        } else {

            showButton(muteButton);
            hideButton(unmuteButton);
        }

        if (playState.IsPaused) {

            hideButton(pauseButton);
            showButton(unpauseButton);

        } else {

            showButton(pauseButton);
            hideButton(unpauseButton);
        }

        if (!isVolumeSliderActive) {
            volumeSlider.val(playState.VolumeLevel || 0);
        }

        volumeSlider.slider('refresh');

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

    var currentImgUrl;
    function updateNowPlayingInfo(state) {

        var nameHtml = MediaPlayer.getNowPlayingNameHtml(state) || '';

        if (nameHtml.indexOf('<br/>') != -1) {
            nowPlayingTextElement.addClass('nowPlayingDoubleText');
        } else {
            nowPlayingTextElement.removeClass('nowPlayingDoubleText');
        }

        nowPlayingTextElement.html(nameHtml);

        var url;
        
        var nowPlayingItem = state.NowPlayingItem;

        if (nowPlayingItem.PrimaryImageTag) {

            url = ApiClient.getScaledImageUrl(nowPlayingItem.PrimaryImageItemId, {
                type: "Primary",
                height: 40,
                tag: nowPlayingItem.PrimaryImageTag
            });
        }
        else if (nowPlayingItem.BackdropImageTag) {

            url = ApiClient.getScaledImageUrl(nowPlayingItem.BackdropItemId, {
                type: "Backdrop",
                height: 40,
                tag: nowPlayingItem.BackdropImageTag,
                index: 0
            });

        } else if (nowPlayingItem.ThumbImageTag) {

            url = ApiClient.getScaledImageUrl(nowPlayingItem.ThumbImageItemId, {
                type: "Thumb",
                height: 40,
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
        getNowPlayingBar().hide();
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

    function bindToPlayer(player) {

        releaseCurrentPlayer();

        currentPlayer = player;

        player.getPlayerState().done(function (state) {

            if (state.NowPlayingItem) {
                player.beginPlayerUpdates();
            }

            onStateChanged.call(player, {type: 'init'}, state);
        });

        $(player).on('playbackstart.nowplayingbar', onPlaybackStart)
            .on('playbackstop.nowplayingbar', onPlaybackStopped)
            .on('volumechange.nowplayingbar', onStateChanged)
            .on('playstatechange.nowplayingbar', onStateChanged)
            .on('positionchange.nowplayingbar', onStateChanged);
    }

    $(function () {

        $(MediaController).on('playerchange', function () {

            bindToPlayer(MediaController.getCurrentPlayer());
        });

        bindToPlayer(MediaController.getCurrentPlayer());
    });

})(window, document, jQuery, setTimeout, clearTimeout);