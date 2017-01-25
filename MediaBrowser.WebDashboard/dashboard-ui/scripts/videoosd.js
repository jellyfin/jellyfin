define(['playbackManager', 'dom', 'inputmanager', 'datetime', 'itemHelper', 'mediaInfo', 'focusManager', 'imageLoader', 'scrollHelper', 'events', 'connectionManager', 'browser', 'globalize', 'apphost', 'layoutManager', 'scrollStyles', 'emby-slider'], function (playbackManager, dom, inputManager, datetime, itemHelper, mediaInfo, focusManager, imageLoader, scrollHelper, events, connectionManager, browser, globalize, appHost, layoutManager) {
    'use strict';

    function seriesImageUrl(item, options) {

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

        options = options || {};
        options.type = options.type || "Primary";

        if (item.ImageTags && item.ImageTags[options.type]) {

            options.tag = item.ImageTags[options.type];
            return connectionManager.getApiClient(item.ServerId).getScaledImageUrl(item.PrimaryImageItemId || item.Id, options);
        }

        if (options.type === 'Primary') {
            if (item.AlbumId && item.AlbumPrimaryImageTag) {

                options.tag = item.AlbumPrimaryImageTag;
                return connectionManager.getApiClient(item.ServerId).getScaledImageUrl(item.AlbumId, options);
            }
        }

        return null;
    }

    function logoImageUrl(item, apiClient, options) {

        options = options || {};
        options.type = "Logo";

        if (item.ImageTags && item.ImageTags.Logo) {

            options.tag = item.ImageTags.Logo;
            return apiClient.getScaledImageUrl(item.Id, options);
        }

        if (item.ParentLogoImageTag) {
            options.tag = item.ParentLogoImageTag;
            return apiClient.getScaledImageUrl(item.ParentLogoItemId, options);
        }

        return null;
    }

    return function (view, params) {

        var self = this;
        var currentPlayer;
        var currentPlayerSupportedCommands = [];
        var currentRuntimeTicks = 0;
        var lastUpdateTime = 0;
        var isEnabled;
        var currentItem;

        var nowPlayingVolumeSlider = view.querySelector('.osdVolumeSlider');
        var nowPlayingVolumeSliderContainer = view.querySelector('.osdVolumeSliderContainer');

        var nowPlayingPositionSlider = view.querySelector('.osdPositionSlider');

        var nowPlayingPositionText = view.querySelector('.osdPositionText');
        var nowPlayingDurationText = view.querySelector('.osdDurationText');
        var endsAtText = view.querySelector('.endsAtText');

        var btnRewind = view.querySelector('.btnRewind');
        var btnFastForward = view.querySelector('.btnFastForward');

        var transitionEndEventName = dom.whichTransitionEvent();

        var headerElement = document.querySelector('.skinHeader');
        var osdBottomElement = document.querySelector('.videoOsdBottom');

        function updateNowPlayingInfo(state) {

            var item = state.NowPlayingItem;
            currentItem = item;

            setPoster(item);

            if (!item) {
                Emby.Page.setTitle('');
                nowPlayingVolumeSlider.disabled = true;
                nowPlayingPositionSlider.disabled = true;
                btnFastForward.disabled = true;
                btnRewind.disabled = true;

                view.querySelector('.btnSubtitles').classList.add('hide');
                view.querySelector('.btnAudio').classList.add('hide');

                view.querySelector('.osdTitle').innerHTML = '';
                view.querySelector('.osdMediaInfo').innerHTML = '';
                return;
            }

            setTitle(item);

            view.querySelector('.osdTitle').innerHTML = itemHelper.getDisplayName(item);
            view.querySelector('.osdMediaInfo').innerHTML = mediaInfo.getPrimaryMediaInfoHtml(item, {
                runtime: false,
                subtitles: false,
                tomatoes: false,
                endsAt: false
            });

            nowPlayingVolumeSlider.disabled = false;
            nowPlayingPositionSlider.disabled = false;
            btnFastForward.disabled = false;
            btnRewind.disabled = false;

            if (playbackManager.subtitleTracks(currentPlayer).length) {
                view.querySelector('.btnSubtitles').classList.remove('hide');
            } else {
                view.querySelector('.btnSubtitles').classList.add('hide');
            }

            if (playbackManager.audioTracks(currentPlayer).length > 1) {
                view.querySelector('.btnAudio').classList.remove('hide');
            } else {
                view.querySelector('.btnAudio').classList.add('hide');
            }

        }

        function setTitle(item) {

            var url = logoImageUrl(item, connectionManager.getApiClient(item.ServerId), {});

            if (url) {

                //var pageTitle = document.querySelector('.pageTitle');
                //pageTitle.style.backgroundImage = "url('" + url + "')";
                //pageTitle.classList.add('pageTitleWithLogo');
                //pageTitle.innerHTML = '';
                //document.querySelector('.headerLogo').classList.add('hide');
            } else {
                Emby.Page.setTitle('');
            }
        }

        function setPoster(item) {

            var osdPoster = view.querySelector('.osdPoster');

            if (item) {

                var imgUrl = seriesImageUrl(item, { type: 'Primary' }) ||
                    seriesImageUrl(item, { type: 'Thumb' }) ||
                    imageUrl(item, { type: 'Primary' });

                if (imgUrl) {
                    osdPoster.innerHTML = '<img src="' + imgUrl + '" />';
                    return;
                }
            }

            osdPoster.innerHTML = '';
        }

        var _osdOpen = true;

        function isOsdOpen() {
            return _osdOpen;
        }

        function showOsd() {

            slideDownToShow(headerElement);
            slideUpToShow(osdBottomElement);
            startHideTimer();
        }

        function hideOsd() {

            slideUpToHide(headerElement);
            slideDownToHide(osdBottomElement);
        }

        var hideTimeout;

        function startHideTimer() {
            stopHideTimer();
            hideTimeout = setTimeout(hideOsd, 5000);
        }

        function stopHideTimer() {
            if (hideTimeout) {
                clearTimeout(hideTimeout);
                hideTimeout = null;
            }
        }

        function slideDownToShow(elem) {

            elem.classList.remove('osdHeader-hidden');
        }

        function slideUpToHide(elem) {

            elem.classList.add('osdHeader-hidden');
        }

        function clearBottomPanelAnimationEventListeners(elem) {

            dom.removeEventListener(elem, transitionEndEventName, onSlideDownComplete, {
                once: true
            });
        }

        function slideUpToShow(elem) {

            if (_osdOpen) {
                return;
            }

            _osdOpen = true;

            clearBottomPanelAnimationEventListeners(elem);

            elem.classList.remove('hide');

            // trigger a reflow to force it to animate again
            void elem.offsetWidth;

            elem.classList.remove('videoOsdBottom-hidden');

            focusManager.focus(elem.querySelector('.btnPause'));

            view.dispatchEvent(new CustomEvent('video-osd-show', {
                bubbles: true,
                cancelable: false
            }));
        }

        function onSlideDownComplete(e) {

            var elem = e.target;

            elem.classList.add('hide');

            dom.removeEventListener(elem, transitionEndEventName, onSlideDownComplete, {
                once: true
            });

            view.dispatchEvent(new CustomEvent('video-osd-hide', {
                bubbles: true,
                cancelable: false
            }));
        }

        function slideDownToHide(elem) {

            if (!_osdOpen) {
                return;
            }

            clearBottomPanelAnimationEventListeners(elem);

            // trigger a reflow to force it to animate again
            void elem.offsetWidth;

            elem.classList.add('videoOsdBottom-hidden');

            dom.addEventListener(elem, transitionEndEventName, onSlideDownComplete, {
                once: true
            });

            _osdOpen = false;
        }

        var lastMouseMoveData;

        function onMouseMove(e) {

            var eventX = e.screenX || 0;
            var eventY = e.screenY || 0;

            var obj = lastMouseMoveData;
            if (!obj) {
                lastMouseMoveData = {
                    x: eventX,
                    y: eventY
                };
                return;
            }

            // if coord are same, it didn't move
            if (Math.abs(eventX - obj.x) < 10 && Math.abs(eventY - obj.y) < 10) {
                return;
            }

            obj.x = eventX;
            obj.y = eventY;

            showOsd();
        }

        function onInputCommand(e) {

            switch (e.detail.command) {

                case 'left':
                    if (isOsdOpen()) {
                        showOsd();
                    } else {
                        e.preventDefault();
                        playbackManager.rewind();
                    }
                    break;
                case 'right':
                    if (isOsdOpen()) {
                        showOsd();
                    } else {
                        e.preventDefault();
                        playbackManager.fastForward();
                    }
                    break;
                case 'up':
                case 'down':
                case 'select':
                case 'menu':
                case 'info':
                case 'play':
                case 'playpause':
                case 'pause':
                case 'fastforward':
                case 'rewind':
                case 'next':
                case 'previous':
                    showOsd();
                    break;
                default:
                    break;
            }
        }

        function updateFullscreenIcon() {
            if (playbackManager.isFullscreen(currentPlayer)) {
                view.querySelector('.btnFullscreen').setAttribute('title', globalize.translate('ExitFullscreen'));
                view.querySelector('.btnFullscreen i').innerHTML = '&#xE5D1;';
            } else {
                view.querySelector('.btnFullscreen').setAttribute('title', globalize.translate('Fullscreen'));
                view.querySelector('.btnFullscreen i').innerHTML = '&#xE5D0;';
            }
        }

        view.addEventListener('viewbeforeshow', function (e) {

            headerElement.classList.add('osdHeader');
            // Make sure the UI is completely transparent
            Emby.Page.setTransparency('full');
        });

        view.addEventListener('viewshow', function (e) {

            events.on(playbackManager, 'playerchange', onPlayerChange);
            bindToPlayer(playbackManager.getCurrentPlayer());

            dom.addEventListener(document, 'mousemove', onMouseMove, {
                passive: true
            });
            document.body.classList.add('autoScrollY');

            showOsd();

            inputManager.on(window, onInputCommand);

            dom.addEventListener(window, 'keydown', onWindowKeyDown, {
                passive: true
            });
        });

        view.addEventListener('viewbeforehide', function () {

            dom.removeEventListener(window, 'keydown', onWindowKeyDown, {
                passive: true
            });

            stopHideTimer();
            headerElement.classList.remove('osdHeader');
            headerElement.classList.remove('osdHeader-hidden');
            dom.removeEventListener(document, 'mousemove', onMouseMove, {
                passive: true
            });
            document.body.classList.remove('autoScrollY');

            inputManager.off(window, onInputCommand);
            events.off(playbackManager, 'playerchange', onPlayerChange);
            releaseCurrentPlayer();
        });

        if (appHost.supports('remotecontrol') && !layoutManager.tv) {
            view.querySelector('.btnCast').classList.remove('hide');
        }

        view.querySelector('.btnCast').addEventListener('click', function () {
            var btn = this;
            require(['playerSelectionMenu'], function (playerSelectionMenu) {
                playerSelectionMenu.show(btn);
            });
        });

        view.querySelector('.btnFullscreen').addEventListener('click', function () {
            playbackManager.toggleFullscreen(currentPlayer);
        });

        view.querySelector('.btnPip').addEventListener('click', function () {
            playbackManager.togglePictureInPicture(currentPlayer);
        });

        view.querySelector('.btnSettings').addEventListener('click', onSettingsButtonClick);

        function onPlayerChange() {

            var player = playbackManager.getCurrentPlayer();

            if (player && !player.isLocalPlayer) {
                view.querySelector('.btnCast i').innerHTML = '&#xE308;';
            } else {
                view.querySelector('.btnCast i').innerHTML = '&#xE307;';
            }
            bindToPlayer(player);
        }

        function onStateChanged(event, state) {

            //console.log('nowplaying event: ' + e.type);
            var player = this;

            if (!state.NowPlayingItem) {
                return;
            }

            isEnabled = true;

            updatePlayerStateInternal(event, state);
            updatePlaylist(player);

            enableStopOnBack(true);
        }

        function onPlayPauseStateChanged(e) {

            if (!isEnabled) {
                return;
            }

            var player = this;
            updatePlayPauseState(player.paused());
        }

        function onVolumeChanged(e) {

            if (!isEnabled) {
                return;
            }

            var player = this;

            updatePlayerVolumeState(player.isMuted(), player.getVolume());
        }

        function onPlaybackStart(e, state) {

            console.log('nowplaying event: ' + e.type);

            var player = this;

            onStateChanged.call(player, e, state);
        }

        function onPlaybackStopped(e, state) {

            currentRuntimeTicks = null;

            console.log('nowplaying event: ' + e.type);

            if (state.nextMediaType !== 'Video') {

                view.removeEventListener('viewbeforehide', onViewHideStopPlayback);

                Emby.Page.back();
            }
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
            events.on(player, 'playbackstop', onPlaybackStopped);
            events.on(player, 'volumechange', onVolumeChanged);
            events.on(player, 'pause', onPlayPauseStateChanged);
            events.on(player, 'playing', onPlayPauseStateChanged);
            events.on(player, 'timeupdate', onTimeUpdate);
            events.on(player, 'fullscreenchange', updateFullscreenIcon);
        }

        function releaseCurrentPlayer() {

            var player = currentPlayer;

            if (player) {

                events.off(player, 'playbackstart', onPlaybackStart);
                events.off(player, 'playbackstop', onPlaybackStopped);
                events.off(player, 'volumechange', onVolumeChanged);
                events.off(player, 'pause', onPlayPauseStateChanged);
                events.off(player, 'playing', onPlayPauseStateChanged);
                events.off(player, 'timeupdate', onTimeUpdate);
                events.off(player, 'fullscreenchange', updateFullscreenIcon);

                currentPlayer = null;
            }
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

        function updatePlayPauseState(isPaused) {

            if (isPaused) {
                view.querySelector('.btnPause i').innerHTML = '&#xE037;';
            } else {
                view.querySelector('.btnPause i').innerHTML = '&#xE034;';
            }
        }

        function updatePlayerStateInternal(event, state) {

            var playerInfo = playbackManager.getPlayerInfo();

            var playState = state.PlayState || {};

            updatePlayPauseState(playState.IsPaused);

            var supportedCommands = playerInfo.supportedCommands;
            currentPlayerSupportedCommands = supportedCommands;

            //if (supportedCommands.indexOf('SetRepeatMode') == -1) {
            //    toggleRepeatButton.classList.add('hide');
            //} else {
            //    toggleRepeatButton.classList.remove('hide');
            //}

            //if (playState.RepeatMode == 'RepeatAll') {
            //    toggleRepeatButtonIcon.innerHTML = "repeat";
            //    toggleRepeatButton.classList.add('repeatActive');
            //}
            //else if (playState.RepeatMode == 'RepeatOne') {
            //    toggleRepeatButtonIcon.innerHTML = "repeat_one";
            //    toggleRepeatButton.classList.add('repeatActive');
            //} else {
            //    toggleRepeatButtonIcon.innerHTML = "repeat";
            //    toggleRepeatButton.classList.remove('repeatActive');
            //}

            updatePlayerVolumeState(playState.IsMuted, playState.VolumeLevel);

            if (nowPlayingPositionSlider && !nowPlayingPositionSlider.dragging) {
                nowPlayingPositionSlider.disabled = !playState.CanSeek;
            }

            btnFastForward.disabled = !playState.CanSeek;
            btnRewind.disabled = !playState.CanSeek;

            var nowPlayingItem = state.NowPlayingItem || {};
            updateTimeDisplay(playState.PositionTicks, nowPlayingItem.RunTimeTicks);

            updateNowPlayingInfo(state);

            if (state.MediaSource && state.MediaSource.SupportsTranscoding && supportedCommands.indexOf('SetMaxStreamingBitrate') !== -1) {
                view.querySelector('.btnSettings').classList.remove('hide');
            } else {
                view.querySelector('.btnSettings').classList.add('hide');
            }

            if (supportedCommands.indexOf('ToggleFullscreen') === -1) {
                view.querySelector('.btnFullscreen').classList.add('hide');
            } else {
                view.querySelector('.btnFullscreen').classList.remove('hide');
            }

            if (supportedCommands.indexOf('PictureInPicture') === -1) {
                view.querySelector('.btnPip').classList.add('hide');
            } else {
                view.querySelector('.btnPip').classList.remove('hide');
            }

            updateFullscreenIcon();
        }

        function updateTimeDisplay(positionTicks, runtimeTicks) {

            if (nowPlayingPositionSlider && !nowPlayingPositionSlider.dragging) {
                if (runtimeTicks) {

                    var pct = positionTicks / runtimeTicks;
                    pct *= 100;

                    nowPlayingPositionSlider.value = pct;

                } else {

                    nowPlayingPositionSlider.value = 0;
                }

                if (runtimeTicks && positionTicks != null) {
                    endsAtText.innerHTML = '&nbsp;&nbsp;-&nbsp;&nbsp;' + mediaInfo.getEndsAtFromPosition(runtimeTicks, positionTicks, true);
                } else {
                    endsAtText.innerHTML = '';
                }
            }

            updateTimeText(nowPlayingPositionText, positionTicks);
            updateTimeText(nowPlayingDurationText, runtimeTicks, true);
        }

        function updatePlayerVolumeState(isMuted, volumeLevel) {

            var supportedCommands = currentPlayerSupportedCommands;

            var showMuteButton = true;
            var showVolumeSlider = true;

            if (supportedCommands.indexOf('Mute') === -1) {
                showMuteButton = false;
            }

            if (supportedCommands.indexOf('SetVolume') === -1) {
                showVolumeSlider = false;
            }

            if (currentPlayer.isLocalPlayer && appHost.supports('physicalvolumecontrol')) {
                showMuteButton = false;
                showVolumeSlider = false;
            }

            if (isMuted) {
                view.querySelector('.buttonMute').setAttribute('title', globalize.translate('Unmute'));
                view.querySelector('.buttonMute i').innerHTML = '&#xE04F;';
            } else {
                view.querySelector('.buttonMute').setAttribute('title', globalize.translate('Mute'));
                view.querySelector('.buttonMute i').innerHTML = '&#xE050;';
            }

            if (showMuteButton) {
                view.querySelector('.buttonMute').classList.remove('hide');
            } else {
                view.querySelector('.buttonMute').classList.add('hide');
            }

            // See bindEvents for why this is necessary
            if (nowPlayingVolumeSlider) {

                if (showVolumeSlider) {
                    nowPlayingVolumeSliderContainer.classList.remove('hide');
                } else {
                    nowPlayingVolumeSliderContainer.classList.add('hide');
                }

                if (!nowPlayingVolumeSlider.dragging) {
                    nowPlayingVolumeSlider.value = volumeLevel || 0;
                }
            }
        }

        function updatePlaylist(player) {

            var btnPreviousTrack = view.querySelector('.btnPreviousTrack');
            var btnNextTrack = view.querySelector('.btnNextTrack');

            btnPreviousTrack.classList.remove('hide');
            btnNextTrack.classList.remove('hide');

            btnNextTrack.disabled = false;
            btnPreviousTrack.disabled = false;
        }

        function updateTimeText(elem, ticks, divider) {

            if (ticks == null) {
                elem.innerHTML = '';
                return;
            }

            var html = datetime.getDisplayRunningTime(ticks);

            if (divider) {
                html = '&nbsp;/&nbsp;' + html;
            }

            elem.innerHTML = html;
        }

        function onSettingsButtonClick(e) {

            var btn = this;

            require(['playerSettingsMenu'], function (playerSettingsMenu) {
                playerSettingsMenu.show({
                    mediaType: 'Video',
                    player: currentPlayer,
                    positionTo: btn
                });
            });
        }

        function showAudioTrackSelection() {

            var player = currentPlayer;

            var audioTracks = playbackManager.audioTracks(player);

            var currentIndex = playbackManager.getAudioStreamIndex(player);

            var menuItems = audioTracks.map(function (stream) {

                var opt = {
                    name: stream.DisplayTitle,
                    id: stream.Index
                };

                if (stream.Index === currentIndex) {
                    opt.selected = true;
                }

                return opt;
            });

            var positionTo = this;

            require(['actionsheet'], function (actionsheet) {

                actionsheet.show({
                    items: menuItems,
                    title: globalize.translate('Audio'),
                    positionTo: positionTo
                }).then(function (id) {
                    var index = parseInt(id);
                    if (index !== currentIndex) {
                        playbackManager.setAudioStreamIndex(index, currentPlayer);
                    }
                });
            });
        }

        function showSubtitleTrackSelection() {

            var player = currentPlayer;

            var streams = playbackManager.subtitleTracks(player);

            var currentIndex = playbackManager.getSubtitleStreamIndex(player);
            if (currentIndex == null) {
                currentIndex = -1;
            }

            streams.unshift({
                Index: -1,
                DisplayTitle: globalize.translate('Off')
            });

            var menuItems = streams.map(function (stream) {

                var opt = {
                    name: stream.DisplayTitle,
                    id: stream.Index
                };

                if (stream.Index === currentIndex) {
                    opt.selected = true;
                }

                return opt;
            });

            var positionTo = this;

            require(['actionsheet'], function (actionsheet) {

                actionsheet.show({
                    title: globalize.translate('Subtitles'),
                    items: menuItems,
                    positionTo: positionTo
                }).then(function (id) {
                    var index = parseInt(id);
                    if (index !== currentIndex) {
                        playbackManager.setSubtitleStreamIndex(index, currentPlayer);
                    }
                });

            });
        }

        view.addEventListener('viewhide', function () {

            headerElement.classList.remove('hide');
        });

        function onWindowKeyDown(e) {
            if (e.keyCode === 32 && !isOsdOpen()) {
                playbackManager.playPause(currentPlayer);
                showOsd();
            }
        }

        view.querySelector('.pageContainer').addEventListener('click', function () {

            // TODO: Replace this check with click vs tap detection
            if (!browser.touch) {
                playbackManager.playPause(currentPlayer);
            }
            showOsd();
        });

        view.querySelector('.buttonMute').addEventListener('click', function () {

            playbackManager.toggleMute(currentPlayer);
        });

        nowPlayingVolumeSlider.addEventListener('change', function () {

            playbackManager.setVolume(this.value, currentPlayer);
        });

        nowPlayingPositionSlider.addEventListener('change', function () {

            if (currentPlayer) {

                var newPercent = parseFloat(this.value);

                playbackManager.seekPercent(newPercent, currentPlayer);
            }
        });

        function getImgUrl(item, chapter, index, maxWidth, apiClient) {

            if (chapter.ImageTag) {

                return apiClient.getScaledImageUrl(item.Id, {
                    maxWidth: maxWidth,
                    tag: chapter.ImageTag,
                    type: "Chapter",
                    index: index
                });
            }

            return null;
        }

        function getChapterBubbleHtml(apiClient, item, chapters, positionTicks) {

            var chapter;
            var index = -1;

            for (var i = 0, length = chapters.length; i < length; i++) {

                var currentChapter = chapters[i];

                if (positionTicks >= currentChapter.StartPositionTicks) {
                    chapter = currentChapter;
                    index = i;
                }
            }

            if (!chapter) {
                return null;
            }

            var src = getImgUrl(item, chapter, index, 400, apiClient);

            if (src) {

                var html = '<div class="chapterThumbContainer">';
                html += '<img class="chapterThumb" src="' + src + '" />';

                html += '<div class="chapterThumbTextContainer">';
                html += '<div class="chapterThumbText chapterThumbText-dim">';
                html += chapter.Name;
                html += '</div>';
                html += '<h1 class="chapterThumbText">';
                html += datetime.getDisplayRunningTime(positionTicks);
                html += '</h1>';
                html += '</div>';

                html += '</div>';

                return html;
            }

            return null;
        }

        nowPlayingPositionSlider.getBubbleHtml = function (value) {

            showOsd();

            if (!currentRuntimeTicks) {
                return '--:--';
            }

            var ticks = currentRuntimeTicks;
            ticks /= 100;
            ticks *= value;

            var item = currentItem;
            if (item && item.Chapters && item.Chapters.length && item.Chapters[0].ImageTag) {
                var html = getChapterBubbleHtml(connectionManager.getApiClient(item.ServerId), item, item.Chapters, ticks);

                if (html) {
                    return html;
                }
            }

            return '<h1 class="sliderBubbleText">' + datetime.getDisplayRunningTime(ticks) + '</h1>';
        };

        view.querySelector('.btnPreviousTrack').addEventListener('click', function () {

            playbackManager.previousChapter(currentPlayer);
        });

        view.querySelector('.btnPause').addEventListener('click', function () {

            playbackManager.playPause(currentPlayer);
        });

        view.querySelector('.btnNextTrack').addEventListener('click', function () {

            playbackManager.nextChapter(currentPlayer);
        });

        btnRewind.addEventListener('click', function () {

            playbackManager.rewind(currentPlayer);
        });

        btnFastForward.addEventListener('click', function () {

            playbackManager.fastForward(currentPlayer);
        });

        view.querySelector('.btnAudio').addEventListener('click', showAudioTrackSelection);
        view.querySelector('.btnSubtitles').addEventListener('click', showSubtitleTrackSelection);

        function onViewHideStopPlayback() {

            if (playbackManager.isPlayingVideo()) {

                var player = currentPlayer;

                // Unbind this event so that we don't go back twice
                view.removeEventListener('viewbeforehide', onViewHideStopPlayback);

                releaseCurrentPlayer();

                playbackManager.stop(player);

                // or 
                //Emby.Page.setTransparency(Emby.TransparencyLevel.Backdrop);
            }
        }

        function enableStopOnBack(enabled) {

            view.removeEventListener('viewbeforehide', onViewHideStopPlayback);

            if (enabled) {
                if (playbackManager.isPlayingVideo(currentPlayer)) {
                    view.addEventListener('viewbeforehide', onViewHideStopPlayback);
                }
            }
        }

    };

});