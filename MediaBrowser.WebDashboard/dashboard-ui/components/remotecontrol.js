define(['browser', 'datetime', 'libraryBrowser', 'listView', 'userdataButtons', 'imageLoader', 'playbackManager', 'nowPlayingHelper', 'events', 'apphost', 'cardStyle'], function (browser, datetime, libraryBrowser, listView, userdataButtons, imageLoader, playbackManager, nowPlayingHelper, events, appHost) {
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

                    player.setAudioStreamIndex(parseInt(id));
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
            name: Globalize.translate('ButtonOff'),
            selected: currentIndex == null
        });

        require(['actionsheet'], function (actionsheet) {

            actionsheet.show({
                items: menuItems,
                positionTo: button,
                callback: function (id) {

                    player.setSubtitleStreamIndex(parseInt(id));
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

        var url;
        var backdropUrl = null;

        if (!item) {
        }
        else if (item.PrimaryImageTag) {

            url = ApiClient.getScaledImageUrl(item.PrimaryImageItemId, {
                type: "Primary",
                maxHeight: 300,
                tag: item.PrimaryImageTag
            });
        }
        else if (item.BackdropImageTag) {

            url = ApiClient.getScaledImageUrl(item.BackdropItemId, {
                type: "Backdrop",
                maxHeight: 300,
                tag: item.BackdropImageTag,
                index: 0
            });

        } else if (item.ThumbImageTag) {

            url = ApiClient.getScaledImageUrl(item.ThumbImageItemId, {
                type: "Thumb",
                maxHeight: 300,
                tag: item.ThumbImageTag
            });
        }

        if (url == currentImgUrl) {
            return;
        }

        if (item && item.BackdropImageTag) {

            backdropUrl = ApiClient.getScaledImageUrl(item.BackdropItemId, {
                type: "Backdrop",
                maxHeight: 300,
                tag: item.BackdropImageTag,
                index: 0
            });

        }

        setImageUrl(context, url);

        if (item) {

            // This should be outside of the IF
            // But for now, if you change songs but keep the same artist, the backdrop will flicker because in-between songs it clears out the image
            if (!browser.slow) {
                // Exclude from mobile because it just doesn't perform well
                require(['backdrop'], function (backdrop) {
                    backdrop.setBackdrop(backdropUrl);
                });
            }

            ApiClient.getItem(Dashboard.getCurrentUserId(), item.Id).then(function (fullItem) {
                userdataButtons.fill({
                    item: fullItem,
                    includePlayed: false,
                    style: 'fab-mini',
                    element: context.querySelector('.nowPlayingPageUserDataButtons')
                });
            });
        } else {
            userdataButtons.destroy({
                element: context.querySelector('.nowPlayingPageUserDataButtons')
            });
        }
    }

    function setImageUrl(context, url) {
        currentImgUrl = url;

        if (url) {
            imageLoader.lazyImage(context.querySelector('.nowPlayingPageImage'), url);
        } else {
            context.querySelector('.nowPlayingPageImage').style.backgroundImage = '';
        }
    }

    function buttonEnabled(btn, enabled) {
        btn.disabled = !enabled;
    }

    function updateSupportedCommands(context, commands) {

        var all = context.querySelectorAll('.btnCommand');

        for (var i = 0, length = all.length; i < length; i++) {
            buttonEnabled(all[i], commands.indexOf(all[i].getAttribute('data-command')) != -1);
        }
    }

    function hideChapterMenu(page) {

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
            
            if (player && lastPlayerState) {
                var state = lastPlayerState;
                switch ((state.PlayState || {}).RepeatMode) {
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

            buttonEnabled(context.querySelector('.btnToggleFullscreen'), item && item.MediaType == 'Video' && supportedCommands.indexOf('ToggleFullscreen') != -1);
            buttonEnabled(context.querySelector('.btnAudioTracks'), hasStreams(item, 'Audio') && supportedCommands.indexOf('SetAudioStreamIndex') != -1);
            buttonEnabled(context.querySelector('.btnSubtitles'), hasStreams(item, 'Subtitle') && supportedCommands.indexOf('SetSubtitleStreamIndex') != -1);

            if (item && item.Chapters && item.Chapters.length && playState.CanSeek) {
                buttonEnabled(context.querySelector('.btnChapters'), true);

            } else {
                buttonEnabled(context.querySelector('.btnChapters'), false);
                hideChapterMenu(context);
            }

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

            buttonEnabled(context.querySelector('.btnStop'), item != null);
            buttonEnabled(context.querySelector('.btnNextTrack'), item != null);
            buttonEnabled(context.querySelector('.btnPreviousTrack'), item != null);

            var positionSlider = context.querySelector('.nowPlayingPositionSlider');
            if (positionSlider && !positionSlider.dragging) {
                positionSlider.disabled = !playState.CanSeek;
            }

            updatePlayPauseState(playState.IsPaused, item != null);

            var runtimeTicks = item ? item.RunTimeTicks : null;
            updateTimeDisplay(playState.PositionTicks, runtimeTicks);
            updatePlayerVolumeState(playState.IsMuted, playState.VolumeLevel);

            if (item && item.MediaType == 'Video') {
                context.classList.remove('hideVideoButtons');
            } else {
                context.classList.add('hideVideoButtons');
            }

            if (playerInfo.isLocalPlayer && appHost.supports('physicalvolumecontrol')) {
                context.classList.add('hideVolumeButtons');
            } else {
                context.classList.remove('hideVolumeButtons');
            }

            if (item && item.MediaType == 'Audio') {
                context.querySelector('.buttonsRow2').classList.add('hide');
            } else {
                context.querySelector('.buttonsRow2').classList.remove('hide');
            }

            var toggleRepeatButton = context.querySelector('.repeatToggleButton');

            if (playState.RepeatMode == 'RepeatAll') {
                toggleRepeatButton.innerHTML = "<i class='md-icon'>repeat</i>";
                toggleRepeatButton.classList.add('nowPlayingPageRepeatActive');
            }
            else if (playState.RepeatMode == 'RepeatOne') {
                toggleRepeatButton.innerHTML = "<i class='md-icon'>repeat_one</i>";
                toggleRepeatButton.classList.add('nowPlayingPageRepeatActive');
            } else {
                toggleRepeatButton.innerHTML = "<i class='md-icon'>repeat</i>";
                toggleRepeatButton.classList.remove('nowPlayingPageRepeatActive');
            }

            updateNowPlayingInfo(context, state);
        }

        function updatePlayerVolumeState(isMuted, volumeLevel) {

        }

        function updatePlayPauseState(isPaused, isActive) {

            var context = dlg;

            var btnPause = context.querySelector('.btnPause');
            var btnPlay = context.querySelector('.btnPlay');

            buttonEnabled(btnPause, isActive);
            buttonEnabled(btnPlay, isActive);

            if (isPaused) {

                hideButton(btnPause);
                showButton(btnPlay);

            } else {

                showButton(btnPause);
                hideButton(btnPlay);
            }
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

        function loadPlaylist(context) {

            var html = '';

            //ApiClient.getItems(Dashboard.getCurrentUserId(), {

            //    SortBy: "SortName",
            //    SortOrder: "Ascending",
            //    IncludeItemTypes: "Audio",
            //    Recursive: true,
            //    Fields: "PrimaryImageAspectRatio,SortName,MediaSourceCount",
            //    StartIndex: 0,
            //    ImageTypeLimit: 1,
            //    EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
            //    Limit: 100

            //}).then(function (result) {

            //    html += listView.getListViewHtml({
            //        items: result.Items,
            //        smallIcon: true
            //    });

            //    page(".playlist").html(html).lazyChildren();
            //});

            html += listView.getListViewHtml({
                items: playbackManager.playlist(),
                smallIcon: true,
                action: 'setplaylistindex'
            });

            playlistNeedsRefresh = false;

            var deps = [];

            require(deps, function () {

                var itemsContainer = context.querySelector('.playlist');

                itemsContainer.innerHTML = html;

                var index = playbackManager.currentPlaylistIndex();

                if (index != -1) {

                    var item = itemsContainer.querySelectorAll('.listItem')[index];
                    if (item) {
                        var img = item.querySelector('.listItemImage');

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

            loadPlaylist(dlg);
        }

        function onPlaybackStopped(e, state) {

            console.log('remotecontrol event: ' + e.type);

            loadPlaylist(dlg);
        }

        function onPlayPauseStateChanged(e) {

            var player = this;
            updatePlayPauseState(player.paused(), true);
        }

        function onStateChanged(event, state) {

            //console.log('nowplaying event: ' + e.type);
            var player = this;

            updatePlayerState(dlg, state);
        }

        function onTimeUpdate(e) {

            // Try to avoid hammering the document with changes
            var now = new Date().getTime();
            if ((now - lastUpdateTime) < 700) {

                return;
            }
            lastUpdateTime = now;

            var player = this;
            var state = lastPlayerState;
            var nowPlayingItem = state.NowPlayingItem || {};
            currentRuntimeTicks = playbackManager.duration(player);
            updateTimeDisplay(playbackManager.currentTime(player), currentRuntimeTicks);
        }

        function onVolumeChanged(e) {

            var player = this;

            updatePlayerVolumeState(player.isMuted(), player.getVolume());
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
            // TODO: Replace this with smaller changes on repeatmodechange. 
            // For now go cheap and just refresh the entire component
            events.on(player, 'repeatmodechange', onPlaybackStart);
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
            var btnCast = context.querySelector('.nowPlayingCastIcon');

            if (info && !info.isLocalPlayer) {

                btnCast.querySelector('i').innerHTML = 'cast_connected';
                btnCast.classList.add('btnActiveCast');
                context.querySelector('.nowPlayingSelectedPlayer').innerHTML = info.deviceName || info.name;
            } else {
                btnCast.querySelector('i').innerHTML = 'cast';
                btnCast.classList.remove('btnActiveCast');
                context.querySelector('.nowPlayingSelectedPlayer').innerHTML = '';
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

            context.querySelector('.btnChapters').addEventListener('click', function () {

                //if (currentPlayer && lastPlayerState) {

                //    var currentPositionTicks = lastPlayerState.PlayState.PositionTicks;
                //    showChapterMenu(context, lastPlayerState.NowPlayingItem, currentPositionTicks);
                //}
            });

            context.querySelector('.btnStop').addEventListener('click', function () {

                if (currentPlayer) {
                    playbackManager.stop(currentPlayer);
                }
            });

            context.querySelector('.btnPlay').addEventListener('click', function () {

                if (currentPlayer) {
                    currentPlayer.unpause();
                }
            });

            context.querySelector('.btnPause').addEventListener('click', function () {

                if (currentPlayer) {
                    currentPlayer.pause();
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

            context.querySelector('.nowPlayingPositionSlider', context).getBubbleText = function (value) {

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

            context.querySelector('.nowPlayingCastIcon').addEventListener('click', function () {
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

            var mdlTabs = context.querySelector('.libraryViewNav');

            context.querySelector('.libraryViewNav').classList.add('bottom');

            libraryBrowser.configurePaperLibraryTabs(ownerView, mdlTabs, ownerView.querySelectorAll('.pageTabContent'));

            mdlTabs.addEventListener('tabchange', function (e) {

                if (e.detail.selectedTabIndex == 2 && playlistNeedsRefresh) {
                    loadPlaylist(context);
                }
            });

            events.on(playbackManager, 'playerchange', onPlayerChange);
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