define(['browser', 'paper-fab', 'paper-tabs', 'paper-slider', 'paper-icon-button'], function (browser) {

    function getAnimatedPagesHtml() {

        var html = '';

        html += '<div class="nowPlayingPageTab hide" data-tab="0">\
                    <div style="text-align:center;">\
                        <div class="nowPlayingPageTitle" style="line-height: normal;">\
                        </div>\
                        <div class="nowPlayingInfoMetadata">\
                            <div class="nowPlayingPageImage" style="margin: 1em auto;"></div>\
                            <div class="nowPlayingPageTimeContainer">\
                                <div>\
                                    <paper-slider pin step="1" min="0" max="100" value="0" class="nowPlayingPositionSlider"></paper-slider>\
                                </div>\
                                <div style="text-align:left;">\
                                    <div class="positionTime" style="float:left;"></div>\
                                    <div class="runtime" style="float: right;"></div>\
                                </div>\
                            </div>\
                        </div>\
                        <div class="nowPlayingInfoButtons">\
                            <div>\
                                <paper-fab icon="skip-previous" class="btnPreviousTrack btnPlayStateCommand subdued" title="${ButtonPreviousTrack}"></paper-fab>\
                                <paper-fab icon="pause" class="btnPause btnPlayStateCommand subdued" title="${ButtonPause}"></paper-fab>\
                                <paper-fab icon="play-arrow" class="btnPlay btnPlayStateCommand subdued" title="${ButtonPlay}"></paper-fab>\
                                <paper-fab icon="stop" class="btnPlayStateCommand btnStop subdued" title="${ButtonStop}"></paper-fab>\
                                <paper-fab icon="skip-next" class="btnPlayStateCommand btnNextTrack subdued" title="${ButtonNextTrack}"></paper-fab>\
                            </div>\
                            <div class="buttonsRow2">\
                                <paper-fab icon="audiotrack" class="btnAudioTracks videoButton btnPlayStateCommand subdued" title="${ButtonAudioTracks}" data-command="GoToSearch"></paper-fab>\
                                <paper-fab icon="closed-caption" class="btnSubtitles videoButton btnPlayStateCommand subdued" title="${ButtonSubtitles}" data-command="GoToSearch"></paper-fab>\
                                <paper-fab icon="movie" class="btnChapters videoButton btnPlayStateCommand subdued" title="${ButtonScenes}" data-command="GoToSearch"></paper-fab>\
                                <paper-fab icon="fullscreen" class="btnToggleFullscreen videoButton btnPlayStateCommand subdued" title="${ButtonFullscreen}" data-command="ToggleFullscreen"></paper-fab>\
                            </div>\
                            <!--<div class="buttonsRow3">\
                                <paper-fab icon="info" class="btnCommand videoButton subdued" title="${ButtonOsd}" data-command="ToggleOsdMenu"></paper-fab>\
                            </div>-->\
                            <div>\
                                <paper-fab icon="repeat" class="btnCommand subdued repeatToggleButton" title="${ButtonRepeat}" data-command="SetRepeatMode"></paper-fab>\
                                <paper-fab icon="volume-off" class="btnCommand subdued volumeButton" title="${ButtonMute}" data-command="ToggleMute"></paper-fab>\
                                <paper-fab icon="volume-down" class="btnCommand subdued volumeButton" title="${ButtonVolumeDown}" data-command="VolumeDown"></paper-fab>\
                                <paper-fab icon="volume-up" class="btnCommand subdued volumeButton" title="${ButtonVolumeUp}" data-command="VolumeUp"></paper-fab>\
                            </div>\
                            <div class="nowPlayingPageUserDataButtons" style="margin-top:1em;">\
                            </div>\
                        </div>\
                    </div>\
                </div>\
                <div class="nowPlayingPageTab hide" data-tab="1">\
                    <div style="text-align:center;">\
                        <div>\
                            <paper-fab icon="keyboard-arrow-up" class="btnArrowUp btnCommand subdued" title="${ButtonArrowUp}" data-command="MoveUp"></paper-fab>\
                        </div>\
                        <div>\
                            <paper-fab icon="keyboard-arrow-left" class="btnArrowLeft btnCommand subdued" title="${ButtonArrowLeft}" data-command="MoveLeft"></paper-fab>\
                            <paper-fab icon="check" class="btnOk btnCommand subdued" title="${ButtonOk}" data-command="Select"></paper-fab>\
                            <paper-fab icon="keyboard-arrow-right" class="btnArrowRight btnCommand subdued" title="${ButtonArrowRight}" data-command="MoveRight"></paper-fab>\
                        </div>\
                        <div>\
                            <paper-fab icon="keyboard-arrow-down" class="btnArrowDown btnCommand subdued" title="${ButtonArrowDown}" data-command="MoveDown"></paper-fab>\
                        </div>\
                        <div>\
                            <paper-fab icon="arrow-back" class="btnBack btnCommand subdued" title="${ButtonBack}" data-command="Back"></paper-fab>\
                            <paper-fab icon="info" class="btnInfo btnCommand subdued" title="${ButtonInfo}" data-command="ToggleContextMenu"></paper-fab>\
                        </div>\
                        <br />\
                        <div>\
                            <paper-fab icon="home" class="btnGoHome btnCommand subdued" title="${ButtonHome}" data-command="GoHome"></paper-fab>\
                            <!--<button data-inline="true" data-iconpos="right" title="${ButtonPageUp}" data-icon="plus" class="btnPageUp btnCommand ui-nodisc-icon" data-command="PageUp">${PageButtonAbbreviation}</button>\
                            <button data-inline="true" data-iconpos="right" title="${ButtonLetterUp}" data-icon="plus" class="btnLetterUp btnCommand ui-nodisc-icon" data-command="NextLetter">${LetterButtonAbbreviation}</button>-->\
                            <paper-fab icon="search" class="btnShowSearch btnCommand subdued" title="${ButtonSearch}" data-command="GoToSearch"></paper-fab>\
                        </div>\
                        <div>\
                            <paper-fab icon="settings" class="bthShowSettings btnCommand subdued" title="${ButtonSettings}" data-command="GoToSettings"></paper-fab>\
                            <!--<button data-inline="true" data-iconpos="right" title="${ButtonPageDown}" data-icon="minus" class="btnPageDown btnCommand ui-nodisc-icon" data-command="PageDown">${PageButtonAbbreviation}</button>\
                            <button data-inline="true" data-iconpos="right" title="${ButtonLetterDown}" data-icon="minus" class="btnLetterDown btnCommand ui-nodisc-icon" data-command="PreviousLetter">${LetterButtonAbbreviation}</button>-->\
                            <paper-fab icon="videocam" class="btnScreenshot btnCommand subdued" title="${ButtonTakeScreenshot}" data-command="TakeScreenshot"></paper-fab>\
                        </div>\
                    </div>\
                    <div class="readOnlyContent" style="margin: 2em auto 0; padding: 0 1em 100px;">\
                        <div class="sendMessageSection">\
                            <br /><h1>${HeaderSendMessage}</h1>\
                            <div style="text-align: left;">\
                                <form class="sendMessageForm">\
                                    <div>\
                                        <paper-input class="sendMessageElement" type="text" id="txtMessageTitle" label="${LabelMessageTitle}" required></paper-input>\
                                    </div>\
                                    <br />\
                                    <div>\
                                        <paper-input class="sendMessageElement" type="text" id="txtMessageText" label="${LabelMessageText}" required></paper-input>\
                                    </div>\
                                    <p>\
                                        <button class="sendMessageElement clearButton" type="submit" data-role="none" style="display:block;"><paper-button class="sendMessageElement accent" type="submit" raised style="display:block;">${ButtonSend}</paper-button></button>\
                                    </p>\
                                </form>\
                            </div>\
                        </div>\
                        <div class="sendTextSection">\
                            <br /><h1>${HeaderTypeText}</h1>\
                            <div style="text-align: left;">\
                                <form class="typeTextForm">\
                                    <div>\
                                        <paper-input class="typeTextElement" type="text" id="txtTypeText" label="${LabelTypeText}" required></paper-input>\
                                    </div>\
                                    <p>\
                                        <button class="typeTextElement clearButton" type="submit" data-role="none" style="display:block;"><paper-button class="typeTextElement accent" type="submit" raised style="display:block;">${ButtonSend}</paper-button></button>\
                                    </p>\
                                </form>\
                            </div>\
                        </div>\
                    </div>\
                </div>\
                <div class="nowPlayingPageTab hide" data-tab="2">\
                    <div class="playlist itemsContainer" style="max-width:800px;margin: 3em auto 0;padding-bottom:200px;">\
                    </div>\
                </div>\
';

        return html;
    }

    function getHeaderHtml() {

        var html = '';

        var position = AppInfo.enableNowPlayingPageBottomTabs ? 'absolute' : 'relative;';

        html += '<div style="background:#080808;">';
        html += '<paper-icon-button icon="arrow-back" class="btnExitRemoteControl" style="position:' + position + ';top:.5em;left:.5em;z-index:1;" tabindex="-1"></paper-icon-button>';

        html += '<div style="float:right;position:' + position + ';top:.5em;right:.5em;text-align:right;">';
        html += '<span class="nowPlayingSelectedPlayer"></span>';
        html += '<paper-icon-button icon="cast" class="nowPlayingCastIcon" style="vertical-align:middle;z-index:1;" tabindex="-1"></paper-icon-button>';
        //html += '<paper-icon-button icon="slideshow" class="btnSlideshow" style="vertical-align:middle;z-index:1;margin-left:.5em;" tabindex="-1"></paper-icon-button>';
        html += '</div>';
        html += '</div>';

        html += '<div class="libraryViewNav hide" style="position:static;">\
            <div>\
                <a href="#" data-index="0">${TabNowPlaying}</a>\
                <a href="#" data-index="1">${TabControls}</a>\
                <a href="#" data-index="2">${TabPlaylist}</a>\
            </div>\
        </div>\
        ';

        return html;
    }

    function getTabsHtml() {

        var html = '';

        html += '<paper-tabs class="nowPlayingPagePaperTabs" hidescrollbuttons noink>\
                    <paper-tab>${TabNowPlaying}</paper-tab>\
                    <paper-tab>${TabControls}</paper-tab>\
                    <paper-tab>${TabPlaylist}</paper-tab>\
                </paper-tabs>';

        return html;
    }

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

            var name = (s.Codec || '').toUpperCase();

            if (s.Profile) {
                name += ' ' + s.Profile;
            }

            if (s.Language) {
                name += ' · ' + s.Language;
            }
            if (s.Layout) {
                name += ' · ' + s.Layout;
            }
            else if (s.Channels) {
                name += ' · ' + s.Channels + ' ch';
            }

            var menuItem = {
                name: name,
                id: s.Index
            };

            if (s.Index == currentIndex) {
                menuItem.ironIcon = 'check';
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

            var name = (s.Language || Globalize.translate('LabelUnknownLanguage'));

            if (s.IsDefault && s.IsForced) {
                name += ' · ' + Globalize.translate('LabelDefaultForcedStream');
            }
            else if (s.IsDefault) {
                name += ' · ' + Globalize.translate('LabelDefaultStream');
            }
            else if (s.IsForced) {
                name += ' · ' + Globalize.translate('LabelForcedStream');
            }

            if (s.Codec) {
                name += ' · ' + s.Codec.toUpperCase();
            }

            var menuItem = {
                name: name,
                id: s.Index
            };

            if (s.Index == currentIndex) {
                menuItem.ironIcon = 'check';
            }

            return menuItem;
        });

        menuItems.unshift({
            id: -1,
            name: Globalize.translate('ButtonOff'),
            ironIcon: currentIndex == null ? 'check' : null
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

    var currentImgUrl;
    function updateNowPlayingInfo(context, state) {

        var item = state.NowPlayingItem;
        var displayName = item ? MediaController.getNowPlayingNameHtml(item).replace('<br/>', ' - ') : '';

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
            if (!browser.mobile) {
                // Exclude from mobile because it just doesn't perform well
                Backdrops.setBackdropUrl(context, backdropUrl);
            }

            ApiClient.getItem(Dashboard.getCurrentUserId(), item.Id).then(function (fullItem) {
                context.querySelector('.nowPlayingPageUserDataButtons').innerHTML = LibraryBrowser.getUserDataIconsHtml(fullItem, false);
            });
        } else {
            context.querySelector('.nowPlayingPageUserDataButtons').innerHTML = '';
        }
    }

    function setImageUrl(context, url) {
        currentImgUrl = url;

        if (url) {
            ImageLoader.lazyImage(context.querySelector('.nowPlayingPageImage'), url);
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

        //$('.chapterMenuOverlay', page).hide();
        //$('.chapterMenu', page).hide();
    }

    return function () {

        var dlg;
        var currentPlayer;
        var lastPlayerState;
        var lastUpdateTime = 0;

        var self = this;
        var playlistNeedsRefresh = true;

        function toggleRepeat(player) {

            if (player && lastPlayerState) {
                var state = lastPlayerState;
                switch ((state.PlayState || {}).RepeatMode) {
                    case 'RepeatNone':
                        player.setRepeatMode('RepeatAll');
                        break;
                    case 'RepeatAll':
                        player.setRepeatMode('RepeatOne');
                        break;
                    case 'RepeatOne':
                        player.setRepeatMode('RepeatNone');
                        break;
                }
            }
        }

        function updatePlayerState(context, state) {

            lastPlayerState = state;

            var item = state.NowPlayingItem;

            var playerInfo = MediaController.getPlayerInfo();

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

            var btnPause = context.querySelector('.btnPause');
            var btnPlay = context.querySelector('.btnPlay');

            buttonEnabled(btnPause, item != null);
            buttonEnabled(btnPlay, item != null);

            if (playState.IsPaused) {

                hideButton(btnPause);
                showButton(btnPlay);

            } else {

                showButton(btnPause);
                hideButton(btnPlay);
            }

            var positionSlider = context.querySelector('.nowPlayingPositionSlider');

            if (!positionSlider.dragging) {

                if (item && item.RunTimeTicks) {

                    var pct = playState.PositionTicks / item.RunTimeTicks;
                    pct *= 100;

                    positionSlider.value = pct;

                } else {

                    positionSlider.value = 0;
                }

                positionSlider.disabled = !playState.CanSeek;
            }

            if (playState.PositionTicks == null) {
                context.querySelector('.positionTime').innerHTML = '--:--';
            } else {
                context.querySelector('.positionTime').innerHTML = Dashboard.getDisplayTime(playState.PositionTicks);
            }

            if (item && item.RunTimeTicks != null) {
                context.querySelector('.runtime').innerHTML = Dashboard.getDisplayTime(item.RunTimeTicks);
            } else {
                context.querySelector('.runtime').innerHTML = '--:--';
            }

            if (item && item.MediaType == 'Video') {
                context.classList.remove('hideVideoButtons');
            } else {
                context.classList.add('hideVideoButtons');
            }

            if (playerInfo.isLocalPlayer && AppInfo.hasPhysicalVolumeButtons) {
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
                toggleRepeatButton.icon = "repeat";
                toggleRepeatButton.classList.add('nowPlayingPageRepeatActive');
            }
            else if (playState.RepeatMode == 'RepeatOne') {
                toggleRepeatButton.icon = "repeat-one";
                toggleRepeatButton.classList.add('nowPlayingPageRepeatActive');
            } else {
                toggleRepeatButton.icon = "repeat";
                toggleRepeatButton.classList.remove('nowPlayingPageRepeatActive');
            }

            updateNowPlayingInfo(context, state);
        }

        function loadPlaylist(context) {

            var html = '';

            //ApiClient.getItems(Dashboard.getCurrentUserId(), {

            //    SortBy: "SortName",
            //    SortOrder: "Ascending",
            //    IncludeItemTypes: "Audio",
            //    Recursive: true,
            //    Fields: "PrimaryImageAspectRatio,SortName,MediaSourceCount,SyncInfo",
            //    StartIndex: 0,
            //    ImageTypeLimit: 1,
            //    EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
            //    Limit: 100

            //}).then(function (result) {

            //    html += LibraryBrowser.getListViewHtml({
            //        items: result.Items,
            //        smallIcon: true
            //    });

            //    $(".playlist", page).html(html).lazyChildren();
            //});

            var playlistOpen = isPlaylistOpen(context);

            if (playlistOpen) {

                html += LibraryBrowser.getListViewHtml({
                    items: MediaController.playlist(),
                    smallIcon: true
                });

                playlistNeedsRefresh = false;
            }

            var deps = [];

            if (playlistOpen) {
                deps.push('paper-icon-item');
                deps.push('paper-item-body');
            }

            require(deps, function () {

                var itemsContainer = context.querySelector('.playlist');

                itemsContainer.innerHTML = html;

                if (playlistOpen) {

                    var index = MediaController.currentPlaylistIndex();

                    if (index != -1) {

                        var item = itemsContainer.querySelectorAll('.listItem')[index];
                        if (item) {
                            var img = item.querySelector('.listviewImage');

                            img.classList.remove('lazy');
                            img.classList.add('playlistIndexIndicatorImage');
                        }
                    }
                }

                ImageLoader.lazyChildren(itemsContainer);
            });
        }

        function isPlaylistOpen(context) {
            return context.querySelector('paper-tabs').selected == 2;
        }

        function onStateChanged(e, state) {

            if (e.type == 'positionchange') {
                // Try to avoid hammering the document with changes
                var now = new Date().getTime();
                if ((now - lastUpdateTime) < 700) {

                    return;
                }
                lastUpdateTime = now;
            }

            updatePlayerState(dlg, state);
        }

        function onPlaybackStart(e, state) {

            var player = this;

            player.beginPlayerUpdates();

            onStateChanged.call(player, e, state);

            if (isPlaylistOpen(dlg)) {
                loadPlaylist(dlg);
            } else {
                playlistNeedsRefresh = true;
            }
        }

        function onPlaybackStopped(e, state) {

            var player = this;

            player.endPlayerUpdates();

            onStateChanged.call(player, e, {});

            if (isPlaylistOpen(dlg)) {
                loadPlaylist(dlg);
            } else {
                playlistNeedsRefresh = true;
            }
        }

        function releaseCurrentPlayer() {

            if (currentPlayer) {

                Events.off(currentPlayer, 'playbackstart', onPlaybackStart);
                Events.off(currentPlayer, 'playbackstop', onPlaybackStopped);
                Events.off(currentPlayer, 'volumechange', onStateChanged);
                Events.off(currentPlayer, 'playstatechange', onStateChanged);
                Events.off(currentPlayer, 'positionchange', onStateChanged);

                currentPlayer.endPlayerUpdates();
                currentPlayer = null;
            }
        }

        function bindToPlayer(context, player) {

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
            Events.on(player, 'volumechange', onStateChanged);
            Events.on(player, 'playstatechange', onStateChanged);
            Events.on(player, 'positionchange', onStateChanged);

            var playerInfo = MediaController.getPlayerInfo();

            var supportedCommands = playerInfo.supportedCommands;

            updateSupportedCommands(context, supportedCommands);
        }

        function updateCastIcon(context) {

            var info = MediaController.getPlayerInfo();

            if (info.isLocalPlayer) {

                context.querySelector('.nowPlayingCastIcon').icon = 'cast';
                context.querySelector('.nowPlayingSelectedPlayer').innerHTML = '';

            } else {

                context.querySelector('.nowPlayingCastIcon').icon = 'cast-connected';
                context.querySelector('.nowPlayingSelectedPlayer').innerHTML = info.deviceName || info.name;
            }
        }

        function parentWithClass(elem, className) {

            while (!elem.classList || !elem.classList.contains(className)) {
                elem = elem.parentNode;

                if (!elem) {
                    return null;
                }
            }

            return elem;
        }

        function onContextClick(e) {

            var lnkPlayFromIndex = parentWithClass(e.target, 'lnkPlayFromIndex');
            if (lnkPlayFromIndex != null) {
                var index = parseInt(lnkPlayFromIndex.getAttribute('data-index'));

                MediaController.currentPlaylistIndex(index);
                loadPlaylist(context);

                return false;
            }
            var lnkRemoveFromPlaylist = parentWithClass(e.target, 'lnkRemoveFromPlaylist');
            if (lnkRemoveFromPlaylist != null) {
                var index = parseInt(lnkRemoveFromPlaylist.getAttribute('data-index'));

                MediaController.removeFromPlaylist(index);
                loadPlaylist(context);

                return false;
            }

            var mediaItem = parentWithClass(e.target, 'mediaItem');
            if (mediaItem != null) {
                var info = LibraryBrowser.getListItemInfo(mediaItem);

                MediaController.currentPlaylistIndex(info.index);

                return false;
            }
        }

        function bindEvents(context) {

            $('.btnCommand', context).on('click', function () {

                if (currentPlayer) {

                    if (this.classList.contains('repeatToggleButton')) {
                        toggleRepeat(currentPlayer);
                    } else {
                        MediaController.sendCommand({
                            Name: this.getAttribute('data-command')

                        }, currentPlayer);
                    }
                }
            });

            context.querySelector('.btnToggleFullscreen').addEventListener('click', function (e) {

                if (currentPlayer) {
                    MediaController.sendCommand({
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
                    currentPlayer.stop();
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
                    currentPlayer.nextTrack();
                }
            });

            context.querySelector('.btnPreviousTrack').addEventListener('click', function () {

                if (currentPlayer) {
                    currentPlayer.previousTrack();
                }
            });

            context.querySelector('.nowPlayingPositionSlider').addEventListener('change', function () {

                var value = this.value;

                if (currentPlayer && lastPlayerState) {

                    var newPercent = parseFloat(value);
                    var newPositionTicks = (newPercent / 100) * lastPlayerState.NowPlayingItem.RunTimeTicks;
                    currentPlayer.seek(Math.floor(newPositionTicks));
                }
            });

            context.querySelector('.nowPlayingPositionSlider', context)._setPinValue = function (value) {

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

            context.addEventListener('click', onContextClick);
        }

        function onPlayerChange() {

            var context = dlg;
            updateCastIcon(context);
            bindToPlayer(context, MediaController.getCurrentPlayer());
        }

        function onMessageSubmit(e) {

            var form = e.target;

            MediaController.sendCommand({
                Name: 'DisplayMessage',
                Arguments: {

                    Header: $('#txtMessageTitle', form).val(),
                    Text: $('#txtMessageText', form).val()
                }

            }, currentPlayer);

            $('input', form).val('');
            Dashboard.alert('Message sent.');

            e.preventDefault();
            e.stopPropagation();
            return false;
        }

        function onSendStringSubmit(e) {

            var form = e.target;

            MediaController.sendCommand({
                Name: 'SendString',
                Arguments: {

                    String: $('#txtTypeText', form).val()
                }

            }, currentPlayer);

            $('input', form).val('');
            Dashboard.alert('Text sent.');

            e.preventDefault();
            e.stopPropagation();
            return false;
        }

        function showTab(index) {

            var all = dlg.querySelectorAll('.nowPlayingPageTab');

            index = (index || 0).toString();

            for (var i = 0, length = all.length; i < length; i++) {

                var tab = all[i];

                if (tab.getAttribute('data-tab') == index) {
                    tab.classList.remove('hide');
                } else {
                    tab.classList.add('hide');
                }
            }
        }

        function init(context) {

            Dashboard.importCss('css/nowplaying.css');
            bindEvents(context);

            context.querySelector('.sendMessageForm').addEventListener('submit', onMessageSubmit);
            context.querySelector('.typeTextForm').addEventListener('submit', onSendStringSubmit);

            context.querySelector('.nowPlayingCastIcon').addEventListener('click', function () {
                MediaController.showPlayerSelection();
            });

            context.querySelector('.btnExitRemoteControl').addEventListener('click', function () {
                history.back();
            });

            //context.querySelector('.btnSlideshow').addEventListener('click', function () {
            //    showSlideshowMenu(context);
            //});

            var tabs = context.querySelector('paper-tabs');

            if (AppInfo.enableNowPlayingPageBottomTabs) {
                tabs.classList.remove('hide');
                context.querySelector('.libraryViewNav').classList.add('hide');
            } else {
                tabs.classList.add('hide');
                context.querySelector('.libraryViewNav').classList.remove('hide');
            }

            tabs.classList.add('bottom');
            tabs.alignBottom = true;

            tabs.addEventListener('iron-select', function (e) {

                var btn = context.querySelector('.libraryViewNav a.ui-btn-active');

                if (btn) {
                    btn.classList.remove('ui-btn-active');
                }

                context.querySelector('.libraryViewNav a[data-index=\'' + e.target.selected + '\']').classList.add('ui-btn-active');

                if (e.target.selected == 2 && playlistNeedsRefresh) {
                    loadPlaylist(context);
                }

                showTab(e.target.selected);
            });

            $(context.querySelectorAll('.libraryViewNav a')).on('click', function () {
                var newSelected = this.getAttribute('data-index');

                tabs.selected = newSelected;
            });

            Events.on(MediaController, 'playerchange', onPlayerChange);

            $(context.querySelector('.itemsContainer')).createCardMenus();

        }

        function onDialogClosed(e) {

            releaseCurrentPlayer();

            Events.off(MediaController, 'playerchange', onPlayerChange);

            lastPlayerState = null;
        }

        function onShow(context, tab) {

            currentImgUrl = null;

            bindToPlayer(context, MediaController.getCurrentPlayer());

            var selected = tab == '#playlist' ? 2 : 0;

            if (AppInfo.enableNowPlayingPageBottomTabs) {
                context.querySelector('paper-tabs').selected = selected;
            } else {

                showTab(selected);
            }

            updateCastIcon(context);
        }

        self.init = function (context) {

            var html = '';

            dlg = context;
            html += '<div style="margin:0;padding:0;">';
            html += Globalize.translateDocument(getHeaderHtml());
            html += Globalize.translateDocument(getAnimatedPagesHtml());
            html += Globalize.translateDocument(getTabsHtml());
            html += '</div>';

            dlg.innerHTML = html;

            init(dlg);
        };

        self.onShow = function () {
            onShow(dlg, window.location.hash);
        };

        self.destroy = function () {
            onDialogClosed();
        };

    };
});