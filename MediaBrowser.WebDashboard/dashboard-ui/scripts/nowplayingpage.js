(function (window, document, $, setTimeout, clearTimeout) {

    var currentPlayer;
    var lastPlayerState;
    var isPositionSliderActive;

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

            html += '<div class="posterItemImage lazy"' + dataSrc + '>';

            html += '<div class="posterItemTextOverlay">';

            if (chapter.Name) {
                html += "<div class='posterItemText'>";
                html += chapter.Name;
                html += "</div>";
            }

            html += "<div class='posterItemProgress posterItemText'>";
            var pct = 100 * (chapter.StartPositionTicks / runtimeTicks);
            html += '<progress class="itemProgressBar" min="0" max="100" value="' + pct + '" style="opacity:.8;"></progress>';
            html += "</div>";

            html += "</div>";

            html += "</div>";

            html += "</div>";
        }

        elem.html(html).trigger('create');
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

    function showAudioMenu(page, item, currentIndex) {

        var streams = (item.MediaStreams || []).filter(function (i) {

            return i.Type == 'Audio';
        });

        var elem = $('#popupAudioTrackMenu', page);

        var html = '<ul data-role="listview" data-inset="true" style="min-width: 210px;"><li data-role="list-divider">' + Globalize.translate('HeaderSelectAudio') + '</li>';

        html += streams.map(function (s) {

            var streamHtml = '<li><a data-index="' + s.Index + '" href="#" class="lnkTrackOption">';

            streamHtml += '<h3>';

            if (s.Index == currentIndex) {
                streamHtml += '<img src="css/images/checkmarkgreen.png" style="width:18px;border-radius:3px;margin-right:.5em;vertical-align:top;" />';
            }

            streamHtml += (s.Codec || '').toUpperCase();

            if (s.Profile) {
                streamHtml += ' ' + s.Profile;
            }

            streamHtml += '</h3><p>';

            var extras = [];

            if (s.Language) {
                extras.push(s.Language);
            }
            if (s.Layout) {
                extras.push(s.Layout);
            }
            else if (s.Channels) {
                extras.push(s.Channels + ' ch');
            }

            if (s.BitRate) {
                extras.push((parseInt(s.BitRate / 1000)) + ' kbps');
            }

            streamHtml += extras.join(' - ');

            streamHtml += '</p></a></li>';

            return streamHtml;

        }).join('');

        html += '</ul>';

        $('.trackList', elem).html(html).trigger('create');

        elem.popup('open');
    }

    function showSubtitleMenu(page, item, currentIndex) {

        var currentStreamImage = '<img src="css/images/checkmarkgreen.png" style="width:18px;border-radius:3px;margin-right:.5em;vertical-align:top;" />';

        var streams = (item.MediaStreams || []).filter(function (i) {

            return i.Type == 'Subtitle';
        });

        var elem = $('#popupSubtitleTrackMenu', page);

        var html = '<ul data-role="listview" data-inset="true" style="min-width: 210px;"><li data-role="list-divider">' + Globalize.translate('HeaderSelectSubtitles') + '</li>';

        html += '<li><a href="#" data-index="-1" class="lnkTrackOption"><h3>';

        if (currentIndex == null) {
            html += currentStreamImage;
        }

        html += 'Off';
        html += '</h3></a></li>';

        html += streams.map(function (s) {

            var streamHtml = '<li><a data-index="' + s.Index + '" href="#" class="lnkTrackOption">';

            streamHtml += '<h3>';

            if (s.Index == currentIndex) {
                streamHtml += currentStreamImage;
            }

            streamHtml += (s.Language || Globalize.translate('LabelUnknownLanguage'));

            if (s.IsDefault && s.IsForced) {
                streamHtml += ' ' + Globalize.translate('LabelDefaultForcedStream');
            }
            else if (s.IsDefault) {
                streamHtml += ' ' + Globalize.translate('LabelDefaultStream');
            }
            else if (s.IsForced) {
                streamHtml += ' ' + Globalize.translate('LabelForcedStream');
            }

            streamHtml += '</h3><p>';

            streamHtml += (s.Codec || '').toUpperCase();

            streamHtml += '</p></a></li>';

            return streamHtml;

        }).join('');

        html += '</ul>';

        $('.trackList', elem).html(html).trigger('create');

        elem.popup('open');
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
                var ticks = this.getAttribute('data-positionticks');

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

        $('#popupAudioTrackMenu', page).on('click', '.lnkTrackOption', function () {

            if (currentPlayer && lastPlayerState) {

                var index = this.getAttribute('data-index');

                currentPlayer.setAudioStreamIndex(parseInt(index));

                $('#popupAudioTrackMenu', page).popup('close');
            }
        });

        $('#popupSubtitleTrackMenu', page).on('click', '.lnkTrackOption', function () {

            if (currentPlayer && lastPlayerState) {
                var index = this.getAttribute('data-index');

                currentPlayer.setSubtitleStreamIndex(parseInt(index));

                $('#popupSubtitleTrackMenu', page).popup('close');
            }
        });

        $('.btnAudioTracks', page).on('click', function () {

            if (currentPlayer && lastPlayerState && lastPlayerState.PlayState) {

                var currentIndex = lastPlayerState.PlayState.AudioStreamIndex;
                showAudioMenu(page, lastPlayerState.NowPlayingItem, currentIndex);
            }
        });

        $('.btnSubtitles', page).on('click', function () {

            if (currentPlayer && lastPlayerState && lastPlayerState.PlayState) {

                var currentIndex = lastPlayerState.PlayState.SubtitleStreamIndex;
                showSubtitleMenu(page, lastPlayerState.NowPlayingItem, currentIndex);
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

        $('.positionSlider', page).on('slidestart', function () {

            isPositionSliderActive = true;

        }).on('slidestop', function () {

            isPositionSliderActive = false;

            if (currentPlayer && lastPlayerState) {

                var newPercent = parseFloat(this.value);
                var newPositionTicks = (newPercent / 100) * lastPlayerState.NowPlayingItem.RunTimeTicks;

                currentPlayer.seek(Math.floor(newPositionTicks));
            }
        });
    }

    function onPlaybackStart(e, state) {

        var player = this;

        player.beginPlayerUpdates();

        onStateChanged.call(player, e, state);
    }

    function onPlaybackStopped(e, state) {

        var player = this;

        player.endPlayerUpdates();

        onStateChanged.call(player, e, {});
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

        var playState = state.PlayState || {};

        if (playState.IsPaused) {

            hideButton(btnPause);
            showButton(btnPlay);

        } else {

            showButton(btnPause);
            hideButton(btnPlay);
        }

        if (!isPositionSliderActive) {

            var positionSlider = $('.positionSlider', page);

            if (item && item.RunTimeTicks) {

                var pct = playState.PositionTicks / item.RunTimeTicks;
                pct *= 100;

                positionSlider.val(pct);

            } else {

                positionSlider.val(0);
            }

            if (playState.CanSeek) {
                positionSlider.slider("enable");
            } else {
                positionSlider.slider("disable");
            }

            positionSlider.slider('refresh');
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

        $('.itemName', page).html(item ? MediaPlayer.getNowPlayingNameHtml(state) : '');

        var url;

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

        setImageUrl(page, url);
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

    $(document).on('pageinit', "#nowPlayingPage", function () {

        var page = this;

        bindEvents(page);

    }).on('pageshow', "#nowPlayingPage", function () {

        var page = this;

        $('.tabButton:first', page).trigger('click');

        $(function () {

            $(MediaController).on('playerchange.nowplayingpage', function () {

                bindToPlayer(page, MediaController.getCurrentPlayer());
            });

            bindToPlayer(page, MediaController.getCurrentPlayer());

        });

    }).on('pagehide', "#nowPlayingPage", function () {

        releaseCurrentPlayer();

        $(MediaController).off('playerchange.nowplayingpage');

        lastPlayerState = null;
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