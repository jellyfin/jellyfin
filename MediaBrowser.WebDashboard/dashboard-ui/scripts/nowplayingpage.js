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

        elem.html(html).trigger('create').lazyChildren();
    }

    function selectCurrentChapter(elem, positionTicks) {

        var elems = $('.chapterPosterItem', elem).removeClass('currentChapter');

        var matches = elems.get().filter(function (i) {

            var ticks = i.getAttribute('data-positionticks');

            return positionTicks >= ticks;

        });

        var chapterElem = matches[matches.length - 1];

        $(chapterElem).addClass('currentChapter');

        chapterElem.scrollIntoView();

        elem[0].scrollLeft += 50;
    }

    function showChapterMenu(page, item, currentPositionTicks) {

        $('.chapterMenuOverlay', page).show();

        var elem = $('.chapterMenu', page).show();

        if (item.Id == elem.attr('data-itemid')) {

            selectCurrentChapter(elem, currentPositionTicks);
            return;
        }

        var innerElem = $('.chapterMenuInner', elem);

        populateChapters(innerElem, item.Chapters, item.Id, item.RunTimeTicks);

        elem.attr('data-itemid', item.Id);

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
                MediaController.sendCommand({
                    Name: this.getAttribute('data-command')

                }, currentPlayer);
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

        $(page).on('click', '.mediaItem', onListItemClick);
    }

    function onPlaybackStart(e, state) {

        var player = this;

        player.beginPlayerUpdates();

        onStateChanged.call(player, e, state);
        loadPlaylist($.mobile.activePage);
    }

    function onPlaybackStopped(e, state) {

        var player = this;

        player.endPlayerUpdates();

        onStateChanged.call(player, e, {});
        loadPlaylist($.mobile.activePage);
    }

    function onStateChanged(e, state) {

        updatePlayerState($.mobile.activePage, state);
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

        updateNowPlayingInfo(page, state);
    }

    var currentImgUrl;
    function updateNowPlayingInfo(page, state) {

        var item = state.NowPlayingItem;
        var displayName = item ? MediaController.getNowPlayingNameHtml(item).replace('<br/>', ' - ') : '';

        $('.nowPlayingPageTitle', page).html(displayName).visible(displayName.length > 0);

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

        Backdrops.setBackdropUrl(page, backdropUrl);
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

            $(currentPlayer).off('.nowplayingpage');
            currentPlayer.endPlayerUpdates();
            currentPlayer = null;
        }
    }

    function bindToPlayer(page, player) {

        releaseCurrentPlayer();

        currentPlayer = player;

        player.getPlayerState().done(function (state) {

            if (state.NowPlayingItem) {
                player.beginPlayerUpdates();
            }

            onStateChanged.call(player, { type: 'init' }, state);
        });

        $(player).on('playbackstart.nowplayingpage', onPlaybackStart)
            .on('playbackstop.nowplayingpage', onPlaybackStopped)
            .on('volumechange.nowplayingpage', onStateChanged)
            .on('playstatechange.nowplayingpage', onStateChanged)
            .on('positionchange.nowplayingpage', onStateChanged);

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
        //    Fields: "PrimaryImageAspectRatio,SortName,MediaSourceCount,IsUnidentified,SyncInfo",
        //    StartIndex: 0,
        //    ImageTypeLimit: 1,
        //    EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
        //    Limit: 100

        //}).done(function (result) {

        //    html += LibraryBrowser.getListViewHtml({
        //        items: result.Items,
        //        smallIcon: true
        //    });

        //    $(".playlist", page).html(html).trigger('create').lazyChildren();
        //});

        html += LibraryBrowser.getListViewHtml({
            items: MediaController.playlist(),
            smallIcon: true
        });

        $(".playlist", page).html(html).trigger('create').lazyChildren();
    }

    function onListItemClick(e) {

        var info = LibraryBrowser.getListItemInfo(this);

        MediaController.currentPlaylistIndex(info.index);

        return false;
    }

    function getBackdropUrl(item) {

        var screenWidth = screen.availWidth;

        if (item.BackdropImageTags && item.BackdropImageTags.length) {

            return ApiClient.getScaledImageUrl(item.Id, {
                type: "Backdrop",
                index: 0,
                maxWidth: screenWidth,
                tag: item.BackdropImageTags[0]
            });

        }
        else if (item.ParentBackdropItemId && item.ParentBackdropImageTags && item.ParentBackdropImageTags.length) {

            return ApiClient.getScaledImageUrl(item.ParentBackdropItemId, {
                type: 'Backdrop',
                index: 0,
                maxWidth: screenWidth,
                tag: item.ParentBackdropImageTags[0]
            });

        }

        return null;
    };

    function updateCastIcon() {

        var info = MediaController.getPlayerInfo();

        if (info.isLocalPlayer) {

            $('.nowPlayingCastIcon').each(function () {
                this.icon = 'cast';
            });
            $('.headerSelectedPlayer').html('');

        } else {

            $('.nowPlayingCastIcon').each(function () {
                this.icon = 'cast-connected';
            });

            $('.headerSelectedPlayer').html((info.deviceName || info.name));
        }
    }

    function allowSwipe(e) {

        var target = $(e.target);

        if (target.is('.noSwipe')) {
            return false;
        }
        if (target.parents('.noSwipe').length) {
            return false;
        }

        return true;
    }

    $(document).on('pageinitdepends', "#nowPlayingPage", function () {

        var page = this;

        bindEvents(page);

        $('.sendMessageForm').off('submit', NowPlayingPage.onMessageSubmit).on('submit', NowPlayingPage.onMessageSubmit);
        $('.typeTextForm').off('submit', NowPlayingPage.onSendStringSubmit).on('submit', NowPlayingPage.onSendStringSubmit);

        $('.requiresJqmCreate', this).trigger('create');

        $(page).on('swipeleft', function (e) {

            if (allowSwipe(e)) {
                var pages = this.querySelectorAll('neon-animated-pages')[0];
                var tabs = this.querySelectorAll('paper-tabs')[0];

                var selected = parseInt(pages.selected || '0');
                if (selected < 2) {
                    pages.entryAnimation = 'slide-from-right-animation';
                    pages.exitAnimation = 'slide-left-animation';
                    tabs.selectNext();
                }
            }
        });

        $(page).on('swiperight', function (e) {

            if (allowSwipe(e)) {
                var pages = this.querySelectorAll('neon-animated-pages')[0];
                var tabs = this.querySelectorAll('paper-tabs')[0];

                var selected = parseInt(pages.selected || '0');
                if (selected > 0) {
                    pages.entryAnimation = 'slide-from-left-animation';
                    pages.exitAnimation = 'slide-right-animation';
                    tabs.selectPrevious();
                }
            }
        });

        $(MediaController).on('playerchange', function () {
            updateCastIcon(page);
        });

        $('paper-tabs').on('iron-select', function () {
            page.querySelectorAll('neon-animated-pages')[0].selected = this.selected;
        });

    }).on('pagebeforeshowready', "#nowPlayingPage", function () {

        $(document.body).addClass('hiddenViewMenuBar');
        var page = this;

        currentImgUrl = null;

        Dashboard.ready(function () {

            $(MediaController).on('playerchange.nowplayingpage', function () {

                bindToPlayer(page, MediaController.getCurrentPlayer());
            });

            bindToPlayer(page, MediaController.getCurrentPlayer());

        });

        loadPlaylist(page);

        var tab = getParameterByName('tab');
        var selected = tab == 'Playlist' ? 2 : 0;;
        this.querySelectorAll('paper-tabs')[0].selected = selected;

        updateCastIcon(page);

    }).on('pagebeforehide', "#nowPlayingPage", function () {

        releaseCurrentPlayer();

        $(MediaController).off('playerchange.nowplayingpage');

        lastPlayerState = null;
        $(document.body).removeClass('hiddenViewMenuBar');
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