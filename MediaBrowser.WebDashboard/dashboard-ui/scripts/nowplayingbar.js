(function (window, document, $, setTimeout, clearTimeout) {

    var currentPlayer;

    var currentTimeElement;
    var nowPlayingImageElement;
    var nowPlayingTextElement;
    var nowPlayingUserData;
    var unmuteButton;
    var muteButton;
    var volumeSlider;
    var unpauseButton;
    var pauseButton;
    var positionSlider;

    var lastPlayerState;

    function getNowPlayingBarHtml() {

        var html = '';

        // add return false because on iOS clicking the bar often ends up clicking the content underneath. 
        html += '<div class="nowPlayingBar" style="display:none;">';

        html += '<div class="nowPlayingBarPositionContainer">';
        html += '<paper-slider pin step=".1" min="0" max="100" value="0" class="nowPlayingBarPositionSlider"></paper-slider>';
        html += '</div>';

        html += '<div class="nowPlayingBarInfoContainer">';
        html += '<div class="nowPlayingImage"></div>';
        html += '<div class="nowPlayingBarText"></div>';
        html += '</div>';

        // The onclicks are needed due to the return false above
        html += '<div class="nowPlayingBarCenter">';

        html += '<paper-icon-button icon="skip-previous" class="previousTrackButton mediaButton"></paper-icon-button>';

        html += '<paper-icon-button icon="play-arrow" class="mediaButton unpauseButton"></paper-icon-button>';
        html += '<paper-icon-button icon="pause" class="mediaButton pauseButton"></paper-icon-button>';

        html += '<paper-icon-button icon="stop" class="stopButton mediaButton"></paper-icon-button>';

        html += '<paper-icon-button icon="skip-next" class="nextTrackButton mediaButton"></paper-icon-button>';

        html += '<div class="nowPlayingBarCurrentTime"></div>';
        html += '</div>';

        html += '<div class="nowPlayingBarRight">';

        html += '<paper-icon-button icon="volume-up" class="muteButton mediaButton"></paper-icon-button>';
        html += '<paper-icon-button icon="volume-off" class="unmuteButton mediaButton"></paper-icon-button>';

        html += '<paper-slider pin step="1" min="0" max="100" value="0" class="nowPlayingBarVolumeSlider" style="width:100px;vertical-align:middle;"></paper-slider>';

        html += '<div class="nowPlayingBarUserDataButtons">';
        html += '</div>';

        html += '<paper-icon-button icon="play-arrow" class="mediaButton unpauseButton"></paper-icon-button>';
        html += '<paper-icon-button icon="pause" class="mediaButton pauseButton"></paper-icon-button>';
        html += '<paper-icon-button icon="tablet-android" onclick="Dashboard.navigate(\'nowplaying.html\', false);" class="mediaButton remoteControlButton"></paper-icon-button>';
        html += '<paper-icon-button icon="queue-music" class="mediaButton playlistButton"></paper-icon-button>';

        html += '</div>';

        html += '</div>';

        return html;
    }

    function bindEvents(elem) {

        currentTimeElement = $('.nowPlayingBarCurrentTime', elem);
        nowPlayingImageElement = $('.nowPlayingImage', elem);
        nowPlayingTextElement = $('.nowPlayingBarText', elem);
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

        $('.playlistButton', elem).on('click', function () {

            $.mobile.changePage('nowplaying.html', {
                dataUrl: 'nowplaying.html#playlist'
            });
        });

        // Unfortunately this is necessary because the polymer elements might not be ready immediately and there doesn't seem to be an event-driven way to find out when
        setTimeout(function() {
            volumeSlider = $('.nowPlayingBarVolumeSlider', elem).on('change', function () {

                if (currentPlayer) {
                    currentPlayer.setVolume(this.value);
                }

            })[0];

            positionSlider = $('.nowPlayingBarPositionSlider', elem).on('change', function () {

                if (currentPlayer && lastPlayerState) {

                    var newPercent = parseFloat(this.value);
                    var newPositionTicks = (newPercent / 100) * lastPlayerState.NowPlayingItem.RunTimeTicks;

                    currentPlayer.seek(Math.floor(newPositionTicks));
                }

            })[0];

            positionSlider._setPinValue = function (value) {

                var state = lastPlayerState;

                if (!state || !state.NowPlayingItem || !state.NowPlayingItem.RunTimeTicks) {
                    this.pinValue = '--:--';
                    return;
                }

                var ticks = state.NowPlayingItem.RunTimeTicks;
                ticks /= 100;
                ticks *= value;

                this.pinValue = Dashboard.getDisplayTime(ticks);
            };
        }, 300);
    }

    function getNowPlayingBar() {

        var elem = document.querySelector('.nowPlayingBar');

        if (elem) {
            return elem;
        }

        elem = $(getNowPlayingBarHtml()).insertBefore('#footerNotifications')[0];

        if (($.browser.safari || !AppInfo.isNativeApp) && $.browser.mobile) {
            // Not handled well here. The wrong elements receive events, bar doesn't update quickly enough, etc.
            elem.classList.add('noMediaProgress');
        }

        bindEvents(elem);
        $.mobile.loadPage('nowplaying.html');

        return elem;
    }

    function showButton(button) {
        button.removeClass('hide');
    }

    function hideButton(button) {
        button.addClass('hide');
    }

    var lastUpdateTime = 0;

    function updatePlayerState(event, state) {

        if (state.NowPlayingItem) {
            showNowPlayingBar();
        } else {
            hideNowPlayingBar();
            return;
        }

        if (event.type == 'positionchange') {
            // Try to avoid hammering the document with changes
            var now = new Date().getTime();
            if ((now - lastUpdateTime) < 700) {

                return;
            }
            lastUpdateTime = now;
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

        // See bindEvents for why this is necessary
        if (positionSlider) {
            if (!positionSlider.dragging) {

                if (nowPlayingItem.RunTimeTicks) {

                    var pct = playState.PositionTicks / nowPlayingItem.RunTimeTicks;
                    pct *= 100;

                    positionSlider.value = pct;

                } else {

                    positionSlider.value = 0;
                }

                positionSlider.disabled = !playState.CanSeek;
            }
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

        // See bindEvents for why this is necessary
        if (volumeSlider) {
            $(volumeSlider).visible(showVolumeSlider);

            if (!volumeSlider.dragging) {
                volumeSlider.value = playState.VolumeLevel || 0;
            }
        }
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
        var imgHeight = 80;

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

        nowPlayingImageElement.html(imgHtml);

        if (nowPlayingItem.Id) {
            ApiClient.getItem(Dashboard.getCurrentUserId(), nowPlayingItem.Id).done(function (item) {
                nowPlayingUserData.html(LibraryBrowser.getUserDataIconsHtml(item, false));
            });
        } else {
            nowPlayingUserData.html('');
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

        $(nowPlayingBar).show();
    }

    function hideNowPlayingBar() {

        // Use a timeout to prevent the bar from hiding and showing quickly
        // in the event of a stop->play command

        // Don't call getNowPlayingBar here because we don't want to end up creating it just to hide it
        var elem = document.getElementsByClassName('nowPlayingBar')[0];
        if (elem) {
            elem.style.display = 'none';
        }
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

        updatePlayerState(e, state);
    }

    function releaseCurrentPlayer() {

        if (currentPlayer) {

            $(currentPlayer).off('playbackstart', onPlaybackStart)
                .off('playbackstop', onPlaybackStopped)
                .off('volumechange', onVolumeChanged)
                .off('playstatechange', onStateChanged)
                .off('positionchange', onStateChanged);

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

        $(player).on('playbackstart', onPlaybackStart)
            .on('playbackstop', onPlaybackStopped)
            .on('volumechange', onVolumeChanged)
            .on('playstatechange', onStateChanged)
            .on('positionchange', onStateChanged);
    }

    Dashboard.ready(function () {

        Events.on(MediaController, 'playerchange', function () {

            bindToPlayer(MediaController.getCurrentPlayer());
        });

        bindToPlayer(MediaController.getCurrentPlayer());
    });

})(window, document, jQuery, setTimeout, clearTimeout);