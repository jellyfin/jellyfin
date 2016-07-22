define(['datetime', 'userdataButtons', 'paper-icon-button-light'], function (datetime, userdataButtons) {

    var currentPlayer;

    var currentTimeElement;
    var nowPlayingImageElement;
    var nowPlayingTextElement;
    var nowPlayingUserData;
    var unmuteButton;
    var muteButton;
    var volumeSlider;
    var volumeSliderContainer;
    var unpauseButtons;
    var pauseButtons;
    var positionSlider;
    var toggleRepeatButton;
    var toggleRepeatButtonIcon;

    var lastPlayerState;

    function getNowPlayingBarHtml() {

        var html = '';

        html += '<div class="nowPlayingBar hide">';

        html += '<div class="nowPlayingBarPositionContainer sliderContainer">';
        html += '<input type="range" is="emby-slider" pin step=".1" min="0" max="100" value="0" class="nowPlayingBarPositionSlider"/>';
        html += '</div>';

        html += '<div class="nowPlayingBarInfoContainer">';
        html += '<div class="nowPlayingImage"></div>';
        html += '<div class="nowPlayingBarText"></div>';
        html += '</div>';

        // The onclicks are needed due to the return false above
        html += '<div class="nowPlayingBarCenter">';

        html += '<button is="paper-icon-button-light" class="previousTrackButton mediaButton autoSize"><i class="md-icon">skip_previous</i></button>';

        html += '<button is="paper-icon-button-light" class="unpauseButton mediaButton autoSize"><i class="md-icon">play_arrow</i></button>';
        html += '<button is="paper-icon-button-light" class="pauseButton mediaButton autoSize"><i class="md-icon">pause</i></button>';

        html += '<button is="paper-icon-button-light" class="stopButton mediaButton autoSize"><i class="md-icon">stop</i></button>';
        html += '<button is="paper-icon-button-light" class="nextTrackButton mediaButton autoSize"><i class="md-icon">skip_next</i></button>';

        html += '<div class="nowPlayingBarCurrentTime"></div>';
        html += '</div>';

        html += '<div class="nowPlayingBarRight">';

        html += '<button is="paper-icon-button-light" class="muteButton mediaButton autoSize"><i class="md-icon">volume_up</i></button>';
        html += '<button is="paper-icon-button-light" class="unmuteButton mediaButton autoSize"><i class="md-icon">volume_off</i></button>';

        html += '<div class="sliderContainer nowPlayingBarVolumeSliderContainer hide" style="width:100px;vertical-align:middle;display:inline-flex;">';
        html += '<input type="range" is="emby-slider" pin step="1" min="0" max="100" value="0" class="nowPlayingBarVolumeSlider"/>';
        html += '</div>';

        html += '<button is="paper-icon-button-light" class="toggleRepeatButton mediaButton autoSize"><i class="md-icon">repeat</i></button>';

        html += '<div class="nowPlayingBarUserDataButtons">';
        html += '</div>';

        html += '<button is="paper-icon-button-light" class="unpauseButton mediaButton autoSize"><i class="md-icon">play_arrow</i></button>';
        html += '<button is="paper-icon-button-light" class="pauseButton mediaButton autoSize"><i class="md-icon">pause</i></button>';
        html += '<button is="paper-icon-button-light" class="remoteControlButton mediaButton autoSize"><i class="md-icon">tablet_android</i></button>';
        html += '<button is="paper-icon-button-light" class="playlistButton mediaButton autoSize"><i class="md-icon">queue_music</i></button>';

        html += '</div>';

        html += '</div>';

        return html;
    }

    var height;

    function getHeight(elem) {

        if (!height) {
            height = elem.offsetHeight;
        }

        return height + 'px';
    }

    function slideDown(elem) {

        if (elem.classList.contains('hide')) {
            return;
        }

        var onfinish = function () {
            elem.classList.add('hide');
        };

        if (!browserInfo.animate || browserInfo.mobile) {
            onfinish();
            return;
        }

        requestAnimationFrame(function () {
            var keyframes = [
              { height: getHeight(elem), offset: 0 },
              { height: '0', display: 'none', offset: 1 }];
            var timing = { duration: 200, iterations: 1, fill: 'both', easing: 'ease-out' };
            elem.animate(keyframes, timing).onfinish = onfinish;
        });
    }

    function slideUp(elem) {

        if (!elem.classList.contains('hide')) {
            return;
        }

        elem.classList.remove('hide');

        if (!browserInfo.animate || browserInfo.mobile) {
            return;
        }

        requestAnimationFrame(function () {

            var keyframes = [
              { height: '0', offset: 0 },
              { height: getHeight(elem), offset: 1 }];
            var timing = { duration: 200, iterations: 1, fill: 'both', easing: 'ease-out' };
            elem.animate(keyframes, timing);
        });
    }

    function onPauseClick() {
        if (currentPlayer) {
            currentPlayer.pause();
        }
    }

    function onUnpauseClick() {
        if (currentPlayer) {
            currentPlayer.unpause();
        }
    }

    function bindEvents(elem) {

        currentTimeElement = elem.querySelector('.nowPlayingBarCurrentTime');
        nowPlayingImageElement = elem.querySelector('.nowPlayingImage');
        nowPlayingTextElement = elem.querySelector('.nowPlayingBarText');
        nowPlayingUserData = elem.querySelector('.nowPlayingBarUserDataButtons');

        unmuteButton = elem.querySelector('.unmuteButton');
        unmuteButton.addEventListener('click', function () {

            if (currentPlayer) {
                currentPlayer.unMute();
            }

        });

        muteButton = elem.querySelector('.muteButton');
        muteButton.addEventListener('click', function () {

            if (currentPlayer) {
                currentPlayer.mute();
            }

        });

        elem.querySelector('.stopButton').addEventListener('click', function () {

            if (currentPlayer) {
                currentPlayer.stop();
            }
        });

        var i, length;
        pauseButtons = elem.querySelectorAll('.pauseButton');
        for (i = 0, length = pauseButtons.length; i < length; i++) {
            pauseButtons[i].addEventListener('click', onPauseClick);
        }
        unpauseButtons = elem.querySelectorAll('.unpauseButton');
        for (i = 0, length = unpauseButtons.length; i < length; i++) {
            unpauseButtons[i].addEventListener('click', onUnpauseClick);
        }

        elem.querySelector('.nextTrackButton').addEventListener('click', function () {

            if (currentPlayer) {
                currentPlayer.nextTrack();
            }
        });

        elem.querySelector('.previousTrackButton').addEventListener('click', function () {

            if (currentPlayer) {
                currentPlayer.previousTrack();
            }
        });

        elem.querySelector('.remoteControlButton').addEventListener('click', function () {

            showRemoteControl();
        });

        elem.querySelector('.playlistButton').addEventListener('click', function () {

            showRemoteControl(2);
        });

        toggleRepeatButton = elem.querySelector('.toggleRepeatButton');
        toggleRepeatButton.addEventListener('click', function () {

            if (currentPlayer) {
                var state = lastPlayerState || {};

                switch ((state.PlayState || {}).RepeatMode) {
                    case 'RepeatAll':
                        currentPlayer.setRepeatMode('RepeatOne');
                        break;
                    case 'RepeatOne':
                        currentPlayer.setRepeatMode('RepeatNone');
                        break;
                    default:
                        currentPlayer.setRepeatMode('RepeatAll');
                        break;
                }
            }
        });

        toggleRepeatButtonIcon = toggleRepeatButton.querySelector('i');

        volumeSlider = elem.querySelector('.nowPlayingBarVolumeSlider');
        volumeSliderContainer = elem.querySelector('.nowPlayingBarVolumeSliderContainer');

        if (AppInfo.hasPhysicalVolumeButtons) {
            volumeSliderContainer.classList.add('hide');
        } else {
            volumeSliderContainer.classList.remove('hide');
        }

        volumeSlider.addEventListener('change', function () {

            if (currentPlayer) {
                currentPlayer.setVolume(this.value);
            }

        });

        positionSlider = elem.querySelector('.nowPlayingBarPositionSlider');
        positionSlider.addEventListener('change', function () {

            if (currentPlayer && lastPlayerState) {

                var newPercent = parseFloat(this.value);
                var newPositionTicks = (newPercent / 100) * lastPlayerState.NowPlayingItem.RunTimeTicks;

                currentPlayer.seek(Math.floor(newPositionTicks));
            }

        });

        positionSlider.getBubbleText = function (value) {

            var state = lastPlayerState;

            if (!state || !state.NowPlayingItem || !state.NowPlayingItem.RunTimeTicks) {
                return '--:--';
            }

            var ticks = state.NowPlayingItem.RunTimeTicks;
            ticks /= 100;
            ticks *= value;

            return datetime.getDisplayRunningTime(ticks);
        };
    }

    function showRemoteControl(tabIndex) {

        if (tabIndex) {
            Dashboard.navigate('nowplaying.html?tab=' + tabIndex);
        } else {
            Dashboard.navigate('nowplaying.html');
        }
    }

    var nowPlayingBarElement;
    function getNowPlayingBar() {

        if (nowPlayingBarElement) {
            return Promise.resolve(nowPlayingBarElement);
        }

        return new Promise(function (resolve, reject) {

            require(['itemShortcuts', 'css!css/nowplayingbar.css', 'emby-slider'], function (itemShortcuts) {

                nowPlayingBarElement = document.querySelector('.nowPlayingBar');

                if (nowPlayingBarElement) {
                    resolve(nowPlayingBarElement);
                    return;
                }

                document.body.insertAdjacentHTML('beforeend', getNowPlayingBarHtml());
                nowPlayingBarElement = document.querySelector('.nowPlayingBar');

                if (browserInfo.safari && browserInfo.mobile) {
                    // Not handled well here. The wrong elements receive events, bar doesn't update quickly enough, etc.
                    nowPlayingBarElement.classList.add('noMediaProgress');
                }

                itemShortcuts.on(nowPlayingBarElement);

                bindEvents(nowPlayingBarElement);
                resolve(nowPlayingBarElement);
            });
        });
    }

    function showButton(button) {
        button.classList.remove('hide');
    }

    function hideButton(button) {
        button.classList.add('hide');
    }

    var lastUpdateTime = 0;

    function updatePlayerState(event, state) {

        if (!state.NowPlayingItem) {
            hideNowPlayingBar();
            return;
        }

        if (nowPlayingBarElement) {
            updatePlayerStateInternal(event, state);
            return;
        }

        getNowPlayingBar().then(function () {
            updatePlayerStateInternal(event, state);
        });
    }

    function updatePlayerStateInternal(event, state) {

        showNowPlayingBar();

        if (event.type == 'positionchange') {
            // Try to avoid hammering the document with changes
            var now = new Date().getTime();
            if ((now - lastUpdateTime) < 700) {

                return;
            }
            lastUpdateTime = now;
        }

        lastPlayerState = state;

        var playerInfo = MediaController.getPlayerInfo();

        var playState = state.PlayState || {};
        var i, length;

        if (playState.IsPaused) {

            for (i = 0, length = pauseButtons.length; i < length; i++) {
                hideButton(pauseButtons[i]);
            }
            for (i = 0, length = unpauseButtons.length; i < length; i++) {
                showButton(unpauseButtons[i]);
            }

        } else {

            for (i = 0, length = pauseButtons.length; i < length; i++) {
                showButton(pauseButtons[i]);
            }
            for (i = 0, length = unpauseButtons.length; i < length; i++) {
                hideButton(unpauseButtons[i]);
            }
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

        var timeText = datetime.getDisplayRunningTime(playState.PositionTicks);

        if (nowPlayingItem.RunTimeTicks) {

            timeText += " / " + datetime.getDisplayRunningTime(nowPlayingItem.RunTimeTicks);

        }

        currentTimeElement.innerHTML = timeText;

        updateNowPlayingInfo(state);
    }

    function updatePlayerVolumeState(state, playerInfo) {

        playerInfo = playerInfo || MediaController.getPlayerInfo();

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

        if (supportedCommands.indexOf('SetRepeatMode') == -1) {
            toggleRepeatButton.classList.add('hide');
        } else {
            toggleRepeatButton.classList.remove('hide');
        }

        if (playState.RepeatMode == 'RepeatAll') {
            toggleRepeatButtonIcon.innerHTML = "repeat";
            toggleRepeatButton.classList.add('repeatActive');
        }
        else if (playState.RepeatMode == 'RepeatOne') {
            toggleRepeatButtonIcon.innerHTML = "repeat_one";
            toggleRepeatButton.classList.add('repeatActive');
        } else {
            toggleRepeatButtonIcon.innerHTML = "repeat";
            toggleRepeatButton.classList.remove('repeatActive');
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

            if (showVolumeSlider) {
                volumeSliderContainer.classList.remove('hide');
            } else {
                volumeSliderContainer.classList.add('hide');
            }

            if (!volumeSlider.dragging) {
                volumeSlider.value = playState.VolumeLevel || 0;
            }
        }
    }

    var currentImgUrl;
    function updateNowPlayingInfo(state) {

        nowPlayingTextElement.innerHTML = MediaController.getNowPlayingNames(state.NowPlayingItem).map(function (nowPlayingName) {

            if (nowPlayingName.item) {
                return '<div>' + LibraryBrowser.getTextActionButton(nowPlayingName.item, nowPlayingName.text) + '</div>';
            }

            return '<div>' + nowPlayingName.text + '</div>';

        }).join('');

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

        ImageLoader.lazyImage(nowPlayingImageElement, url);

        if (nowPlayingItem.Id) {
            ApiClient.getItem(Dashboard.getCurrentUserId(), nowPlayingItem.Id).then(function (item) {
                nowPlayingUserData.innerHTML = userdataButtons.getIconsHtml({
                    item: item,
                    includePlayed: false
                });
            });
        } else {
            nowPlayingUserData.innerHTML = '';
        }
    }

    function onPlaybackStart(e, state) {

        console.log('nowplaying event: ' + e.type);

        var player = this;

        player.beginPlayerUpdates();

        onStateChanged.call(player, e, state);
    }

    function showNowPlayingBar() {

        getNowPlayingBar().then(slideUp);
    }

    function hideNowPlayingBar() {

        // Use a timeout to prevent the bar from hiding and showing quickly
        // in the event of a stop->play command

        // Don't call getNowPlayingBar here because we don't want to end up creating it just to hide it
        var elem = document.getElementsByClassName('nowPlayingBar')[0];
        if (elem) {
            slideDown(elem);
        }
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

        updatePlayerState(e, state);
    }

    function releaseCurrentPlayer() {

        if (currentPlayer) {

            Events.off(currentPlayer, 'playbackstart', onPlaybackStart);
            Events.off(currentPlayer, 'playbackstop', onPlaybackStopped);
            Events.off(currentPlayer, 'volumechange', onVolumeChanged);
            Events.off(currentPlayer, 'playstatechange', onStateChanged);
            Events.off(currentPlayer, 'positionchange', onStateChanged);

            currentPlayer.endPlayerUpdates();
            currentPlayer = null;

            hideNowPlayingBar();
        }
    }

    function onVolumeChanged(e) {

        var player = this;

        Promise.all([player.getPlayerState(), getNowPlayingBar()]).then(function (responses) {

            var state = responses[0];

            if (player.isDefaultPlayer && state.NowPlayingItem && state.NowPlayingItem.MediaType == 'Video') {
                return;
            }

            updatePlayerVolumeState(state);
        });
    }

    function bindToPlayer(player) {

        releaseCurrentPlayer();

        currentPlayer = player;

        player.getPlayerState().then(function (state) {

            if (state.NowPlayingItem) {
                player.beginPlayerUpdates();
            }

            onStateChanged.call(player, { type: 'init' }, state);
        });

        Events.on(player, 'playbackstart', onPlaybackStart);
        Events.on(player, 'playbackstop', onPlaybackStopped);
        Events.on(player, 'volumechange', onVolumeChanged);
        Events.on(player, 'playstatechange', onStateChanged);
        Events.on(player, 'positionchange', onStateChanged);
    }

    Events.on(MediaController, 'playerchange', function () {

        bindToPlayer(MediaController.getCurrentPlayer());
    });

    bindToPlayer(MediaController.getCurrentPlayer());

});