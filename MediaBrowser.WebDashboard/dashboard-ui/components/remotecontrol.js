define(['browser', 'datetime', 'backdrop', 'libraryBrowser', 'listView', 'userdataButtons', 'imageLoader', 'playbackManager', 'nowPlayingHelper', 'events', 'connectionManager', 'apphost', 'globalize', 'cardStyle'], function (browser, datetime, backdrop, libraryBrowser, listView, userdataButtons, imageLoader, playbackManager, nowPlayingHelper, events, connectionManager, appHost, globalize) {
    'use strict';

    function showSlideshowMenu(context) {
        require(['scripts/slideshow'], function () {
            SlideShow.showMenu();
        });
    }

    function showAudioMenu(context, player, button, item, currentIndex) {

        var streams = (item.MediaStreams || []).filter(function (i) {

            return i.Type == 'Audio';
        });

        var menuItems = streams.map(function (s) {

            var menuItem = {
                name: s.DisplayTitle,
                id: s.Index
            };

            if (s.Index == currentIndex) {
                menuItem.selected = true;
            }

            return menuItem;
        });

        require(['actionsheet'], function (actionsheet) {

            actionsheet.show({
                items: menuItems,
                positionTo: button,
                callback: function (id) {

                    playbackManager.setAudioStreamIndex(parseInt(id), player);
                }
            });

        });
    }

    function showSubtitleMenu(context, player, button, item, currentIndex) {

        var streams = (item.MediaStreams || []).filter(function (i) {

            return i.Type == 'Subtitle';
        });

        var menuItems = streams.map(function (s) {

            var menuItem = {
                name: s.DisplayTitle,
                id: s.Index
            };

            if (s.Index == currentIndex) {
                menuItem.selected = true;
            }

            return menuItem;
        });

        menuItems.unshift({
            id: -1,
            name: globalize.translate('ButtonOff'),
            selected: currentIndex == null
        });

        require(['actionsheet'], function (actionsheet) {

            actionsheet.show({
                items: menuItems,
                positionTo: button,
                callback: function (id) {

                    playbackManager.setSubtitleStreamIndex(parseInt(id), player);
                }
            });

        });
    }

    function showButton(button) {
        button.classList.remove('hide');
    }

    function hideButton(button) {
        button.classList.add('hide');
    }

    function hasStreams(item, type) {
        return item && item.MediaStreams && item.MediaStreams.filter(function (i) {
            return i.Type == type;
        }).length > 0;
    }

    function getNowPlayingNameHtml(nowPlayingItem, includeNonNameInfo) {

        var names = nowPlayingHelper.getNowPlayingNames(nowPlayingItem, includeNonNameInfo);

        return names.map(function (i) {

            return i.text;

        }).join('<br/>');
    }

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

        if (item.AlbumId && item.AlbumPrimaryImageTag) {

            options.tag = item.AlbumPrimaryImageTag;
            return connectionManager.getApiClient(item.ServerId).getScaledImageUrl(item.AlbumId, options);
        }

        return null;
    }
    var currentImgUrl;
    function updateNowPlayingInfo(context, state) {

        var item = state.NowPlayingItem;
        var displayName = item ? getNowPlayingNameHtml(item).replace('<br/>', ' - ') : '';

        context.querySelector('.nowPlayingPageTitle').innerHTML = displayName;

        if (displayName.length > 0) {
            context.querySelector('.nowPlayingPageTitle').classList.remove('hide');
        } else {
            context.querySelector('.nowPlayingPageTitle').classList.add('hide');
        }

        var url = item ? seriesImageUrl(item, {

            maxHeight: 300

        }) || imageUrl(item, {
            maxHeight: 300

        }) : null;

        if (url === currentImgUrl) {
            return;
        }

        setImageUrl(context, url);

        if (item) {

            backdrop.setBackdrops([item]);

            ApiClient.getItem(Dashboard.getCurrentUserId(), item.Id).then(function (fullItem) {
                userdataButtons.fill({
                    item: fullItem,
                    includePlayed: false,
                    style: 'icon',
                    element: context.querySelector('.nowPlayingPageUserDataButtons'),
                });
            });
        } else {

            backdrop.clear();

            userdataButtons.destroy({
                element: context.querySelector('.nowPlayingPageUserDataButtons')
            });
        }
    }

    function setImageUrl(context, url) {
        currentImgUrl = url;

        var imgContainer = context.querySelector('.nowPlayingPageImageContainer');

        if (url) {
            imgContainer.innerHTML = '<img class="nowPlayingPageImage" src="' + url + '" />';
            imgContainer.classList.remove('hide');
        } else {
            imgContainer.classList.add('hide');
            imgContainer.innerHTML = '';
        }
    }

    function buttonEnabled(btn, enabled) {
        btn.disabled = !enabled;
    }

    function buttonVisible(btn, enabled) {
        if (enabled) {
            btn.classList.remove('hide');
        } else {
            btn.classList.add('hide');
        }
    }

    function updateSupportedCommands(context, commands) {

        var all = context.querySelectorAll('.btnCommand');

        for (var i = 0, length = all.length; i < length; i++) {
            buttonEnabled(all[i], commands.indexOf(all[i].getAttribute('data-command')) != -1);
        }
    }

    return function () {

        var dlg;
        var currentPlayer;
        var currentPlayerSupportedCommands = [];
        var lastPlayerState;
        var lastUpdateTime = 0;
        var currentRuntimeTicks = 0;

        var self = this;
        var playlistNeedsRefresh = true;

        function toggleRepeat(player) {

            if (player) {
                switch (playbackManager.getRepeatMode(player)) {
                    case 'RepeatNone':
                        playbackManager.setRepeatMode('RepeatAll', player);
                        break;
                    case 'RepeatAll':
                        playbackManager.setRepeatMode('RepeatOne', player);
                        break;
                    case 'RepeatOne':
                        playbackManager.setRepeatMode('RepeatNone', player);
                        break;
                }
            }
        }

        function updatePlayerState(context, state) {

            lastPlayerState = state;

            var item = state.NowPlayingItem;

            var playerInfo = playbackManager.getPlayerInfo();

            var supportedCommands = playerInfo.supportedCommands;
            var playState = state.PlayState || {};

            buttonVisible(context.querySelector('.btnToggleFullscreen'), item && item.MediaType == 'Video' && supportedCommands.indexOf('ToggleFullscreen') != -1);
            buttonVisible(context.querySelector('.btnAudioTracks'), hasStreams(item, 'Audio') && supportedCommands.indexOf('SetAudioStreamIndex') != -1);
            buttonVisible(context.querySelector('.btnSubtitles'), hasStreams(item, 'Subtitle') && supportedCommands.indexOf('SetSubtitleStreamIndex') != -1);

            if (supportedCommands.indexOf('DisplayMessage') != -1) {
                context.querySelector('.sendMessageSection').classList.remove('hide');
            } else {
                context.querySelector('.sendMessageSection').classList.add('hide');
            }
            if (supportedCommands.indexOf('SendString') != -1) {
                context.querySelector('.sendTextSection').classList.remove('hide');
            } else {
                context.querySelector('.sendTextSection').classList.add('hide');
            }

            buttonVisible(context.querySelector('.btnStop'), item != null);
            buttonVisible(context.querySelector('.btnNextTrack'), item != null);
            buttonVisible(context.querySelector('.btnPreviousTrack'), item != null);

            var positionSlider = context.querySelector('.nowPlayingPositionSlider');
            if (positionSlider && !positionSlider.dragging) {
                positionSlider.disabled = !playState.CanSeek;
            }

            updatePlayPauseState(playState.IsPaused, item != null);

            var runtimeTicks = item ? item.RunTimeTicks : null;
            updateTimeDisplay(playState.PositionTicks, runtimeTicks);
            updatePlayerVolumeState(context, playState.IsMuted, playState.VolumeLevel);

            if (item && item.MediaType == 'Video') {
                context.classList.remove('hideVideoButtons');
            } else {
                context.classList.add('hideVideoButtons');
            }

            updateRepeatModeDisplay(playState.RepeatMode);
            updateNowPlayingInfo(context, state);
        }

        function updateRepeatModeDisplay(repeatMode) {

            var context = dlg;
            var toggleRepeatButton = context.querySelector('.repeatToggleButton');

            if (repeatMode == 'RepeatAll') {
                toggleRepeatButton.innerHTML = "<i class='md-icon'>repeat</i>";
                toggleRepeatButton.classList.add('nowPlayingPageRepeatActive');
            }
            else if (repeatMode == 'RepeatOne') {
                toggleRepeatButton.innerHTML = "<i class='md-icon'>repeat_one</i>";
                toggleRepeatButton.classList.add('nowPlayingPageRepeatActive');
            } else {
                toggleRepeatButton.innerHTML = "<i class='md-icon'>repeat</i>";
                toggleRepeatButton.classList.remove('nowPlayingPageRepeatActive');
            }
        }

        function updatePlayerVolumeState(context, isMuted, volumeLevel) {

            var view = context;
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

            var nowPlayingVolumeSlider = context.querySelector('.nowPlayingVolumeSlider');
            var nowPlayingVolumeSliderContainer = context.querySelector('.nowPlayingVolumeSliderContainer');

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

        function updatePlayPauseState(isPaused, isActive) {

            var context = dlg;

            var btnPlayPause = context.querySelector('.btnPlayPause');
            if (isPaused) {
                btnPlayPause.querySelector('i').innerHTML = 'play_arrow';
            } else {
                btnPlayPause.querySelector('i').innerHTML = 'pause';
            }

            buttonVisible(btnPlayPause, isActive);
        }

        function updateTimeDisplay(positionTicks, runtimeTicks) {

            // See bindEvents for why this is necessary
            var context = dlg;
            var positionSlider = context.querySelector('.nowPlayingPositionSlider');

            if (positionSlider && !positionSlider.dragging) {
                if (runtimeTicks) {

                    var pct = positionTicks / runtimeTicks;
                    pct *= 100;

                    positionSlider.value = pct;

                } else {

                    positionSlider.value = 0;
                }
            }

            if (positionTicks == null) {
                context.querySelector('.positionTime').innerHTML = '--:--';
            } else {
                context.querySelector('.positionTime').innerHTML = datetime.getDisplayRunningTime(positionTicks);
            }

            if (runtimeTicks != null) {
                context.querySelector('.runtime').innerHTML = datetime.getDisplayRunningTime(runtimeTicks);
            } else {
                context.querySelector('.runtime').innerHTML = '--:--';
            }
        }

        function getPlaylistItems(player) {

            return playbackManager.getPlaylist(player);

            return ApiClient.getItems(Dashboard.getCurrentUserId(), {

                SortBy: "SortName",
                SortOrder: "Ascending",
                IncludeItemTypes: "Audio",
                Recursive: true,
                Fields: "PrimaryImageAspectRatio,SortName,MediaSourceCount",
                StartIndex: 0,
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                Limit: 100

            }).then(function (result) {

                return result.Items;
            });
        }

        function loadPlaylist(context, player) {

            getPlaylistItems(player).then(function (items) {

                var html = '';

                html += listView.getListViewHtml({
                    items: items,
                    smallIcon: true,
                    action: 'setplaylistindex',
                    enableUserDataButtons: false,
                    rightButtons: [
                    {
                        icon: '&#xE15D;',
                        title: globalize.translate('ButtonRemove'),
                        id: 'remove'
                    }],
                    dragHandle: true
                });

                playlistNeedsRefresh = false;

                var itemsContainer = context.querySelector('.playlist');

                itemsContainer.innerHTML = html;

                var playlistItemId = playbackManager.getCurrentPlaylistItemId(player);

                if (playlistItemId) {

                    var img = itemsContainer.querySelector('.listItem[data-playlistItemId="' + playlistItemId + '"] .listItemImage');
                    if (img) {

                        img.classList.remove('lazy');
                        img.classList.add('playlistIndexIndicatorImage');
                    }
                }

                imageLoader.lazyChildren(itemsContainer);
            });
        }

        function onPlaybackStart(e, state) {

            console.log('remotecontrol event: ' + e.type);

            var player = this;
            onStateChanged.call(player, e, state);

            loadPlaylist(dlg, player);
        }

        function onRepeatModeChange(e) {

            var player = this;

            updateRepeatModeDisplay(playbackManager.getRepeatMode(player));
        }

        function onPlaylistUpdate(e) {

            var player = this;

            playbackManager.getPlayerState(player).then(function (state) {

                onStateChanged.call(player, { type: 'init' }, state);
            });
        }

        function onPlaylistItemRemoved(e, info) {

            var context = dlg;

            var playlistItemIds = info.playlistItemIds;

            for (var i = 0, length = playlistItemIds.length; i < length; i++) {

                var listItem = context.querySelector('.listItem[data-playlistItemId="' + playlistItemIds[i] + '"]');

                if (listItem) {
                    listItem.parentNode.removeChild(listItem);
                }
            }
        }

        function onPlaybackStopped(e, stopInfo) {

            console.log('remotecontrol event: ' + e.type);
            var player = this;

            if (!stopInfo.nextMediaType) {
                updatePlayerState(dlg, {});
                loadPlaylist(dlg);
            }
        }

        function onPlayPauseStateChanged(e) {

            var player = this;
            updatePlayPauseState(player.paused(), true);
        }

        function onStateChanged(event, state) {

            //console.log('nowplaying event: ' + e.type);
            var player = this;

            updatePlayerState(dlg, state);
            loadPlaylist(dlg, player);
        }

        function onTimeUpdate(e) {

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

        function onVolumeChanged(e) {

            var player = this;

            updatePlayerVolumeState(dlg, player.isMuted(), player.getVolume());
        }

        function releaseCurrentPlayer() {

            var player = currentPlayer;

            if (player) {

                events.off(player, 'playbackstart', onPlaybackStart);
                events.off(player, 'statechange', onPlaybackStart);
                events.off(player, 'repeatmodechange', onRepeatModeChange);
                events.off(player, 'playlistitemremove', onPlaylistUpdate);
                events.off(player, 'playlistitemmove', onPlaylistUpdate);
                events.off(player, 'playbackstop', onPlaybackStopped);
                events.off(player, 'volumechange', onVolumeChanged);
                events.off(player, 'pause', onPlayPauseStateChanged);
                events.off(player, 'playing', onPlayPauseStateChanged);
                events.off(player, 'timeupdate', onTimeUpdate);

                currentPlayer = null;
            }
        }

        function bindToPlayer(context, player) {

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
            events.on(player, 'repeatmodechange', onRepeatModeChange);
            events.on(player, 'playlistitemremove', onPlaylistItemRemoved);
            events.on(player, 'playlistitemmove', onPlaylistUpdate);
            events.on(player, 'playbackstop', onPlaybackStopped);
            events.on(player, 'volumechange', onVolumeChanged);
            events.on(player, 'pause', onPlayPauseStateChanged);
            events.on(player, 'playing', onPlayPauseStateChanged);
            events.on(player, 'timeupdate', onTimeUpdate);

            var playerInfo = playbackManager.getPlayerInfo();

            var supportedCommands = playerInfo.supportedCommands;
            currentPlayerSupportedCommands = supportedCommands;

            updateSupportedCommands(context, supportedCommands);
        }

        function updateCastIcon(context) {

            var info = playbackManager.getPlayerInfo();
            var btnCast = context.querySelector('.btnCast');

            if (info && !info.isLocalPlayer) {

                btnCast.querySelector('i').innerHTML = 'cast_connected';
                btnCast.classList.add('btnActiveCast');
            } else {
                btnCast.querySelector('i').innerHTML = 'cast';
                btnCast.classList.remove('btnActiveCast');
            }
        }

        function onBtnCommandClick() {
            if (currentPlayer) {

                if (this.classList.contains('repeatToggleButton')) {
                    toggleRepeat(currentPlayer);
                } else {
                    playbackManager.sendCommand({
                        Name: this.getAttribute('data-command')

                    }, currentPlayer);
                }
            }
        }

        function bindEvents(context) {

            var btnCommand = context.querySelectorAll('.btnCommand');
            for (var i = 0, length = btnCommand.length; i < length; i++) {
                btnCommand[i].addEventListener('click', onBtnCommandClick);
            }

            context.querySelector('.btnToggleFullscreen').addEventListener('click', function (e) {

                if (currentPlayer) {
                    playbackManager.sendCommand({
                        Name: e.target.getAttribute('data-command')

                    }, currentPlayer);
                }
            });

            context.querySelector('.btnAudioTracks').addEventListener('click', function (e) {

                if (currentPlayer && lastPlayerState && lastPlayerState.PlayState) {

                    var currentIndex = lastPlayerState.PlayState.AudioStreamIndex;
                    showAudioMenu(context, currentPlayer, e.target, lastPlayerState.NowPlayingItem, currentIndex);
                }
            });

            context.querySelector('.btnSubtitles').addEventListener('click', function (e) {

                if (currentPlayer && lastPlayerState && lastPlayerState.PlayState) {

                    var currentIndex = lastPlayerState.PlayState.SubtitleStreamIndex;
                    showSubtitleMenu(context, currentPlayer, e.target, lastPlayerState.NowPlayingItem, currentIndex);
                }
            });

            context.querySelector('.btnStop').addEventListener('click', function () {

                if (currentPlayer) {
                    playbackManager.stop(currentPlayer);
                }
            });

            context.querySelector('.btnPlayPause').addEventListener('click', function () {

                if (currentPlayer) {
                    playbackManager.playPause(currentPlayer);
                }
            });

            context.querySelector('.btnNextTrack').addEventListener('click', function () {

                if (currentPlayer) {
                    playbackManager.nextTrack(currentPlayer);
                }
            });

            context.querySelector('.btnPreviousTrack').addEventListener('click', function () {

                if (currentPlayer) {
                    playbackManager.previousTrack(currentPlayer);
                }
            });

            context.querySelector('.nowPlayingPositionSlider').addEventListener('change', function () {

                var value = this.value;

                if (currentPlayer) {

                    var newPercent = parseFloat(value);
                    playbackManager.seekPercent(newPercent, currentPlayer);
                }
            });

            context.querySelector('.nowPlayingPositionSlider').getBubbleText = function (value) {

                var state = lastPlayerState;

                if (!state || !state.NowPlayingItem || !currentRuntimeTicks) {
                    return '--:--';
                }

                var ticks = currentRuntimeTicks;
                ticks /= 100;
                ticks *= value;

                return datetime.getDisplayRunningTime(ticks);
            };

            context.querySelector('.nowPlayingVolumeSlider').addEventListener('change', function () {

                playbackManager.setVolume(this.value, currentPlayer);
            });

            context.querySelector('.buttonMute').addEventListener('click', function () {

                playbackManager.toggleMute(currentPlayer);
            });

            var playlistContainer = context.querySelector('.playlist');

            playlistContainer.addEventListener('action-remove', function (e) {

                playbackManager.removeFromPlaylist([e.detail.playlistItemId], currentPlayer);
            });
            playlistContainer.addEventListener('itemdrop', function (e) {

                var newIndex = e.detail.newIndex;
                var playlistItemId = e.detail.playlistItemId;

                playbackManager.movePlaylistItem(playlistItemId, newIndex, currentPlayer);
            });

            playlistContainer.enableDragReordering(true);
        }

        function onPlayerChange() {

            var context = dlg;
            updateCastIcon(context);
            bindToPlayer(context, playbackManager.getCurrentPlayer());
        }

        function onMessageSubmit(e) {

            var form = e.target;

            playbackManager.sendCommand({
                Name: 'DisplayMessage',
                Arguments: {

                    Header: form.querySelector('#txtMessageTitle').value,
                    Text: form.querySelector('#txtMessageText', form).value
                }

            }, currentPlayer);

            form.querySelector('input').value = '';
            require(['toast'], function (toast) {
                toast('Message sent.');
            });

            e.preventDefault();
            e.stopPropagation();
            return false;
        }

        function onSendStringSubmit(e) {

            var form = e.target;

            playbackManager.sendCommand({
                Name: 'SendString',
                Arguments: {

                    String: form.querySelector('#txtTypeText', form).value
                }

            }, currentPlayer);

            form.querySelector('input').value = '';
            require(['toast'], function (toast) {
                toast('Text sent.');
            });

            e.preventDefault();
            e.stopPropagation();
            return false;
        }

        function init(ownerView, context) {

            require(['css!css/nowplaying.css']);
            bindEvents(context);

            context.querySelector('.sendMessageForm').addEventListener('submit', onMessageSubmit);
            context.querySelector('.typeTextForm').addEventListener('submit', onSendStringSubmit);

            context.querySelector('.btnCast').addEventListener('click', function () {
                var btn = this;
                require(['playerSelectionMenu'], function (playerSelectionMenu) {
                    playerSelectionMenu.show(btn);
                });
            });

            context.querySelector('.btnExitRemoteControl').addEventListener('click', function () {
                history.back();
            });

            //context.querySelector('.btnSlideshow').addEventListener('click', function () {
            //    showSlideshowMenu(context);
            //});

            events.on(playbackManager, 'playerchange', onPlayerChange);

            if (appHost.supports('remotecontrol')) {
                context.querySelector('.btnCast').classList.remove('hide');
            }
        }

        function onDialogClosed(e) {

            releaseCurrentPlayer();

            events.off(playbackManager, 'playerchange', onPlayerChange);

            lastPlayerState = null;
        }

        function onShow(context, tab) {

            currentImgUrl = null;

            bindToPlayer(context, playbackManager.getCurrentPlayer());

            updateCastIcon(context);
        }

        self.init = function (ownerView, context) {

            dlg = context;

            init(ownerView, dlg);
        };

        self.onShow = function () {
            onShow(dlg, window.location.hash);
        };

        self.destroy = function () {
            onDialogClosed();
        };

    };
});