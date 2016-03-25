define(['jQuery'], function ($) {

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
    var toggleRepeatButton;

    var lastPlayerState;

    function getNowPlayingBarHtml() {

        var html = '';

        html += '<div class="nowPlayingBar hide">';

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

        html += '<paper-slider pin step="1" min="0" max="100" value="0" class="nowPlayingBarVolumeSlider" style="width:100px;vertical-align:middle;display:inline-block;"></paper-slider>';

        html += '<paper-icon-button icon="repeat" class="mediaButton toggleRepeatButton"></paper-icon-button>';

        html += '<div class="nowPlayingBarUserDataButtons">';
        html += '</div>';

        html += '<paper-icon-button icon="play-arrow" class="mediaButton unpauseButton"></paper-icon-button>';
        html += '<paper-icon-button icon="pause" class="mediaButton pauseButton"></paper-icon-button>';
        html += '<paper-icon-button icon="tablet-android" class="mediaButton remoteControlButton"></paper-icon-button>';
        html += '<paper-icon-button icon="queue-music" class="mediaButton playlistButton"></paper-icon-button>';

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

    function bindEvents(elem) {

        currentTimeElement = elem.querySelector('.nowPlayingBarCurrentTime');
        nowPlayingImageElement = elem.querySelector('.nowPlayingImage');
        nowPlayingTextElement = elem.querySelector('.nowPlayingBarText');
        nowPlayingUserData = elem.querySelector('.nowPlayingBarUserDataButtons');

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

        elem.querySelector('.remoteControlButton').addEventListener('click', function () {

            showRemoteControl();
        });

        elem.querySelector('.playlistButton').addEventListener('click', function () {

            showRemoteControl('playlist');
        });

        toggleRepeatButton = $('.toggleRepeatButton', elem).on('click', function () {

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
        })[0];

        // Unfortunately this is necessary because the polymer elements might not be ready immediately and there doesn't seem to be an event-driven way to find out when
        setTimeout(function () {
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

    function showRemoteControl(tab) {

        if (tab) {
            Dashboard.navigate('nowplaying.html?tab=' + tab);
        } else {
            Dashboard.navigate('nowplaying.html');
        }
    }

    var nowPlayingBarElement;
    function getNowPlayingBar() {

        return new Promise(function (resolve, reject) {

            if (nowPlayingBarElement) {
                resolve(nowPlayingBarElement);
                return;
            }

            require(['jQuery', 'css!css/nowplayingbar.css', 'paper-slider'], function ($) {

                nowPlayingBarElement = document.querySelector('.nowPlayingBar');

                if (nowPlayingBarElement) {
                    resolve(nowPlayingBarElement);
                    return;
                }

                nowPlayingBarElement = $(getNowPlayingBarHtml()).appendTo(document.body)[0];

                if ((browserInfo.safari || !AppInfo.isNativeApp) && browserInfo.mobile) {
                    // Not handled well here. The wrong elements receive events, bar doesn't update quickly enough, etc.
                    nowPlayingBarElement.classList.add('noMediaProgress');
                }

                bindEvents(nowPlayingBarElement);
                resolve(nowPlayingBarElement);
            });
        });
    }

    function showButton(button) {
        button.removeClass('hide');
    }

    function hideButton(button) {
        button.addClass('hide');
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
            toggleRepeatButton.icon = "repeat";
            toggleRepeatButton.classList.add('repeatActive');
        }
        else if (playState.RepeatMode == 'RepeatOne') {
            toggleRepeatButton.icon = "repeat-one";
            toggleRepeatButton.classList.add('repeatActive');
        } else {
            toggleRepeatButton.icon = "repeat";
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
                volumeSlider.classList.remove('hide');
            } else {
                volumeSlider.classList.add('hide');
            }

            if (!volumeSlider.dragging) {
                volumeSlider.value = playState.VolumeLevel || 0;
            }
        }
    }

    var currentImgUrl;
    function updateNowPlayingInfo(state) {

        var nameHtml = MediaController.getNowPlayingNameHtml(state.NowPlayingItem) || '';

        if (nameHtml.indexOf('<br/>') != -1) {
            nowPlayingTextElement.classList.add('nowPlayingDoubleText');
        } else {
            nowPlayingTextElement.classList.remove('nowPlayingDoubleText');
        }

        if (state.NowPlayingItem.Id) {
            nameHtml = '<a style="color:inherit;text-decoration:none;" href="' + LibraryBrowser.getHref(state.NowPlayingItem) + '">' + nameHtml + '</a>';
        }

        nowPlayingTextElement.innerHTML = nameHtml;

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
                nowPlayingUserData.innerHTML = LibraryBrowser.getUserDataIconsHtml(item, false);
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