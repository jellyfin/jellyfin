(function (document, setTimeout, clearTimeout, screen, localStorage, $, setInterval, window) {

    function mediaPlayer() {

        var self = this;

        var testableAudioElement = document.createElement('audio');
        var testableVideoElement = document.createElement('video');
        var currentMediaElement;
        var currentProgressInterval;
        var positionSlider;
        var isPositionSliderActive;
        var currentTimeElement;
        var currentItem;
        var volumeSlider;
        var muteButton;
        var unmuteButton;
        var startTimeTicksOffset;
        var curentDurationTicks;
        var isStaticStream;
        var culturesPromise;
        var timeout;
        var idleState = true;

        self.playlist = [];
        var currentPlaylistIndex = 0;

        function requestFullScreen(element) {
            // Supports most browsers and their versions.
            var requestMethod = element.requestFullScreen || element.webkitRequestFullScreen || element.mozRequestFullScreen || element.msRequestFullScreen;

            if (requestMethod) { // Native full screen.
                requestMethod.call(element);
            } else {
                $('.itemVideo').addClass('fullscreenVideo');
            }
        }

        function isFullScreen() {
            return document.fullscreenEnabled || document.mozFullscreenEnabled || document.webkitIsFullScreen || document.mozFullScreen ? true : false;
        }

        $(document).on('webkitfullscreenchange mozfullscreenchange fullscreenchange', function () {
            var nowPlayingBar = ('#nowPlayingBar');
            $('.itemVideo').off('mousemove keydown scroll', idleHandler);
            if (isFullScreen()) {
                $('.itemVideo').addClass('fullscreenVideo');
                idleState = true;
                $('.itemVideo').on('mousemove keydown scroll', idleHandler).trigger('mousemove');
            } else {
                $(".mediaButton,.currentTime,.nowPlayingMediaInfo,.sliderContainer,.barBackground", nowPlayingBar).removeClass("highPosition");
                $('.itemVideo').removeClass('fullscreenVideo');
            }

        });

        $(window).on("beforeunload", function () {

            var item = currentItem;
            var media = currentMediaElement;

            // Try to report playback stopped before the browser closes
            if (item && media && currentProgressInterval) {

                var endTime = currentMediaElement.currentTime;

                var position = Math.floor(10000000 * endTime) + startTimeTicksOffset;

                ApiClient.reportPlaybackStopped(Dashboard.getCurrentUserId(), currentItem.Id, position);
            }
        });

        function replaceQueryString(url, param, value) {
            var re = new RegExp("([?|&])" + param + "=.*?(&|$)", "i");
            if (url.match(re))
                return url.replace(re, '$1' + param + "=" + value + '$2');
            else
                return url + '&' + param + "=" + value;
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

        function getCurrentTicks(mediaElement) {
            return Math.floor(10000000 * (mediaElement || currentMediaElement).currentTime) + startTimeTicksOffset;
        }

        function onPlaybackStopped() {

            $(this).off('ended.playbackstopped');

            currentTimeElement.empty();

            var endTime = this.currentTime;

            this.currentTime = 0;

            clearProgressInterval();

            var position = Math.floor(10000000 * endTime) + startTimeTicksOffset;

            ApiClient.reportPlaybackStopped(Dashboard.getCurrentUserId(), currentItem.Id, position);

            if (currentItem.MediaType == "Video") {
                ApiClient.stopActiveEncodings();
            }
        }

        function playNextAfterEnded() {

            $(this).off('ended.playnext');

            self.nextTrack();
        }

        function startProgressInterval(itemId) {

            clearProgressInterval();

            var intervalTime = ApiClient.isWebSocketOpen() ? 4000 : 20000;

            currentProgressInterval = setInterval(function () {

                if (currentMediaElement) {
                    sendProgressUpdate(itemId);
                }

            }, intervalTime);
        }

        function sendProgressUpdate(itemId) {

            ApiClient.reportPlaybackProgress(Dashboard.getCurrentUserId(), itemId, getCurrentTicks(), currentMediaElement.paused, currentMediaElement.volume == 0);
        }

        function clearProgressInterval() {

            if (currentProgressInterval) {
                clearTimeout(currentProgressInterval);
                currentProgressInterval = null;
            }
        }

        function changeStream(ticks, params) {

            var element = currentMediaElement;

            if (isStaticStream && params == null) {

                element.currentTime = ticks / (1000 * 10000);

            } else {

                params = params || {};

                var currentSrc = element.currentSrc;

                currentSrc = replaceQueryString(currentSrc, 'starttimeticks', ticks);

                if (params.AudioStreamIndex != null) {
                    currentSrc = replaceQueryString(currentSrc, 'AudioStreamIndex', params.AudioStreamIndex);
                }
                if (params.SubtitleStreamIndex != null) {
                    currentSrc = replaceQueryString(currentSrc, 'SubtitleStreamIndex', params.SubtitleStreamIndex);
                }
                if (params.MaxWidth != null) {
                    currentSrc = replaceQueryString(currentSrc, 'MaxWidth', params.MaxWidth);
                }
                if (params.VideoBitrate != null) {
                    currentSrc = replaceQueryString(currentSrc, 'VideoBitrate', params.VideoBitrate);
                }

                clearProgressInterval();

                $(element).off('ended.playbackstopped').off('ended.playnext').on("play.onceafterseek", function () {

                    $(this).off('play.onceafterseek').on('ended.playbackstopped', onPlaybackStopped).on('ended.playnext', playNextAfterEnded);

                    startProgressInterval(currentItem.Id);
                    sendProgressUpdate(currentItem.Id);

                });

                ApiClient.stopActiveEncodings().done(function () {

                    startTimeTicksOffset = ticks;

                    element.src = currentSrc;
                });
            }
        }

        function onPositionSliderChange() {

            isPositionSliderActive = false;

            var newPercent = parseInt(this.value);

            var newPositionTicks = (newPercent / 100) * currentItem.RunTimeTicks;

            changeStream(Math.floor(newPositionTicks));
        }

        $(function () {

            muteButton = $('#muteButton');
            unmuteButton = $('#unmuteButton');

            currentTimeElement = $('.currentTime');

            volumeSlider = $('.volumeSlider').on('slidestop', function () {

                var vol = this.value;

                updateVolumeButtons(vol);
                currentMediaElement.volume = vol;
            });

            positionSlider = $(".positionSlider").on('slidestart', function () {

                isPositionSliderActive = true;

            }).on('slidestop', onPositionSliderChange);

            $('#chaptersFlyout').on('click', '.mediaFlyoutOption', function () {

                var ticks = parseInt(this.getAttribute('data-positionticks'));

                changeStream(ticks);

                hideFlyout($('#chaptersFlyout'));
            });

            $('#audioTracksFlyout').on('click', '.mediaFlyoutOption', function () {

                if (!$(this).hasClass('selectedMediaFlyoutOption')) {
                    var index = parseInt(this.getAttribute('data-index'));

                    changeStream(getCurrentTicks(), { AudioStreamIndex: index });
                }

                hideFlyout($('#audioTracksFlyout'));
            });

            $('#subtitleFlyout').on('click', '.mediaFlyoutOption', function () {

                if (!$(this).hasClass('selectedMediaFlyoutOption')) {
                    var index = parseInt(this.getAttribute('data-index'));

                    changeStream(getCurrentTicks(), { SubtitleStreamIndex: index });
                }

                hideFlyout($('#subtitleFlyout'));
            });

            $('#qualityFlyout').on('click', '.mediaFlyoutOption', function () {

                if (!$(this).hasClass('selectedMediaFlyoutOption')) {

                    var maxWidth = parseInt(this.getAttribute('data-maxwidth'));
                    var videoBitrate = parseInt(this.getAttribute('data-videobitrate'));

                    changeStream(getCurrentTicks(), { MaxWidth: maxWidth, VideoBitrate: videoBitrate });
                }

                hideFlyout($('#qualityFlyout'));
            });
        });

        function endsWith(text, pattern) {

            text = text.toLowerCase();
            pattern = pattern.toLowerCase();

            var d = text.length - pattern.length;
            return d >= 0 && text.lastIndexOf(pattern) === d;
        }

        function setCurrentTime(ticks, item, updateSlider) {

            // Convert to ticks
            ticks = Math.floor(ticks);

            var timeText = Dashboard.getDisplayTime(ticks);

            if (curentDurationTicks) {

                timeText += " / " + Dashboard.getDisplayTime(curentDurationTicks);

                if (updateSlider) {
                    var percent = ticks / curentDurationTicks;
                    percent *= 100;

                    positionSlider.val(percent).slider('enable').slider('refresh');
                }
            } else {
                positionSlider.slider('disable');
            }

            currentTimeElement.html(timeText);
        }

        function playAudio(item, params) {

            var baseParams = {
                audioChannels: 2,
                audioBitrate: 128000
            };

            $.extend(baseParams, params);

            var mp3Url = ApiClient.getUrl('Audio/' + item.Id + '/stream.mp3', $.extend({}, baseParams, {
                audioCodec: 'mp3'
            }));

            var aacUrl = ApiClient.getUrl('Audio/' + item.Id + '/stream.aac', $.extend({}, baseParams, {
                audioCodec: 'aac'
            }));

            var webmUrl = ApiClient.getUrl('Audio/' + item.Id + '/stream.webm', $.extend({}, baseParams, {
                audioCodec: 'Vorbis'
            }));

            var mediaStreams = item.MediaStreams || [];

            for (var i = 0, length = mediaStreams.length; i < length; i++) {

                var stream = mediaStreams[i];

                if (stream.Type == "Audio") {

                    // Stream statically when possible
                    if (endsWith(item.Path, ".aac") && stream.BitRate <= 256000) {
                        aacUrl += "&static=true";
                    }
                    else if (endsWith(item.Path, ".mp3") && stream.BitRate <= 256000) {
                        mp3Url += "&static=true";
                    }
                    break;
                }

            }

            var html = '';

            var requiresControls = $.browser.android || ($.browser.webkit && !$.browser.chrome);

            // Can't autoplay in these browsers so we need to use the full controls
            if (requiresControls) {
                html += '<audio preload="auto" autoplay controls>';
            } else {
                html += '<audio preload="auto" style="display:none;" autoplay>';
            }
            html += '<source type="audio/mpeg" src="' + mp3Url + '" />';
            html += '<source type="audio/aac" src="' + aacUrl + '" />';
            html += '<source type="audio/webm" src="' + webmUrl + '" />';
            html += '</audio';

            var nowPlayingBar = $('#nowPlayingBar').show();
            //show stop button
            $('#stopButton', nowPlayingBar).show();
            $('#playButton', nowPlayingBar).hide();
            $('#pauseButton', nowPlayingBar).show();
            $('#fullscreenButton', nowPlayingBar).hide();

            $('#previousTrackButton', nowPlayingBar).show();
            $('#nextTrackButton', nowPlayingBar).show();
            $('#playlistButton', nowPlayingBar).show();

            $('#qualityButton', nowPlayingBar).hide();
            $('#audioTracksButton', nowPlayingBar).hide();
            $('#subtitleButton', nowPlayingBar).hide();
            $('#chaptersButton', nowPlayingBar).hide();

            $('#mediaElement', nowPlayingBar).html(html);
            var audioElement = $("audio", nowPlayingBar);

            var initialVolume = localStorage.getItem("volume") || 0.5;

            audioElement.each(function () {
                this.volume = initialVolume;
            });

            volumeSlider.val(initialVolume).slider('refresh');
            updateVolumeButtons(initialVolume);

            audioElement.on("volumechange", function () {

                var vol = this.volume;

                localStorage.setItem("volume", vol);

                updateVolumeButtons(vol);

            }).on("play.once", function () {

                if (!requiresControls) {
                    audioElement.hide();
                }

                var duration = this.duration;
                isStaticStream = duration && !isNaN(duration) && duration != Number.POSITIVE_INFINITY && duration != Number.NEGATIVE_INFINITY;

                audioElement.off("play.once");

                ApiClient.reportPlaybackStart(Dashboard.getCurrentUserId(), item.Id, true, item.MediaType);

                startProgressInterval(item.Id);

            }).on("pause", function () {

                $('#playButton', nowPlayingBar).show();
                $('#pauseButton', nowPlayingBar).hide();

            }).on("playing", function () {

                $('#playButton', nowPlayingBar).hide();
                $('#pauseButton', nowPlayingBar).show();

            }).on("timeupdate", function () {

                if (!isPositionSliderActive) {

                    setCurrentTime(getCurrentTicks(this), item, true);
                }

            }).on("ended.playbackstopped", onPlaybackStopped).on('ended.playnext', playNextAfterEnded);

            currentItem = item;
            curentDurationTicks = item.RunTimeTicks;

            return audioElement[0];
        }

        function playVideo(item, startPosition, user) {

            // Account for screen rotation. Use the larger dimension as the width.
            var screenWidth = Math.max(screen.height, screen.width);

            var mediaStreams = item.MediaStreams || [];

            var baseParams = {
                audioChannels: 2,
                audioBitrate: 128000,
                videoBitrate: 2000000,
                maxWidth: Math.min(screenWidth, 1280),
                StartTimeTicks: 0,
                SubtitleStreamIndex: getInitialSubtitleStreamIndex(mediaStreams, user),
                AudioStreamIndex: getInitialAudioStreamIndex(mediaStreams, user),
                deviceId: ApiClient.deviceId(),
                Type: item.Type
            };

            var videoStream = mediaStreams.filter(function (i) {
                return i.Type == "Video";
            })[0];

            if (videoStream && videoStream.Width) {

                if (videoStream.Width >= 1280) {
                    baseParams.videoBitrate = 2000000;
                }

                else if (videoStream.Width >= 720) {
                    baseParams.videoBitrate = 700000;
                }
            }

            // Webm must be ahead of mp4 due to the issue of mp4 playing too fast in chrome
            var prioritizeWebmOverH264 = $.browser.chrome || $.browser.msie;

            var h264Codec = 'h264';
            var h264AudioCodec = 'aac';

            if (startPosition) {
                baseParams.StartTimeTicks = startPosition;
            }

            var mp4VideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.mp4', $.extend({}, baseParams, {
                videoCodec: h264Codec,
                audioCodec: h264AudioCodec,
                profile: 'baseline',
                level: 3
            }));

            var webmVideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.webm', $.extend({}, baseParams, {
                videoCodec: 'vpx',
                audioCodec: 'Vorbis'
            }));

            var hlsVideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.m3u8', $.extend({}, baseParams, {
                videoCodec: h264Codec,
                audioCodec: h264AudioCodec,
                profile: 'baseline',
                level: 3,
                timeStampOffsetMs: 0
            }));

            var html = '';

            var requiresControls = $.browser.msie || $.browser.android || ($.browser.webkit && !$.browser.chrome);

            // Can't autoplay in these browsers so we need to use the full controls
            if (requiresControls) {
                html += '<video class="itemVideo" autoplay controls preload="none">';
            } else {
                html += '<video class="itemVideo" autoplay preload="none">';
            }

            // HLS must be at the top for safari
            html += '<source type="application/x-mpegURL" src="' + hlsVideoUrl + '" />';

            if (prioritizeWebmOverH264) {

                html += '<source type="video/webm" src="' + webmVideoUrl + '" />';
                html += '<source type="video/mp4" src="' + mp4VideoUrl + '" />';
            } else {
                html += '<source type="video/mp4" src="' + mp4VideoUrl + '" />';
                html += '<source type="video/webm" src="' + webmVideoUrl + '" />';
            }

            html += '</video';

            var nowPlayingBar = $('#nowPlayingBar').show();
            //show stop button
            $('#stopButton', nowPlayingBar).show();
            $('#playButton', nowPlayingBar).hide();
            $('#pauseButton', nowPlayingBar).show();
            $('#playlistButton', nowPlayingBar).hide();
            $('#previousTrackButton', nowPlayingBar).hide();
            $('#nextTrackButton', nowPlayingBar).hide();
            $('#mediaElement', nowPlayingBar).html(html);

            $('#qualityButton', nowPlayingBar).show();

            if (mediaStreams.filter(function (i) {
                return i.Type == "Audio";
            }).length) {
                $('#audioTracksButton', nowPlayingBar).show();
            } else {
                $('#audioTracksButton', nowPlayingBar).hide();
            }

            if (mediaStreams.filter(function (i) {
                return i.Type == "Subtitle";
            }).length) {
                $('#subtitleButton', nowPlayingBar).show();
            } else {
                $('#subtitleButton', nowPlayingBar).hide();
            }

            if (item.Chapters && item.Chapters.length) {
                $('#chaptersButton', nowPlayingBar).show();
            } else {
                $('#chaptersButton', nowPlayingBar).hide();
            }

            if (requiresControls) {
                $('#fullscreenButton', nowPlayingBar).hide();
            } else {
                $('#fullscreenButton', nowPlayingBar).show();
            }

            var videoElement = $("video", nowPlayingBar);

            var initialVolume = localStorage.getItem("volume") || 0.5;

            videoElement.each(function () {
                this.volume = initialVolume;
            });

            volumeSlider.val(initialVolume).slider('refresh');
            updateVolumeButtons(initialVolume);

            videoElement.on("volumechange", function () {

                var vol = this.volume;

                localStorage.setItem("volume", vol);

                updateVolumeButtons(vol);

            }).on("play.once", function () {

                var duration = this.duration;
                isStaticStream = duration && !isNaN(duration) && duration != Number.POSITIVE_INFINITY && duration != Number.NEGATIVE_INFINITY;

                videoElement.off("play.once");

                ApiClient.reportPlaybackStart(Dashboard.getCurrentUserId(), item.Id, true, item.MediaType);

                startProgressInterval(item.Id);

            }).on("pause", function () {

                $('#playButton', nowPlayingBar).show();
                $('#pauseButton', nowPlayingBar).hide();

            }).on("playing", function () {

                $('#playButton', nowPlayingBar).hide();
                $('#pauseButton', nowPlayingBar).show();

            }).on("timeupdate", function () {

                if (!isPositionSliderActive) {

                    setCurrentTime(getCurrentTicks(this), item, true);
                }

            }).on("error", function () {


                var errorCode = this.error ? this.error.code : '';
                console.log('Html5 Video error code: ' + errorCode);

            }).on("ended.playbackstopped", onPlaybackStopped).on('ended.playnext', playNextAfterEnded);

            currentItem = item;
            curentDurationTicks = item.RunTimeTicks;

            return videoElement[0];
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
        }

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
        }

        function idleHandler() {
            var nowPlayingBar = $("#nowPlayingBar");
            if (timeout) {
                window.clearTimeout(timeout);
            }
            if (idleState == true) {
                $(".mediaButton,.currentTime,.nowPlayingMediaInfo,.sliderContainer,.barBackground", nowPlayingBar).addClass("highPosition");
            }
            idleState = false;
            timeout = window.setTimeout(function () {
                idleState = true;
                $(".mediaButton,.currentTime,.nowPlayingMediaInfo,.sliderContainer,.barBackground", nowPlayingBar).removeClass("highPosition");
            }, 4000);
        }

        self.canPlay = function (item) {

            if (item.Type == "MusicAlbum" || item.Type == "MusicArtist" || item.Type == "MusicGenre") {
                return true;
            }

            if (item.GameSystem == "Nintendo" && item.MediaType == "Game" && item.ProviderIds.NesBox && item.ProviderIds.NesBoxRom) {
                return true;
            }

            if (item.GameSystem == "Super Nintendo" && item.MediaType == "Game" && item.ProviderIds.NesBox && item.ProviderIds.NesBoxRom) {
                return true;
            }

            return self.canPlayMediaType(item.MediaType);
        };

        self.getPlayUrl = function (item) {


            if (item.GameSystem == "Nintendo" && item.MediaType == "Game" && item.ProviderIds.NesBox && item.ProviderIds.NesBoxRom) {

                return "http://nesbox.com/game/" + item.ProviderIds.NesBox + '/rom/' + item.ProviderIds.NesBoxRom;
            }

            if (item.GameSystem == "Super Nintendo" && item.MediaType == "Game" && item.ProviderIds.NesBox && item.ProviderIds.NesBoxRom) {

                return "http://snesbox.com/game/" + item.ProviderIds.NesBox + '/rom/' + item.ProviderIds.NesBoxRom;
            }

            return null;
        };

        self.canPlayMediaType = function (mediaType) {

            var media;

            if (mediaType === "Video") {
                media = testableVideoElement;
                if (media.canPlayType) {

                    return media.canPlayType('video/mp4').replace(/no/, '') || media.canPlayType('video/mp2t').replace(/no/, '') || media.canPlayType('video/webm').replace(/no/, '') || media.canPlayType('application/x-mpegURL').replace(/no/, '') || media.canPlayType('video/ogv').replace(/no/, '');
                }

                return false;
            }

            if (mediaType === "Audio") {
                media = testableAudioElement;
                if (media.canPlayType) {

                    return media.canPlayType('audio/mpeg').replace(/no/, '') || media.canPlayType('audio/webm').replace(/no/, '') || media.canPlayType('audio/aac').replace(/no/, '');
                }

                return false;
            }

            return false;
        };

        self.play = function (items, startPosition) {

            Dashboard.getCurrentUser().done(function (user) {

                var item = items[0];

                var videoType = (item.VideoType || "").toLowerCase();

                var expirementalText = "This feature is experimental. It may not work at all with some titles. Do you wish to continue?";

                if (videoType == "dvd") {

                    self.playWithWarning(items, startPosition, user, "dvdstreamconfirmed", "Dvd Folder Streaming", expirementalText);
                    return;
                }
                else if (videoType == "bluray") {

                    self.playWithWarning(items, startPosition, user, "bluraystreamconfirmed", "Blu-ray Folder Streaming", expirementalText);
                    return;
                }
                else if (videoType == "iso") {

                    var isoType = (item.IsoType || "").toLowerCase();

                    if (isoType == "dvd") {

                        self.playWithWarning(items, startPosition, user, "dvdisostreamconfirmed", "Dvd Iso Streaming", expirementalText);
                        return;
                    }
                    else if (isoType == "bluray") {

                        self.playWithWarning(items, startPosition, user, "blurayisostreamconfirmed", "Blu-ray Iso Streaming", expirementalText);
                        return;
                    }
                }
                else if ($.browser.msie && videoType) {

                    self.playWithWarning(items, startPosition, user, "iewebmplugin", "Internet Explorer Playback", "For optimal video playback of Internet Explorer desktop edition, please install google's webm plugin for IE.<br/><br/><a target='_blank' href='https://tools.google.com/dlpage/webmmf'>https://tools.google.com/dlpage/webmmf</a>");
                    return;

                }

                self.playInternal(items[0], startPosition, user);
                self.onPlaybackStarted(items);
            });
        };

        self.playWithWarning = function (items, startPosition, user, localStorageKeyName, header, text) {

            localStorageKeyName += new Date().getMonth();

            if (localStorage.getItem(localStorageKeyName) == "1") {

                self.playInternal(items[0], startPosition, user);

                self.onPlaybackStarted(items);

                return;
            }

            Dashboard.confirm(text, header, function (result) {

                if (result) {

                    localStorage.setItem(localStorageKeyName, "1");

                    self.playInternal(items[0], startPosition, user);

                    self.onPlaybackStarted(items);
                }

            });

        };

        self.onPlaybackStarted = function (items) {

            self.playlist = items;
            currentPlaylistIndex = 0;
        };

        self.playInternal = function (item, startPosition, user) {

            if (item == null) {
                throw new Error("item cannot be null");
            }

            if (self.isPlaying()) {
                self.stop();
            }

            var mediaElement;

            if (item.MediaType === "Video") {

                mediaElement = playVideo(item, startPosition, user);
            } else if (item.MediaType === "Audio") {

                mediaElement = playAudio(item);
            } else {
                throw new Error("Unrecognized media type");
            }

            startTimeTicksOffset = startPosition || 0;

            currentMediaElement = mediaElement;

            var nowPlayingBar = $('#nowPlayingBar').show();

            //display image and title
            var imageTags = item.ImageTags || {};
            var html = '';

            var url = "";

            if (imageTags.Primary) {

                url = ApiClient.getImageUrl(item.Id, {
                    type: "Primary",
                    height: 80,
                    tag: item.ImageTags.Primary
                });
            }
            else if (item.BackdropImageTags && item.BackdropImageTags.length) {

                url = ApiClient.getImageUrl(item.Id, {
                    type: "Backdrop",
                    height: 80,
                    tag: item.BackdropImageTags[0]
                });
            } else if (imageTags.Thumb) {

                url = ApiClient.getImageUrl(item.Id, {
                    type: "Thumb",
                    height: 80,
                    tag: item.ImageTags.Thumb
                });

            } else {
                url = "css/images/items/detail/video.png";
            }

            var name = item.Name;
            var seriesName = '';

            if (item.IndexNumber != null) {
                name = item.IndexNumber + " - " + name;
            }
            if (item.ParentIndexNumber != null) {
                name = item.ParentIndexNumber + "." + name;
            }
            if (item.SeriesName || item.Album || item.ProductionYear) {
                seriesName = item.SeriesName || item.Album || item.ProductionYear;
            }

            html += "<div><a href='itemdetails.html?id=" + item.Id + "'><img class='nowPlayingBarImage ' alt='' title='' src='" + url + "' style='height:36px;display:inline-block;' /></a></div>";

            if (item.SeriesName || item.Album) {
                html += '<div class="nowPlayingText">' + seriesName + '<br/>' + name + '</div>';
            } else {
                html += '<div class="nowPlayingText">' + name + '<br/>' + seriesName + '</div>';
            }

            $('.nowPlayingMediaInfo', nowPlayingBar).html(html);
        };

        var getItemFields = "MediaStreams,Chapters,Path";

        self.getItemsForPlayback = function (query) {

            var userId = Dashboard.getCurrentUserId();

            query.Limit = query.Limit || 100;
            query.Fields = getItemFields;

            return ApiClient.getItems(userId, query);
        };

        self.playById = function (id, itemType, startPositionTicks) {

            if (itemType == "Recording") {

                ApiClient.getLiveTvRecording(id, Dashboard.getCurrentUserId()).done(function (item) {

                    self.play([item], startPositionTicks);

                });

                return;
            }

            if (itemType == "Channel") {

                ApiClient.getLiveTvChannel(id, Dashboard.getCurrentUserId()).done(function (item) {

                    self.play([item], startPositionTicks);

                });

                return;
            }

            ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

                if (item.IsFolder) {

                    self.getItemsForPlayback({

                        ParentId: id,
                        Recursive: true,
                        SortBy: "SortName"

                    }).done(function (result) {

                        self.play(result.Items, startPositionTicks);

                    });

                } else {
                    self.play([item], startPositionTicks);
                }

            });

        };

        self.playInstantMixFromSong = function (id) {

            ApiClient.getInstantMixFromSong(id, {

                UserId: Dashboard.getCurrentUserId(),
                Fields: getItemFields,
                Limit: 50

            }).done(function (result) {

                self.play(result.Items);
            });

        };

        self.playInstantMixFromAlbum = function (id) {

            ApiClient.getInstantMixFromAlbum(id, {

                UserId: Dashboard.getCurrentUserId(),
                Fields: getItemFields,
                Limit: 50

            }).done(function (result) {

                self.play(result.Items);
            });

        };

        self.playInstantMixFromArtist = function (name) {

            ApiClient.getInstantMixFromArtist(name, {

                UserId: Dashboard.getCurrentUserId(),
                Fields: getItemFields,
                Limit: 50

            }).done(function (result) {

                self.play(result.Items);
            });

        };

        self.playInstantMixFromMusicGenre = function (name) {

            ApiClient.getInstantMixFromMusicGenre(name, {

                UserId: Dashboard.getCurrentUserId(),
                Fields: getItemFields,
                Limit: 50

            }).done(function (result) {

                self.play(result.Items);
            });

        };

        self.playArtist = function (artist) {

            self.getItemsForPlayback({

                Artists: artist,
                Recursive: true,
                SortBy: "Album,SortName",
                IncludeItemTypes: "Audio"

            }).done(function (result) {

                self.play(result.Items);

            });

        };

        self.shuffleArtist = function (artist) {

            self.getItemsForPlayback({

                Artists: artist,
                Recursive: true,
                SortBy: "Random",
                IncludeItemTypes: "Audio"

            }).done(function (result) {

                self.play(result.Items);

            });

        };

        self.shuffleMusicGenre = function (genre) {

            self.getItemsForPlayback({

                Genres: genre,
                Recursive: true,
                SortBy: "Random",
                IncludeItemTypes: "Audio"

            }).done(function (result) {

                self.play(result.Items);

            });

        };

        self.shuffleFolder = function (id) {

            self.getItemsForPlayback({

                ParentId: id,
                Recursive: true,
                SortBy: "Random"

            }).done(function (result) {

                self.play(result.Items);

            });

        };

        self.toggleFullscreen = function () {
            if (isFullScreen()) {
                if (document.cancelFullScreen) { document.cancelFullScreen(); }
                else if (document.mozCancelFullScreen) { document.mozCancelFullScreen(); }
                else if (document.webkitCancelFullScreen) {
                    document.webkitCancelFullScreen();
                } else {
                    $('.itemVideo').removeClass('fullscreenVideo');

                }
            } else {
                requestFullScreen(document.body);
            }
        };

        self.removeFromPlaylist = function (index) {

            self.playlist.remove(index);

        };

        // Gets or sets the current playlist index
        self.currentPlaylistIndex = function (i) {

            if (i == null) {
                return currentPlaylistIndex;
            }

            var newItem = self.playlist[i];

            Dashboard.getCurrentUser().done(function (user) {

                self.playInternal(newItem, 0, user);
                currentPlaylistIndex = i;
            });
        };

        self.nextTrack = function () {

            var newIndex = currentPlaylistIndex + 1;
            var newItem = self.playlist[newIndex];

            if (newItem) {
                Dashboard.getCurrentUser().done(function (user) {

                    self.playInternal(newItem, 0, user);
                    currentPlaylistIndex = newIndex;
                });
            }
        };

        self.previousTrack = function () {
            var newIndex = currentPlaylistIndex - 1;
            if (newIndex >= 0) {
                var newItem = self.playlist[newIndex];

                if (newItem) {
                    Dashboard.getCurrentUser().done(function (user) {

                        self.playInternal(newItem, 0, user);
                        currentPlaylistIndex = newIndex;
                    });
                }
            }
        };

        self.queueItemsNext = function (items) {

            var insertIndex = 1;

            for (var i = 0, length = items.length; i < length; i++) {

                self.playlist.splice(insertIndex, 0, items[i]);

                insertIndex++;
            }
        };

        self.queueItems = function (items) {

            for (var i = 0, length = items.length; i < length; i++) {

                self.playlist.push(items[i]);
            }
        };

        self.queue = function (id) {

            ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

                if (item.IsFolder) {

                    self.getItemsForPlayback({

                        ParentId: id,
                        Recursive: true,
                        SortBy: "SortName"

                    }).done(function (result) {

                        self.queueItems(result.Items);

                    });

                } else {
                    self.queueItems([item]);
                }

            });
        };

        self.queueArtist = function (artist) {

            self.getItemsForPlayback({

                Artists: artist,
                Recursive: true,
                SortBy: "Album,SortName",
                IncludeItemTypes: "Audio"

            }).done(function (result) {

                self.queueItems(result.Items);

            });

        };

        self.pause = function () {
            currentMediaElement.pause();
        };

        self.unpause = function () {
            currentMediaElement.play();
        };

        self.seek = function (position) {

            changeStream(position);

        };

        self.mute = function () {

            if (currentMediaElement) {
                currentMediaElement.volume = 0;
            }
        };

        self.unmute = function () {
            if (currentMediaElement) {
                currentMediaElement.volume = volumeSlider.val();
            }
        };

        self.toggleMute = function () {

            if (currentMediaElement) {
                currentMediaElement.volume = currentMediaElement.volume ? 0 : volumeSlider.val();
            }
        };

        self.volumeDown = function () {

            if (currentMediaElement) {
                currentMediaElement.volume = Math.max(currentMediaElement.volume - .02, 0);

                volumeSlider.val(currentMediaElement.volume).slider('refresh');
            }
        };

        self.volumeUp = function () {

            if (currentMediaElement) {
                currentMediaElement.volume = Math.min(currentMediaElement.volume + .02, 1);

                volumeSlider.val(currentMediaElement.volume).slider('refresh');
            }
        };

        self.stop = function () {

            var elem = currentMediaElement;

            elem.pause();

            $(elem).off('ended.playnext').on('ended', function () {

                $(this).remove();
                elem.src = "";
                currentMediaElement = null;

            }).trigger('ended');

            $('#nowPlayingBar').hide();

        };

        self.isPlaying = function () {
            return currentMediaElement;
        };

        function hideFlyout(flyout) {
            flyout.hide().empty();
            $(document.body).off("mousedown.hidesearchhints");
        }

        function showFlyout(flyout, button) {

            $(document.body).off("mousedown.mediaflyout").on("mousedown.mediaflyout", function (e) {

                var elem = $(e.target);

                var flyoutId = flyout[0].id;
                var safeItems = button + ',#' + flyoutId;

                if (!elem.is(safeItems) && !elem.parents(safeItems).length) {
                    hideFlyout(flyout);
                }

            });

            flyout.show();
        }

        function getChaptersFlyoutHtml(item) {

            var html = '';

            var currentTicks = getCurrentTicks();

            var chapters = item.Chapters || [];

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

                    imgUrl = ApiClient.getImageUrl(item.Id, {
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
        }

        function getAudioTracksHtml(item, cultures) {

            var streams = item.MediaStreams.filter(function (i) {
                return i.Type == "Audio";
            });

            var currentIndex = getParameterByName('AudioStreamIndex', currentMediaElement.currentSrc);

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

                var language = null;

                if (stream.Language && stream.Language != "und") {

                    var culture = cultures.filter(function (current) {
                        return current.ThreeLetterISOLanguageName.toLowerCase() == stream.Language.toLowerCase();
                    });

                    if (culture.length) {
                        language = culture[0].DisplayName;
                    }
                }

                html += '<div class="mediaFlyoutOptionName">' + (language || 'Unknown language') + '</div>';

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

            return html;
        }

        function getSubtitleTracksHtml(item, cultures) {

            var streams = item.MediaStreams.filter(function (i) {
                return i.Type == "Subtitle";
            });

            var currentIndex = getParameterByName('SubtitleStreamIndex', currentMediaElement.currentSrc) || -1;

            var html = '';

            for (var i = 0, length = streams.length; i < length; i++) {

                var stream = streams[i];

                if (stream.Index == currentIndex) {
                    html += '<div data-index="' + stream.Index + '" class="mediaFlyoutOption selectedMediaFlyoutOption">';
                } else {
                    html += '<div data-index="' + stream.Index + '" class="mediaFlyoutOption">';
                }

                html += '<img class="mediaFlyoutOptionImage" src="css/images/media/subtitleflyout.png" />';

                html += '<div class="mediaFlyoutOptionContent">';

                var language = null;

                if (stream.Language && stream.Language != "und") {

                    var culture = cultures.filter(function (current) {
                        return current.ThreeLetterISOLanguageName.toLowerCase() == stream.Language.toLowerCase();
                    });

                    if (culture.length) {
                        language = culture[0].DisplayName;
                    }
                }

                html += '<div class="mediaFlyoutOptionName">' + (language || 'Unknown language') + '</div>';

                var options = [];

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

            return html;
        }

        function getQualityFlyoutHtml(item) {

            var html = '';

            var videoStream = item.MediaStreams.filter(function (i) {
                return i.Type == "Video";
            })[0];

            var currentVideoBitrate = getParameterByName('videoBitrate', currentMediaElement.currentSrc);

            var maxAllowedWidth = Math.max(screen.height, screen.width);

            var options = [];

            // We have media info
            if (videoStream && videoStream.Width) {

                maxAllowedWidth = videoStream.Width;
            }

            // Some 1080- videos are reported as 1912?
            if (maxAllowedWidth >= 1910) {
                options.push({ name: '1080p+', maxWidth: 1920, videoBitrate: 4000000 });
                options.push({ name: '1080p', maxWidth: 1920, videoBitrate: 2500000 });
            }

            if (maxAllowedWidth >= 1270) {
                options.push({ name: '720p+', maxWidth: 1280, videoBitrate: 2000000 });
                options.push({ name: '720p', maxWidth: 1280, videoBitrate: 1000000 });
            }

            if (maxAllowedWidth >= 480) {
            	 options.push({ name: '480p+', maxWidth: 720, videoBitrate: 700000 });
                options.push({ name: '480p', maxWidth: 720, videoBitrate: 420000 });
            }
            if (maxAllowedWidth >= 360) {
                options.push({ name: '360p', maxWidth: 640, videoBitrate: 410000 });
            }
            if (maxAllowedWidth >= 240) {
                options.push({ name: '240p', maxWidth: 426, videoBitrate: 400000 });
            }

            for (var i = 0, length = options.length; i < length; i++) {

                var option = options[i];

                var cssClass = "mediaFlyoutOption";

                if (option.videoBitrate == currentVideoBitrate) {
                    cssClass += " selectedMediaFlyoutOption";
                }

                html += '<div data-maxwidth="' + option.maxWidth + '" data-videobitrate="' + option.videoBitrate + '" class="' + cssClass + '">';

                html += '<div class="mediaFlyoutOptionContent">';

                html += '<div class="mediaFlyoutOptionName" style="padding:.5em;">' + option.name + '</div>';

                html += "</div>";

                html += "</div>";
            }

            return html;
        }

        self.showAudioTracksFlyout = function () {

            var flyout = $('#audioTracksFlyout');

            if (!flyout.is(':visible')) {

                culturesPromise = culturesPromise || ApiClient.getCultures();

                culturesPromise.done(function (cultures) {

                    showFlyout(flyout, '#audioTracksButton');

                    flyout.html(getAudioTracksHtml(currentItem, cultures)).scrollTop(0);
                });
            }
        };

        self.showChaptersFlyout = function () {

            var flyout = $('#chaptersFlyout');

            if (!flyout.is(':visible')) {

                showFlyout(flyout, '#chaptersButton');

                flyout.html(getChaptersFlyoutHtml(currentItem)).scrollTop(0);
            }
        };

        self.showQualityFlyout = function () {

            var flyout = $('#qualityFlyout');

            if (!flyout.is(':visible')) {

                showFlyout(flyout, '#qualityButton');

                flyout.html(getQualityFlyoutHtml(currentItem)).scrollTop(0);
            }
        };

        self.showSubtitleMenu = function () {

            var flyout = $('#subtitleFlyout');

            if (!flyout.is(':visible')) {

                culturesPromise = culturesPromise || ApiClient.getCultures();

                culturesPromise.done(function (cultures) {

                    showFlyout(flyout, '#subtitleButton');

                    flyout.html(getSubtitleTracksHtml(currentItem, cultures)).scrollTop(0);
                });

            }
        };
    }

    window.MediaPlayer = new mediaPlayer();

})(document, setTimeout, clearTimeout, screen, localStorage, $, setInterval, window);