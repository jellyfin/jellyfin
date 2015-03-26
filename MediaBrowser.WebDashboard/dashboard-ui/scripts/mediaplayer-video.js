(function () {

    function createVideoPlayer(self) {

        var timeout;
        var initialVolume;
        var fullscreenExited = false;
        var idleState = true;
        var remoteFullscreen = false;

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

        self.remoteFullscreen = function () {

            if (remoteFullscreen) {
                exitFullScreenToWindow();
            } else {
                enterFullScreen();
            }

            remoteFullscreen = !remoteFullscreen;
        };

        self.toggleFullscreen = function () {
            if (self.isFullScreen()) {
                if (document.cancelFullScreen) {
                    document.cancelFullScreen();
                }
                else if (document.mozCancelFullScreen) {
                    document.mozCancelFullScreen();
                }
                else if (document.webkitExitFullscreen) {
                    document.webkitExitFullscreen();
                }
                else if (document.webkitCancelFullScreen) {
                    document.webkitCancelFullScreen();
                }
                $('#videoPlayer').removeClass('fullscreenVideo');
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
            } else if (document.mozExitFullScreen) {
                document.mozExitFullScreen();
            } else if (document.webkitExitFullscreen) {
                document.webkitExitFullscreen();
            }

            $('#videoPlayer').removeClass('fullscreenVideo');

            fullscreenExited = true;
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
                $('.itemVideo').css('visibility', 'hidden');
            }
        }

        self.showSubtitleMenu = function () {

            var elem = $('.videoSubtitlePopup').html(getSubtitleTracksHtml())
                .trigger('create')
                .popup("option", "positionTo", $('.videoSubtitleButton'))
                .off('popupafterclose', onFlyoutClose)
                .on('popupafterclose', onFlyoutClose);

            onPopupOpen(elem);
        };

        self.showQualityFlyout = function () {

            var elem = $('.videoQualityPopup').html(getQualityFlyoutHtml())
                .trigger('create')
                .popup("option", "positionTo", $('.videoQualityButton'))
                .off('popupafterclose', onFlyoutClose)
                .on('popupafterclose', onFlyoutClose);

            onPopupOpen(elem);
        };

        self.showChaptersFlyout = function () {

            var elem = $('.videoChaptersPopup').html(getChaptersFlyoutHtml())
                .trigger('create')
                .popup("option", "positionTo", $('.videoChaptersButton'))
                .off('popupafterclose', onFlyoutClose)
                .on('popupafterclose', onFlyoutClose);

            onPopupOpen(elem);
        };

        self.showAudioTracksFlyout = function () {

            var elem = $('.videoAudioPopup').html(getAudioTracksHtml())
                .trigger('create')
                .popup("option", "positionTo", $('.videoAudioButton'))
                .off('popupafterclose', onFlyoutClose)
                .on('popupafterclose', onFlyoutClose);

            onPopupOpen(elem);
        };

        self.setAudioStreamIndex = function (index) {
            self.changeStream(self.getCurrentTicks(), { AudioStreamIndex: index });
        };

        self.supportsSubtitleStreamExternally = function (stream) {
            return stream.Type == 'Subtitle' && stream.IsTextSubtitleStream && stream.SupportsExternalStream;
        }

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

                if (!self.supportsSubtitleStreamExternally(currentStream)) {

                    // Need to change the transcoded stream to remove subs
                    self.changeStream(self.getCurrentTicks(), { SubtitleStreamIndex: -1 });
                }
            }
            else if (!currentStream && newStream) {

                if (self.supportsSubtitleStreamExternally(newStream)) {
                    selectedTrackElementIndex = index;
                } else {

                    // Need to change the transcoded stream to add subs
                    self.changeStream(self.getCurrentTicks(), { SubtitleStreamIndex: index });
                }
            }
            else if (currentStream && newStream) {

                if (self.supportsSubtitleStreamExternally(newStream)) {
                    selectedTrackElementIndex = index;

                    if (!self.supportsSubtitleStreamExternally(currentStream)) {
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
                return self.supportsSubtitleStreamExternally(s);
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

            var state = self.getPlayerStateInternal(self.currentMediaElement, item, self.currentMediaSource);

            var url = "";

            if (state.NowPlayingItem.PrimaryImageTag) {

                url = ApiClient.getScaledImageUrl(state.NowPlayingItem.PrimaryImageItemId, {
                    type: "Primary",
                    width: 150,
                    tag: state.NowPlayingItem.PrimaryImageTag
                });
            }
            else if (state.NowPlayingItem.PrimaryImageTag) {

                url = ApiClient.getScaledImageUrl(state.NowPlayingItem.PrimaryImageItemId, {
                    type: "Primary",
                    width: 150,
                    tag: state.NowPlayingItem.PrimaryImageTag
                });
            }
            else if (state.NowPlayingItem.BackdropImageTag) {

                url = ApiClient.getScaledImageUrl(state.NowPlayingItem.BackdropItemId, {
                    type: "Backdrop",
                    height: 300,
                    tag: state.NowPlayingItem.BackdropImageTag,
                    index: 0
                });

            }
            else if (state.NowPlayingItem.ThumbImageTag) {

                url = ApiClient.getScaledImageUrl(state.NowPlayingItem.ThumbImageItemId, {
                    type: "Thumb",
                    height: 300,
                    tag: state.NowPlayingItem.ThumbImageTag
                });
            }

            var nowPlayingTextElement = $('.nowPlayingText', mediaControls);
            var nameHtml = self.getNowPlayingNameHtml(state);

            if (nameHtml.indexOf('<br/>') != -1) {
                nowPlayingTextElement.addClass('nowPlayingDoubleText');
            } else {
                nowPlayingTextElement.removeClass('nowPlayingDoubleText');
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

            nowPlayingTextElement.html(nameHtml);
        };

        function onPositionSliderChange() {

            isPositionSliderActive = false;

            var newPercent = parseInt(this.value);

            var newPositionTicks = (newPercent / 100) * self.currentMediaSource.RunTimeTicks;

            self.changeStream(Math.floor(newPositionTicks));
        }

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

            $('.videoChaptersPopup').on('click', '.mediaPopupOption', function () {

                var ticks = parseInt(this.getAttribute('data-positionticks') || '0');

                self.changeStream(ticks);

                $('.videoChaptersPopup').popup('close');
            });

            $('.videoAudioPopup').on('click', '.mediaPopupOption', function () {

                if (!$(this).hasClass('selectedMediaPopupOption')) {
                    var index = parseInt(this.getAttribute('data-index'));

                    self.setAudioStreamIndex(index);
                }

                $('.videoAudioPopup').popup('close');
            });

            $('.videoSubtitlePopup').on('click', '.mediaPopupOption', function () {

                $('.videoSubtitlePopup').popup('close');

                if (!$(this).hasClass('selectedMediaPopupOption')) {

                    var index = parseInt(this.getAttribute('data-index'));

                    self.setSubtitleStreamIndex(index);
                }
            });

            $('.videoQualityPopup').on('click', '.mediaPopupOption', function () {

                if (!$(this).hasClass('selectedMediaPopupOption')) {

                    var bitrate = parseInt(this.getAttribute('data-bitrate'));

                    AppSettings.maxStreamingBitrate(bitrate);

                    self.changeStream(self.getCurrentTicks(), {

                        Bitrate: bitrate
                    });
                }

                $('.videoQualityPopup').popup('close');
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

            $('.videoSubtitleButton').on('click', function () {

                self.showSubtitleMenu();
            });

            $('.videoQualityButton').on('click', function () {

                self.showQualityFlyout();
            });

            $('.videoAudioButton').on('click', function () {

                self.showAudioTracksFlyout();
            });

            $('.videoChaptersButton').on('click', function () {

                self.showChaptersFlyout();
            });
        });

        function idleHandler() {

            if (timeout) {
                window.clearTimeout(timeout);
            }

            if (idleState == true) {
                $('.hiddenOnIdle').removeClass("inactive");
                $('#videoPlayer').removeClass('idlePlayer');
            }

            idleState = false;

            timeout = window.setTimeout(function () {
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
            var requestMethod = element.requestFullscreen || element.webkitRequestFullscreen || element.webkitRequestFullScreen || element.mozRequestFullScreen;

            if (requestMethod) { // Native full screen.
                requestMethod.call(element);
            } else {
                enterFullScreen();
            }

        }

        function enterFullScreen() {

            var player = $("#videoPlayer");

            player.addClass("fullscreenVideo");

            remoteFullscreen = true;

        }

        function exitFullScreenToWindow() {

            var player = $("#videoPlayer");

            player.removeClass("fullscreenVideo");

            remoteFullscreen = false;

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

                if (currentTicks >= chapter.StartPositionTicks) {
                    var nextChapter = chapters[index + 1];
                    selected = !nextChapter || currentTicks < nextChapter.StartPositionTicks;
                }

                var optionHtml = '<li><a data-positionticks="' + chapter.StartPositionTicks + '" class="' + cssClass + '" href="#" style="padding-top:0;padding-bottom:0;">';

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

            var currentIndex = getParameterByName('AudioStreamIndex', self.getCurrentSrc(self.currentMediaElement));

            var html = '';
            html += '<div class="videoPlayerPopupContent">';
            html += '<ul data-role="listview" data-inset="true"><li data-role="list-divider">' + Globalize.translate('HeaderAudioTracks') + '</li>';
            html += '</ul>';

            html += '<div class="videoPlayerPopupScroller">';
            html += '<ul data-role="listview" data-inset="true">';

            html += streams.map(function (stream) {

                var cssClass = "mediaPopupOption";

                var selected = stream.Index == currentIndex;

                if (selected) {
                    cssClass += ' selectedMediaPopupOption';
                }

                var optionHtml = '<li><a data-index="' + stream.Index + '" class="' + cssClass + '" href="#">';

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

                if (selected) {
                    cssClass += ' selectedMediaPopupOption';
                }

                var optionHtml = '<li><a data-index="' + stream.Index + '" class="' + cssClass + '" href="#">';

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

            var currentSrc = self.getCurrentSrc(self.currentMediaElement).toLowerCase();
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

                if (option.selected) {
                    cssClass += ' selectedMediaPopupOption';
                }

                var optionHtml = '<li><a data-bitrate="' + option.bitrate + '" class="' + cssClass + '" href="#">';

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

            var hideElementsOnIdle = !$.browser.mobile;

            if (hideElementsOnIdle) {
                $('.itemVideo').off('mousemove.videoplayer keydown.videoplayer scroll.videoplayer', idleHandler);
                $('.itemVideo').on('mousemove.videoplayer keydown.videoplayer scroll.videoplayer', idleHandler).trigger('mousemove');
            }

            $(document).on('webkitfullscreenchange.videoplayer mozfullscreenchange.videoplayer fullscreenchange.videoplayer', function (e) {

                if (self.isFullScreen()) {
                    enterFullScreen();
                    idleState = true;

                } else {
                    exitFullScreenToWindow();
                }

                fullscreenExited = self.isFullScreen() == false;

            }).on("msfullscreenchange.videoplayer", function (e) {

                fullscreenExited = self.isFullScreen() == false;

            }).on("keyup.videoplayer", function (e) {

                if (fullscreenExited) {
                    $("#videoPlayer", $("#mediaPlayer")).removeClass("fullscreenVideo");
                    fullscreenExited = false;
                    return;
                }

                if (e.keyCode == 27) {
                    self.stop();
                }
            });

            // Stop playback on browser back button nav
            $(window).one("popstate.videoplayer", function () {
                self.stop();
                return;
            });

            if (hideElementsOnIdle) {
                $(document.body).on("mousemove.videplayer", "#itemVideo", function () {

                    idleHandler(this);
                });
            }
        }

        function unbindEventsForPlayback() {

            $(document).off('webkitfullscreenchange.videoplayer mozfullscreenchange.videoplayer fullscreenchange.videoplayer').off("msfullscreenchange.videoplayer").off("keyup.videoplayer");

            // Stop playback on browser back button nav
            $(window).off("popstate.videoplayer");

            $(document.body).off("mousemove.videplayer");

            $('.itemVideo').off('mousemove.videoplayer keydown.videoplayer scroll.videoplayer');
        }

        self.canAutoPlayVideo = function () {

            if ($.browser.msie || $.browser.mobile) {
                return false;
            }

            return true;
        };

        // Replace audio version
        self.cleanup = function (playerElement) {

            if (playerElement.tagName.toLowerCase() == 'video') {
                currentTimeElement.html('--:--');

                unbindEventsForPlayback();
            }
        };

        self.playVideo = function (deviceProfile, playbackInfo, item, mediaSource, startPosition) {

            var videoUrl;
            var contentType;

            var mediaStreams = mediaSource.MediaStreams || [];
            var subtitleStreams = mediaStreams.filter(function (s) {
                return s.Type == 'Subtitle';
            });

            if (mediaSource.enableDirectPlay) {
                videoUrl = mediaSource.Path;
                self.startTimeTicksOffset = 0;
                contentType = 'video/' + mediaSource.Container;
            } else {

                var selectedSubtitleStream = subtitleStreams.filter(function (s) {
                    return s.Index == mediaSource.DefaultSubtitleStreamIndex;

                })[0];

                var transcodingParams = {
                    audioChannels: 2,
                    StartTimeTicks: startPosition,
                    AudioStreamIndex: mediaSource.DefaultAudioStreamIndex,
                    deviceId: ApiClient.deviceId(),
                    mediaSourceId: mediaSource.Id,
                    api_key: ApiClient.accessToken(),
                    StreamId: playbackInfo.StreamId,
                    ClientTime: new Date().getTime()
                };

                if (selectedSubtitleStream && (!self.supportsSubtitleStreamExternally(selectedSubtitleStream) || !self.supportsTextTracks())) {
                    transcodingParams.SubtitleStreamIndex = mediaSource.DefaultSubtitleStreamIndex;
                }

                self.startTimeTicksOffset = mediaSource.SupportsDirectStream ? 0 : startPosition || 0;
                var startPositionInSeekParam = startPosition ? (startPosition / 10000000) : 0;
                var seekParam = startPositionInSeekParam ? '#t=' + startPositionInSeekParam : '';

                if (mediaSource.SupportsDirectStream) {

                    videoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.' + mediaSource.Container, {
                        Static: true,
                        mediaSourceId: mediaSource.Id,
                        api_key: ApiClient.accessToken()
                    });
                    videoUrl += seekParam;
                    contentType = 'video/' + mediaSource.Container;

                } else {
                    videoUrl = ApiClient.getUrl(mediaSource.TranscodingUrl);

                    if (mediaSource.TranscodingSubProtocol == 'hls') {

                        videoUrl += seekParam;
                        contentType = 'application/x-mpegURL';
                    }
                    else {

                        contentType = 'video/' + mediaSource.TranscodingContainer;
                    }
                }
            }

            //======================================================================================>

            // Create video player
            var html = '';

            var requiresNativeControls = !self.canAutoPlayVideo();

            // Can't autoplay in these browsers so we need to use the full controls
            if (requiresNativeControls) {
                html += '<video class="itemVideo" id="itemVideo" preload="none" autoplay="autoplay" controls="controls">';
            } else {

                // Chrome 35 won't play with preload none
                html += '<video class="itemVideo" id="itemVideo" preload="metadata" autoplay>';
            }

            html += '<source type="' + contentType + '" src="' + videoUrl + '" />';

            if (self.supportsTextTracks()) {
                var textStreams = subtitleStreams.filter(function (s) {
                    return self.supportsSubtitleStreamExternally(s);
                });

                for (var i = 0, length = textStreams.length; i < length; i++) {

                    var textStream = textStreams[i];
                    var textStreamUrl = ApiClient.getUrl('Videos/' + item.Id + '/' + mediaSource.Id + '/Subtitles/' + textStream.Index + '/Stream.vtt', {
                        startPositionTicks: (startPosition || 0),
                        api_key: ApiClient.accessToken()
                    });

                    var defaultAttribute = textStream.Index == mediaSource.DefaultSubtitleStreamIndex ? ' default' : '';

                    html += '<track kind="subtitles" src="' + textStreamUrl + '" srclang="' + (textStream.Language || 'und') + '"' + defaultAttribute + '></track>';
                }
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

            if (item.Chapters && item.Chapters.length) {
                $('.videoChaptersButton').show();
            } else {
                $('.videoChaptersButton').hide();
            }

            if (requiresNativeControls) {
                $('#video-fullscreenButton', videoControls).hide();
            } else {
                $('#video-fullscreenButton', videoControls).show();
            }

            if ($.browser.mobile) {
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

            var video = $("video", videoElement);

            initialVolume = self.getSavedVolume();

            video.each(function () {
                this.volume = initialVolume;
            });

            volumeSlider.val(initialVolume).slider('refresh');
            updateVolumeButtons(initialVolume);

            video.one("loadedmetadata.mediaplayerevent", function (e) {

                // TODO: This is not working in chrome. Is it too early?

                // Appending #t=xxx to the query string doesn't seem to work with HLS
                if (startPositionInSeekParam && this.currentSrc && this.currentSrc.toLowerCase().indexOf('.m3u8') != -1) {
                    this.currentTime = startPositionInSeekParam;
                }

            }).on("volumechange.mediaplayerevent", function (e) {

                var vol = this.volume;

                updateVolumeButtons(vol);

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

                var errorCode = this.error ? this.error.code : '';
                console.log('Html5 Video error code: ' + errorCode);

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

                if (this.paused) {
                    self.unpause();
                } else {
                    self.pause();
                }

            }).on("dblclick.mediaplayerevent", function () {

                self.toggleFullscreen();

            });

            bindEventsForPlayback();

            mediaPlayerContainer.trigger("create");

            fullscreenExited = false;

            self.currentSubtitleStreamIndex = mediaSource.DefaultSubtitleStreamIndex;

            $('body').addClass('bodyWithPopupOpen');

            return video[0];
        };

        self.updatePlaylistUi = function () {
            var index = self.currentPlaylistIndex(null),
                length = self.playlist.length,
                requiresNativeControls = !self.canAutoPlayVideo(),
                controls = $(requiresNativeControls ? '.videoAdvancedControls' : '.videoControls');

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