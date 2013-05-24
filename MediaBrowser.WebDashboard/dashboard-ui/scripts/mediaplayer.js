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

        self.playing = '';
        self.queue = [];

        function requestFullScreen(element) {
            // Supports most browsers and their versions.
            var requestMethod = element.requestFullScreen || element.webkitRequestFullScreen || element.mozRequestFullScreen || element.msRequestFullScreen;

            if (requestMethod) { // Native full screen.
                requestMethod.call(element);
            } else if (typeof window.ActiveXObject !== "undefined") { // Older IE.
                var wscript = new ActiveXObject("WScript.Shell");
                if (wscript !== null) {
                    wscript.SendKeys("{F11}");
                }
            }
        }

        function isFullScreen() {
            return document.fullscreenEnabled || document.mozFullscreenEnabled || document.webkitIsFullScreen ? true : false;
        }

        $(document).on('webkitfullscreenchange mozfullscreenchange fullscreenchange', function () {

            if (isFullScreen()) {
                $('.itemVideo').addClass('fullscreenVideo');
            } else {
                $('.itemVideo').removeClass('fullscreenVideo');
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

        function onPlaybackStopped() {

            currentTimeElement.hide();

            var endTime = this.currentTime;

            this.currentTime = 0;

            clearProgressInterval();

            var position = Math.floor(10000000 * endTime) + startTimeTicksOffset;

            ApiClient.reportPlaybackStopped(Dashboard.getCurrentUserId(), currentItem.Id, position);

            MediaPlayer.queuePlayNext();
        }

        function startProgressInterval(itemId) {

            clearProgressInterval();

            var intervalTime = ApiClient.isWebSocketOpen() ? 10000 : 30000;

            currentProgressInterval = setInterval(function () {

                if (currentMediaElement) {
                    sendProgressUpdate(itemId);
                }

            }, intervalTime);
        }

        function sendProgressUpdate(itemId) {
            var position = Math.floor(10000000 * currentMediaElement.currentTime) + startTimeTicksOffset;

            ApiClient.reportPlaybackProgress(Dashboard.getCurrentUserId(), itemId, position);
        }

        function clearProgressInterval() {

            if (currentProgressInterval) {
                clearTimeout(currentProgressInterval);
                currentProgressInterval = null;
            }
        }

        function onPositionSliderChange() {

            isPositionSliderActive = false;

            var element = currentMediaElement;

            var newPercent = parseInt(this.value);

            var newPositionTicks = (newPercent / 100) * currentItem.RunTimeTicks;

            if (isStaticStream) {

                element.currentTime = newPositionTicks / (1000 * 10000);

            } else {

                var currentSrc = element.currentSrc;

                if (currentSrc.toLowerCase().indexOf('starttimeticks') == -1) {

                    currentSrc += "&starttimeticks=" + newPositionTicks;

                } else {
                    currentSrc = replaceQueryString(currentSrc, 'starttimeticks', newPositionTicks);
                }

                clearProgressInterval();

                $(element).off('ended.playbackstopped').on("play.onceafterseek", function () {

                    $(this).off('play.onceafterseek').on('ended.playbackstopped', onPlaybackStopped);
                    startProgressInterval(currentItem.Id);
                    sendProgressUpdate(currentItem.Id);

                });
                startTimeTicksOffset = newPositionTicks;

                element.src = currentSrc;
            }
        }

        $(function () {

            muteButton = $('#muteButton');
            unmuteButton = $('#unmuteButton');

            currentTimeElement = $('.currentTime');

            volumeSlider = $('.volumeSlider').on('change', function () {

                var vol = this.value;
                updateVolumeButtons(vol);
                currentMediaElement.volume = vol;
            });

            $(".jqueryuislider").slider({ orientation: "horizontal" });

            positionSlider = $(".positionSlider").on('mousedown', function () {

                isPositionSliderActive = true;

            });

            if ($.browser.mozilla) {

                positionSlider.on('change', onPositionSliderChange);

            } else {

                positionSlider.on('change', function () {

                    isPositionSliderActive = true;

                    setCurrentTimePercent(parseInt(this.value), currentItem);

                }).on('changed', onPositionSliderChange);
            }


            (function (el, timeout) {
                var timer, trig = function () { el.trigger("changed"); };
                el.bind("change", function () {
                    if (timer) {
                        clearTimeout(timer);
                    }
                    timer = setTimeout(trig, timeout);
                });
            })(positionSlider, 500);
        });

        function endsWith(text, pattern) {

            text = text.toLowerCase();
            pattern = pattern.toLowerCase();

            var d = text.length - pattern.length;
            return d >= 0 && text.lastIndexOf(pattern) === d;
        }

        function setCurrentTimePercent(percent, item) {

            var position = (percent / 100) * curentDurationTicks;
            setCurrentTime(position, item, false);
        }

        function setCurrentTime(ticks, item, updateSlider) {

            // Convert to ticks
            ticks = Math.floor(ticks);

            var timeText = DashboardPage.getDisplayText(ticks);

            if (curentDurationTicks) {

                timeText += " / " + DashboardPage.getDisplayText(curentDurationTicks);

                if (updateSlider) {
                    var percent = ticks / curentDurationTicks;
                    percent *= 100;

                    positionSlider.val(percent);
                }
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

            var requiresControls = $.browser.android || $.browser.iphone || $.browser.ipad;

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
            $('#playlistButton', nowPlayingBar).show();
            $('#previousTrackButton', nowPlayingBar).show();
            $('#nextTrackButton', nowPlayingBar).show();
            $('#mediaElement', nowPlayingBar).html(html);
            $('#fullscreenButton', nowPlayingBar).hide();

            var audioElement = $("audio", nowPlayingBar);

            var initialVolume = localStorage.getItem("volume") || 0.5;

            audioElement.each(function () {
                this.volume = initialVolume;
            });

            volumeSlider.val(initialVolume);
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

                currentTimeElement.show();

                audioElement.off("play.once");

                ApiClient.reportPlaybackStart(Dashboard.getCurrentUserId(), item.Id);

                startProgressInterval(item.Id);

            }).on("pause", function () {

                $('#playButton', nowPlayingBar).show();
                $('#pauseButton', nowPlayingBar).hide();

            }).on("playing", function () {

                $('#playButton', nowPlayingBar).hide();
                $('#pauseButton', nowPlayingBar).show();

            }).on("timeupdate", function () {

                if (!isPositionSliderActive) {

                    var ticks = startTimeTicksOffset + this.currentTime * 1000 * 10000;

                    setCurrentTime(ticks, item, true);
                }

            }).on("ended.playbackstopped", onPlaybackStopped);

            MediaPlayer.nowPlaying(item);

            currentItem = item;
            curentDurationTicks = item.RunTimeTicks;

            return audioElement[0];
        }

        function playVideo(item, startPosition) {

            //stop/kill videoJS
            if (currentMediaElement) self.stop();

            // Account for screen rotation. Use the larger dimension as the width.
            var screenWidth = Math.max(screen.height, screen.width);
            var screenHeight = Math.min(screen.height, screen.width);

            var user = Dashboard.getCurrentUser();
            var defaults = { languageIndex: null, subtitleIndex: null };

            var userConfig = user.Configuration || {};
            if (item.MediaStreams && item.MediaStreams.length) {
                $.each(item.MediaStreams, function (i, stream) {
                    //get default subtitle stream
                    if (stream.Type == "Subtitle") {
                        if (userConfig.UseForcedSubtitlesOnly == true && userConfig.SubtitleLanguagePreference && !defaults.subtitleIndex) {
                            if (stream.Language == userConfig.SubtitleLanguagePreference && stream.IsForced == true) {
                                defaults.subtitleIndex = i;
                            }
                        } else if (userConfig.SubtitleLanguagePreference && !defaults.subtitleIndex) {
                            if (stream.Language == userConfig.SubtitleLanguagePreference) {
                                defaults.subtitleIndex = i;
                            }
                        } else if (userConfig.UseForcedSubtitlesOnly == true && !defaults.subtitleIndex) {
                            if (stream.IsForced == true) {
                                defaults.subtitleIndex = i;
                            }
                        }
                    } else if (stream.Type == "Audio") {
                        //get default language stream
                        if (userConfig.AudioLanguagePreference && !defaults.languageIndex) {
                            if (stream.Language == userConfig.AudioLanguagePreference) {
                                defaults.languageIndex = i;
                            }
                        }
                    }
                });
            }

            var baseParams = {
                audioChannels: 2,
                audioBitrate: 128000,
                videoBitrate: 1500000,
                maxWidth: screenWidth,
                maxHeight: screenHeight,
                StartTimeTicks: 0,
                SubtitleStreamIndex: null,
                AudioStreamIndex: null
            };

            if (startPosition) {
                baseParams.StartTimeTicks = startPosition;
            }

            var mp4VideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.mp4', $.extend({}, baseParams, {
                videoCodec: 'h264',
                audioCodec: 'aac',
                profile: 'high',
                videoBitrate: 2500000
            }));

            var tsVideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.ts', $.extend({}, baseParams, {
                videoCodec: 'h264',
                audioCodec: 'aac',
                profile: 'high',
                videoBitrate: 2500000
            }));

            var webmVideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.webm', $.extend({}, baseParams, {
                videoCodec: 'vpx',
                audioCodec: 'Vorbis',
                videoBitrate: 2500000
            }));

            var hlsVideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.m3u8', $.extend({}, baseParams, {
                videoCodec: 'h264',
                audioCodec: 'aac'
            }));

            var ogvVideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.ogv', $.extend({}, baseParams, {
                videoCodec: 'theora',
                audioCodec: 'Vorbis'
            }));

            var html = '';

            // HLS must be at the top for safari
            // Webm must be ahead of mp4 due to the issue of mp4 playing too fast in chrome

            // Can't autoplay in these browsers so we need to use the full controls
            if ($.browser.msie || $.browser.android || $.browser.iphone || $.browser.ipad) {
                html += '<video class="itemVideo" preload="auto" autoplay controls>';
            } else {
                html += '<video class="itemVideo" preload="auto" autoplay>';
            }

            html += '<source type="application/x-mpegURL" src="' + hlsVideoUrl + '" />';
            html += '<source type="video/webm" src="' + webmVideoUrl + '" />';
            html += '<source type="video/mp4" src="' + mp4VideoUrl + '" />';
            html += '<source type="video/mp2t" src="' + tsVideoUrl + '" />';
            html += '<source type="video/ogg" src="' + ogvVideoUrl + '" />';
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

            if ($.browser.msie) {
                $('#fullscreenButton', nowPlayingBar).hide();
            } else {
                $('#fullscreenButton', nowPlayingBar).show();
            }

            var videoElement = $("video", nowPlayingBar);

            var initialVolume = localStorage.getItem("volume") || 0.5;

            videoElement.each(function () {
                this.volume = initialVolume;
            });

            volumeSlider.val(initialVolume);
            updateVolumeButtons(initialVolume);

            videoElement.on("volumechange", function () {

                var vol = this.volume;

                localStorage.setItem("volume", vol);

                updateVolumeButtons(vol);

            }).on("play.once", function () {

                var duration = this.duration;
                isStaticStream = duration && !isNaN(duration) && duration != Number.POSITIVE_INFINITY && duration != Number.NEGATIVE_INFINITY;

                currentTimeElement.show();

                videoElement.off("play.once");

                ApiClient.reportPlaybackStart(Dashboard.getCurrentUserId(), item.Id);

                startProgressInterval(item.Id);

            }).on("pause", function () {

                $('#playButton', nowPlayingBar).show();
                $('#pauseButton', nowPlayingBar).hide();

            }).on("playing", function () {

                $('#playButton', nowPlayingBar).hide();
                $('#pauseButton', nowPlayingBar).show();

            }).on("timeupdate", function () {

                if (!isPositionSliderActive) {

                    var ticks = startTimeTicksOffset + this.currentTime * 1000 * 10000;

                    setCurrentTime(ticks, item, true);
                }

            }).on("ended.playbackstopped", onPlaybackStopped);

            MediaPlayer.nowPlaying(item);

            currentItem = item;
            curentDurationTicks = item.RunTimeTicks;

            return videoElement[0];
        }

        self.canPlay = function (item) {

            return self.canPlayMediaType(item.MediaType);
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

            if (self.isPlaying()) {
                self.stop();
            }

            var item = items[0];

            var mediaElement;

            if (item.MediaType === "Video") {

                mediaElement = playVideo(item, startPosition);
            } else if (item.MediaType === "Audio") {

                mediaElement = playAudio(item);
            }

            startTimeTicksOffset = startPosition || 0;

            if (!mediaElement) {
                return;
            }

            currentMediaElement = mediaElement;

            var nowPlayingBar = $('#nowPlayingBar').show();

            if (items.length > 1) {
                $('#previousTrackButton', nowPlayingBar)[0].disabled = false;
                $('#nextTrackButton', nowPlayingBar)[0].disabled = false;
            } else {
                $('#previousTrackButton', nowPlayingBar)[0].disabled = true;
                $('#nextTrackButton', nowPlayingBar)[0].disabled = true;
            }

            //display image and title
            var imageTags = item.ImageTags || {};
            var html = '';

            var url = "";

            if (item.BackdropImageTags && item.BackdropImageTags.length) {

                url = ApiClient.getImageUrl(item.Id, {
                    type: "Backdrop",
                    height: 36,
                    tag: item.BackdropImageTags[0]
                });
            } else if (imageTags.Thumb) {

                url = ApiClient.getImageUrl(item.Id, {
                    type: "Thumb",
                    height: 36,
                    tag: item.ImageTags.Thumb
                });
            } else if (imageTags.Primary) {

                url = ApiClient.getImageUrl(item.Id, {
                    type: "Primary",
                    height: 36,
                    tag: item.ImageTags.Primary
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
            if (item.Type == "Movie")
                html += '<div>' + name + '<br/>' + seriesName + '</div>';
            else
                html += '<div>' + seriesName + '<br/>' + name + '</div>';

            $('.nowPlayingMediaInfo', nowPlayingBar).html(html);
        };

        self.playById = function (id, startPositionTicks) {

            ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

                self.play([item], startPositionTicks);

            });

        };

        self.nowPlaying = function (item) {
            self.playing = item;
        };

        self.toggleFullscreen = function () {

            if (isFullScreen()) {
                if (document.cancelFullScreen) { document.cancelFullScreen(); }
                else if (document.mozCancelFullScreen) { document.mozCancelFullScreen(); }
                else if (document.webkitCancelFullScreen) { document.webkitCancelFullScreen(); }
            } else {
                requestFullScreen(document.body);
            }
        };

        self.canQueue = function (mediaType) {
            return mediaType == "Audio";
        };

        self.queueAdd = function (item) {
            self.queue.push(item);
        };

        self.queueRemove = function (elem) {
            var index = $(elem).attr("data-queue-index");

            self.queue.splice(index, 1);

            $(elem).parent().parent().remove();
            return false;
        };

        self.queuePlay = function (elem) {
            var index = $(elem).attr("data-queue-index");

            MediaPlayer.play(new Array(self.queue[index]));
            self.queue.splice(index, 1);
        };

        self.queuePlayNext = function (item) {
            if (typeof self.queue[0] != "undefined") {
                MediaPlayer.play(new Array(self.queue[0]));
                self.queue.shift();
            }
        };

        self.queueAddNext = function (item) {
            if (typeof self.queue[0] != "undefined") {
                self.queue.unshift(item);
            } else {
                self.queueAdd(item);
            }
        };

        self.inQueue = function (item) {
            $.each(MediaPlayer.queue, function (i, queueItem) {
                if (item.Id == queueItem.Id) {
                    return true;
                }
            });
            return false;
        };

        self.playLast = function (itemId) {
            ApiClient.getItem(Dashboard.getCurrentUserId(), itemId).done(function (item) {
                self.queueAdd(item);
            });
        };

        self.playNext = function (itemId) {
            ApiClient.getItem(Dashboard.getCurrentUserId(), itemId).done(function (item) {
                self.queueAddNext(item);
            });
        };

        self.pause = function () {
            currentMediaElement.pause();
        };

        self.unpause = function () {
            currentMediaElement.play();
        };

        self.mute = function () {
            currentMediaElement.volume = 0;
        };

        self.unmute = function () {
            currentMediaElement.volume = volumeSlider.val();
        };

        self.stop = function () {

            var elem = currentMediaElement;

            elem.pause();

            var jelem = $(elem).trigger('ended');
            elem.src = "";

            jelem.remove();

            $('#nowPlayingBar').hide();

            currentMediaElement = null;
        };

        self.isPlaying = function () {
            return currentMediaElement;
        };
    }

    window.MediaPlayer = new mediaPlayer();

})(document, setTimeout, clearTimeout, screen, localStorage, $, setInterval, window);