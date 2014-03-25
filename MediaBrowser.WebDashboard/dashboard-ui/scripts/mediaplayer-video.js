(function () {
    videoPlayer = function (mediaPlayer, item, mediaSource, startPosition, user) {
        if (mediaPlayer == null) {
            throw new Error("mediaPlayer cannot be null");
        }

        if (item == null) {
            throw new Error("item cannot be null");
        }

        if (user == null) {
            throw new Error("user cannot be null");
        }

        var self = mediaPlayer;

        var currentItem;
        var currentMediaSource;
        var timeout;
        var video;
        var initialVolume;
        var fullscreenExited = false;
        var idleState = true;
        var remoteFullscreen = false;

        self.initVideoPlayer = function () {
            video = playVideo(item, mediaSource, startPosition, user);
            return video;
        };

        self.remoteFullscreen = function () {

            var videoControls = $("#videoControls");
            
            if (remoteFullscreen) {
                exitFullScreenToWindow();
                videoControls.removeClass("inactive");
            } else {
                enterFullScreen();
                videoControls.addClass("inactive");
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
            $('#videoPlayer').removeClass('fullscreenVideo');
            $("#videoControls").removeClass("inactive")
            $("video").remove();
            $("html").css("cursor", "default");
            $(".ui-loader").hide();
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

        self.showSubtitleMenu = function () {

            var flyout = $('#video-subtitleFlyout');

            if (!flyout.is(':visible')) {

                flyout.html(getSubtitleTracksHtml()).trigger('create').scrollTop(0);
                toggleFlyout(flyout, '#video-subtitleButton');

            } else {
                toggleFlyout(flyout, '#video-subtitleButton');
            }
        };

        self.showQualityFlyout = function () {

            var flyout = $('#video-qualityFlyout');

            if (!flyout.is(':visible')) {
                flyout.html(getQualityFlyoutHtml()).scrollTop(0);
            }

            toggleFlyout(flyout, '#video-qualityButton');
        };

        self.showChaptersFlyout = function () {

            var flyout = $('#video-chaptersFlyout');

            if (!flyout.is(':visible')) {
                flyout.html(getChaptersFlyoutHtml()).scrollTop(0);
            }

            toggleFlyout(flyout, '#video-chaptersButton');
        };

        self.showAudioTracksFlyout = function () {

            var flyout = $('#video-audioTracksFlyout');

            if (!flyout.is(':visible')) {

                flyout.html(getAudioTracksHtml()).trigger('create').scrollTop(0);
                toggleFlyout(flyout, '#video-audioTracksButton');
            } else {
                toggleFlyout(flyout, '#video-audioTracksButton');
            }
        };

        $(document).on('webkitfullscreenchange mozfullscreenchange fullscreenchange', function (e) {

            var videoControls = $('#videoControls');

            $('.itemVideo').off('mousemove keydown scroll', idleHandler);

            if (self.isFullScreen()) {
                enterFullScreen();
                idleState = true;
                $('.itemVideo').on('mousemove keydown scroll', idleHandler).trigger('mousemove');
            } else {
                videoControls.removeClass("active inactive");
                exitFullScreenToWindow();
            }
        });

        $(function () {
            $('#video-chaptersFlyout').on('click', '.mediaFlyoutOption', function () {

                var ticks = parseInt(this.getAttribute('data-positionticks'));

                self.changeStream(ticks);

                hideFlyout($('#video-chaptersFlyout'));
            });

            $('#video-audioTracksFlyout').on('click', '.mediaFlyoutOption', function () {

                if (!$(this).hasClass('selectedMediaFlyoutOption')) {
                    var index = parseInt(this.getAttribute('data-index'));

                    self.changeStream(self.getCurrentTicks(), { AudioStreamIndex: index });
                }

                hideFlyout($('#video-audioTracksFlyout'));
            });

            $('#video-subtitleFlyout').on('click', '.mediaFlyoutOption', function () {

                if (!$(this).hasClass('selectedMediaFlyoutOption')) {
                    var index = parseInt(this.getAttribute('data-index'));

                    self.changeStream(self.getCurrentTicks(), { SubtitleStreamIndex: index });
                }

                hideFlyout($('#video-subtitleFlyout'));
            });

            $('#video-qualityFlyout').on('click', '.mediaFlyoutOption', function () {

                if (!$(this).hasClass('selectedMediaFlyoutOption')) {

                    var maxWidth = parseInt(this.getAttribute('data-maxwidth'));
                    var bitrate = parseInt(this.getAttribute('data-bitrate'));

                    localStorage.setItem('preferredVideoBitrate', bitrate);

                    self.changeStream(self.getCurrentTicks(), {

                        MaxWidth: maxWidth,
                        Bitrate: bitrate
                    });
                }

                hideFlyout($('#video-qualityFlyout'));
            });

            $("body").on("mousemove", "#videoPlayer.fullscreenVideo #itemVideo", function () {

                idleHandler(this);

            });
        });

        function idleHandler() {
            var video = $(".itemVideo");
            var videoControls = $("#videoControls");

            if (timeout) {
                window.clearTimeout(timeout);
            }

            if (idleState == true) {
                video.removeClass("cursor-inactive").addClass("cursor-active");
                videoControls.removeClass("inactive").addClass("active");
            }

            idleState = false;

            timeout = window.setTimeout(function () {
                idleState = true;
                video.removeClass("cursor-active").addClass("cursor-inactive");
                videoControls.removeClass("active").addClass("inactive");
            }, 4000);
        };

        function requestFullScreen(element) {

            // Supports most browsers and their versions.
            var requestMethod = element.requestFullscreen || element.webkitRequestFullscreen || element.webkitRequestFullScreen || element.mozRequestFullScreen;

            if (requestMethod) { // Native full screen.
                requestMethod.call(element);
            } else {
                enterFullScreen();
            }

        };

        function changeHandler(event) {

            document.addEventListener(event, function () {
                fullscreenExited = self.isFullScreen() == false;
            });

        };

        function enterFullScreen() {

            var player = $("#videoPlayer");

            player.addClass("fullscreenVideo");

            remoteFullscreen = true;

        };

        function exitFullScreenToWindow() {

            var player = $("#videoPlayer");

            player.removeClass("fullscreenVideo");

            remoteFullscreen = false;

        };

        function toggleFlyout(flyout, button) {

            $(document.body).off("mousedown.mediaflyout").on("mousedown.mediaflyout", function (e) {

                var elem = $(e.target);

                var flyoutId = flyout[0].id;
                var safeItems = button + ',#' + flyoutId;

                if (!elem.is(safeItems) && !elem.parents(safeItems).length) {

                    hideFlyout(flyout);
                }

            });

            var visible = $(flyout).is(":visible");

            if (!visible) {

                flyout.slideDown();

            } else {

                $(button).blur();

                hideFlyout(flyout);
            }
        };

        function hideFlyout(flyout) {

            flyout.slideUp().empty();

            $(document.body).off("mousedown.hidesearchhints");
        };

        function getChaptersFlyoutHtml() {

            var html = '';

            var currentTicks = self.getCurrentTicks();

            var chapters = currentItem.Chapters || [];

            for (var i = 0, length = chapters.length; i < length; i++) {

                var chapter = chapters[i];

                var isSelected = false;

                if (currentTicks >= chapter.StartPositionTicks) {

                    var nextChapter = chapters[i + 1];

                    isSelected = !nextChapter || currentTicks < nextChapter.StartPositionTicks;
                }

                if (isSelected) {
                    html += '<div data-positionticks="' + chapter.StartPositionTicks + '" class="mediaFlyoutOption selectedMediaFlyoutOption">';
                } else {
                    html += '<div data-positionticks="' + chapter.StartPositionTicks + '" class="mediaFlyoutOption">';
                }

                var imgUrl;

                if (chapter.ImageTag) {

                    imgUrl = ApiClient.getImageUrl(currentItem.Id, {
                        maxwidth: 200,
                        tag: chapter.ImageTag,
                        type: "Chapter",
                        index: i
                    });

                } else {
                    imgUrl = "css/images/media/chapterflyout.png";
                }

                html += '<img class="mediaFlyoutOptionImage" src="' + imgUrl + '" />';

                html += '<div class="mediaFlyoutOptionContent">';

                var name = chapter.Name || "Chapter " + (i + 1);

                html += '<div class="mediaFlyoutOptionName">' + name + '</div>';
                html += '<div class="mediaFlyoutOptionSecondaryText">' + Dashboard.getDisplayTime(chapter.StartPositionTicks) + '</div>';

                html += '</div>';

                html += "</div>";
            }

            return html;
        };

        function getAudioTracksHtml() {
            
            var streams = currentMediaSource.MediaStreams.filter(function (currentStream) {
                return currentStream.Type == "Audio";
            });

            var currentIndex = getParameterByName('AudioStreamIndex', video.currentSrc);

            var html = '';

            for (var i = 0, length = streams.length; i < length; i++) {

                var stream = streams[i];

                if (stream.Index == currentIndex) {
                    html += '<div data-index="' + stream.Index + '" class="mediaFlyoutOption selectedMediaFlyoutOption">';
                } else {
                    html += '<div data-index="' + stream.Index + '" class="mediaFlyoutOption">';
                }

                html += '<img class="mediaFlyoutOptionImage" src="css/images/media/audioflyout.png" />';

                html += '<div class="mediaFlyoutOptionContent">';

                html += '<div class="mediaFlyoutOptionName">' + (stream.Language || 'Unknown language') + '</div>';

                var options = [];

                if (stream.Codec) {
                    options.push(stream.Codec);
                }
                if (stream.Profile) {
                    options.push(stream.Profile);
                }

                if (stream.BitRate) {
                    options.push((Math.floor(stream.BitRate / 1000)) + ' kbps');
                }

                if (stream.Channels) {
                    options.push(stream.Channels + ' ch');
                }

                if (options.length) {
                    html += '<div class="mediaFlyoutOptionSecondaryText">' + options.join('&nbsp;&#149;&nbsp;') + '</div>';
                }

                options = [];

                if (stream.IsDefault) {
                    options.push('Default');
                }
                if (stream.IsForced) {
                    options.push('Forced');
                }

                if (options.length) {
                    html += '<div class="mediaFlyoutOptionSecondaryText">' + options.join('&nbsp;&#149;&nbsp;') + '</div>';
                }

                html += "</div>";

                html += "</div>";
            }

            html += '<div style="padding: 5px;"><a data-role="button" data-icon="gear" href="usersettings.html?userid=' + Dashboard.getCurrentUserId() + '" data-mini="true" data-theme="a">Preferences</a></div>';

            return html;
        };

        function getSubtitleTracksHtml() {

            var streams = currentMediaSource.MediaStreams.filter(function (currentStream) {
                return currentStream.Type == "Subtitle";
            });

            var currentIndex = getParameterByName('SubtitleStreamIndex', video.currentSrc) || -1;

            var html = '';

            streams.unshift({
                Index: -1,
                Language: "Off"
            });

            for (var i = 0, length = streams.length; i < length; i++) {

                var stream = streams[i];

                if (stream.Index == currentIndex) {
                    html += '<div data-index="' + stream.Index + '" class="mediaFlyoutOption selectedMediaFlyoutOption">';
                } else {
                    html += '<div data-index="' + stream.Index + '" class="mediaFlyoutOption">';
                }

                if (stream.Index != -1) {
                    html += '<img class="mediaFlyoutOptionImage" src="css/images/media/subtitleflyout.png" />';
                } else {
                    html += '<div class="mediaFlyoutOptionImage"></div>';
                }

                html += '<div class="mediaFlyoutOptionContent">';

                var options = [];

                if (stream.Language == "Off") {
                    options.push('&nbsp;');
                }

                html += '<div class="mediaFlyoutOptionName">' + (stream.Language || 'Unknown language') + '</div>';

                if (stream.Codec) {
                    options.push(stream.Codec);
                }

                if (options.length) {
                    html += '<div class="mediaFlyoutOptionSecondaryText">' + options.join('&nbsp;&#149;&nbsp;') + '</div>';
                }

                options = [];

                if (stream.IsDefault) {
                    options.push('Default');
                }
                if (stream.IsForced) {
                    options.push('Forced');
                }
                if (stream.IsExternal) {
                    options.push('External');
                }

                if (options.length) {
                    html += '<div class="mediaFlyoutOptionSecondaryText">' + options.join('&nbsp;&#149;&nbsp;') + '</div>';
                }

                html += "</div>";

                html += "</div>";
            }

            html += '<div style="padding: 5px;"><a data-role="button" data-icon="gear" href="usersettings.html?userid=' + Dashboard.getCurrentUserId() + '" data-mini="true" data-theme="a">Preferences</a></div>';

            return html;
        };

        function getQualityFlyoutHtml() {

            var html = '';

            var currentSrc = video.currentSrc.toLowerCase();
            var isStatic = currentSrc.indexOf('static=true') != -1;

            var transcodingExtension = self.getTranscodingExtension();

            var currentAudioStreamIndex = getParameterByName('AudioStreamIndex', video.currentSrc);

            var options = getVideoQualityOptions(currentMediaSource.MediaStreams, currentAudioStreamIndex, transcodingExtension);

            if (isStatic) {
                options[0].name = "Direct";
            }

            for (var i = 0, length = options.length; i < length; i++) {

                var option = options[i];

                var cssClass = "mediaFlyoutOption";

                if (option.selected) {
                    cssClass += " selectedMediaFlyoutOption";
                }

                html += '<div data-maxwidth="' + option.maxWidth + '" data-bitrate="' + option.bitrate + '" class="' + cssClass + '">';

                html += '<div class="mediaFlyoutOptionContent">';

                html += '<div class="mediaFlyoutOptionName" style="padding:.5em;">' + option.name + '</div>';

                html += "</div>";

                html += "</div>";
            }

            return html;
        };

        function getInitialSubtitleStreamIndex(mediaStreams, user) {

            var i, length, mediaStream;

            // Find the first forced subtitle stream
            for (i = 0, length = mediaStreams.length; i < length; i++) {
                mediaStream = mediaStreams[i];

                if (mediaStream.Type == "Subtitle" && mediaStream.IsForced) {
                    return mediaStream.Index;
                }

            }

            // If none then look at user configuration
            if (user.Configuration.SubtitleLanguagePreference) {

                for (i = 0, length = mediaStreams.length; i < length; i++) {
                    mediaStream = mediaStreams[i];

                    if (mediaStream.Type == "Subtitle" && mediaStream.Language == user.Configuration.SubtitleLanguagePreference) {

                        if (user.Configuration.UseForcedSubtitlesOnly) {

                            if (mediaStream.IsForced) {
                                return mediaStream.Index;
                            }

                        } else {
                            return mediaStream.Index;
                        }
                    }

                }
            }

            return null;
        };

        function getInitialAudioStreamIndex(mediaStreams, user) {

            // Find all audio streams with at least one channel
            var audioStreams = mediaStreams.filter(function (stream) {
                return stream.Type == "Audio" && stream.Channels;
            });

            if (user.Configuration.AudioLanguagePreference) {

                for (var i = 0, length = audioStreams.length; i < length; i++) {
                    var mediaStream = audioStreams[i];

                    if (mediaStream.Language == user.Configuration.AudioLanguagePreference) {
                        return mediaStream.Index;
                    }

                }
            }

            // Just use the first audio stream
            return audioStreams.length ? audioStreams[0].Index : null;
        };
        
        function getVideoQualityOptions(mediaStreams) {

            var videoStream = mediaStreams.filter(function (stream) {
                return stream.Type == "Video";
            })[0];

            var bitrateSetting = self.getBitrateSetting();

            var maxAllowedWidth = Math.max(screen.height, screen.width);

            var options = [];

            // We have media info
            if (videoStream && videoStream.Width) {

                maxAllowedWidth = videoStream.Width;
            }

            // Some 1080- videos are reported as 1912?
            if (maxAllowedWidth >= 1910) {
                options.push({ name: '1080p - 20Mbps', maxWidth: 1920, bitrate: 20000000 });
                options.push({ name: '1080p - 15Mbps', maxWidth: 1920, bitrate: 15000000 });
                options.push({ name: '1080p - 10Mbps', maxWidth: 1920, bitrate: 10000000 });
                options.push({ name: '1080p - 8Mbps', maxWidth: 1920, bitrate: 8000000 });
                options.push({ name: '1080p - 6Mbps', maxWidth: 1920, bitrate: 6000000 });
                options.push({ name: '1080p - 5Mbps', maxWidth: 1920, bitrate: 5000000 });
            }
            else if (maxAllowedWidth >= 1270) {
                options.push({ name: '720p - 10Mbps', maxWidth: 1280, bitrate: 10000000 });
                options.push({ name: '720p - 8Mbps', maxWidth: 1280, bitrate: 8000000 });
                options.push({ name: '720p - 6Mbps', maxWidth: 1280, bitrate: 6000000 });
                options.push({ name: '720p - 5Mbps', maxWidth: 1280, bitrate: 5000000 });
            }
            else if (maxAllowedWidth >= 470) {
                options.push({ name: '480p - 4Mbps', maxWidth: 720, bitrate: 4000000 });
                options.push({ name: '480p - 3.5Mbps', maxWidth: 720, bitrate: 3500000 });
                options.push({ name: '480p - 3Mbps', maxWidth: 720, bitrate: 3000000 });
                options.push({ name: '480p - 2.5Mbps', maxWidth: 720, bitrate: 2500000 });
                options.push({ name: '480p - 2Mbps', maxWidth: 720, bitrate: 2000000 });
                options.push({ name: '480p - 1.5Mbps', maxWidth: 720, bitrate: 1500000 });
            }

            if (maxAllowedWidth >= 1270) {
                options.push({ name: '720p - 4Mbps', maxWidth: 1280, bitrate: 4000000 });
                options.push({ name: '720p - 3Mbps', maxWidth: 1280, bitrate: 3000000 });
                options.push({ name: '720p - 2Mbps', maxWidth: 1280, bitrate: 2000000 });
                options.push({ name: '720p - 1.5Mbps', maxWidth: 1280, bitrate: 1500000 });
            }

            options.push({ name: '480p - 1.0Mbps', maxWidth: 720, bitrate: 1000000 });
            options.push({ name: '480p - 720 kbps', maxWidth: 720, bitrate: 700000 });
            options.push({ name: '480p - 420 kbps', maxWidth: 720, bitrate: 420000 });
            options.push({ name: '360p', maxWidth: 640, bitrate: 400000 });
            options.push({ name: '240p', maxWidth: 426, bitrate: 320000 });

            var i, length, option;
            var selectedIndex = -1;
            for (i = 0, length = options.length; i < length; i++) {

                option = options[i];

                if (selectedIndex == -1 && option.bitrate <= bitrateSetting) {
                    selectedIndex = i;
                }
            }

            if (selectedIndex == -1) {

                selectedIndex = options.length - 1;
            }

            options[selectedIndex].selected = true;

            return options;
        };

        function playVideo(item, mediaSource, startPosition, user) {

            var mediaStreams = mediaSource.MediaStreams || [];

            var baseParams = {
                audioChannels: 2,
                StartTimeTicks: startPosition || 0,
                SubtitleStreamIndex: getInitialSubtitleStreamIndex(mediaStreams, user),
                AudioStreamIndex: getInitialAudioStreamIndex(mediaStreams, user),
                deviceId: ApiClient.deviceId(),
                Static: false,
                mediaSourceId: mediaSource.Id
            };

            var mp4Quality = getVideoQualityOptions(mediaStreams).filter(function (opt) {
                return opt.selected;
            })[0];
            mp4Quality = $.extend(mp4Quality, self.getFinalVideoParams(mediaSource, mp4Quality.maxWidth, mp4Quality.bitrate, baseParams.AudioStreamIndex, baseParams.SubtitleStreamIndex, '.mp4'));

            var webmQuality = getVideoQualityOptions(mediaStreams).filter(function (opt) {
                return opt.selected;
            })[0];
            webmQuality = $.extend(webmQuality, self.getFinalVideoParams(mediaSource, webmQuality.maxWidth, webmQuality.bitrate, baseParams.AudioStreamIndex, baseParams.SubtitleStreamIndex, '.webm'));

            var m3U8Quality = getVideoQualityOptions(mediaStreams).filter(function (opt) {
                return opt.selected;
            })[0];
            m3U8Quality = $.extend(m3U8Quality, self.getFinalVideoParams(mediaSource, mp4Quality.maxWidth, mp4Quality.bitrate, baseParams.AudioStreamIndex, baseParams.SubtitleStreamIndex, '.mp4'));

            // Webm must be ahead of mp4 due to the issue of mp4 playing too fast in chrome
            var prioritizeWebmOverH264 = $.browser.chrome || $.browser.msie;

            var isStatic = mp4Quality.isStatic;

            self.startTimeTicksOffset = isStatic ? 0 : startPosition || 0;

            var seekParam = isStatic && startPosition ? '#t=' + (startPosition / 10000000) : '';

            var mp4VideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.mp4', $.extend({}, baseParams, {
                profile: 'baseline',
                level: 3,
                Static: isStatic,
                maxWidth: mp4Quality.maxWidth,
                videoBitrate: mp4Quality.videoBitrate,
                audioBitrate: mp4Quality.audioBitrate,
                VideoCodec: mp4Quality.videoCodec,
                AudioCodec: mp4Quality.audioCodec

            })) + seekParam;

            var webmVideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.webm', $.extend({}, baseParams, {

                VideoCodec: 'vpx',
                AudioCodec: 'Vorbis',
                maxWidth: webmQuality.maxWidth,
                videoBitrate: webmQuality.videoBitrate,
                audioBitrate: webmQuality.audioBitrate

            })) + seekParam;

            var hlsVideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.m3u8', $.extend({}, baseParams, {
                profile: 'baseline',
                level: 3,
                timeStampOffsetMs: 0,
                maxWidth: m3U8Quality.maxWidth,
                videoBitrate: m3U8Quality.videoBitrate,
                audioBitrate: m3U8Quality.audioBitrate,
                VideoCodec: m3U8Quality.videoCodec,
                AudioCodec: m3U8Quality.audioCodec

            })) + seekParam;

            //======================================================================================>

            // Show loading animation
            $(".ui-loader").show();
            $("html").css("cursor", "wait");

            // Create video player
            var html = '';

            var requiresControls = $.browser.msie || $.browser.android || ($.browser.webkit && !$.browser.chrome);

            // Can't autoplay in these browsers so we need to use the full controls
            if (requiresControls) {
                html += '<video class="itemVideo" id="itemVideo" autoplay controls preload="none">';
            } else {
                html += '<video class="itemVideo" id="itemVideo" autoplay preload="none">';
            }

            if (!isStatic) {
                // HLS must be at the top for safari
                html += '<source type="application/x-mpegURL" src="' + hlsVideoUrl + '" />';
            }

            if (prioritizeWebmOverH264 && !isStatic) {

                html += '<source type="video/webm" src="' + webmVideoUrl + '" />';
            }

            html += '<source type="video/mp4" src="' + mp4VideoUrl + '" />';

            if (!prioritizeWebmOverH264 && !isStatic) {

                html += '<source type="video/webm" src="' + webmVideoUrl + '" />';
            }

            html += '</video>';

            var mediaPlayer = $("#mediaPlayer").show();
            var videoPlayer = $("#videoPlayer", mediaPlayer);
            var videoControls = $('#videoControls', mediaPlayer);

            //show stop button
            $('#video-stopButton', videoControls).show();
            $('#video-playButton', videoControls).hide();
            $('#video-pauseButton', videoControls).show();
            $('#video-playlistButton', videoControls).hide();
            $('#video-previousTrackButton', videoControls).hide();
            $('#video-nextTrackButton', videoControls).hide();
            var videoElement = $('#videoElement', mediaPlayer).prepend(html);

            $('#video-qualityButton', videoControls).show();

            if (mediaStreams.filter(function (i) {
                return i.Type == "Audio";
            }).length) {
                $('#video-audioTracksButton', videoControls).show();
            } else {
                $('#video-audioTracksButton', videoControls).hide();
            }

            if (mediaStreams.filter(function (i) {
                return i.Type == "Subtitle";
            }).length) {
                $('#video-subtitleButton', videoControls).show();
            } else {
                $('#video-subtitleButton', videoControls).hide();
            }

            if (item.Chapters && item.Chapters.length) {
                $('#video-chaptersButton', videoControls).show();
            } else {
                $('#video-chaptersButton', videoControls).hide();
            }

            if (requiresControls) {
                $('#video-fullscreenButton', videoControls).hide();
            } else {
                $('#video-fullscreenButton', videoControls).show();
            }

            var videoElement = $("video", videoElement);

            initialVolume = localStorage.getItem("volume") || 0.5;

            videoElement.each(function () {
                this.volume = initialVolume;
            });

            self.volumeSlider.val(initialVolume).slider('refresh');
            self.updateVolumeButtons(initialVolume);

            videoElement.on("volumechange", function (e) {

                var muted = this.muted;

                var vol = this.volume;

                if (!muted && this.volume > 0) {
                    localStorage.setItem("volume", vol);
                }

                this.muted = this.volume == 0;

                self.updateVolumeButtons(vol);

            }).on("play.once", function () {

                videoElement.off("play.once");

            }).on("playing.once", function () {

                self.updateCanClientSeek(this);

                videoElement.off("playing.once");

                ApiClient.reportPlaybackStart(Dashboard.getCurrentUserId(), item.Id, mediaSource.Id, true, item.MediaType);

                self.startProgressInterval(item.Id, mediaSource.Id);

            }).on("pause", function (e) {

                $('#video-playButton', videoControls).show();
                $('#video-pauseButton', videoControls).hide();
                $("#pause", videoElement).show().addClass("fadeOut");
                setTimeout(function () {
                    $("#pause", videoElement).hide().removeClass("fadeOut");
                }, 300);

            }).on("playing", function (e) {

                $('#video-playButton', videoControls).hide();
                $('#video-pauseButton', videoControls).show();
                $("#play", videoElement).show().addClass("fadeOut");
                setTimeout(function () {
                    $("#play", videoElement).hide().removeClass("fadeOut");
                }, 300);

            }).on("timeupdate", function () {

                if (!self.isPositionSliderActive) {

                    self.setCurrentTime(self.getCurrentTicks(this), item, true);
                }

            }).on("error", function () {

                $("html").css("cursor", "default");
                $(".ui-loader").hide();
                self.resetEnhancements();

                var errorCode = this.error ? this.error.code : '';
                console.log('Html5 Video error code: ' + errorCode);

                var errorMsg = 'There was an error playing the video.';

                if (item.Type == "TvChannel") {
                    errorMsg += " Please ensure there is an open tuner availalble.";
                }

                Dashboard.alert({
                    title: 'Video Error',
                    message: errorMsg
                });

            }).on("click", function (e) {

                if (this.paused) {
                    self.unpause();
                } else {
                    self.pause();
                }

            }).on("dblclick", function () {

                self.toggleFullscreen();

            }).on("seeking", function (e) {

                $("html").css("cursor", "wait");

            }).on("seeked", function (e) {

                $("html").css("cursor", "default");

            }).on("loadstart", function () {

                $("html").css("cursor", "progress");

            }).on("canplay", function () {

                $(".ui-loader").hide();
                $("html").css("cursor", "default");

            }).on("ended.playbackstopped", self.onPlaybackStopped)
                .on('ended.playnext', self.playNextAfterEnded);

            // Stop playback on browser back button nav
            $(window).on("popstate", function () {
                self.stop();
                return;
            });

            $(".mediaFlyoutContainer").on("click", "a", function (e) {
                if (confirm("This option will close the video player. Proceed?")) {
                    self.stop();
                } else {
                    e.preventDefault();
                }
            });

            changeHandler("fullscreenchange");
            changeHandler("mozfullscreenchange");
            changeHandler("webkitfullscreenchange");
            changeHandler("msfullscreenchange");

            $(document).on("keyup.enhancePlayer", function (e) {
                if (fullscreenExited) {
                    videoPlayer.removeClass("fullscreenVideo");
                    fullscreenExited = false;
                    return;
                }

                if (e.keyCode == 27) {
                    self.stop();
                    $(this).unbind("keyup.enhancePlayer");
                }
            });

            mediaPlayer.trigger("create");

            fullscreenExited = false;

            currentItem = item;
            currentMediaSource = mediaSource;

            return videoElement[0];
        };
    };
})();