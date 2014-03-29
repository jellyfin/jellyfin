(function (document, setTimeout, clearTimeout, screen, localStorage, $, setInterval, window) {

    function mediaPlayer() {

        var self = this;

        var testableVideoElement = document.createElement('video');
        var currentMediaElement;
        var currentProgressInterval;
        var currentItem;
        var currentMediaSource;
        var curentDurationTicks;
        var canClientSeek;
        var currentPlaylistIndex = 0;

        self.currentTimeElement = null;
        self.unmuteButton = null;
        self.muteButton = null;
        self.positionSlider = null;
        self.isPositionSliderActive = null;
        self.volumeSlider = null;
        self.startTimeTicksOffset = null;

        self.playlist = [];

        self.isLocalPlayer = true;
        self.name = 'Html5 Player';

        self.getTargets = function () {

            var targets = [{
                name: 'My Browser',
                id: ApiClient.deviceId(),
                playerName: self.name,
                playableMediaTypes: ['Audio', 'Video']
            }];

            return targets;
        };

        self.updateCanClientSeek = function (elem) {
            var duration = elem.duration;
            canClientSeek = duration && !isNaN(duration) && duration != Number.POSITIVE_INFINITY && duration != Number.NEGATIVE_INFINITY;
        };

        self.updateVolumeButtons = function (vol) {

            if (vol) {
                self.muteButton.show().prop("disabled", false);
                self.unmuteButton.hide().prop("disabled", true);
            } else {
                self.muteButton.hide().prop("disabled", true);
                self.unmuteButton.show().prop("disabled", false);
            }
        };

        self.getCurrentTicks = function (mediaElement) {
            return Math.floor(10000000 * (mediaElement || currentMediaElement).currentTime) + self.startTimeTicksOffset;
        };

        self.onPlaybackStopped = function () {

            $(this).off('ended.playbackstopped');

            self.currentTimeElement.empty();

            var endTime = this.currentTime;

            clearProgressInterval();

            var position = Math.floor(10000000 * endTime) + self.startTimeTicksOffset;

            ApiClient.reportPlaybackStopped(Dashboard.getCurrentUserId(), currentItem.Id, currentMediaSource.Id, position);

            if (currentItem.MediaType == "Video") {
                ApiClient.stopActiveEncodings();
                if (self.isFullScreen()) {
                    self.exitFullScreen();
                }
                self.resetEnhancements();
            }
        };

        self.playNextAfterEnded = function () {

            $(this).off('ended.playnext');

            self.nextTrack();
        };

        self.startProgressInterval = function (itemId, mediaSourceId) {

            clearProgressInterval();

            var intervalTime = ApiClient.isWebSocketOpen() ? 2000 : 20000;

            currentProgressInterval = setInterval(function () {

                if (currentMediaElement) {
                    sendProgressUpdate(itemId, mediaSourceId);
                }

            }, intervalTime);
        };

        self.getTranscodingExtension = function () {

            var media = testableVideoElement;

            // safari
            if (media.canPlayType('application/x-mpegURL').replace(/no/, '') ||
                media.canPlayType('application/vnd.apple.mpegURL').replace(/no/, '')) {
                return '.m3u8';
            }

            // Chrome or IE with plugin installed
            if (canPlayWebm() && !$.browser.mozilla) {
                return '.webm';
            }

            return '.mp4';
        };

        self.changeStream = function (ticks, params) {

            var element = currentMediaElement;

            if (canClientSeek && params == null) {

                element.currentTime = ticks / (1000 * 10000);

            } else {

                params = params || {};

                var currentSrc = element.currentSrc;

                if (params.AudioStreamIndex != null) {
                    currentSrc = replaceQueryString(currentSrc, 'AudioStreamIndex', params.AudioStreamIndex);
                }
                if (params.SubtitleStreamIndex != null) {
                    currentSrc = replaceQueryString(currentSrc, 'SubtitleStreamIndex', (params.SubtitleStreamIndex == -1 ? '' : params.SubtitleStreamIndex));
                }

                var maxWidth = params.MaxWidth || getParameterByName('MaxWidth', currentSrc);
                var audioStreamIndex = params.AudioStreamIndex == null ? getParameterByName('AudioStreamIndex', currentSrc) : params.AudioStreamIndex;
                var subtitleStreamIndex = params.SubtitleStreamIndex == null ? getParameterByName('SubtitleStreamIndex', currentSrc) : params.SubtitleStreamIndex;
                var videoBitrate = parseInt(getParameterByName('VideoBitrate', currentSrc) || '0');
                var audioBitrate = parseInt(getParameterByName('AudioBitrate', currentSrc) || '0');
                var bitrate = params.Bitrate || (videoBitrate + audioBitrate);

                var transcodingExtension = self.getTranscodingExtension();

                var finalParams = self.getFinalVideoParams(currentMediaSource, maxWidth, bitrate, audioStreamIndex, subtitleStreamIndex, transcodingExtension);
                currentSrc = replaceQueryString(currentSrc, 'MaxWidth', finalParams.maxWidth);
                currentSrc = replaceQueryString(currentSrc, 'VideoBitrate', finalParams.videoBitrate);
                currentSrc = replaceQueryString(currentSrc, 'AudioBitrate', finalParams.audioBitrate);
                currentSrc = replaceQueryString(currentSrc, 'Static', finalParams.isStatic);

                currentSrc = replaceQueryString(currentSrc, 'AudioCodec', finalParams.audioCodec);
                currentSrc = replaceQueryString(currentSrc, 'VideoCodec', finalParams.videoCodec);

                currentSrc = replaceQueryString(currentSrc, 'profile', finalParams.profile || '');
                currentSrc = replaceQueryString(currentSrc, 'level', finalParams.level || '');

                if (finalParams.isStatic) {
                    currentSrc = currentSrc.replace('.webm', '.mp4').replace('.m3u8', '.mp4');
                    currentSrc = replaceQueryString(currentSrc, 'starttimeticks', '');
                } else {
                    currentSrc = currentSrc.replace('.mp4', transcodingExtension).replace('.m4v', transcodingExtension);
                    currentSrc = replaceQueryString(currentSrc, 'starttimeticks', ticks);
                }

                clearProgressInterval();

                $(element).off('ended.playbackstopped').off('ended.playnext').on("play.onceafterseek", function () {

                    self.updateCanClientSeek(this);

                    $(this).off('play.onceafterseek').on('ended.playbackstopped', self.onPlaybackStopped).on('ended.playnext', self.playNextAfterEnded);

                    self.startProgressInterval(currentItem.Id, currentMediaSource.Id);
                    sendProgressUpdate(currentItem.Id, currentMediaSource.Id);

                });

                ApiClient.stopActiveEncodings().done(function () {

                    self.startTimeTicksOffset = ticks;
                    element.src = currentSrc;

                });
            }
        };

        self.setCurrentTime = function (ticks, item, updateSlider) {

            // Convert to ticks
            ticks = Math.floor(ticks);

            var timeText = Dashboard.getDisplayTime(ticks);

            if (curentDurationTicks) {

                timeText += " / " + Dashboard.getDisplayTime(curentDurationTicks);

                if (updateSlider) {
                    var percent = ticks / curentDurationTicks;
                    percent *= 100;

                    self.positionSlider.val(percent).slider('enable').slider('refresh');
                }
            } else {
                self.positionSlider.slider('disable').slider('refresh');
            }

            self.currentTimeElement.html(timeText);
        };

        self.canPlayVideoDirect = function (mediaSource, videoStream, audioStream, subtitleStream, maxWidth, bitrate) {

            if (mediaSource.VideoType != "VideoFile" || mediaSource.LocationType != "FileSystem") {
                console.log('Transcoding because the content is not a video file');
                return false;
            }

            if ((videoStream.Codec || '').toLowerCase().indexOf('h264') == -1) {
                console.log('Transcoding because the content is not h264');
                return false;
            }

            if (audioStream && !canPlayAudioStreamDirect(audioStream)) {
                console.log('Transcoding because the audio cannot be played directly.');
                return false;
            }

            if (subtitleStream) {
                console.log('Transcoding because subtitles are required');
                return false;
            }

            if (!videoStream.Width || videoStream.Width > maxWidth) {
                console.log('Transcoding because resolution is too high');
                return false;
            }

            var videoBitrate = videoStream.BitRate || 0;
            var audioBitrate = audioStream ? audioStream.BitRate || 0 : null;

            if ((videoBitrate + audioBitrate) > bitrate) {
                console.log('Transcoding because bitrate is too high');
                return false;
            }

            var extension = mediaSource.Path.substring(mediaSource.Path.lastIndexOf('.') + 1).toLowerCase();

            if (extension == 'm4v') {
                return $.browser.chrome;
            }

            return extension.toLowerCase() == 'mp4';
        };

        self.getFinalVideoParams = function (mediaSource, maxWidth, bitrate, audioStreamIndex, subtitleStreamIndex, transcodingExtension) {

            var mediaStreams = mediaSource.MediaStreams;

            var videoStream = mediaStreams.filter(function (stream) {
                return stream.Type === "Video";
            })[0];

            var audioStream = mediaStreams.filter(function (stream) {
                return stream.Index === audioStreamIndex;
            })[0];

            var subtitleStream = mediaStreams.filter(function (stream) {
                return stream.Index === subtitleStreamIndex;
            })[0];

            var canPlayDirect = self.canPlayVideoDirect(mediaSource, videoStream, audioStream, subtitleStream, maxWidth, bitrate);

            var audioBitrate = bitrate >= 700000 ? 128000 : 64000;

            var videoBitrate = bitrate - audioBitrate;

            var params = {
                isStatic: canPlayDirect,
                maxWidth: maxWidth,
                audioCodec: transcodingExtension == '.webm' ? 'vorbis' : 'aac',
                videoCodec: transcodingExtension == '.webm' ? 'vpx' : 'h264',
                audioBitrate: audioBitrate,
                videoBitrate: videoBitrate
            };

            if (params.videoCodec == 'h264') {
                params.profile = 'baseline';
                params.level = '3';
            }

            return params;
        };

        self.canQueueMediaType = function (mediaType) {

            return currentItem && currentItem.MediaType == mediaType;
        };

        self.play = function (options) {

            Dashboard.getCurrentUser().done(function (user) {

                if (options.items) {

                    self.playInternal(options.items[0], options.startPositionTicks, user);

                    self.playlist = options.items;
                    currentPlaylistIndex = 0;

                } else {

                    self.getItemsForPlayback({

                        Ids: options.ids.join(',')

                    }).done(function (result) {

                        options.items = result.Items;

                        self.playInternal(options.items[0], options.startPositionTicks, user);

                        self.playlist = options.items;
                        currentPlaylistIndex = 0;

                    });
                }

            });

        };

        self.getBitrateSetting = function () {
            return parseInt(localStorage.getItem('preferredVideoBitrate') || '') || 1500000;
        };

        function getOptimalMediaSource(mediaType, versions) {

            var optimalVersion;

            if (mediaType == 'Video') {

                var bitrateSetting = self.getBitrateSetting();

                var maxAllowedWidth = Math.max(screen.height, screen.width);

                optimalVersion = versions.filter(function (v) {

                    var videoStream = v.MediaStreams.filter(function (s) {
                        return s.Type == 'Video';
                    })[0];

                    var audioStream = v.MediaStreams.filter(function (s) {
                        return s.Type == 'Audio';
                    })[0];

                    return self.canPlayVideoDirect(v, videoStream, audioStream, null, maxAllowedWidth, bitrateSetting);

                })[0];
            }

            return optimalVersion || versions[0];
        }

        self.playInternal = function (item, startPosition, user) {

            if (item == null) {
                throw new Error("item cannot be null");
            }

            if (self.isPlaying()) {
                self.stop();
            }

            var mediaElement;

            var mediaControls = $('#nowPlayingBar');

            if (item.MediaType === "Video") {

                currentItem = item;
                currentMediaSource = getOptimalMediaSource(item.MediaType, item.MediaSources);

                videoPlayer(self, item, currentMediaSource, startPosition, user);
                mediaElement = self.initVideoPlayer();
                curentDurationTicks = currentMediaSource.RunTimeTicks;

                mediaControls = $("#videoControls");

            } else if (item.MediaType === "Audio") {

                currentItem = item;
                currentMediaSource = getOptimalMediaSource(item.MediaType, item.MediaSources);

                mediaElement = playAudio(item, currentMediaSource, startPosition);
                mediaControls.show();

                curentDurationTicks = currentMediaSource.RunTimeTicks;

            } else {
                throw new Error("Unrecognized media type");
            }

            currentMediaElement = mediaElement;

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

            }
            else if (item.Type == "TvChannel" || item.Type == "Recording") {
                url = "css/images/items/detail/tv.png";
            }
            else if (item.MediaType == "Audio") {
                url = "css/images/items/detail/audio.png";
            }
            else {
                url = "css/images/items/detail/video.png";
            }

            var name = item.Name;
            var seriesName = '';

            // Channel number
            if (item.Number) {
                name = item.Number + ' ' + name;
            }
            if (item.IndexNumber != null) {
                name = item.IndexNumber + " - " + name;
            }
            if (item.ParentIndexNumber != null) {
                name = item.ParentIndexNumber + "." + name;
            }
            if (item.SeriesName || item.Album || item.ProductionYear) {
                seriesName = item.SeriesName || item.Album || item.ProductionYear;
            }
            if (item.CurrentProgram) {
                seriesName = item.CurrentProgram.Name;
            }

            var href = LibraryBrowser.getHref(item.CurrentProgram || item);

            var nowPlayingText = (name ? name + "\n" : "") + (seriesName || "---");
            if (item.SeriesName || item.Album || item.CurrentProgram) {
                nowPlayingText = (seriesName ? seriesName : "") + "\n" + (name || "---");
            }

            // Fix for apostrophes and quotes
            var htmlTitle = trimTitle(nowPlayingText).replace(/'/g, '&apos;').replace(/"/g, '&quot;');
            html += "<div><a href='" + href + "'><img class='nowPlayingBarImage' alt='" + htmlTitle +
                "' title='" + htmlTitle + "' src='" + url + "' style='height:40px;display:inline-block;' /></a></div>";
            html += "<div class='nowPlayingText' title='" + htmlTitle + "'>" + titleHtml(nowPlayingText) + "</div>";

            $('.nowPlayingMediaInfo', mediaControls).html(html);
        };

        self.getItemsForPlayback = function (query) {

            var userId = Dashboard.getCurrentUserId();

            query.Limit = query.Limit || 100;
            query.Fields = getItemFields;
            query.ExcludeLocationTypes = "Virtual";

            return ApiClient.getItems(userId, query);
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

        self.queue = function (options) {

            if (!currentMediaElement) {
                self.play(options);
                return;
            }

            Dashboard.getCurrentUser().done(function (user) {

                if (options.items) {

                    self.queueItems(options.items);

                } else {

                    self.getItemsForPlayback({

                        Ids: options.ids.join(',')

                    }).done(function (result) {

                        options.items = result.Items;

                        self.queueItems(options.items);

                    });
                }

            });
        };

        self.queueNext = function (options) {

            if (!currentMediaElement) {
                self.play(options);
                return;
            }

            Dashboard.getCurrentUser().done(function (user) {

                if (options.items) {

                    self.queueItemsNext(options.items);

                } else {

                    self.getItemsForPlayback({

                        Ids: options.ids.join(',')

                    }).done(function (result) {

                        options.items = result.Items;

                        self.queueItemsNext(options.items);

                    });
                }

            });
        };

        self.pause = function () {
            currentMediaElement.pause();
        };

        self.unpause = function () {
            currentMediaElement.play();
        };

        self.seek = function (position) {

            self.changeStream(position);

        };

        self.mute = function () {

            if (currentMediaElement) {
                currentMediaElement.volume = 0;
                currentMediaElement.muted = true;
                self.volumeSlider.val(0).slider('refresh');
            }
        };

        self.unmute = function () {

            if (currentMediaElement) {
                var volume = localStorage.getItem("volume") || self.volumeSlider.val();
                currentMediaElement.volume = volume;
                currentMediaElement.muted = false;
                self.volumeSlider.val(volume).slider('refresh');
            }
        };

        self.toggleMute = function () {

            if (currentMediaElement) {
                var volume = localStorage.getItem("volume") || self.volumeSlider.val();
                currentMediaElement.volume = currentMediaElement.volume ? 0 : volume;
                currentMediaElement.muted = currentMediaElement.volume == 0;
                self.volumeSlider.val(volume).slider('refresh');
            }
        };

        self.volumeDown = function () {

            if (currentMediaElement) {
                currentMediaElement.volume = Math.max(currentMediaElement.volume - .02, 0);
                localStorage.setItem("volume", currentMediaElement.volume);
                self.volumeSlider.val(currentMediaElement.volume).slider('refresh');
            }
        };

        self.volumeUp = function () {

            if (currentMediaElement) {
                currentMediaElement.volume = Math.min(currentMediaElement.volume + .02, 1);
                localStorage.setItem("volume", currentMediaElement.volume);
                self.volumeSlider.val(currentMediaElement.volume).slider('refresh');
            }
        };

        self.shuffle = function (id) {

            var userId = Dashboard.getCurrentUserId();

            ApiClient.getItem(userId, id).done(function (item) {

                var query = {
                    UserId: userId,
                    Fields: getItemFields,
                    Limit: 50,
                    Filters: "IsNotFolder",
                    Recursive: true,
                    SortBy: "Random"
                };

                if (item.IsFolder) {
                    query.ParentId = id;

                }
                else if (item.Type == "MusicArtist") {

                    query.MediaTypes = "Audio";
                    query.Artists = item.Name;

                }
                else if (item.Type == "MusicGenre") {

                    query.MediaTypes = "Audio";
                    query.Genres = item.Name;

                } else {
                    return;
                }

                self.getItemsForPlayback(query).done(function (result) {

                    self.play({ items: result.Items });

                });

            });

        };

        self.instantMix = function (id) {

            var userId = Dashboard.getCurrentUserId();

            ApiClient.getItem(userId, id).done(function (item) {

                var promise;

                if (item.Type == "MusicArtist") {

                    promise = ApiClient.getInstantMixFromArtist(name, {
                        UserId: Dashboard.getCurrentUserId(),
                        Fields: getItemFields,
                        Limit: 50
                    });

                }
                else if (item.Type == "MusicGenre") {

                    promise = ApiClient.getInstantMixFromMusicGenre(name, {
                        UserId: Dashboard.getCurrentUserId(),
                        Fields: getItemFields,
                        Limit: 50
                    });

                }
                else if (item.Type == "MusicAlbum") {

                    promise = ApiClient.getInstantMixFromAlbum(id, {
                        UserId: Dashboard.getCurrentUserId(),
                        Fields: getItemFields,
                        Limit: 50
                    });

                }
                else if (item.Type == "Audio") {

                    promise = ApiClient.getInstantMixFromSong(id, {
                        UserId: Dashboard.getCurrentUserId(),
                        Fields: getItemFields,
                        Limit: 50
                    });

                }
                else {
                    return;
                }

                promise.done(function (result) {

                    self.play({ items: result.Items });

                });

            });

        };

        self.stop = function () {

            var elem = currentMediaElement;

            elem.pause();

            $(elem).off("ended.playnext").on("ended", function () {

                $(this).remove();
                elem.src = "";
                currentMediaElement = null;

            }).trigger("ended");

            if (currentItem.MediaType == "Video") {
                if (self.isFullScreen()) {
                    self.exitFullScreen();
                }
                self.resetEnhancements();
            } else {
                $('#nowPlayingBar').hide();
            }
        };

        self.isPlaying = function () {
            return currentMediaElement;
        };

        self.bindPositionSlider = function () {
            self.positionSlider.on('slidestart', function (e) {

                self.isPositionSliderActive = true;

            }).on('slidestop', onPositionSliderChange);
        };

        self.bindVolumeSlider = function () {
            self.volumeSlider.on('slidestop', function () {

                var vol = this.value;

                self.updateVolumeButtons(vol);
                currentMediaElement.volume = vol;
            });
        };

        $(window).on("beforeunload popstate", function () {

            var item = currentItem;
            var media = currentMediaElement;

            // Try to report playback stopped before the browser closes
            if (item && media && currentProgressInterval) {

                var endTime = currentMediaElement.currentTime;

                var position = Math.floor(10000000 * endTime) + self.startTimeTicksOffset;

                ApiClient.reportPlaybackStopped(Dashboard.getCurrentUserId(), currentItem.Id, currentMediaSource.Id, position);
            }
        });

        $(function () {
            initPlayer();
        });

        function initPlayer() {
            self.muteButton = $('.muteButton');
            self.unmuteButton = $('.unmuteButton');
            self.currentTimeElement = $('.currentTime');
            self.volumeSlider = $('.volumeSlider');
            self.positionSlider = $(".positionSlider");

            self.bindVolumeSlider();
            self.bindPositionSlider();
        };

        function replaceQueryString(url, param, value) {
            var re = new RegExp("([?|&])" + param + "=.*?(&|$)", "i");
            if (url.match(re))
                return url.replace(re, '$1' + param + "=" + value + '$2');
            else
                return url + '&' + param + "=" + value;
        };

        function sendProgressUpdate(itemId, mediaSourceId) {

            ApiClient.reportPlaybackProgress(Dashboard.getCurrentUserId(), itemId, mediaSourceId, self.getCurrentTicks(), currentMediaElement.paused, currentMediaElement.volume == 0);
        };

        function clearProgressInterval() {

            if (currentProgressInterval) {
                clearTimeout(currentProgressInterval);
                currentProgressInterval = null;
            }
        };

        function canPlayWebm() {

            return testableVideoElement.canPlayType('video/webm').replace(/no/, '');
        };

        function onPositionSliderChange() {

            self.isPositionSliderActive = false;

            var newPercent = parseInt(this.value);

            var newPositionTicks = (newPercent / 100) * currentMediaSource.RunTimeTicks;

            self.changeStream(Math.floor(newPositionTicks));
        };

        function endsWith(text, pattern) {

            text = text.toLowerCase();
            pattern = pattern.toLowerCase();

            var d = text.length - pattern.length;
            return d >= 0 && text.lastIndexOf(pattern) === d;
        };

        function playAudio(item, mediaSource, startPositionTicks) {

            startPositionTicks = startPositionTicks || 0;

            var baseParams = {
                audioChannels: 2,
                audioBitrate: 128000,
                StartTimeTicks: startPositionTicks,
                mediaSourceId: mediaSource.Id
            };

            var mp3Url = ApiClient.getUrl('Audio/' + item.Id + '/stream.mp3', $.extend({}, baseParams, {
                audioCodec: 'mp3'
            }));

            var aacUrl = ApiClient.getUrl('Audio/' + item.Id + '/stream.aac', $.extend({}, baseParams, {
                audioCodec: 'aac'
            }));

            var webmUrl = ApiClient.getUrl('Audio/' + item.Id + '/stream.webm', $.extend({}, baseParams, {
                audioCodec: 'Vorbis'
            }));

            var mediaStreams = mediaSource.MediaStreams;

            var isStatic = false;
            var seekParam = isStatic && startPositionTicks ? '#t=' + (startPositionTicks / 10000000) : '';

            for (var i = 0, length = mediaStreams.length; i < length; i++) {

                var stream = mediaStreams[i];

                if (stream.Type == "Audio") {

                    // Stream statically when possible
                    if (endsWith(mediaSource.Path, ".aac") && stream.BitRate <= 256000) {
                        aacUrl += "&static=true" + seekParam;
                        isStatic = true;
                    }
                    else if (endsWith(mediaSource.Path, ".mp3") && stream.BitRate <= 256000) {
                        mp3Url += "&static=true" + seekParam;
                        isStatic = true;
                    }
                    break;
                }
            }

            self.startTimeTicksOffset = isStatic ? 0 : startPositionTicks;

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
            html += '</audio>';

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
            var audioElement = $("audio", mediaElement);

            var initialVolume = localStorage.getItem("volume") || 0.5;

            audioElement.each(function () {
                this.volume = initialVolume;
            });

            self.volumeSlider.val(initialVolume).slider('refresh');
            self.updateVolumeButtons(initialVolume);

            audioElement.on("volumechange", function () {

                var vol = this.volume;

                localStorage.setItem("volume", vol);

                self.updateVolumeButtons(vol);

            }).on("play.once", function () {

                if (!requiresControls) {
                    audioElement.hide();
                }

                self.updateCanClientSeek(this);

                audioElement.off("play.once");

                ApiClient.reportPlaybackStart(Dashboard.getCurrentUserId(), item.Id, mediaSource.Id, true, item.MediaType);

                self.startProgressInterval(item.Id, mediaSource.Id);

            }).on("pause", function () {

                $('#playButton', nowPlayingBar).show();
                $('#pauseButton', nowPlayingBar).hide();

            }).on("playing", function () {

                $('#playButton', nowPlayingBar).hide();
                $('#pauseButton', nowPlayingBar).show();

            }).on("timeupdate", function () {

                if (!self.isPositionSliderActive) {

                    self.setCurrentTime(self.getCurrentTicks(this), item, true);
                }

            }).on("ended.playbackstopped", self.onPlaybackStopped).on('ended.playnext', self.playNextAfterEnded);

            return audioElement[0];
        };

        function canPlayAudioStreamDirect(audioStream) {

            var audioCodec = (audioStream.Codec || '').toLowerCase().replace('-', '');

            if (audioCodec.indexOf('aac') == -1 &&
                audioCodec.indexOf('mp3') == -1 &&
                audioCodec.indexOf('mpeg') == -1) {

                return false;
            }

            if (audioStream.Channels == null) {
                return false;
            }

            // IE won't play at all if more than two channels
            if (audioStream.Channels > 2 && $.browser.msie) {
                return false;
            }

            return true;
        };

        function trunc(string, len) {
            if (string.length > 0 && string.length < len) return string;
            var trimmed = $.trim(string).substring(0, len).split(" ").slice(0, -1).join(" ");
            if (trimmed) {
                trimmed += "...";
            } else {
                trimmed = "---"
            }
            return trimmed;
        };

        function trimTitle(title) {
            return title.replace("\n---", "");
        };

        function titleHtml(title) {
            var titles = title.split("\n");
            return (trunc(titles[0], 30) + "<br />" + trunc(titles[1], 30)).replace("---", "&nbsp;");
        };

        var getItemFields = "MediaSources,Chapters";
    }

    window.MediaPlayer = new mediaPlayer();

    window.MediaController.registerPlayer(window.MediaPlayer);
    window.MediaController.setActivePlayer(window.MediaPlayer, window.MediaPlayer.getTargets()[0]);


})(document, setTimeout, clearTimeout, screen, localStorage, $, setInterval, window);