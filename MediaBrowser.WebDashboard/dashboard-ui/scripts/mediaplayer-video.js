define(['appSettings', 'datetime', 'mediaInfo', 'scrollStyles', 'paper-icon-button-light'], function (appSettings, datetime, mediaInfo) {

    function createVideoPlayer(self) {

        var initialVolume;
        var idleState = true;

        var muteButton = null;
        var unmuteButton = null;
        var volumeSlider = null;
        var volumeSliderContainer = null;
        var positionSlider;
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

        function setClass(elems, method, className) {

            for (var i = 0, length = elems.length; i < length; i++) {
                elems[i].classList[method](className);
            }
        }

        self.resetEnhancements = function () {

            if (!initComplete) {
                return;
            }

            if (self.isFullScreen()) {
                self.exitFullScreen();
            }

            var videoPlayerElement = document.querySelector('#videoPlayer');

            fadeOut(videoPlayerElement);
            videoPlayerElement.classList.remove('fullscreenVideo');
            videoPlayerElement.classList.remove('idlePlayer');

            setClass(document.querySelectorAll('.hiddenOnIdle'), 'remove', 'inactive');
            var video = videoPlayerElement.querySelector('video');
            if (video) {
                video.parentNode.removeChild(video);
            }

            document.querySelector('.mediaButton.infoButton').classList.remove('active');
            document.querySelector('.videoControls .nowPlayingInfo').classList.add('hide');
            document.querySelector('.videoControls').classList.add('hiddenOnIdle');
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

            document.querySelector('#videoPlayer').classList.remove('fullscreenVideo');
        };

        self.isFullScreen = function () {
            return document.fullscreen || document.mozFullScreen || document.webkitIsFullScreen || document.msFullscreenElement ? true : false;
        };

        self.showSubtitleMenu = function () {

            var streams = self.currentMediaSource.MediaStreams.filter(function (currentStream) {
                return currentStream.Type == "Subtitle";
            });

            var currentIndex = self.currentSubtitleStreamIndex;
            if (currentIndex == null) {
                currentIndex = -1;
            }

            streams.unshift({
                Index: -1,
                Language: Globalize.translate('ButtonOff')
            });

            var menuItems = streams.map(function (stream) {

                var attributes = [];

                attributes.push(stream.Language || Globalize.translate('LabelUnknownLanguage'));

                if (stream.Codec) {
                    attributes.push(stream.Codec);
                }

                var name = attributes.join(' - ');

                if (stream.IsDefault) {
                    name += ' (D)';
                }
                if (stream.IsForced) {
                    name += ' (F)';
                }
                if (stream.External) {
                    name += ' (EXT)';
                }

                var opt = {
                    name: stream.DisplayTitle || name,
                    id: stream.Index
                };

                if (stream.Index == currentIndex) {
                    opt.selected = true;
                }

                return opt;
            });

            require(['actionsheet'], function (actionsheet) {

                actionsheet.show({
                    items: menuItems,
                    // history.back() will cause the video player to stop
                    enableHistory: false,
                    positionTo: document.querySelector('.videoSubtitleButton'),
                    callback: function (id) {

                        var index = parseInt(id);
                        if (index != currentIndex) {
                            self.onSubtitleOptionSelected(index);
                        }
                    }
                });

            });
        };

        self.showQualityFlyout = function () {

            require(['qualityoptions', 'actionsheet'], function (qualityoptions, actionsheet) {

                var currentSrc = self.getCurrentSrc(self.currentMediaRenderer).toLowerCase();
                var isStatic = currentSrc.indexOf('static=true') != -1;

                var videoStream = self.currentMediaSource.MediaStreams.filter(function (stream) {
                    return stream.Type == "Video";
                })[0];
                var videoWidth = videoStream ? videoStream.Width : null;

                var options = qualityoptions.getVideoQualityOptions(appSettings.maxStreamingBitrate(), videoWidth);

                if (isStatic) {
                    options[0].name = "Direct";
                }

                var menuItems = options.map(function (o) {

                    var opt = {
                        name: o.name,
                        id: o.bitrate
                    };

                    if (o.selected) {
                        opt.selected = true;
                    }

                    return opt;
                });

                var selectedId = options.filter(function (o) {
                    return o.selected;
                });
                selectedId = selectedId.length ? selectedId[0].bitrate : null;
                actionsheet.show({
                    items: menuItems,
                    // history.back() will cause the video player to stop
                    enableHistory: false,
                    positionTo: document.querySelector('.videoQualityButton'),
                    callback: function (id) {

                        var bitrate = parseInt(id);
                        if (bitrate != selectedId) {
                            self.onQualityOptionSelected(bitrate);
                        }
                    }
                });

            });
        };

        self.showAudioTracksFlyout = function () {

            var options = self.currentMediaSource.MediaStreams.filter(function (currentStream) {
                return currentStream.Type == "Audio";
            });

            var currentIndex = getParameterByName('AudioStreamIndex', self.getCurrentSrc(self.currentMediaRenderer));

            var menuItems = options.map(function (stream) {

                var attributes = [];

                attributes.push(stream.Language || Globalize.translate('LabelUnknownLanguage'));

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

                var name = attributes.join(' - ');

                if (stream.IsDefault) {
                    name += ' (D)';
                }

                var opt = {
                    name: stream.DisplayTitle || name,
                    id: stream.Index
                };

                if (stream.Index == currentIndex) {
                    opt.selected = true;
                }

                return opt;
            });

            require(['actionsheet'], function (actionsheet) {

                actionsheet.show({
                    items: menuItems,
                    // history.back() will cause the video player to stop
                    enableHistory: false,
                    positionTo: document.querySelector('.videoAudioButton'),
                    callback: function (id) {

                        var index = parseInt(id);
                        if (index != currentIndex) {
                            self.onAudioOptionSelected(index);
                        }
                    }
                });

            });
        };

        self.setAudioStreamIndex = function (index) {
            self.changeStream(self.getCurrentTicks(), { AudioStreamIndex: index });
        };

        self.setSubtitleStreamIndex = function (index) {

            if (!self.currentMediaRenderer.supportsTextTracks()) {

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

            self.currentMediaRenderer.setCurrentTrackElement(index);
        };

        self.updateTextStreamUrls = function (startPositionTicks) {

            self.currentMediaRenderer.updateTextStreamUrls(startPositionTicks);
        };

        self.updateNowPlayingInfo = function (item) {

            if (!item) {
                throw new Error('item cannot be null');
            }

            var mediaControls = document.querySelector("#videoPlayer");

            var state = self.getPlayerStateInternal(self.currentMediaRenderer, item.CurrentProgram || item, self.currentMediaSource);

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
                mediaControls.querySelector('.nowPlayingImage').innerHTML = '<img src="' + url + '" />';
            } else {
                mediaControls.querySelector('.nowPlayingImage').innerHTML = '';
            }

            if (state.NowPlayingItem.LogoItemId) {

                url = ApiClient.getScaledImageUrl(state.NowPlayingItem.LogoItemId, {
                    type: "Logo",
                    height: 42,
                    tag: state.NowPlayingItem.LogoImageTag
                });

                mediaControls.querySelector('.videoTopControlsLogo').innerHTML = '<img src="' + url + '" />';
            } else {
                mediaControls.querySelector('.videoTopControlsLogo').innerHTML = '';
            }

            var elem = mediaControls.querySelector('.nowPlayingTabs');
            elem.innerHTML = getNowPlayingTabsHtml(item.CurrentProgram || item);
            ImageLoader.lazyChildren(elem);

            function onTabButtonClick() {
                if (!this.classList.contains('selectedNowPlayingTabButton')) {

                    var selectedNowPlayingTabButton = document.querySelector('.selectedNowPlayingTabButton');
                    if (selectedNowPlayingTabButton) {
                        selectedNowPlayingTabButton.classList.remove('selectedNowPlayingTabButton');
                    }
                    this.classList.add('selectedNowPlayingTabButton');
                    setClass(document.querySelectorAll('.nowPlayingTab'), 'add', 'hide');
                    document.querySelector('.' + this.getAttribute('data-tab')).classList.remove('hide');
                }
            }

            var nowPlayingTabButtons = elem.querySelectorAll('.nowPlayingTabButton');
            for (var i = 0, length = nowPlayingTabButtons.length; i < length; i++) {
                nowPlayingTabButtons[i].addEventListener('click', onTabButtonClick);
            }

            elem.addEventListener('click', function (e) {

                var chapterCard = parentWithClass(e.target, 'chapterCard');
                if (chapterCard) {
                    self.seek(parseInt(chapterCard.getAttribute('data-position')));
                }
            });
        };

        function parentWithClass(elem, className) {

            while (!elem.classList || !elem.classList.contains(className)) {
                elem = elem.parentNode;

                if (!elem) {
                    return null;
                }
            }

            return elem;
        }

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

            var miscInfo = mediaInfo.getPrimaryMediaInfoHtml(item);
            if (miscInfo) {

                nameHtml += '<div class="videoNowPlayingRating">' + miscInfo + '</div>';
            }

            if (item.Overview) {

                nameHtml += '<div class="videoNowPlayingOverview">' + item.Overview + '</div>';
            }

            html += nameHtml;
            html += '</div>';

            if (item.Chapters && item.Chapters.length) {
                html += '<div class="tabScenes nowPlayingTab smoothScrollX hide" style="white-space:nowrap;margin-bottom:2em;">';
                var chapterIndex = 0;
                html += item.Chapters.map(function (c) {

                    var width = 240;
                    var chapterHtml = '<a class="card backdropCard chapterCard" href="#" style="margin-right:1em;width:' + width + 'px;" data-position="' + c.StartPositionTicks + '">';
                    chapterHtml += '<div class="cardBox">';
                    chapterHtml += '<div class="cardScalable visualCardBox-cardScalable">';
                    chapterHtml += '<div class="cardPadder cardPadder-backdrop"></div>';

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
                    chapterHtml += '<div class="cardText">' + datetime.getDisplayRunningTime(c.StartPositionTicks) + '</div>';
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
                html += '<div class="tabCast nowPlayingTab smoothScrollX hide" style="white-space:nowrap;">';
                html += item.People.map(function (cast) {

                    var personHtml = '<div class="tileItem smallPosterTileItem" style="width:300px;">';

                    var imgUrl;
                    var height = 150;

                    if (cast.PrimaryImageTag) {

                        imgUrl = ApiClient.getScaledImageUrl(cast.Id, {
                            height: height,
                            tag: cast.PrimaryImageTag,
                            type: "primary",
                            minScale: 2
                        });

                        personHtml += '<div class="tileImage lazy" data-src="' + imgUrl + '" style="height:' + height + 'px;"></div>';
                    } else {

                        imgUrl = "css/images/items/list/person.png";
                        personHtml += '<div class="tileImage" style="background-image:url(\'' + imgUrl + '\');height:' + height + 'px;"></div>';
                    }

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

        function getSeekableDuration() {

            if (self.currentMediaSource && self.currentMediaSource.RunTimeTicks) {
                return self.currentMediaSource.RunTimeTicks;
            }

            if (self.currentMediaRenderer) {
                return self.getCurrentTicks(self.currentMediaRenderer);
            }

            return null;
        }

        function onPositionSliderChange() {

            var newPercent = parseFloat(this.value);

            var newPositionTicks = (newPercent / 100) * getSeekableDuration();

            self.changeStream(Math.floor(newPositionTicks));
        }

        self.onAudioOptionSelected = function (index) {

            self.setAudioStreamIndex(index);
        };

        self.onSubtitleOptionSelected = function (index) {

            self.setSubtitleStreamIndex(index);
        };

        self.onQualityOptionSelected = function (bitrate) {

            appSettings.maxStreamingBitrate(bitrate);
            appSettings.enableAutomaticBitrateDetection(false);

            self.changeStream(self.getCurrentTicks(), {
                Bitrate: bitrate
            });
        };

        self.toggleInfo = function () {

            var button = document.querySelector('.mediaButton.infoButton');
            var nowPlayingInfo = document.querySelector('.videoControls .nowPlayingInfo');

            if (button.classList.contains('active')) {
                button.classList.remove('active');
                document.querySelector('.videoControls').classList.add('hiddenOnIdle');

                fadeOutDown(nowPlayingInfo);

            } else {
                button.classList.add('active');
                document.querySelector('.videoControls').classList.remove('hiddenOnIdle');
                nowPlayingInfo.classList.remove('hide');
                fadeInUp(nowPlayingInfo);
            }
        };

        self.toggleGuide = function () {

            var button = document.querySelector('.mediaButton.guideButton');
            var nowPlayingInfo = document.querySelector('.videoControls .guide');

            if (button.classList.contains('active')) {
                button.classList.remove('active');
                document.querySelector('.videoControls').classList.add('hiddenOnIdle');

                fadeOutDown(nowPlayingInfo);

            } else {
                button.classList.add('active');
                document.querySelector('.videoControls').classList.remove('hiddenOnIdle');
                nowPlayingInfo.classList.remove('hide');
                fadeInUp(nowPlayingInfo);

                if (!self.guideInstance) {
                    require(['tvguide'], function (tvguide) {

                        self.guideInstance = new tvguide({
                            element: nowPlayingInfo,
                            enablePaging: false
                        });
                    });
                }
            }
        };

        function fadeInUp(elem) {
            var keyframes = [
              { transform: 'translate3d(0, 100%, 0)', offset: 0 },
              { transform: 'translate3d(0, 0, 0)', offset: 1 }];
            var timing = { duration: 300, iterations: 1 };

            if (elem.animate) {
                elem.animate(keyframes, timing);
            }
        }

        function fadeOutDown(elem) {
            var keyframes = [{ transform: 'translate3d(0, 0, 0)', offset: 0 },
              { transform: 'translate3d(0, 100%, 0)', offset: 1 }];
            var timing = { duration: 300, iterations: 1 };

            var onFinish = function () {
                elem.classList.add('hide');
            };

            if (elem.animate) {
                elem.animate(keyframes, timing).onfinish = onFinish;
            } else {
                onFinish();
            }
        }

        function ensureVideoPlayerElements() {

            var html = '';

            html += '<div id="videoPlayer" class="hide">';

            html += '<div id="videoElement">';
            html += '<div id="play" class="status"></div>';
            html += '<div id="pause" class="status"></div>';
            html += '</div>';

            var hiddenOnIdleClass = AppInfo.isNativeApp && browserInfo.android ? 'hiddenOnIdle hide' : 'hiddenOnIdle';

            html += '<div class="videoTopControls ' + hiddenOnIdleClass + '">';
            html += '<div class="videoTopControlsLogo"></div>';
            html += '<div class="videoAdvancedControls">';

            html += '<button is="paper-icon-button-light" class="previousTrackButton mediaButton videoTrackControl hide autoSize" onclick="MediaPlayer.previousTrack();"><i class="md-icon">skip_previous</i></button>';
            html += '<button is="paper-icon-button-light" class="nextTrackButton mediaButton videoTrackControl hide autoSize" onclick="MediaPlayer.nextTrack();"><i class="md-icon">skip_next</i></button>';

            // Embedding onclicks due to issues not firing in cordova safari
            html += '<button is="paper-icon-button-light" class="mediaButton videoAudioButton autoSize" onclick="MediaPlayer.showAudioTracksFlyout();"><i class="md-icon">audiotrack</i></button>';
            html += '<button is="paper-icon-button-light" class="mediaButton videoSubtitleButton autoSize" onclick="MediaPlayer.showSubtitleMenu();"><i class="md-icon">closed_caption</i></button>';
            html += '<button is="paper-icon-button-light" class="mediaButton videoQualityButton autoSize" onclick="MediaPlayer.showQualityFlyout();"><i class="md-icon">settings</i></button>';
            html += '<button is="paper-icon-button-light" class="mediaButton autoSize" onclick="MediaPlayer.stop();"><i class="md-icon">close</i></button>';

            html += '</div>'; // videoAdvancedControls
            html += '</div>'; // videoTopControls

            // Create controls
            html += '<div class="videoControls ' + hiddenOnIdleClass + '">';

            html += '<div class="nowPlayingInfo hide">';
            html += '<div class="nowPlayingImage"></div>';
            html += '<div class="nowPlayingTabs"></div>';
            html += '</div>'; // nowPlayingInfo

            html += '<div class="guide hide">';
            html += '</div>'; // guide

            html += '<div class="videoControlButtons">';
            html += '<button is="paper-icon-button-light" class="previousTrackButton mediaButton videoTrackControl hide autoSize" onclick="MediaPlayer.previousTrack();"><i class="md-icon">skip_previous</i></button>';
            html += '<button is="paper-icon-button-light" id="video-playButton" class="mediaButton unpauseButton autoSize" onclick="MediaPlayer.unpause();"><i class="md-icon">play_arrow</i></button>';
            html += '<button is="paper-icon-button-light" id="video-pauseButton" class="mediaButton pauseButton autoSize" onclick="MediaPlayer.pause();"><i class="md-icon">pause</i></button>';
            html += '<button is="paper-icon-button-light" class="nextTrackButton mediaButton videoTrackControl hide autoSize" onclick="MediaPlayer.nextTrack();"><i class="md-icon">skip_next</i></button>';

            html += '<div class="sliderContainer videoPositionSliderContainer" style="display:inline-flex;margin-right:2em;">';
            html += '<input type="range" is="emby-slider" pin step=".1" min="0" max="100" value="0" class="videoPositionSlider" />';
            html += '</div>'; // guide

            html += '<div class="currentTime">--:--</div>';

            html += '<button is="paper-icon-button-light" class="muteButton mediaButton autoSize" onclick="MediaPlayer.mute();"><i class="md-icon">volume_up</i></button>';
            html += '<button is="paper-icon-button-light" class="unmuteButton mediaButton autoSize" onclick="MediaPlayer.unMute();"><i class="md-icon">volume_off</i></button>';

            html += '<div class="sliderContainer volumeSliderContainer" style="width:100px;vertical-align:middle;;margin-right:2em;display:inline-flex;">';
            html += '<input type="range" is="emby-slider" pin step="1" min="0" max="100" value="0" class="videoVolumeSlider"/>';
            html += '</div>'; // guide

            html += '<button is="paper-icon-button-light" class="mediaButton castButton autoSize" onclick="MediaController.showPlayerSelection(this, false);"><i class="md-icon">cast</i></button>';
            html += '<button is="paper-icon-button-light" class="mediaButton fullscreenButton autoSize" onclick="MediaPlayer.toggleFullscreen();" id="video-fullscreenButton"><i class="md-icon">fullscreen</i></button>';
            html += '<button is="paper-icon-button-light" class="mediaButton infoButton autoSize" onclick="MediaPlayer.toggleInfo();"><i class="md-icon">info</i></button>';

            html += '</div>';

            html += '</div>'; // videoControls

            html += '</div>'; // videoPlayer

            var div = document.createElement('div');
            div.innerHTML = html;
            document.body.appendChild(div);
        }

        var initComplete;

        function initVideoElements() {

            if (initComplete) {
                return;
            }

            initComplete = true;
            ensureVideoPlayerElements();

            var parent = document.querySelector("#videoPlayer");

            muteButton = parent.querySelector('.muteButton');
            unmuteButton = parent.querySelector('.unmuteButton');
            currentTimeElement = parent.querySelector('.currentTime');

            positionSlider = parent.querySelector(".videoPositionSlider", parent);
            positionSlider.addEventListener('change', onPositionSliderChange);

            positionSlider.getBubbleText = function (value) {

                var seekableDuration = getSeekableDuration();
                if (!self.currentMediaSource || !seekableDuration) {
                    return '--:--';
                }

                var ticks = seekableDuration;
                ticks /= 100;
                ticks *= value;

                return datetime.getDisplayRunningTime(ticks);
            };

            volumeSlider = parent.querySelector('.videoVolumeSlider');
            volumeSliderContainer = parent.querySelector('.volumeSliderContainer');
            volumeSlider.addEventListener('change', function () {

                var vol = this.value;

                updateVolumeButtons(vol);
                self.setVolume(vol);
            });
        }

        var idleHandlerTimeout;
        function idleHandler() {

            if (idleHandlerTimeout) {
                window.clearTimeout(idleHandlerTimeout);
            }

            if (idleState == true) {
                setClass(document.querySelectorAll('.hiddenOnIdle'), 'remove', 'inactive');
                document.querySelector('#videoPlayer').classList.remove('idlePlayer');
            }

            idleState = false;

            idleHandlerTimeout = window.setTimeout(function () {
                idleState = true;
                setClass(document.querySelectorAll('.hiddenOnIdle'), 'add', 'inactive');
                document.querySelector('#videoPlayer').classList.add('idlePlayer');
            }, 3500);
        }

        function updateVolumeButtons(vol) {

            if (!AppInfo.hasPhysicalVolumeButtons) {
                if (vol) {
                    muteButton.classList.remove('hide');
                    unmuteButton.classList.add('hide');
                } else {
                    muteButton.classList.add('hide');
                    unmuteButton.classList.remove('hide');
                }
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

            document.querySelector("#videoPlayer").classList.add("fullscreenVideo");
        }

        function exitFullScreenToWindow() {

            document.querySelector("#videoPlayer").classList.remove("fullscreenVideo");
        }

        function onPopState() {
            // Stop playback on browser back button nav
            window.removeEventListener("popstate", onPopState);
            self.stop();
            return;
        }

        function onFullScreenChange() {
            if (self.isFullScreen()) {
                enterFullScreen();
                idleState = true;

            } else {
                exitFullScreenToWindow();
            }
        }

        var lastMousePosition = {};
        function onMouseMove(evt) {

            if (evt.clientX == lastMousePosition.x && evt.clientY == lastMousePosition.y) {
                return;
            }
            lastMousePosition.x = evt.clientX;
            lastMousePosition.y = evt.clientY;

            idleHandler();
        }

        function bindEventsForPlayback(mediaRenderer) {

            Events.on(mediaRenderer, 'playing', onOnePlaying);
            Events.on(mediaRenderer, 'playing', onPlaying);
            Events.on(mediaRenderer, 'volumechange', onVolumeChange);
            Events.on(mediaRenderer, 'pause', onPause);
            Events.on(mediaRenderer, 'timeupdate', onTimeUpdate);
            Events.on(mediaRenderer, 'error', onError);
            Events.on(mediaRenderer, 'click', onClick);
            Events.on(mediaRenderer, 'dblclick', onDoubleClick);

            var hideElementsOnIdle = true;

            if (hideElementsOnIdle) {

                var itemVideo = document.querySelector('.itemVideo');
                if (itemVideo) {
                    //Events.on(itemVideo, 'mousemove', onMouseMove);
                    itemVideo.addEventListener('keydown', idleHandler);
                    itemVideo.addEventListener('scroll', idleHandler);
                    itemVideo.addEventListener('mousedown', idleHandler);
                    idleHandler();
                }
            }

            document.addEventListener('webkitfullscreenchange', onFullScreenChange);
            document.addEventListener('mozfullscreenchange', onFullScreenChange);
            document.addEventListener('msfullscreenchange', onFullScreenChange);
            document.addEventListener('fullscreenchange', onFullScreenChange);

            window.addEventListener("popstate", onPopState);

            if (hideElementsOnIdle) {
                document.body.addEventListener("mousemove", onMouseMove);
            }
        }

        function unbindEventsForPlayback(mediaRenderer) {

            Events.off(mediaRenderer, 'playing', onOnePlaying);
            Events.off(mediaRenderer, 'playing', onPlaying);
            Events.off(mediaRenderer, 'volumechange', onVolumeChange);

            Events.off(mediaRenderer, 'pause', onPause);
            Events.off(mediaRenderer, 'timeupdate', onTimeUpdate);
            Events.off(mediaRenderer, 'error', onError);
            Events.off(mediaRenderer, 'click', onClick);
            Events.off(mediaRenderer, 'dblclick', onDoubleClick);

            document.removeEventListener('webkitfullscreenchange', onFullScreenChange);
            document.removeEventListener('mozfullscreenchange', onFullScreenChange);
            document.removeEventListener('msfullscreenchange', onFullScreenChange);
            document.removeEventListener('fullscreenchange', onFullScreenChange);

            // Stop playback on browser back button nav
            window.removeEventListener("popstate", onPopState);

            document.body.removeEventListener("mousemove", onMouseMove);

            var itemVideo = document.querySelector('.itemVideo');
            if (itemVideo) {
                //Events.off(itemVideo, 'mousemove', onMouseMove);
                itemVideo.removeEventListener('keydown', idleHandler);
                itemVideo.removeEventListener('scroll', idleHandler);
                itemVideo.removeEventListener('mousedown', idleHandler);
            }
        }

        // Replace audio version
        self.cleanup = function (mediaRenderer) {

            if (currentTimeElement) {
                currentTimeElement.innerHTML = '--:--';
            }

            unbindEventsForPlayback(mediaRenderer);
        };

        self.playVideo = function (item, mediaSource, startPosition, callback) {

            if (browserInfo.msie) {

                if (!window.MediaSource || !mediaSource.RunTimeTicks) {
                    alert('Playback of this content is not supported in Internet Explorer. For a better experience, please try a modern browser such as Google Chrome, Firefox, Opera, or Microsoft Edge.');
                }
            }

            // TODO: remove dependency on nowplayingbar
            requirejs(['videorenderer', 'css!css/nowplayingbar.css', 'css!css/mediaplayer-video.css', 'emby-slider'], function () {

                initVideoElements();

                self.createStreamInfo('Video', item, mediaSource, startPosition).then(function (streamInfo) {

                    var isHls = streamInfo.url.toLowerCase().indexOf('.m3u8') != -1;

                    // Huge hack alert. Safari doesn't seem to like if the segments aren't available right away when playback starts
                    // This will start the transcoding process before actually feeding the video url into the player
                    // Edit: Also seeing stalls from hls.js
                    if (!mediaSource.RunTimeTicks && isHls) {

                        Dashboard.showLoadingMsg();
                        var hlsPlaylistUrl = streamInfo.url.replace('master.m3u8', 'live.m3u8');
                        ApiClient.ajax({

                            type: 'GET',
                            url: hlsPlaylistUrl

                        }).then(function () {
                            Dashboard.hideLoadingMsg();
                            streamInfo.url = hlsPlaylistUrl;

                            // add a delay to continue building up the buffer. without this we see failures in safari mobile
                            setTimeout(function () {
                                self.playVideoInternal(item, mediaSource, startPosition, streamInfo, callback);
                            }, 2000);

                        }, function () {
                            Dashboard.hideLoadingMsg();
                        });

                    } else {
                        self.playVideoInternal(item, mediaSource, startPosition, streamInfo, callback);
                    }
                });
            });
        };

        function fadeOut(elem) {

            if (elem.classList.contains('hide')) {
                return;
            }

            var onfinish = function () {
                elem.classList.add('hide');
            };

            //if (!browserInfo.animate) {
            onfinish();
            return;
            //}

            requestAnimationFrame(function () {
                var keyframes = [
                  { opacity: '1', offset: 0 },
                  { opacity: '0', offset: 1 }];
                var timing = { duration: 600, iterations: 1, easing: 'ease-out' };
                elem.animate(keyframes, timing).onfinish = onfinish;
            });
        }

        function fadeIn(elem) {

            if (!elem.classList.contains('hide')) {
                return;
            }

            elem.classList.remove('hide');

            if (!browserInfo.animate || browserInfo.slow) {
                return;
            }

            requestAnimationFrame(function () {

                var keyframes = [
                    { transform: 'scale3d(.2, .2, .2)  ', opacity: '.6', offset: 0 },
                  { transform: 'none', opacity: '1', offset: 1 }
                ];

                var timing = { duration: 200, iterations: 1, easing: 'ease-out' };
                elem.animate(keyframes, timing);
            });
        }
        self.playVideoInternal = function (item, mediaSource, startPosition, streamInfo, callback) {

            self.startTimeTicksOffset = streamInfo.startTimeTicksOffset;

            var mediaStreams = mediaSource.MediaStreams || [];
            var subtitleStreams = mediaStreams.filter(function (s) {
                return s.Type == 'Subtitle';
            });

            // Create video player
            var mediaPlayerContainer = document.querySelector('#videoPlayer');
            fadeIn(mediaPlayerContainer);
            var videoControls = mediaPlayerContainer.querySelector('.videoControls');

            //show stop button
            document.querySelector('#video-playButton').classList.add('hide');
            document.querySelector('#video-pauseButton').classList.remove('hide');

            document.querySelector('.videoTrackControl').classList.add('hide');
            document.querySelector('.videoQualityButton').classList.remove('hide');

            if (mediaStreams.filter(function (s) {
                return s.Type == "Audio";
            }).length) {
                document.querySelector('.videoAudioButton').classList.remove('hide');
            } else {
                document.querySelector('.videoAudioButton').classList.add('hide');
            }

            if (subtitleStreams.length) {
                document.querySelector('.videoSubtitleButton').classList.remove('hide');
            } else {
                document.querySelector('.videoSubtitleButton').classList.add('hide');
            }

            var mediaRenderer = new VideoRenderer({

                poster: self.getPosterUrl(item)
            });

            var requiresNativeControls = !mediaRenderer.enableCustomVideoControls();

            if (requiresNativeControls || AppInfo.isNativeApp) {
                videoControls.querySelector('#video-fullscreenButton').classList.add('hide');
            } else {
                videoControls.querySelector('#video-fullscreenButton').classList.remove('hide');
            }

            if (AppInfo.hasPhysicalVolumeButtons) {
                volumeSliderContainer.classList.add('hide');
                videoControls.querySelector('.muteButton').classList.add('hide');
                videoControls.querySelector('.unmuteButton').classList.add('hide');
            } else {
                volumeSliderContainer.classList.remove('hide');
                videoControls.querySelector('.muteButton').classList.remove('hide');
                videoControls.querySelector('.unmuteButton').classList.remove('hide');
            }

            if (requiresNativeControls) {
                videoControls.classList.add('hide');
            } else {
                videoControls.classList.remove('hide');
            }

            initialVolume = self.getSavedVolume();

            mediaRenderer.volume(initialVolume);

            volumeSlider.value = initialVolume * 100;
            updateVolumeButtons(initialVolume);

            bindEventsForPlayback(mediaRenderer);

            self.currentSubtitleStreamIndex = mediaSource.DefaultSubtitleStreamIndex;

            document.body.classList.add('bodyWithPopupOpen');

            self.currentMediaRenderer = mediaRenderer;
            self.currentDurationTicks = self.currentMediaSource.RunTimeTicks;

            self.updateNowPlayingInfo(item);

            mediaRenderer.init().then(function () {

                self.onBeforePlaybackStart(mediaRenderer, item, mediaSource);

                self.setSrcIntoRenderer(mediaRenderer, streamInfo, item, self.currentMediaSource);
                self.streamInfo = streamInfo;

                if (callback) {
                    callback();
                }
            });
        };

        function onOnePlaying() {

            Events.off(this, 'playing', onOnePlaying);

            // For some reason this is firing at the start, so don't bind until playback has begun
            Events.on(this, 'ended', self.onPlaybackStopped);
            Events.on(this, 'ended', self.playNextAfterEnded);

            self.onPlaybackStart(this, self.currentItem, self.currentMediaSource);
        }

        function onPlaying() {

            var videoControls = document.querySelector('#videoPlayer .videoControls');
            var videoElement = document.querySelector('#videoPlayer #videoElement');

            videoControls.querySelector('#video-playButton').classList.add('hide');
            videoControls.querySelector('#video-pauseButton').classList.remove('hide');

            var buttonToAnimate = videoElement.querySelector('#play');
            buttonToAnimate.classList.remove('hide');
            buttonToAnimate.classList.add('fadeOut');

            setTimeout(function () {
                buttonToAnimate.classList.add('hide');
                buttonToAnimate.classList.remove('fadeOut');
            }, 300);
        }

        function onVolumeChange() {

            updateVolumeButtons(this.volume());
        }

        function onPause() {

            var videoControls = document.querySelector('#videoPlayer .videoControls');
            var videoElement = document.querySelector('#videoPlayer #videoElement');

            videoControls.querySelector('#video-playButton').classList.remove('hide');
            videoControls.querySelector('#video-pauseButton').classList.add('hide');

            var buttonToAnimate = videoElement.querySelector('#pause');
            buttonToAnimate.classList.remove('hide');
            buttonToAnimate.classList.add('fadeOut');

            setTimeout(function () {
                buttonToAnimate.classList.add('hide');
                buttonToAnimate.classList.remove('fadeOut');
            }, 300);
        }

        function onTimeUpdate() {
            if (!positionSlider.dragging) {

                self.setCurrentTime(self.getCurrentTicks(this), positionSlider, currentTimeElement);
            }
        }

        function onError() {
            var errorMsg = Globalize.translate('MessageErrorPlayingVideo');

            var item = self.currentItem;
            if (item && item.Type == "TvChannel") {
                errorMsg += '<p>';
                errorMsg += Globalize.translate('MessageEnsureOpenTuner');
                errorMsg += '</p>';
            }

            Dashboard.alert({
                title: Globalize.translate('HeaderVideoError'),
                message: errorMsg
            });

            var mediaRenderer = self.currentMediaRenderer;
            if (mediaRenderer) {
                self.onPlaybackStopped.call(mediaRenderer);
            }
            self.nextTrack();
        }

        function onClick() {

            if (!browserInfo.mobile) {
                if (this.paused()) {
                    self.unpause();
                } else {
                    self.pause();
                }
            }
        }

        function onDoubleClick() {
            if (!browserInfo.mobile) {
                self.toggleFullscreen();
            }
        }

        self.updatePlaylistUi = function () {

            if (!initComplete) {
                return;
            }

            var index = self.currentPlaylistIndex(null);
            var length = self.playlist.length;

            var requiresNativeControls = false;

            if (self.currentMediaRenderer && self.currentMediaRenderer.enableCustomVideoControls) {
                requiresNativeControls = self.currentMediaRenderer.enableCustomVideoControls();
            }

            if (length < 2) {
                document.querySelector('.videoTrackControl').classList.add('hide');
                return;
            }

            var controls = requiresNativeControls ? '.videoAdvancedControls' : '.videoControls';
            controls = document.querySelector(controls);

            var previousTrackButton = controls.getElementsByClassName('previousTrackButton')[0];
            var nextTrackButton = controls.getElementsByClassName('nextTrackButton')[0];

            if (index === 0) {
                previousTrackButton.setAttribute('disabled', 'disabled');
            } else {
                previousTrackButton.removeAttribute('disabled');
            }

            if ((index + 1) >= length) {
                nextTrackButton.setAttribute('disabled', 'disabled');
            } else {
                nextTrackButton.removeAttribute('disabled');
            }

            previousTrackButton.classList.remove('hide');
            nextTrackButton.classList.remove('hide');
        };
    }

    createVideoPlayer(MediaPlayer);

});