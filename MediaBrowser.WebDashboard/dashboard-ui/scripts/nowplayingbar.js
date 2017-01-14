define(['datetime', 'userdataButtons', 'itemHelper', 'events', 'browser', 'imageLoader', 'playbackManager', 'nowPlayingHelper', 'apphost', 'dom', 'connectionManager', 'paper-icon-button-light'], function (datetime, userdataButtons, itemHelper, events, browser, imageLoader, playbackManager, nowPlayingHelper, appHost, dom, connectionManager) {
    'use strict';

    var currentPlayer;
    var currentPlayerSupportedCommands = [];

    var currentTimeElement;
    var nowPlayingImageElement;
    var nowPlayingTextElement;
    var nowPlayingUserData;
    var muteButton;
    var volumeSlider;
    var volumeSliderContainer;
    var playPauseButtons;
    var positionSlider;
    var toggleRepeatButton;
    var toggleRepeatButtonIcon;

    var lastUpdateTime = 0;
    var lastPlayerState = {};
    var isEnabled;
    var currentRuntimeTicks = 0;

    function getNowPlayingBarHtml() {

        var html = '';

        html += '<div class="nowPlayingBar hide nowPlayingBar-hidden">';

        html += '<div class="nowPlayingBarTop">';
        html += '<div class="nowPlayingBarPositionContainer sliderContainer">';
        html += '<input type="range" is="emby-slider" pin step=".01" min="0" max="100" value="0" class="nowPlayingBarPositionSlider"/>';
        html += '</div>';

        html += '<div class="nowPlayingBarInfoContainer">';
        html += '<div class="nowPlayingImage"></div>';
        html += '<div class="nowPlayingBarText"></div>';
        html += '</div>';

        // The onclicks are needed due to the return false above
        html += '<div class="nowPlayingBarCenter">';

        html += '<button is="paper-icon-button-light" class="previousTrackButton mediaButton autoSize"><i class="md-icon">skip_previous</i></button>';

        html += '<button is="paper-icon-button-light" class="playPauseButton mediaButton autoSize"><i class="md-icon">pause</i></button>';

        html += '<button is="paper-icon-button-light" class="stopButton mediaButton autoSize"><i class="md-icon">stop</i></button>';
        html += '<button is="paper-icon-button-light" class="nextTrackButton mediaButton autoSize"><i class="md-icon">skip_next</i></button>';

        html += '<div class="nowPlayingBarCurrentTime"></div>';
        html += '</div>';

        html += '<div class="nowPlayingBarRight">';

        html += '<button is="paper-icon-button-light" class="muteButton mediaButton autoSize"><i class="md-icon">volume_up</i></button>';

        html += '<div class="sliderContainer nowPlayingBarVolumeSliderContainer hide" style="width:100px;vertical-align:middle;display:inline-flex;">';
        html += '<input type="range" is="emby-slider" pin step="1" min="0" max="100" value="0" class="nowPlayingBarVolumeSlider"/>';
        html += '</div>';

        html += '<button is="paper-icon-button-light" class="toggleRepeatButton mediaButton autoSize"><i class="md-icon">repeat</i></button>';

        html += '<div class="nowPlayingBarUserDataButtons">';
        html += '</div>';

        html += '<button is="paper-icon-button-light" class="playPauseButton mediaButton autoSize"><i class="md-icon">pause</i></button>';
        html += '<button is="paper-icon-button-light" class="remoteControlButton mediaButton autoSize"><i class="md-icon">tablet_android</i></button>';
        html += '<button is="paper-icon-button-light" class="playlistButton mediaButton autoSize"><i class="md-icon">queue_music</i></button>';

        html += '</div>';
        html += '</div>';

        html += '</div>';

        return html;
    }

    function onSlideDownComplete() {
        this.classList.add('hide');
    }

    function slideDown(elem) {

        requestAnimationFrame(function () {

            // trigger reflow
            void elem.offsetWidth;

            elem.classList.add('nowPlayingBar-hidden');

            dom.addEventListener(elem, dom.whichTransitionEvent(), onSlideDownComplete, {
                once: true
            });
        });
    }

    function slideUp(elem) {

        elem.classList.remove('hide');

        dom.removeEventListener(elem, dom.whichTransitionEvent(), onSlideDownComplete, {
            once: true
        });

        requestAnimationFrame(function () {

            // trigger reflow
            void elem.offsetWidth;

            elem.classList.remove('nowPlayingBar-hidden');
        });
    }

    function slideUpToFullScreen(elem) {

        if (!elem.classList.contains('hide')) {
            return;
        }

        elem.classList.remove('hide');

        if (!browser.animate || browser.slow) {
            return;
        }

        requestAnimationFrame(function () {

            var keyframes = [
              { transform: 'none', offset: 0 },
              { transform: 'translateY(-100%)', offset: 1 }];
            var timing = { duration: 200, iterations: 1, fill: 'both', easing: 'ease-out' };
            elem.animate(keyframes, timing);
        });
    }

    function onPlayPauseClick() {
        playbackManager.playPause(currentPlayer);
    }

    function bindEvents(elem) {

        currentTimeElement = elem.querySelector('.nowPlayingBarCurrentTime');
        nowPlayingImageElement = elem.querySelector('.nowPlayingImage');
        nowPlayingTextElement = elem.querySelector('.nowPlayingBarText');
        nowPlayingUserData = elem.querySelector('.nowPlayingBarUserDataButtons');

        muteButton = elem.querySelector('.muteButton');
        muteButton.addEventListener('click', function () {

            if (currentPlayer) {
                playbackManager.toggleMute(currentPlayer);
            }

        });

        elem.querySelector('.stopButton').addEventListener('click', function () {

            if (currentPlayer) {
                playbackManager.stop(currentPlayer);
            }
        });

        var i, length;
        playPauseButtons = elem.querySelectorAll('.playPauseButton');
        for (i = 0, length = playPauseButtons.length; i < length; i++) {
            playPauseButtons[i].addEventListener('click', onPlayPauseClick);
        }

        elem.querySelector('.nextTrackButton').addEventListener('click', function () {

            if (currentPlayer) {
                playbackManager.nextTrack(currentPlayer);
            }
        });

        elem.querySelector('.previousTrackButton').addEventListener('click', function () {

            if (currentPlayer) {
                playbackManager.previousTrack(currentPlayer);
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
                        playbackManager.setRepeatMode('RepeatOne', currentPlayer);
                        break;
                    case 'RepeatOne':
                        playbackManager.setRepeatMode('RepeatNone', currentPlayer);
                        break;
                    default:
                        playbackManager.setRepeatMode('RepeatAll', currentPlayer);
                        break;
                }
            }
        });

        toggleRepeatButtonIcon = toggleRepeatButton.querySelector('i');

        volumeSlider = elem.querySelector('.nowPlayingBarVolumeSlider');
        volumeSliderContainer = elem.querySelector('.nowPlayingBarVolumeSliderContainer');

        if (appHost.supports('physicalvolumecontrol')) {
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

            if (currentPlayer) {

                var newPercent = parseFloat(this.value);

                playbackManager.seekPercent(newPercent, currentPlayer);
            }

        });

        positionSlider.getBubbleText = function (value) {

            var state = lastPlayerState;

            if (!state || !state.NowPlayingItem || !currentRuntimeTicks) {
                return '--:--';
            }

            var ticks = currentRuntimeTicks;
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

            require(['appfooter-shared', 'itemShortcuts', 'css!css/nowplayingbar.css', 'emby-slider'], function (appfooter, itemShortcuts) {

                var parentContainer = appfooter.element;
                nowPlayingBarElement = parentContainer.querySelector('.nowPlayingBar');

                if (nowPlayingBarElement) {
                    resolve(nowPlayingBarElement);
                    return;
                }

                parentContainer.insertAdjacentHTML('afterbegin', getNowPlayingBarHtml());
                nowPlayingBarElement = parentContainer.querySelector('.nowPlayingBar');

                if (browser.safari && browser.slow) {
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

    function updatePlayPauseState(isPaused) {

        var i, length;

        if (playPauseButtons) {
            if (isPaused) {

                for (i = 0, length = playPauseButtons.length; i < length; i++) {
                    playPauseButtons[i].querySelector('i').innerHTML = 'play_arrow';
                }

            } else {

                for (i = 0, length = playPauseButtons.length; i < length; i++) {
                    playPauseButtons[i].querySelector('i').innerHTML = 'pause';
                }
            }
        }
    }

    function updatePlayerStateInternal(event, state) {

        showNowPlayingBar();

        lastPlayerState = state;

        var playerInfo = playbackManager.getPlayerInfo();

        var playState = state.PlayState || {};

        updatePlayPauseState(playState.IsPaused);

        var supportedCommands = playerInfo.supportedCommands;
        currentPlayerSupportedCommands = supportedCommands;

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

        updatePlayerVolumeState(playState.IsMuted, playState.VolumeLevel);

        if (positionSlider && !positionSlider.dragging) {
            positionSlider.disabled = !playState.CanSeek;
        }

        var nowPlayingItem = state.NowPlayingItem || {};
        updateTimeDisplay(playState.PositionTicks, nowPlayingItem.RunTimeTicks);

        updateNowPlayingInfo(state);
    }

    function updateTimeDisplay(positionTicks, runtimeTicks) {

        // See bindEvents for why this is necessary
        if (positionSlider && !positionSlider.dragging) {
            if (runtimeTicks) {

                var pct = positionTicks / runtimeTicks;
                pct *= 100;

                positionSlider.value = pct;

            } else {

                positionSlider.value = 0;
            }
        }

        if (currentTimeElement) {

            var timeText = positionTicks == null ? '--:--' : datetime.getDisplayRunningTime(positionTicks);

            if (runtimeTicks) {
                timeText += " / " + datetime.getDisplayRunningTime(runtimeTicks);
            }

            currentTimeElement.innerHTML = timeText;
        }
    }

    function updatePlayerVolumeState(isMuted, volumeLevel) {

        var supportedCommands = currentPlayerSupportedCommands;

        var showMuteButton = true;
        var showVolumeSlider = true;

        if (supportedCommands.indexOf('ToggleMute') == -1) {
            showMuteButton = false;
        }

        if (isMuted) {
            muteButton.querySelector('i').innerHTML = '&#xE04F;';
        } else {
            muteButton.querySelector('i').innerHTML = '&#xE050;';
        }

        if (supportedCommands.indexOf('SetVolume') == -1) {
            showVolumeSlider = false;
        }

        if (currentPlayer.isLocalPlayer && appHost.supports('physicalvolumecontrol')) {
            showMuteButton = false;
            showVolumeSlider = false;
        }

        if (showMuteButton) {
            showButton(muteButton);
        } else {
            hideButton(muteButton);
        }

        // See bindEvents for why this is necessary
        if (volumeSlider) {

            if (showVolumeSlider) {
                volumeSliderContainer.classList.remove('hide');
            } else {
                volumeSliderContainer.classList.add('hide');
            }

            if (!volumeSlider.dragging) {
                volumeSlider.value = volumeLevel || 0;
            }
        }
    }

    function getTextActionButton(item, text) {

        if (!text) {
            text = itemHelper.getDisplayName(item);
        }

        var html = '<button data-id="' + item.Id + '" data-type="' + item.Type + '" data-mediatype="' + item.MediaType + '" data-channelid="' + item.ChannelId + '" data-isfolder="' + item.IsFolder + '" type="button" class="itemAction textActionButton" data-action="link">';
        html += text;
        html += '</button>';

        return html;
    }

    function seriesImageUrl(item, options) {

        if (!item) {
            throw new Error('item cannot be null!');
        }

        if (item.Type !== 'Episode') {
            return null;
        }

        options = options || {};
        options.type = options.type || "Primary";

        if (options.type === 'Primary') {

            if (item.SeriesPrimaryImageTag) {

                options.tag = item.SeriesPrimaryImageTag;

                return connectionManager.getApiClient(item.ServerId).getScaledImageUrl(item.SeriesId, options);
            }
        }

        if (options.type === 'Thumb') {

            if (item.SeriesThumbImageTag) {

                options.tag = item.SeriesThumbImageTag;

                return connectionManager.getApiClient(item.ServerId).getScaledImageUrl(item.SeriesId, options);
            }
            if (item.ParentThumbImageTag) {

                options.tag = item.ParentThumbImageTag;

                return connectionManager.getApiClient(item.ServerId).getScaledImageUrl(item.ParentThumbItemId, options);
            }
        }

        return null;
    }

    function imageUrl(item, options) {

        if (!item) {
            throw new Error('item cannot be null!');
        }

        options = options || {};
        options.type = options.type || "Primary";

        if (item.ImageTags && item.ImageTags[options.type]) {

            options.tag = item.ImageTags[options.type];
            return connectionManager.getApiClient(item.ServerId).getScaledImageUrl(item.PrimaryImageItemId || item.Id, options);
        }

        if (item.AlbumId && item.AlbumPrimaryImageTag) {

            options.tag = item.AlbumPrimaryImageTag;
            return connectionManager.getApiClient(item.ServerId).getScaledImageUrl(item.AlbumId, options);
        }

        return null;
    }

    var currentImgUrl;
    function updateNowPlayingInfo(state) {

        var nowPlayingItem = state.NowPlayingItem;

        nowPlayingTextElement.innerHTML = nowPlayingItem ? nowPlayingHelper.getNowPlayingNames(nowPlayingItem).map(function (nowPlayingName) {

            if (nowPlayingName.item) {
                return '<div>' + getTextActionButton(nowPlayingName.item, nowPlayingName.text) + '</div>';
            }

            return '<div>' + nowPlayingName.text + '</div>';

        }).join('') : '';

        var imgHeight = 70;

        var url = nowPlayingItem ? (seriesImageUrl(nowPlayingItem, {
            height: imgHeight
        }) || imageUrl(nowPlayingItem, {
            height: imgHeight
        })) : null;

        if (url !== currentImgUrl) {
            currentImgUrl = url;

            imageLoader.lazyImage(nowPlayingImageElement, url);
        }

        if (nowPlayingItem.Id) {
            ApiClient.getItem(Dashboard.getCurrentUserId(), nowPlayingItem.Id).then(function(item) {
                userdataButtons.fill({
                    item: item,
                    includePlayed: false,
                    element: nowPlayingUserData
                });
            });
        } else {
            userdataButtons.destroy({
                element: nowPlayingUserData
            });
        }
    }

    function onPlaybackStart(e, state) {

        console.log('nowplaying event: ' + e.type);

        var player = this;

        onStateChanged.call(player, e, state);
    }

    function showNowPlayingBar() {

        getNowPlayingBar().then(slideUp);
    }

    function hideNowPlayingBar() {

        isEnabled = false;

        // Use a timeout to prevent the bar from hiding and showing quickly
        // in the event of a stop->play command

        // Don't call getNowPlayingBar here because we don't want to end up creating it just to hide it
        var elem = document.getElementsByClassName('nowPlayingBar')[0];
        if (elem) {

            // If it's not currently visible, don't bother with the animation
            // transitionend events not firing in mobile chrome/safari when hidden
            if (document.body.classList.contains('hiddenNowPlayingBar')) {

                dom.removeEventListener(elem, dom.whichTransitionEvent(), onSlideDownComplete, {
                    once: true
                });
                elem.classList.add('hide');
                elem.classList.add('nowPlayingBar-hidden');

            } else {
                slideDown(elem);
            }
        }
    }

    function onPlaybackStopped(e, stopInfo) {

        console.log('nowplaying event: ' + e.type);
        var player = this;

        if (stopInfo.nextMediaType !== 'Audio') {
            hideNowPlayingBar();
        }
    }

    function onPlayPauseStateChanged(e) {

        if (!isEnabled) {
            return;
        }

        var player = this;
        updatePlayPauseState(player.paused());
    }

    function onStateChanged(event, state) {

        //console.log('nowplaying event: ' + e.type);
        var player = this;

        if (!state.NowPlayingItem) {
            hideNowPlayingBar();
            return;
        }

        if (player.isLocalPlayer && state.NowPlayingItem && state.NowPlayingItem.MediaType == 'Video') {
            hideNowPlayingBar();
            return;
        }

        isEnabled = true;

        if (nowPlayingBarElement) {
            updatePlayerStateInternal(event, state);
            return;
        }

        getNowPlayingBar().then(function () {
            updatePlayerStateInternal(event, state);
        });
    }

    function onTimeUpdate(e) {

        if (!isEnabled) {
            return;
        }

        // Try to avoid hammering the document with changes
        var now = new Date().getTime();
        if ((now - lastUpdateTime) < 700) {

            return;
        }
        lastUpdateTime = now;

        var player = this;
        currentRuntimeTicks = playbackManager.duration(player);
        updateTimeDisplay(playbackManager.currentTime(player), currentRuntimeTicks);
    }

    function releaseCurrentPlayer() {

        var player = currentPlayer;

        if (player) {
            events.off(player, 'playbackstart', onPlaybackStart);
            events.off(player, 'statechange', onPlaybackStart);
            events.off(player, 'repeatmodechange', onPlaybackStart);
            events.off(player, 'playbackstop', onPlaybackStopped);
            events.off(player, 'volumechange', onVolumeChanged);
            events.off(player, 'pause', onPlayPauseStateChanged);
            events.off(player, 'playing', onPlayPauseStateChanged);
            events.off(player, 'timeupdate', onTimeUpdate);

            currentPlayer = null;
            hideNowPlayingBar();
        }
    }

    function onVolumeChanged(e) {

        if (!isEnabled) {
            return;
        }

        var player = this;

        updatePlayerVolumeState(player.isMuted(), player.getVolume());
    }

    function bindToPlayer(player) {

        if (player === currentPlayer) {
            return;
        }

        releaseCurrentPlayer();

        currentPlayer = player;

        if (!player) {
            return;
        }

        playbackManager.getPlayerState(player).then(function (state) {

            onStateChanged.call(player, { type: 'init' }, state);
        });

        events.on(player, 'playbackstart', onPlaybackStart);
        events.on(player, 'statechange', onPlaybackStart);
        // TODO: Replace this with smaller changes on repeatmodechange. 
        // For now go cheap and just refresh the entire component
        events.on(player, 'repeatmodechange', onPlaybackStart);
        events.on(player, 'playbackstop', onPlaybackStopped);
        events.on(player, 'volumechange', onVolumeChanged);
        events.on(player, 'pause', onPlayPauseStateChanged);
        events.on(player, 'playing', onPlayPauseStateChanged);
        events.on(player, 'timeupdate', onTimeUpdate);
    }

    events.on(playbackManager, 'playerchange', function () {
        bindToPlayer(playbackManager.getCurrentPlayer());
    });

    bindToPlayer(playbackManager.getCurrentPlayer());

});