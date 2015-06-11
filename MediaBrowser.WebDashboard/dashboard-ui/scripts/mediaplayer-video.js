(function () {

    function createVideoPlayer(self) {

        var initialVolume;
        var idleState = true;

        var muteButton = null;
        var unmuteButton = null;
        var volumeSlider = null;
        var positionSlider;
        var isPositionSliderActive;
        var currentTimeElement;

        self.currentSubtitleStreamIndex = null;

        self.getCurrentSubtitleStream = function () {
            return self.getSubtitleStream(self.currentSubtitleStreamIndex);
        };

        self.getSubtitleStream = function (index) {
            return self.currentMediaSource.MediaStreams.filter(function (s) {
                return s.Type == 'Subtitle' && s.Index == index;
            })[0];
        };

        self.toggleFullscreen = function () {
            if (self.isFullScreen()) {

                self.exitFullScreen();
            } else {
                requestFullScreen(document.body);
            }
        };

        self.resetEnhancements = function () {
            $("#mediaPlayer").hide();
            $('#videoPlayer').removeClass('fullscreenVideo').removeClass('idlePlayer');
            $('.hiddenOnIdle').removeClass("inactive");
            $("video").remove();
        };

        self.exitFullScreen = function () {

            if (document.exitFullscreen) {
                document.exitFullscreen();
            } else if (document.mozCancelFullScreen) {
                document.mozCancelFullScreen();
            } else if (document.webkitExitFullscreen) {
                document.webkitExitFullscreen();
            } else if (document.msExitFullscreen) {
                document.msExitFullscreen();
            }

            $('#videoPlayer').removeClass('fullscreenVideo');
        };

        self.isFullScreen = function () {
            return document.fullscreen || document.mozFullScreen || document.webkitIsFullScreen || document.msFullscreenElement ? true : false;
        };

        function onFlyoutClose() {
            $('.itemVideo').css('visibility', 'visible');
        }

        function onPopupOpen(elem) {
            elem.popup("open").parents(".ui-popup-container").css("margin-top", 30);

            if ($.browser.safari) {
                //$('.itemVideo').css('visibility', 'hidden');
            }
        }

        self.showChaptersFlyout = function () {

            var html = getChaptersFlyoutHtml();

            var elem = $('.videoChaptersPopup').html(html)
                .trigger('create')
                .popup("option", "positionTo", $('.videoChaptersButton'))
                .off('popupafterclose', onFlyoutClose)
                .on('popupafterclose', onFlyoutClose);

            onPopupOpen(elem);
        };

        self.showSubtitleMenu = function () {

            var html = getSubtitleTracksHtml();

            var elem = $('.videoSubtitlePopup').html(html)
                .trigger('create')
                .popup("option", "positionTo", $('.videoSubtitleButton'))
                .off('popupafterclose', onFlyoutClose)
                .on('popupafterclose', onFlyoutClose);

            onPopupOpen(elem);
        };

        self.showQualityFlyout = function () {

            var html = getQualityFlyoutHtml();

            var elem = $('.videoQualityPopup').html(html)
                .trigger('create')
                .popup("option", "positionTo", $('.videoQualityButton'))
                .off('popupafterclose', onFlyoutClose)
                .on('popupafterclose', onFlyoutClose);

            onPopupOpen(elem);
        };

        self.showAudioTracksFlyout = function () {

            var html = getAudioTracksHtml();

            var elem = $('.videoAudioPopup').html(html)
                .trigger('create')
                .popup("option", "positionTo", $('.videoAudioButton'))
                .off('popupafterclose', onFlyoutClose)
                .on('popupafterclose', onFlyoutClose);

            onPopupOpen(elem);
        };

        self.setAudioStreamIndex = function (index) {
            self.changeStream(self.getCurrentTicks(), { AudioStreamIndex: index });
        };

        self.setSubtitleStreamIndex = function (index) {

            if (!self.supportsTextTracks()) {

                self.changeStream(self.getCurrentTicks(), { SubtitleStreamIndex: index });
                self.currentSubtitleStreamIndex = index;
                return;
            }

            var currentStream = self.getCurrentSubtitleStream();

            var newStream = self.getSubtitleStream(index);

            if (!currentStream && !newStream) return;

            var selectedTrackElementIndex = -1;

            if (currentStream && !newStream) {

                if (currentStream.DeliveryMethod != 'External') {

                    // Need to change the transcoded stream to remove subs
                    self.changeStream(self.getCurrentTicks(), { SubtitleStreamIndex: -1 });
                }
            }
            else if (!currentStream && newStream) {

                if (newStream.DeliveryMethod == 'External') {
                    selectedTrackElementIndex = index;
                } else {

                    // Need to change the transcoded stream to add subs
                    self.changeStream(self.getCurrentTicks(), { SubtitleStreamIndex: index });
                }
            }
            else if (currentStream && newStream) {

                if (newStream.DeliveryMethod == 'External') {
                    selectedTrackElementIndex = index;

                    if (currentStream.DeliveryMethod != 'External') {
                        self.changeStream(self.getCurrentTicks(), { SubtitleStreamIndex: -1 });
                    }
                } else {

                    // Need to change the transcoded stream to add subs
                    self.changeStream(self.getCurrentTicks(), { SubtitleStreamIndex: index });
                }
            }

            self.setCurrentTrackElement(selectedTrackElementIndex);

            self.currentSubtitleStreamIndex = index;
        };

        self.setCurrentTrackElement = function (index) {

            var modes = ['disabled', 'showing', 'hidden'];

            var textStreams = self.currentMediaSource.MediaStreams.filter(function (s) {
                return s.DeliveryMethod == 'External';
            });

            var newStream = textStreams.filter(function (s) {
                return s.Index == index;
            })[0];

            var trackIndex = newStream ? textStreams.indexOf(newStream) : -1;

            console.log('Setting new text track index to: ' + trackIndex);

            var allTracks = self.currentMediaElement.textTracks; // get list of tracks

            for (var i = 0; i < allTracks.length; i++) {

                var mode;

                if (trackIndex == i) {
                    mode = 1; // show this track
                } else {
                    mode = 0; // hide all other tracks
                }

                console.log('Setting track ' + i + ' mode to: ' + mode);

                // Safari uses integers for the mode property
                // http://www.jwplayer.com/html5/scripting/
                var useNumericMode = false;

                if (!isNaN(allTracks[i].mode)) {
                    useNumericMode = true;
                }

                if (useNumericMode) {
                    allTracks[i].mode = mode;
                } else {
                    allTracks[i].mode = modes[mode];
                }
            }
        };

        self.updateTextStreamUrls = function (startPositionTicks) {

            if (!self.supportsTextTracks()) {
                return;
            }

            var allTracks = self.currentMediaElement.textTracks; // get list of tracks

            for (var i = 0; i < allTracks.length; i++) {

                var track = allTracks[i];

                // This throws an error in IE, but is fine in chrome
                // In IE it's not necessary anyway because changing the src seems to be enough
                try {
                    while (track.cues.length) {
                        track.removeCue(track.cues[0]);
                    }
                } catch (e) {
                    console.log('Error removing cue from textTrack');
                }
            }

            $('track', self.currentMediaElement).each(function () {

                var currentSrc = this.src;

                currentSrc = replaceQueryString(currentSrc, 'startPositionTicks', startPositionTicks);

                this.src = currentSrc;

            });
        };

        self.updateNowPlayingInfo = function (item) {

            if (!item) {
                throw new Error('item cannot be null');
            }

            var mediaControls = $("#videoPlayer");

            var state = self.getPlayerStateInternal(self.currentMediaRenderer, item, self.currentMediaSource);

            var url = "";
            var imageWidth = 400;
            var imageHeight = 300;

            if (state.NowPlayingItem.PrimaryImageTag) {

                url = ApiClient.getScaledImageUrl(state.NowPlayingItem.PrimaryImageItemId, {
                    type: "Primary",
                    width: imageWidth,
                    tag: state.NowPlayingItem.PrimaryImageTag
                });
            }
            else if (state.NowPlayingItem.PrimaryImageTag) {

                url = ApiClient.getScaledImageUrl(state.NowPlayingItem.PrimaryImageItemId, {
                    type: "Primary",
                    width: imageWidth,
                    tag: state.NowPlayingItem.PrimaryImageTag
                });
            }
            else if (state.NowPlayingItem.BackdropImageTag) {

                url = ApiClient.getScaledImageUrl(state.NowPlayingItem.BackdropItemId, {
                    type: "Backdrop",
                    height: imageHeight,
                    tag: state.NowPlayingItem.BackdropImageTag,
                    index: 0
                });

            }
            else if (state.NowPlayingItem.ThumbImageTag) {

                url = ApiClient.getScaledImageUrl(state.NowPlayingItem.ThumbImageItemId, {
                    type: "Thumb",
                    height: imageHeight,
                    tag: state.NowPlayingItem.ThumbImageTag
                });
            }

            if (url) {
                $('.nowPlayingImage', mediaControls).html('<img src="' + url + '" />');
            } else {
                $('.nowPlayingImage', mediaControls).html('');
            }

            if (state.NowPlayingItem.LogoItemId) {

                url = ApiClient.getScaledImageUrl(state.NowPlayingItem.LogoItemId, {
                    type: "Logo",
                    height: 42,
                    tag: state.NowPlayingItem.LogoImageTag
                });

                $('.videoTopControlsLogo', mediaControls).html('<img src="' + url + '" />');
            } else {
                $('.videoTopControlsLogo', mediaControls).html('');
            }

            var elem = $('.nowPlayingTabs', mediaControls).html(getNowPlayingTabsHtml(item)).lazyChildren();

            $('.nowPlayingTabButton', elem).on('click', function () {

                if (!$(this).hasClass('selectedNowPlayingTabButton')) {
                    $('.selectedNowPlayingTabButton').removeClass('selectedNowPlayingTabButton');
                    $(this).addClass('selectedNowPlayingTabButton');
                    $('.nowPlayingTab').hide();
                    $('.' + this.getAttribute('data-tab')).show().trigger('scroll');
                }
            });

            $('.chapterCard', elem).on('click', function () {

                self.seek(parseInt(this.getAttribute('data-position')));
            });
        };

        function getNowPlayingTabsHtml(item) {

            var html = '';

            html += '<div class="nowPlayingTabButtons">';

            html += '<a href="#" class="nowPlayingTabButton selectedNowPlayingTabButton" data-tab="tabInfo">' + Globalize.translate('TabInfo') + '</a>';

            if (item.Chapters && item.Chapters.length) {
                html += '<a href="#" class="nowPlayingTabButton" data-tab="tabScenes">' + Globalize.translate('TabScenes') + '</a>';
            }

            if (item.People && item.People.length) {
                html += '<a href="#" class="nowPlayingTabButton" data-tab="tabCast">' + Globalize.translate('TabCast') + '</a>';
            }

            html += '</div>';

            html += '<div class="tabInfo nowPlayingTab">';
            var nameHtml = MediaController.getNowPlayingNameHtml(item, false);
            nameHtml = '<div class="videoNowPlayingName">' + nameHtml + '</div>';

            var miscInfo = LibraryBrowser.getMiscInfoHtml(item);
            if (miscInfo) {

                nameHtml += '<div class="videoNowPlayingRating">' + miscInfo + '</div>';
            }

            var ratingHtml = LibraryBrowser.getRatingHtml(item);
            if (ratingHtml) {

                nameHtml += '<div class="videoNowPlayingRating">' + ratingHtml + '</div>';
            }

            if (item.Overview) {

                nameHtml += '<div class="videoNowPlayingOverview">' + item.Overview + '</div>';
            }

            html += nameHtml;
            html += '</div>';

            if (item.Chapters && item.Chapters.length) {
                html += '<div class="tabScenes nowPlayingTab hiddenScrollX" style="display:none;white-space:nowrap;margin-bottom:2em;">';
                var chapterIndex = 0;
                html += item.Chapters.map(function (c) {

                    var width = 240;
                    var chapterHtml = '<a class="card backdropCard chapterCard" href="#" style="margin-right:1em;width:' + width + 'px;" data-position="' + c.StartPositionTicks + '">';
                    chapterHtml += '<div class="cardBox">';
                    chapterHtml += '<div class="cardScalable">';
                    chapterHtml += '<div class="cardPadder"></div>';

                    chapterHtml += '<div class="cardContent">';

                    if (c.ImageTag) {

                        var chapterImageUrl = ApiClient.getScaledImageUrl(item.Id, {
                            width: width,
                            tag: c.ImageTag,
                            type: "Chapter",
                            index: chapterIndex
                        });
                        chapterHtml += '<div class="cardImage lazy" data-src="' + chapterImageUrl + '"></div>';

                    } else {
                        chapterHtml += '<div class="cardImage" style="background:#444;"></div>';
                    }

                    chapterHtml += '<div class="cardFooter">';
                    if (c.Name) {
                        chapterHtml += '<div class="cardText">' + c.Name + '</div>';
                    }
                    chapterHtml += '<div class="cardText">' + Dashboard.getDisplayTime(c.StartPositionTicks) + '</div>';
                    chapterHtml += '</div>';
                    chapterHtml += '</div>';

                    chapterHtml += '</div>';
                    chapterHtml += '</div>';
                    chapterHtml += '</a>';

                    chapterIndex++;

                    return chapterHtml;

                }).join('');
                html += '</div>';
            }

            if (item.People && item.People.length) {
                html += '<div class="tabCast nowPlayingTab hiddenScrollX" style="display:none;white-space:nowrap;">';
                html += item.People.map(function (cast) {

                    var personHtml = '<div class="tileItem smallPosterTileItem" style="width:300px;">';

                    var imgUrl;
                    var height = 160;

                    if (cast.PrimaryImageTag) {

                        imgUrl = ApiClient.getScaledImageUrl(cast.Id, {
                            height: height,
                            tag: cast.PrimaryImageTag,
                            type: "primary",
                            minScale: 2
                        });

                    } else {

                        imgUrl = "css/images/items/list/person.png";
                    }

                    personHtml += '<div class="tileImage lazy" data-src="' + imgUrl + '" style="height:' + height + 'px;"></div>';



                    personHtml += '<div class="tileContent">';

                    personHtml += '<p>' + cast.Name + '</p>';

                    var role = cast.Role ? Globalize.translate('ValueAsRole', cast.Role) : cast.Type;

                    if (role == "GuestStar") {
                        role = Globalize.translate('ValueGuestStar');
                    }

                    role = role || "";

                    var maxlength = 40;

                    if (role.length > maxlength) {
                        role = role.substring(0, maxlength - 3) + '...';
                    }

                    personHtml += '<p>' + role + '</p>';

                    personHtml += '</div>';

                    personHtml += '</div>';
                    return personHtml;

                }).join('');
                html += '</div>';
            }

            return html;
        }

        function onPositionSliderChange() {

            isPositionSliderActive = false;

            var newPercent = parseInt(this.value);

            var newPositionTicks = (newPercent / 100) * self.currentMediaSource.RunTimeTicks;

            self.changeStream(Math.floor(newPositionTicks));
        }

        self.onChapterOptionSelected = function (elem) {

            if (!$(elem).hasClass('selectedMediaPopupOption')) {
                var ticks = parseInt(elem.getAttribute('data-value') || '0');

                self.changeStream(ticks);
            }
            $('.videoChaptersPopup').popup('close');
        };

        self.onAudioOptionSelected = function (elem) {

            if (!$(elem).hasClass('selectedMediaPopupOption')) {
                var index = parseInt(elem.getAttribute('data-value'));

                self.setAudioStreamIndex(index);
            }
            $('.videoAudioPopup').popup('close');
        };

        self.onSubtitleOptionSelected = function (elem) {

            if (!$(elem).hasClass('selectedMediaPopupOption')) {
                var index = parseInt(elem.getAttribute('data-value'));

                self.setSubtitleStreamIndex(index);
            }
            $('.videoSubtitlePopup').popup('close');
        };

        self.onQualityOptionSelected = function (elem) {

            if (!$(elem).hasClass('selectedMediaPopupOption')) {
                var bitrate = parseInt(elem.getAttribute('data-value'));

                AppSettings.maxStreamingBitrate(bitrate);

                $('.videoQualityPopup').popup('close');

                self.changeStream(self.getCurrentTicks(), {
                    Bitrate: bitrate
                });
            }

            $('.videoSubtitlePopup').popup('close');
        };

        $(function () {

            var parent = $("#mediaPlayer");
            muteButton = $('.muteButton', parent);
            unmuteButton = $('.unmuteButton', parent);
            currentTimeElement = $('.currentTime', parent);

            positionSlider = $(".positionSlider", parent).on('slidestart', function (e) {

                isPositionSliderActive = true;

            }).on('slidestop', onPositionSliderChange);

            volumeSlider = $('.volumeSlider', parent).on('slidestop', function () {

                var vol = this.value;

                updateVolumeButtons(vol);
                self.setVolume(vol * 100);
            });

            var trackChange = false;

            var tooltip = $('<div id="slider-tooltip"></div>');

            $(".videoControls .positionSliderContainer .slider").on("change", function (e) {
                if (!trackChange) return;

                var pct = $(this).val();

                var time = self.currentDurationTicks * (Number(pct) * .01);

                var tooltext = Dashboard.getDisplayTime(time);

                tooltip.text(tooltext);

                console.log("slidin", pct, self.currentDurationTicks, time);

            }).on("slidestart", function (e) {
                trackChange = true;

                var handle = $(".videoControls .positionSliderContainer .ui-slider-handle");

                handle.after(tooltip);
            }).on("slidestop", function (e) {
                trackChange = false;

                tooltip.remove();
            });
        });

        var idleHandlerTimeout;
        function idleHandler() {

            if (idleHandlerTimeout) {
                window.clearTimeout(idleHandlerTimeout);
            }

            if (idleState == true) {
                $('.hiddenOnIdle').removeClass("inactive");
                $('#videoPlayer').removeClass('idlePlayer');
            }

            idleState = false;

            idleHandlerTimeout = window.setTimeout(function () {
                idleState = true;
                $('.hiddenOnIdle').addClass("inactive");
                $('#videoPlayer').addClass('idlePlayer');
            }, 4000);
        }

        function updateVolumeButtons(vol) {

            if (vol) {
                muteButton.show();
                unmuteButton.hide();
            } else {
                muteButton.hide();
                unmuteButton.show();
            }
        }

        function requestFullScreen(element) {

            // Supports most browsers and their versions.
            var requestMethod = element.requestFullscreen || element.webkitRequestFullscreen || element.mozRequestFullScreen || element.msRequestFullscreen;

            if (requestMethod) { // Native full screen.
                requestMethod.call(element);
            } else {
                enterFullScreen();
            }

        }

        function enterFullScreen() {

            var player = $("#videoPlayer");

            player.addClass("fullscreenVideo");
        }

        function exitFullScreenToWindow() {

            var player = $("#videoPlayer");

            player.removeClass("fullscreenVideo");
        }

        function getChaptersFlyoutHtml() {

            var item = self.currentItem;
            var currentTicks = self.getCurrentTicks();
            var chapters = item.Chapters || [];

            var html = '';
            html += '<div class="videoPlayerPopupContent">';
            html += '<ul data-role="listview" data-inset="true"><li data-role="list-divider">' + Globalize.translate('HeaderScenes') + '</li>';
            html += '</ul>';

            html += '<div class="videoPlayerPopupScroller">';
            html += '<ul data-role="listview" data-inset="true">';

            var index = 0;

            html += chapters.map(function (chapter) {

                var cssClass = "mediaPopupOption";

                var selected = false;
                // Need to embed onclick handler due to delegation not working in iOS cordova
                var onclick = '';

                if (currentTicks >= chapter.StartPositionTicks) {
                    var nextChapter = chapters[index + 1];
                    selected = !nextChapter || currentTicks < nextChapter.StartPositionTicks;
                }

                if (!selected) {
                    onclick = ' onclick="MediaPlayer.onChapterOptionSelected(this);"';
                }

                var optionHtml = '<li><a' + onclick + ' data-value="' + chapter.StartPositionTicks + '" class="' + cssClass + '" href="#" style="padding-top:0;padding-bottom:0;">';

                var imgUrl = "css/images/media/chapterflyout.png";

                if (chapter.ImageTag) {

                    optionHtml += '<img src="' + imgUrl + '" style="visibility:hidden;" />';
                    imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                        width: 160,
                        tag: chapter.ImageTag,
                        type: "Chapter",
                        index: index
                    });
                    optionHtml += '<div class="videoChapterPopupImage" style="background-image:url(\'' + imgUrl + '\');"></div>';

                } else {
                    optionHtml += '<img src="' + imgUrl + '" />';
                }

                // TODO: Add some indicator if selected = true

                optionHtml += '<p style="margin:12px 0 0;">';

                var textLines = [];
                textLines.push(chapter.Name);
                textLines.push(Dashboard.getDisplayTime(chapter.StartPositionTicks));

                optionHtml += textLines.join('<br/>');

                optionHtml += '</p>';

                optionHtml += '</a></li>';

                index++;

                return optionHtml;

            }).join('');

            html += '</ul>';
            html += '</div>';

            html += '</div>';

            return html;
        }

        function getAudioTracksHtml() {

            var streams = self.currentMediaSource.MediaStreams.filter(function (currentStream) {
                return currentStream.Type == "Audio";
            });

            var currentIndex = getParameterByName('AudioStreamIndex', self.getCurrentSrc(self.currentMediaRenderer));

            var html = '';
            html += '<div class="videoPlayerPopupContent">';
            html += '<ul data-role="listview" data-inset="true"><li data-role="list-divider">' + Globalize.translate('HeaderAudioTracks') + '</li>';
            html += '</ul>';

            html += '<div class="videoPlayerPopupScroller">';
            html += '<ul data-role="listview" data-inset="true">';

            html += streams.map(function (stream) {

                var cssClass = "mediaPopupOption";

                var selected = stream.Index == currentIndex;

                // Need to embed onclick handler due to delegation not working in iOS cordova
                var onclick = '';

                if (selected) {
                    cssClass += ' selectedMediaPopupOption';
                } else {
                    onclick = ' onclick="MediaPlayer.onAudioOptionSelected(this);"';
                }

                var optionHtml = '<li><a' + onclick + ' data-value="' + stream.Index + '" class="' + cssClass + '" href="#">';

                optionHtml += '<p style="margin:0;">';

                if (selected) {
                    optionHtml += '<img src="css/images/checkmarkgreen.png" style="width:18px;border-radius:3px;margin-right:.5em;vertical-align:middle;" />';
                }

                var textLines = [];
                textLines.push(stream.Language || Globalize.translate('LabelUnknownLanguage'));

                var attributes = [];

                if (stream.Codec) {
                    attributes.push(stream.Codec);
                }
                if (stream.Profile) {
                    attributes.push(stream.Profile);
                }

                if (stream.BitRate) {
                    attributes.push((Math.floor(stream.BitRate / 1000)) + ' kbps');
                }

                if (stream.Channels) {
                    attributes.push(stream.Channels + ' ch');
                }

                if (stream.IsDefault) {
                    attributes.push('(D)');
                }

                if (attributes.length) {
                    textLines.push(attributes.join('&nbsp;&#149;&nbsp;'));
                }

                optionHtml += textLines.join('<br/>');

                optionHtml += '</p>';

                optionHtml += '</a></li>';

                return optionHtml;

            }).join('');

            html += '</ul>';
            html += '</div>';

            html += '</div>';

            return html;
        }

        function getSubtitleTracksHtml() {

            var streams = self.currentMediaSource.MediaStreams.filter(function (currentStream) {
                return currentStream.Type == "Subtitle";
            });

            var currentIndex = self.currentSubtitleStreamIndex || -1;

            streams.unshift({
                Index: -1,
                Language: "Off"
            });

            var html = '';
            html += '<div class="videoPlayerPopupContent">';
            html += '<ul data-role="listview" data-inset="true"><li data-role="list-divider">' + Globalize.translate('HeaderSubtitles') + '</li>';
            html += '</ul>';

            html += '<div class="videoPlayerPopupScroller">';
            html += '<ul data-role="listview" data-inset="true">';

            html += streams.map(function (stream) {

                var cssClass = "mediaPopupOption";

                var selected = stream.Index == currentIndex;

                // Need to embed onclick handler due to delegation not working in iOS cordova
                var onclick = '';

                if (selected) {
                    cssClass += ' selectedMediaPopupOption';
                } else {
                    onclick = ' onclick="MediaPlayer.onSubtitleOptionSelected(this);"';
                }

                var optionHtml = '<li><a' + onclick + ' data-value="' + stream.Index + '" class="' + cssClass + '" href="#">';

                optionHtml += '<p style="margin:0;">';

                if (selected) {
                    optionHtml += '<img src="css/images/checkmarkgreen.png" style="width:18px;border-radius:3px;margin-right:.5em;vertical-align:middle;" />';
                }

                var textLines = [];
                textLines.push(stream.Language || Globalize.translate('LabelUnknownLanguage'));

                if (stream.Codec) {
                    textLines.push(stream.Codec);
                }

                var attributes = [];

                if (stream.IsDefault) {
                    attributes.push('Default');
                }
                if (stream.IsForced) {
                    attributes.push('Forced');
                }
                if (stream.IsExternal) {
                    attributes.push('External');
                }

                if (attributes.length) {
                    textLines.push(attributes.join('&nbsp;&#149;&nbsp;'));
                }

                optionHtml += textLines.join('<br/>');

                optionHtml += '</p>';

                optionHtml += '</a></li>';

                return optionHtml;

            }).join('');

            html += '</ul>';
            html += '</div>';

            html += '</div>';

            return html;
        }

        function getQualityFlyoutHtml() {

            var currentSrc = self.getCurrentSrc(self.currentMediaRenderer).toLowerCase();
            var isStatic = currentSrc.indexOf('static=true') != -1;

            var videoStream = self.currentMediaSource.MediaStreams.filter(function (stream) {
                return stream.Type == "Video";
            })[0];
            var videoWidth = videoStream ? videoStream.Width : null;

            var options = self.getVideoQualityOptions(videoWidth);

            if (isStatic) {
                options[0].name = "Direct";
            }

            var html = '';

            html += '<div class="videoPlayerPopupContent">';
            html += '<ul data-role="listview" data-inset="true"><li data-role="list-divider">' + Globalize.translate('HeaderVideoQuality') + '</li>';
            html += '</ul>';

            html += '<div class="videoPlayerPopupScroller">';
            html += '<ul data-role="listview" data-inset="true">';

            html += options.map(function (option) {

                var cssClass = "mediaPopupOption";
                // Need to embed onclick handler due to delegation not working in iOS cordova
                var onclick = '';

                if (option.selected) {
                    cssClass += ' selectedMediaPopupOption';
                } else {
                    onclick = ' onclick="MediaPlayer.onQualityOptionSelected(this);"';
                }

                var optionHtml = '<li><a' + onclick + ' data-value="' + option.bitrate + '" class="' + cssClass + '" href="#">';

                optionHtml += '<p style="margin:0;">';

                if (option.selected) {
                    optionHtml += '<img src="css/images/checkmarkgreen.png" style="width:18px;border-radius:3px;margin-right:.5em;vertical-align:middle;" />';
                }

                optionHtml += option.name;

                optionHtml += '</p>';

                optionHtml += '</a></li>';

                return optionHtml;

            }).join('');

            html += '</ul>';
            html += '</div>';

            html += '</div>';

            return html;
        }

        function bindEventsForPlayback() {

            var hideElementsOnIdle = true;

            if (hideElementsOnIdle) {
                $('.itemVideo').off('mousemove.videoplayer keydown.videoplayer scroll.videoplayer mousedown.videoplayer', idleHandler).on('mousemove.videoplayer keydown.videoplayer scroll.videoplayer mousedown.videoplayer', idleHandler).trigger('mousemove');
            }

            $(document).on('webkitfullscreenchange.videoplayer mozfullscreenchange.videoplayer msfullscreenchange.videoplayer fullscreenchange.videoplayer', function (e) {

                if (self.isFullScreen()) {
                    enterFullScreen();
                    idleState = true;

                } else {
                    exitFullScreenToWindow();
                }
            });

            // Stop playback on browser back button nav
            $(window).one("popstate.videoplayer", function () {
                self.stop();
                return;
            });

            if (hideElementsOnIdle) {
                $(document.body).on("mousemove.videoplayer", function () {

                    idleHandler(this);
                });
            }
        }

        function unbindEventsForPlayback() {

            $(document).off('.videoplayer');

            // Stop playback on browser back button nav
            $(window).off("popstate.videoplayer");

            $(document.body).off("mousemove.videoplayer");

            $('.itemVideo').off('mousemove.videoplayer keydown.videoplayer scroll.videoplayer mousedown.videoplayer');
        }

        self.canAutoPlayVideo = function () {

            if (AppInfo.isNativeApp) {
                return true;
            }

            if ($.browser.mobile) {
                return false;
            }

            return true;
        };

        self.enableCustomVideoControls = function () {

            return self.canAutoPlayVideo() && !$.browser.mobile;
        };

        // Replace audio version
        self.cleanup = function (mediaRenderer) {

            currentTimeElement.html('--:--');

            unbindEventsForPlayback();
        };

        self.playVideo = function (item, mediaSource, startPosition) {

            requirejs(['videorenderer'], function () {

                var streamInfo = self.createStreamInfo('Video', item, mediaSource, startPosition);

                // Huge hack alert. Safari doesn't seem to like if the segments aren't available right away when playback starts
                // This will start the transcoding process before actually feeding the video url into the player
                if ($.browser.safari && !mediaSource.RunTimeTicks) {

                    Dashboard.showLoadingMsg();

                    ApiClient.ajax({
                        type: 'GET',
                        url: streamInfo.url.replace('master.m3u8', 'live.m3u8')
                    }).always(function () {

                        Dashboard.hideLoadingMsg();

                    }).done(function () {
                        self.playVideoInternal(item, mediaSource, startPosition, streamInfo);
                    });

                } else {
                    self.playVideoInternal(item, mediaSource, startPosition, streamInfo);
                }
            });
        };

        function supportsContentOverVideoPlayer() {
            return true;
        }

        self.playVideoInternal = function (item, mediaSource, startPosition, streamInfo) {

            var videoUrl = streamInfo.url;
            var contentType = streamInfo.mimeType;
            var startPositionInSeekParam = streamInfo.startPositionInSeekParam;
            self.startTimeTicksOffset = streamInfo.startTimeTicksOffset;

            var mediaStreams = mediaSource.MediaStreams || [];
            var subtitleStreams = mediaStreams.filter(function (s) {
                return s.Type == 'Subtitle';
            });

            // Reports of stuttering with h264 stream copy in IE
            if (streamInfo.playMethod == 'Transcode' && videoUrl.indexOf('.m3u8') == -1) {
                videoUrl += '&EnableAutoStreamCopy=false';
            }

            var posterCode = self.getPosterUrl(item);
            posterCode = posterCode ? (' poster="' + posterCode + '"') : '';
            //======================================================================================>

            // Create video player
            var html = '';

            var requiresNativeControls = !self.enableCustomVideoControls();

            // Can't autoplay in these browsers so we need to use the full controls
            if (requiresNativeControls && AppInfo.isNativeApp && $.browser.android) {
                html += '<video data-viblast-key="N8FjNTQ3NDdhZqZhNGI5NWU5ZTI=" class="itemVideo" id="itemVideo" preload="metadata" autoplay="autoplay" crossorigin="anonymous" ' + posterCode + ' webkit-playsinline>';
            }
            else if (requiresNativeControls) {
                html += '<video data-viblast-key="N8FjNTQ3NDdhZqZhNGI5NWU5ZTI=" class="itemVideo" id="itemVideo" preload="metadata" autoplay="autoplay" crossorigin="anonymous" controls="controls"' + posterCode + ' webkit-playsinline>';
            }
            else {

                // Chrome 35 won't play with preload none
                html += '<video data-viblast-key="N8FjNTQ3NDdhZqZhNGI5NWU5ZTI=" class="itemVideo" id="itemVideo" preload="metadata" crossorigin="anonymous" autoplay' + posterCode + '>';
            }

            html += '<source type="' + contentType + '" src="' + videoUrl + '" />';

            var textStreams = subtitleStreams.filter(function (s) {
                return s.DeliveryMethod == 'External';
            });

            for (var i = 0, length = textStreams.length; i < length; i++) {

                var textStream = textStreams[i];
                var textStreamUrl = !textStream.IsExternalUrl ? ApiClient.getUrl(textStream.DeliveryUrl) : textStream.DeliveryUrl;
                var defaultAttribute = textStream.Index == mediaSource.DefaultSubtitleStreamIndex ? ' default' : '';

                html += '<track kind="subtitles" src="' + textStreamUrl + '" srclang="' + (textStream.Language || 'und') + '"' + defaultAttribute + '></track>';
            }

            html += '</video>';

            var mediaPlayerContainer = $("#mediaPlayer").show();
            var videoControls = $('.videoControls', mediaPlayerContainer);

            //show stop button
            $('#video-playButton', videoControls).hide();
            $('#video-pauseButton', videoControls).show();
            $('.videoTrackControl').hide();

            var videoElement = $('#videoElement', mediaPlayerContainer).prepend(html);

            $('.videoQualityButton', videoControls).show();

            if (mediaStreams.filter(function (s) {
                return s.Type == "Audio";
            }).length) {
                $('.videoAudioButton').show();
            } else {
                $('.videoAudioButton').hide();
            }

            if (subtitleStreams.length) {
                $('.videoSubtitleButton').show();
            } else {
                $('.videoSubtitleButton').hide();
            }

            if (item.Chapters && item.Chapters.length && supportsContentOverVideoPlayer()) {
                $('.videoChaptersButton').show();
            } else {
                $('.videoChaptersButton').hide();
            }

            if (requiresNativeControls) {
                $('#video-fullscreenButton', videoControls).hide();
            } else {
                $('#video-fullscreenButton', videoControls).show();
            }

            if (AppInfo.hasPhysicalVolumeButtons) {
                $('.volumeSliderContainer', videoControls).addClass('hide');
                $('.muteButton', videoControls).addClass('hide');
                $('.unmuteButton', videoControls).addClass('hide');
            } else {
                $('.volumeSliderContainer', videoControls).removeClass('hide');
                $('.muteButton', videoControls).removeClass('hide');
                $('.unmuteButton', videoControls).removeClass('hide');
            }

            if (requiresNativeControls) {
                videoControls.addClass('hide');
            } else {
                videoControls.removeClass('hide');
            }

            var mediaRenderer = new VideoRenderer('video');

            initialVolume = self.getSavedVolume();

            mediaRenderer.volume(initialVolume);

            volumeSlider.val(initialVolume).slider('refresh');
            updateVolumeButtons(initialVolume);

            $(mediaRenderer).on("volumechange.mediaplayerevent", function (e) {

                updateVolumeButtons(this.volume());

            }).one("playing.mediaplayerevent", function () {


                // For some reason this is firing at the start, so don't bind until playback has begun
                $(this).on("ended.playbackstopped", self.onPlaybackStopped).one('ended.playnext', self.playNextAfterEnded);

                self.onPlaybackStart(this, item, mediaSource);

            }).on("pause.mediaplayerevent", function (e) {

                $('#video-playButton', videoControls).show();
                $('#video-pauseButton', videoControls).hide();
                $("#pause", videoElement).show().addClass("fadeOut");
                setTimeout(function () {
                    $("#pause", videoElement).hide().removeClass("fadeOut");
                }, 300);

            }).on("playing.mediaplayerevent", function (e) {

                $('#video-playButton', videoControls).hide();
                $('#video-pauseButton', videoControls).show();
                $("#play", videoElement).show().addClass("fadeOut");
                setTimeout(function () {
                    $("#play", videoElement).hide().removeClass("fadeOut");
                }, 300);

            }).on("timeupdate.mediaplayerevent", function () {

                if (!isPositionSliderActive) {

                    self.setCurrentTime(self.getCurrentTicks(this), positionSlider, currentTimeElement);
                }

            }).on("error.mediaplayerevent", function () {

                self.stop();

                var errorMsg = Globalize.translate('MessageErrorPlayingVideo');

                if (item.Type == "TvChannel") {
                    errorMsg += '<p>';
                    errorMsg += Globalize.translate('MessageEnsureOpenTuner');
                    errorMsg += '</p>';
                }

                if ($.browser.msie && !$.browser.mobile && !self.canPlayWebm()) {
                    errorMsg += '<p>';
                    errorMsg += '<a href="https://tools.google.com/dlpage/webmmf/" target="_blank">';
                    errorMsg += Globalize.translate('MessageInternetExplorerWebm');
                    errorMsg += '</a>';
                    errorMsg += '</p>';
                }

                Dashboard.alert({
                    title: Globalize.translate('HeaderVideoError'),
                    message: errorMsg
                });

            }).on("click.mediaplayerevent", function (e) {

                if (!$.browser.mobile) {
                    if (this.paused()) {
                        self.unpause();
                    } else {
                        self.pause();
                    }
                }

            }).on("dblclick.mediaplayerevent", function () {

                if (!$.browser.mobile) {
                    self.toggleFullscreen();
                }
            });

            bindEventsForPlayback();

            mediaPlayerContainer.trigger("create");

            self.currentSubtitleStreamIndex = mediaSource.DefaultSubtitleStreamIndex;

            $('body').addClass('bodyWithPopupOpen');

            self.currentMediaRenderer = mediaRenderer;
            self.currentDurationTicks = self.currentMediaSource.RunTimeTicks;

            self.updateNowPlayingInfo(item);
        };

        self.updatePlaylistUi = function () {
            var index = self.currentPlaylistIndex(null);
            var length = self.playlist.length;
            var requiresNativeControls = !self.enableCustomVideoControls();
            var controls = $(requiresNativeControls ? '.videoAdvancedControls' : '.videoControls');

            if (length < 2) {
                $('.videoTrackControl').hide();
                return;
            }

            if (index === 0) {
                $('.previousTrackButton', controls).attr('disabled', 'disabled');
            } else {
                $('.previousTrackButton', controls).removeAttr('disabled');
            }

            if ((index + 1) >= length) {
                $('.nextTrackButton', controls).attr('disabled', 'disabled');
            } else {
                $('.nextTrackButton', controls).removeAttr('disabled');
            }

            $('.videoTrackControl', controls).show();
        };
    }

    createVideoPlayer(MediaPlayer);

})();