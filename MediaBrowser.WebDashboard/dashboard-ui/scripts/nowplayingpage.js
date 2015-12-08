(function (window, document, $, setTimeout, clearTimeout) {

    var currentPlayer;
    var lastPlayerState;

    function populateChapters(elem, chapters, itemId, runtimeTicks) {

        var html = '';

        for (var i = 0, length = chapters.length; i < length; i++) {

            var chapter = chapters[i];

            html += '<div data-positionticks="' + chapter.StartPositionTicks + '" class="posterItem backdropPosterItem chapterPosterItem">';

            var imgUrl;

            if (chapter.ImageTag) {

                imgUrl = ApiClient.getScaledImageUrl(itemId, {
                    width: 240,
                    tag: chapter.ImageTag,
                    type: "Chapter",
                    index: i
                });

            } else {
                imgUrl = "css/images/items/list/chapter.png";
            }

            var dataSrc = ' data-src="' + imgUrl + '"';
            // TODO: This markup needs to be converted to the newer card layout pattern
            html += '<div class="posterItemImage lazy"' + dataSrc + '>';

            html += '<div class="posterItemTextOverlay" style="position:absolute;bottom:0;left:0;right:0;">';

            if (chapter.Name) {
                html += "<div class='posterItemText'>";
                html += chapter.Name;
                html += "</div>";
            }

            html += "<div class='posterItemProgress posterItemText'>";
            var pct = 100 * (chapter.StartPositionTicks / runtimeTicks);
            html += '<progress class="itemProgressBar" min="0" max="100" value="' + pct + '" style="opacity:.8;width:100%;"></progress>';
            html += "</div>";

            html += "</div>";

            html += "</div>";

            html += "</div>";
        }

        elem.innerHTML = html;
        ImageLoader.lazyChildren(elem);
    }

    function selectCurrentChapter(elem, positionTicks) {

        var elems = $('.chapterPosterItem', elem).removeClass('currentChapter');

        var matches = elems.get().filter(function (i) {

            var ticks = i.getAttribute('data-positionticks');

            return positionTicks >= ticks;

        });

        var chapterElem = matches[matches.length - 1];

        chapterElem.classList.add('currentChapter');

        chapterElem.scrollIntoView();

        elem.scrollLeft += 50;
    }

    function showChapterMenu(page, item, currentPositionTicks) {

        $('.chapterMenuOverlay', page).show();

        var elem = page.querySelector('.chapterMenu');
        $(elem).show();

        if (item.Id == elem.getAttribute('data-itemid')) {

            selectCurrentChapter(elem, currentPositionTicks);
            return;
        }

        var innerElem = elem.querySelector('.chapterMenuInner');

        populateChapters(innerElem, item.Chapters, item.Id, item.RunTimeTicks);

        elem.setAttribute('data-itemid', item.Id);

        selectCurrentChapter(elem, currentPositionTicks);
    }

    function hideChapterMenu(page) {

        $('.chapterMenuOverlay', page).hide();
        $('.chapterMenu', page).hide();
    }

    function showAudioMenu(page, button, item, currentIndex) {

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

        require(['actionsheet'], function () {

            ActionSheetElement.show({
                items: menuItems,
                positionTo: button,
                callback: function (id) {

                    currentPlayer.setAudioStreamIndex(parseInt(id));
                }
            });

        });
    }

    function showSubtitleMenu(page, button, item, currentIndex) {

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

        require(['actionsheet'], function () {

            ActionSheetElement.show({
                items: menuItems,
                positionTo: button,
                callback: function (id) {

                    currentPlayer.setSubtitleStreamIndex(parseInt(id));
                }
            });

        });
    }

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

    function bindEvents(page) {

        $('.tabButton', page).on('click', function () {

            var elem = $('.' + this.getAttribute('data-tab'), page);
            elem.siblings('.tabContent').hide();

            elem.show();

            $('.tabButton', page).removeClass('ui-btn-active');
            $(this).addClass('ui-btn-active');
        });

        $('.chapterMenuOverlay', page).on('click', function () {

            hideChapterMenu(page);
        });

        $('.chapterMenu', page).on('click', '.chapterPosterItem', function () {

            if (currentPlayer) {
                var ticks = this.getAttribute('data-positionticks') || '0';

                currentPlayer.seek(parseInt(ticks));
            }

            hideChapterMenu(page);
        });

        $('.btnCommand,.btnToggleFullscreen', page).on('click', function () {

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

        $('.btnAudioTracks', page).on('click', function () {

            if (currentPlayer && lastPlayerState && lastPlayerState.PlayState) {

                var currentIndex = lastPlayerState.PlayState.AudioStreamIndex;
                showAudioMenu(page, this, lastPlayerState.NowPlayingItem, currentIndex);
            }
        });

        $('.btnSubtitles', page).on('click', function () {

            if (currentPlayer && lastPlayerState && lastPlayerState.PlayState) {

                var currentIndex = lastPlayerState.PlayState.SubtitleStreamIndex;
                showSubtitleMenu(page, this, lastPlayerState.NowPlayingItem, currentIndex);
            }
        });

        $('.btnChapters', page).on('click', function () {

            if (currentPlayer && lastPlayerState) {

                var currentPositionTicks = lastPlayerState.PlayState.PositionTicks;
                showChapterMenu(page, lastPlayerState.NowPlayingItem, currentPositionTicks);
            }
        });

        $('.btnStop', page).on('click', function () {

            if (currentPlayer) {
                currentPlayer.stop();
            }
        });

        $('.btnPlay', page).on('click', function () {

            if (currentPlayer) {
                currentPlayer.unpause();
            }
        });

        $('.btnPause', page).on('click', function () {

            if (currentPlayer) {
                currentPlayer.pause();
            }
        });

        $('.btnNextTrack', page).on('click', function () {

            if (currentPlayer) {
                currentPlayer.nextTrack();
            }
        });

        $('.btnPreviousTrack', page).on('click', function () {

            if (currentPlayer) {
                currentPlayer.previousTrack();
            }
        });

        $('.nowPlayingPositionSlider', page).on('change', function () {

            var value = this.value;

            if (currentPlayer && lastPlayerState) {

                var newPercent = parseFloat(value);
                var newPositionTicks = (newPercent / 100) * lastPlayerState.NowPlayingItem.RunTimeTicks;
                currentPlayer.seek(Math.floor(newPositionTicks));
            }
        });

        $('.nowPlayingPositionSlider', page)[0]._setPinValue = function (value) {

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

        $(page).on('click', '.lnkPlayFromIndex', function () {

            var index = parseInt(this.getAttribute('data-index'));

            MediaController.currentPlaylistIndex(index);
            loadPlaylist(page);

        }).on('click', '.lnkRemoveFromPlaylist', function () {

            var index = parseInt(this.getAttribute('data-index'));

            MediaController.removeFromPlaylist(index);
            loadPlaylist(page);
        });

        Events.on(page, 'click', '.mediaItem', onListItemClick);
    }

    function onPlaybackStart(e, state) {

        var player = this;

        player.beginPlayerUpdates();

        onStateChanged.call(player, e, state);
        loadPlaylist($($.mobile.activePage)[0]);
    }

    function onPlaybackStopped(e, state) {

        var player = this;

        player.endPlayerUpdates();

        onStateChanged.call(player, e, {});
        loadPlaylist($($.mobile.activePage)[0]);
    }

    var lastUpdateTime = 0;

    function onStateChanged(e, state) {

        if (e.type == 'positionchange') {
            // Try to avoid hammering the document with changes
            var now = new Date().getTime();
            if ((now - lastUpdateTime) < 700) {

                return;
            }
            lastUpdateTime = now;
        }

        updatePlayerState($($.mobile.activePage)[0], state);
    }

    function showButton(button) {
        button.removeClass('hide');
    }

    function hideButton(button) {
        button.addClass('hide');
    }

    function hasStreams(item, type) {
        return item && item.MediaStreams && item.MediaStreams.filter(function (i) {
            return i.Type == type;
        }).length > 0;
    }

    function updatePlayerState(page, state) {

        lastPlayerState = state;

        var item = state.NowPlayingItem;

        var playerInfo = MediaController.getPlayerInfo();

        var supportedCommands = playerInfo.supportedCommands;
        var playState = state.PlayState || {};

        $('.btnToggleFullscreen', page).buttonEnabled(item && item.MediaType == 'Video' && supportedCommands.indexOf('ToggleFullscreen') != -1);

        $('.btnAudioTracks', page).buttonEnabled(hasStreams(item, 'Audio') && supportedCommands.indexOf('SetAudioStreamIndex') != -1);
        $('.btnSubtitles', page).buttonEnabled(hasStreams(item, 'Subtitle') && supportedCommands.indexOf('SetSubtitleStreamIndex') != -1);

        if (item && item.Chapters && item.Chapters.length && playState.CanSeek) {
            $('.btnChapters', page).buttonEnabled(true);

        } else {
            $('.btnChapters', page).buttonEnabled(false);
            hideChapterMenu(page);
        }

        $('.sendMessageElement', page).buttonEnabled(supportedCommands.indexOf('DisplayMessage') != -1);
        $('.typeTextElement', page).buttonEnabled(supportedCommands.indexOf('SendString') != -1);

        $('.btnStop', page).buttonEnabled(item != null);
        $('.btnNextTrack', page).buttonEnabled(item != null);
        $('.btnPreviousTrack', page).buttonEnabled(item != null);

        var btnPause = $('.btnPause', page).buttonEnabled(item != null);
        var btnPlay = $('.btnPlay', page).buttonEnabled(item != null);

        if (playState.IsPaused) {

            hideButton(btnPause);
            showButton(btnPlay);

        } else {

            showButton(btnPause);
            hideButton(btnPlay);
        }

        var positionSlider = $('.nowPlayingPositionSlider', page)[0];

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
            $('.positionTime', page).html('--:--');
        } else {
            $('.positionTime', page).html(Dashboard.getDisplayTime(playState.PositionTicks));
        }

        if (item && item.RunTimeTicks != null) {
            $('.runtime', page).html(Dashboard.getDisplayTime(item.RunTimeTicks));
        } else {
            $('.runtime', page).html('--:--');
        }

        if (item && item.MediaType == 'Video') {
            $('.videoButton', page).css('visibility', 'visible');
        } else {
            $('.videoButton', page).css('visibility', 'hidden');
        }

        if (playerInfo.isLocalPlayer && AppInfo.hasPhysicalVolumeButtons) {
            $('.volumeButton', page).css('visibility', 'hidden');
        } else {
            $('.volumeButton', page).css('visibility', 'visible');
        }

        if (item && item.MediaType == 'Audio') {
            $('.buttonsRow2', page).hide();
        } else {
            $('.buttonsRow2', page).show();
        }

        var toggleRepeatButton = page.querySelector('.repeatToggleButton');

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

        updateNowPlayingInfo(page, state);
    }

    var currentImgUrl;
    function updateNowPlayingInfo(page, state) {

        var item = state.NowPlayingItem;
        var displayName = item ? MediaController.getNowPlayingNameHtml(item).replace('<br/>', ' - ') : '';

        $('.nowPlayingPageTitle', page).html(displayName);

        if (displayName.length > 0) {
            $('.nowPlayingPageTitle', page).removeClass('hide');
        } else {
            $('.nowPlayingPageTitle', page).addClass('hide');
        }

        var url;
        var backdropUrl = null;

        if (!item) {
        }
        else if (item.PrimaryImageTag) {

            url = ApiClient.getScaledImageUrl(item.PrimaryImageItemId, {
                type: "Primary",
                height: 300,
                tag: item.PrimaryImageTag
            });
        }
        else if (item.BackdropImageTag) {

            url = ApiClient.getScaledImageUrl(item.BackdropItemId, {
                type: "Backdrop",
                height: 300,
                tag: item.BackdropImageTag,
                index: 0
            });

        } else if (item.ThumbImageTag) {

            url = ApiClient.getScaledImageUrl(item.ThumbImageItemId, {
                type: "Thumb",
                height: 300,
                tag: item.ThumbImageTag
            });
        }

        if (url == currentImgUrl) {
            return;
        }

        if (item && item.BackdropImageTag) {

            backdropUrl = ApiClient.getScaledImageUrl(item.BackdropItemId, {
                type: "Backdrop",
                maxWidth: $(window).width(),
                tag: item.BackdropImageTag,
                index: 0
            });

        }

        setImageUrl(page, url);

        if (item) {

            // This should be outside of the IF
            // But for now, if you change songs but keep the same artist, the backdrop will flicker because in-between songs it clears out the image
            if (!browserInfo.safari) {
                // Exclude from safari because it just doesn't perform well
                Backdrops.setBackdropUrl(page, backdropUrl);
            }

            ApiClient.getItem(Dashboard.getCurrentUserId(), item.Id).then(function (fullItem) {
                page.querySelector('.nowPlayingPageUserDataButtons').innerHTML = LibraryBrowser.getUserDataIconsHtml(fullItem, false);
            });
        } else {
            page.querySelector('.nowPlayingPageUserDataButtons').innerHTML = '';
        }
    }

    function setImageUrl(page, url) {
        currentImgUrl = url;

        $('.nowPlayingPageImage', page).html(url ? '<img src="' + url + '" />' : '');
    }

    function updateSupportedCommands(page, commands) {

        $('.btnCommand', page).each(function () {

            $(this).buttonEnabled(commands.indexOf(this.getAttribute('data-command')) != -1);

        });
    }

    function releaseCurrentPlayer() {

        if (currentPlayer) {

            $(currentPlayer).off('playbackstart', onPlaybackStart)
                .off('playbackstop', onPlaybackStopped)
                .off('volumechange', onStateChanged)
                .off('playstatechange', onStateChanged)
                .off('positionchange', onStateChanged);

            currentPlayer.endPlayerUpdates();
            currentPlayer = null;
        }
    }

    function bindToPlayer(page, player) {

        releaseCurrentPlayer();

        currentPlayer = player;

        player.getPlayerState().then(function (state) {

            if (state.NowPlayingItem) {
                player.beginPlayerUpdates();
            }

            onStateChanged.call(player, { type: 'init' }, state);
        });

        $(player).on('playbackstart', onPlaybackStart)
            .on('playbackstop', onPlaybackStopped)
            .on('volumechange', onStateChanged)
            .on('playstatechange', onStateChanged)
            .on('positionchange', onStateChanged);

        var playerInfo = MediaController.getPlayerInfo();

        var supportedCommands = playerInfo.supportedCommands;

        updateSupportedCommands(page, supportedCommands);
    }

    function loadPlaylist(page) {

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

        html += LibraryBrowser.getListViewHtml({
            items: MediaController.playlist(),
            smallIcon: true
        });

        var itemsContainer = page.querySelector('.playlist');
        itemsContainer.innerHTML = html;

        var index = MediaController.currentPlaylistIndex();

        if (index != -1) {

            var item = itemsContainer.querySelectorAll('.listItem')[index];
            if (item) {
                var img = item.querySelector('.listviewImage');

                img.classList.remove('lazy');
                img.classList.add('playlistIndexIndicatorImage');
            }
        }

        ImageLoader.lazyChildren(itemsContainer);
    }

    function onListItemClick(e) {

        var info = LibraryBrowser.getListItemInfo(this);

        MediaController.currentPlaylistIndex(info.index);

        return false;
    }

    function updateCastIcon() {

        var info = MediaController.getPlayerInfo();

        if (info.isLocalPlayer) {

            $('.nowPlayingCastIcon').each(function () {
                this.icon = 'cast';
            });
            $('.nowPlayingSelectedPlayer').html('');

        } else {

            $('.nowPlayingCastIcon').each(function () {
                this.icon = 'cast-connected';
            });

            $('.nowPlayingSelectedPlayer').html((info.deviceName || info.name));
        }
    }

    function onPlayerChange() {
        bindToPlayer($($.mobile.activePage)[0], MediaController.getCurrentPlayer());
    }

    function showSlideshowMenu(page) {
        require(['scripts/slideshow'], function () {
            SlideShow.showMenu();
        });
    }

    pageIdOn('pageinit', "nowPlayingPage", function () {

        var page = this;

        Dashboard.importCss('css/nowplaying.css');
        bindEvents(page);

        $('.sendMessageForm').off('submit', NowPlayingPage.onMessageSubmit).on('submit', NowPlayingPage.onMessageSubmit);
        $('.typeTextForm').off('submit', NowPlayingPage.onSendStringSubmit).on('submit', NowPlayingPage.onSendStringSubmit);

        $('.requiresJqmCreate', this).trigger('create');

        $('.btnSlideshow').on('click', function () {
            showSlideshowMenu(page);
        });

        var tabs = page.querySelector('paper-tabs');

        if (AppInfo.enableNowPlayingPageBottomTabs) {
            tabs.classList.remove('hide');
            page.querySelector('.libraryViewNav').classList.add('hide');
        } else {
            tabs.classList.add('hide');
            page.querySelector('.libraryViewNav').classList.remove('hide');
        }

        tabs.classList.add('bottom');
        tabs.alignBottom = true;
        LibraryBrowser.configureSwipeTabs(page, tabs, page.querySelector('neon-animated-pages'));

        $(tabs).on('iron-select', function () {
            page.querySelector('neon-animated-pages').selected = this.selected;
        });

        $(page.querySelector('neon-animated-pages')).on('iron-select', function () {
            var btn = page.querySelector('.libraryViewNav a.ui-btn-active');

            if (btn) {
                btn.classList.remove('ui-btn-active');
            }

            page.querySelector('.libraryViewNav a[data-index=\'' + this.selected + '\']').classList.add('ui-btn-active');
        });

        $(page.querySelectorAll('.libraryViewNav a')).on('click', function () {
            var newSelected = this.getAttribute('data-index');

            if (AppInfo.enableNowPlayingPageBottomTabs) {
                tabs.selected = newSelected;
            } else {
                page.querySelector('neon-animated-pages').selected = newSelected;
            }
        });

        $(MediaController).on('playerchange', function () {
            updateCastIcon(page);
        });

    });
    pageIdOn('pagebeforeshow', "nowPlayingPage", function () {

        $(document.body).addClass('hiddenViewMenuBar').addClass('hiddenNowPlayingBar');
        var page = this;

        currentImgUrl = null;

        $(MediaController).on('playerchange', onPlayerChange);

        bindToPlayer(page, MediaController.getCurrentPlayer());

        loadPlaylist(page);

        var tab = window.location.hash;
        var selected = tab == '#playlist' ? 2 : 0;;

        this.querySelector('paper-tabs').selected = selected;

        if (AppInfo.enableNowPlayingPageBottomTabs) {
            this.querySelector('paper-tabs').selected = selected;
        } else {

            // hack alert. doing this because the neon elements don't seem to be initialized yet
            setTimeout(function() {
                
                page.querySelector('neon-animated-pages').selected = selected;
            }, 1000);
        }

        updateCastIcon(page);

    });
    pageIdOn('pagebeforehide', "nowPlayingPage", function () {

        releaseCurrentPlayer();

        $(MediaController).off('playerchange', onPlayerChange);

        lastPlayerState = null;
        $(document.body).removeClass('hiddenViewMenuBar').removeClass('hiddenNowPlayingBar');
    });

    window.NowPlayingPage = {

        onMessageSubmit: function () {

            var form = this;

            MediaController.sendCommand({
                Name: 'DisplayMessage',
                Arguments: {

                    Header: $('#txtMessageTitle', form).val(),
                    Text: $('#txtMessageText', form).val()
                }

            }, currentPlayer);

            $('input', form).val('');
            Dashboard.alert('Message sent.');

            return false;
        },

        onSendStringSubmit: function () {

            var form = this;

            MediaController.sendCommand({
                Name: 'SendString',
                Arguments: {

                    String: $('#txtTypeText', form).val()
                }

            }, currentPlayer);

            $('input', form).val('');
            Dashboard.alert('Text sent.');

            return false;
        }

    };

})(window, document, jQuery, setTimeout, clearTimeout);