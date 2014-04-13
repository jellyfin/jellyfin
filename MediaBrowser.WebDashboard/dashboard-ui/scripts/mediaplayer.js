(function (document, setTimeout, clearTimeout, screen, localStorage, $, setInterval, window) {

    function mediaPlayer() {

        var self = this;

        var testableVideoElement = document.createElement('video');
        var currentMediaElement;
        var currentProgressInterval;
        var currentItem;
        var currentMediaSource;
        var canClientSeek;
        var currentPlaylistIndex = 0;

        self.currentDurationTicks = null;
        self.startTimeTicksOffset = null;

        self.playlist = [];

        self.isLocalPlayer = true;
        self.isDefaultPlayer = true;
        self.name = 'Html5 Player';

        self.getTargets = function () {

            var targets = [{
                name: 'My Browser',
                id: ApiClient.deviceId(),
                playerName: self.name,
                playableMediaTypes: ['Audio', 'Video'],
                isLocalPlayer: true,
                supportedCommands: Dashboard.getSupportedRemoteCommands()
            }];

            return targets;
        };

        self.updateCanClientSeek = function (elem) {
            var duration = elem.duration;
            canClientSeek = duration && !isNaN(duration) && duration != Number.POSITIVE_INFINITY && duration != Number.NEGATIVE_INFINITY;
        };

        self.getCurrentTicks = function (mediaElement) {
            return Math.floor(10000000 * (mediaElement || currentMediaElement).currentTime) + self.startTimeTicksOffset;
        };

        self.clearPauseStop = function () {

            if (self.pauseStop) {
                console.log('clearing pause stop timer');
                window.clearTimeout(self.pauseStop);
                self.pauseStop = null;
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
            if (canPlayWebm()) {
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

                if (currentItem.MediaType == "Video") {
                    if (params.AudioStreamIndex != null) {
                        currentSrc = replaceQueryString(currentSrc, 'AudioStreamIndex', params.AudioStreamIndex);
                    }
                    if (params.SubtitleStreamIndex != null) {
                        currentSrc = replaceQueryString(currentSrc, 'SubtitleStreamIndex', (params.SubtitleStreamIndex == -1 ? '' : params.SubtitleStreamIndex));
                    }
                }

                var maxWidth = params.MaxWidth || getParameterByName('MaxWidth', currentSrc);
                var audioStreamIndex = params.AudioStreamIndex == null ? getParameterByName('AudioStreamIndex', currentSrc) : params.AudioStreamIndex;
                var subtitleStreamIndex = params.SubtitleStreamIndex == null ? getParameterByName('SubtitleStreamIndex', currentSrc) : params.SubtitleStreamIndex;
                var videoBitrate = parseInt(getParameterByName('VideoBitrate', currentSrc) || '0');
                var audioBitrate = parseInt(getParameterByName('AudioBitrate', currentSrc) || '0');
                var bitrate = params.Bitrate || (videoBitrate + audioBitrate);

                var transcodingExtension = self.getTranscodingExtension();

                var isStatic;
                if (currentItem.MediaType == "Video") {

                    var finalParams = self.getFinalVideoParams(currentMediaSource, maxWidth, bitrate, audioStreamIndex, subtitleStreamIndex, transcodingExtension);

                    currentSrc = replaceQueryString(currentSrc, 'MaxWidth', finalParams.maxWidth);
                    currentSrc = replaceQueryString(currentSrc, 'VideoBitrate', finalParams.videoBitrate);

                    currentSrc = replaceQueryString(currentSrc, 'VideoCodec', finalParams.videoCodec);

                    currentSrc = replaceQueryString(currentSrc, 'profile', finalParams.profile || '');
                    currentSrc = replaceQueryString(currentSrc, 'level', finalParams.level || '');

                    if (finalParams.isStatic) {
                        currentSrc = currentSrc.replace('.webm', '.mp4').replace('.m3u8', '.mp4');
                    } else {
                        currentSrc = currentSrc.replace('.mp4', transcodingExtension).replace('.m4v', transcodingExtension);
                    }

                    currentSrc = replaceQueryString(currentSrc, 'AudioBitrate', finalParams.audioBitrate);
                    currentSrc = replaceQueryString(currentSrc, 'Static', finalParams.isStatic);
                    currentSrc = replaceQueryString(currentSrc, 'AudioCodec', finalParams.audioCodec);
                    isStatic = finalParams.isStatic;
                }

                if (isStatic || !ticks) {
                    currentSrc = replaceQueryString(currentSrc, 'starttimeticks', '');
                } else {
                    currentSrc = replaceQueryString(currentSrc, 'starttimeticks', ticks);
                }

                clearProgressInterval();

                $(element).off('ended.playbackstopped').off('ended.playnext').one("play", function () {

                    self.updateCanClientSeek(this);

                    $(this).on('ended.playbackstopped', self.onPlaybackStopped).on('ended.playnext', self.playNextAfterEnded);

                    self.startProgressInterval(currentItem.Id, currentMediaSource.Id);
                    sendProgressUpdate(currentItem.Id, currentMediaSource.Id);

                });

                if (currentItem.MediaType == "Video") {
                    ApiClient.stopActiveEncodings().done(function () {

                        self.startTimeTicksOffset = ticks;
                        element.src = currentSrc;

                    });
                } else {
                    self.startTimeTicksOffset = ticks;
                    element.src = currentSrc;
                    element.play();
                }
            }
        };

        self.setCurrentTime = function (ticks, positionSlider, currentTimeElement) {

            // Convert to ticks
            ticks = Math.floor(ticks);

            var timeText = Dashboard.getDisplayTime(ticks);

            if (self.currentDurationTicks) {

                timeText += " / " + Dashboard.getDisplayTime(self.currentDurationTicks);

                if (positionSlider) {

                    var percent = ticks / self.currentDurationTicks;
                    percent *= 100;

                    positionSlider.val(percent).slider('enable').slider('refresh');
                }
            } else {

                if (positionSlider) {

                    positionSlider.slider('disable').slider('refresh');
                }
            }

            if (currentTimeElement) {
                currentTimeElement.html(timeText);
            }

            var state = self.getPlayerStateInternal(currentMediaElement, currentItem, currentMediaSource);

            $(self).trigger('positionchange', [state]);
        };

        self.canPlayVideoDirect = function (mediaSource, videoStream, audioStream, subtitleStream, maxWidth, bitrate) {

            if (!mediaSource) {
                throw new Error('Null mediaSource');
            }

            if (!videoStream) {
                return false;
            }

            if (mediaSource.VideoType != "VideoFile") {
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

            if (!mediaSource.Bitrate || mediaSource.Bitrate > bitrate) {
                console.log('Transcoding because bitrate is too high');
                return false;
            }

            var extension = (mediaSource.Container || '').toLowerCase();

            if (extension == 'm4v') {
                return $.browser.chrome;
            }

            return extension == 'mp4';
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

        function translateItemsForPlayback(items) {

            var deferred = $.Deferred();

            var firstItem = items[0];
            var promise;

            if (firstItem.IsFolder) {

                promise = self.getItemsForPlayback({
                    ParentId: firstItem.Id,
                    Filters: "IsNotFolder",
                    Recursive: true,
                    SortBy: "SortName",
                    MediaTypes: "Audio,Video"
                });
            }
            else if (firstItem.Type == "MusicArtist") {

                promise = self.getItemsForPlayback({
                    Artists: firstItem.Name,
                    Filters: "IsNotFolder",
                    Recursive: true,
                    SortBy: "SortName",
                    MediaTypes: "Audio"
                });

            }
            else if (firstItem.Type == "MusicGenre") {

                promise = self.getItemsForPlayback({
                    Genres: firstItem.Name,
                    Filters: "IsNotFolder",
                    Recursive: true,
                    SortBy: "SortName",
                    MediaTypes: "Audio"
                });
            }

            if (promise) {
                promise.done(function (result) {

                    deferred.resolveWith(null, [result.Items]);
                });
            } else {
                deferred.resolveWith(null, [items]);
            }

            return deferred.promise();
        }

        self.play = function (options) {

            Dashboard.getCurrentUser().done(function (user) {

                if (options.items) {

                    translateItemsForPlayback(options.items).done(function (items) {

                        self.playInternal(items[0], options.startPositionTicks, user);

                        self.playlist = items;
                        currentPlaylistIndex = 0;
                    });

                } else {

                    self.getItemsForPlayback({

                        Ids: options.ids.join(',')

                    }).done(function (result) {

                        translateItemsForPlayback(result.Items).done(function (items) {

                            self.playInternal(items[0], options.startPositionTicks, user);

                            self.playlist = items;
                            currentPlaylistIndex = 0;
                        });

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

            if (item.MediaType === "Video") {

                currentItem = item;
                currentMediaSource = getOptimalMediaSource(item.MediaType, item.MediaSources);

                videoPlayer(self, item, currentMediaSource, startPosition, user);
                mediaElement = self.initVideoPlayer();
                self.currentDurationTicks = currentMediaSource.RunTimeTicks;

            } else if (item.MediaType === "Audio") {

                currentItem = item;
                currentMediaSource = getOptimalMediaSource(item.MediaType, item.MediaSources);

                mediaElement = playAudio(item, currentMediaSource, startPosition);

                self.currentDurationTicks = currentMediaSource.RunTimeTicks;

            } else {
                throw new Error("Unrecognized media type");
            }

            currentMediaElement = mediaElement;

            if (item.MediaType === "Video") {

                self.updateNowPlayingInfo(item);
            }
        };

        self.updateNowPlayingInfo = function (item) {

            if (!item) {
                throw new Error('item cannot be null');
            }

            var mediaControls = $("#videoControls");

            var state = self.getPlayerStateInternal(currentMediaElement, item, currentMediaSource);

            var url = "";

            if (state.primaryImageTag) {

                url = ApiClient.getImageUrl(state.primaryImageItemId, {
                    type: "Primary",
                    height: 80,
                    tag: state.primaryImageTag
                });
            }
            else if (state.backdropImageTag) {

                url = ApiClient.getImageUrl(state.backdropItemId, {
                    type: "Backdrop",
                    height: 80,
                    tag: state.backdropImageTag,
                    index: 0
                });

            } else if (state.thumbImageTag) {

                url = ApiClient.getImageUrl(state.thumbImageItemId, {
                    type: "Thumb",
                    height: 80,
                    tag: state.thumbImageTag
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

            var nowPlayingTextElement = $('.nowPlayingText', mediaControls);
            var nameHtml = self.getNowPlayingNameHtml(state);

            if (nameHtml.indexOf('<br/>') != -1) {
                nowPlayingTextElement.addClass('nowPlayingDoubleText');
            } else {
                nowPlayingTextElement.removeClass('nowPlayingDoubleText');
            }

            $('.nowPlayingImage', mediaControls).html('<img src="' + url + '" />');
            nowPlayingTextElement.html(nameHtml);
        };

        self.getNowPlayingNameHtml = function (playerState) {

            var topText = playerState.itemName;

            if (playerState.mediaType == 'Video') {
                if (playerState.indexNumber != null) {
                    topText = playerState.indexNumber + " - " + topText;
                }
                if (playerState.parentIndexNumber != null) {
                    topText = playerState.parentIndexNumber + "." + topText;
                }
            }

            var bottomText = '';

            if (playerState.artists && playerState.artists.length) {
                bottomText = topText;
                topText = playerState.artists[0];
            }
            else if (playerState.seriesName || playerState.album) {
                bottomText = topText;
                topText = playerState.seriesName || playerState.album;
            }
            else if (playerState.productionYear) {
                bottomText = playerState.productionYear;
            }

            return bottomText ? topText + '<br/>' + bottomText : topText;
        };

        self.displayContent = function (options) {
            
            // Handle it the same as a remote control command
            Dashboard.onBrowseCommand({

                ItemName: options.itemName,
                ItemType: options.itemType,
                ItemId: options.itemId,
                Context: options.context

            });
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

                    translateItemsForPlayback(options.items).done(function (items) {

                        self.queueItems(items);
                    });

                } else {

                    self.getItemsForPlayback({

                        Ids: options.ids.join(',')

                    }).done(function (result) {

                        translateItemsForPlayback(result.Items).done(function (items) {

                            self.queueItems(items);
                        });

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

            self.setVolume(0);
        };

        self.unMute = function () {

            self.setVolume(self.getSavedVolume() * 100);
        };

        self.toggleMute = function () {

            if (currentMediaElement) {

                if (currentMediaElement.volume) {
                    self.mute();
                } else {
                    self.unMute();
                }
            }
        };

        self.volumeDown = function () {

            if (currentMediaElement) {
                self.setVolume(Math.max(currentMediaElement.volume - .02, 0) * 100);
            }
        };

        self.volumeUp = function () {

            if (currentMediaElement) {
                self.setVolume(Math.min(currentMediaElement.volume + .02, 1) * 100);
            }
        };

        // Sets volume using a 0-100 scale
        self.setVolume = function (val) {

            if (currentMediaElement) {

                currentMediaElement.volume = val / 100;

                self.onVolumeChanged(currentMediaElement);

                self.saveVolume();
            }
        };

        self.saveVolume = function (val) {

            if (val) {
                localStorage.setItem("volume", val);
            }

        };

        self.getSavedVolume = function () {
            return localStorage.getItem("volume") || 0.5;
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

            var isVideo = currentItem.MediaType == "Video";

            $(elem).off("ended.playnext").on("ended", function () {

                $(this).off();

                if (this.tagName.toLowerCase() != 'audio') {
                    $(this).remove();
                }

                elem.src = "";
                currentMediaElement = null;
                currentItem = null;
                currentMediaSource = null;

            }).trigger("ended");

            if (isVideo) {
                if (self.isFullScreen()) {
                    self.exitFullScreen();
                }
                self.resetEnhancements();
            }
        };

        self.isPlaying = function () {
            return currentMediaElement;
        };

        self.getPlayerState = function () {

            var deferred = $.Deferred();

            var result = self.getPlayerStateInternal(currentMediaElement, currentItem, currentMediaSource);

            deferred.resolveWith(null, [result]);

            return deferred.promise();
        };

        self.getPlayerStateInternal = function (playerElement, item, mediaSource) {

            var state = {};

            if (playerElement) {

                state.volumeLevel = playerElement.volume * 100;
                state.isMuted = playerElement.volume == 0;
                state.isPaused = playerElement.paused;
                state.positionTicks = self.getCurrentTicks(playerElement);
            }

            if (mediaSource) {

                state.mediaSourceId = mediaSource.Id;
                state.runtimeTicks = mediaSource.RunTimeTicks;

                state.canSeek = mediaSource.RunTimeTicks && mediaSource.RunTimeTicks > 0;
            }

            if (item) {

                state.itemId = item.Id;
                state.mediaType = item.MediaType;
                state.itemType = item.Type;
                state.indexNumber = item.IndexNumber;
                state.indexNumberEnd = item.IndexNumberEnd;
                state.parentIndexNumber = item.ParentIndexNumber;
                state.productionYear = item.ProductionYear;
                state.premiereDate = item.PremiereDate;
                state.seriesName = item.SeriesName;
                state.album = item.Album;
                state.itemName = item.Name;
                state.artists = item.Artists;

                var imageTags = item.ImageTags || {};

                if (imageTags.Primary) {

                    state.primaryImageItemId = item.Id;
                    state.primaryImageTag = imageTags.Primary;
                }
                else if (item.AlbumPrimaryImageTag) {

                    state.primaryImageItemId = item.AlbumId;
                    state.primaryImageTag = item.AlbumPrimaryImageTag;
                }
                else if (item.SeriesPrimaryImageTag) {

                    state.primaryImageItemId = item.SeriesId;
                    state.primaryImageTag = item.SeriesPrimaryImageTag;
                }

                if (item.BackdropImageTags && item.BackdropImageTags.length) {

                    state.backdropItemId = item.Id;
                    state.backdropImageTag = item.BackdropImageTags[0];
                }

                if (imageTags.Thumb) {

                    state.thumbItemId = item.Id;
                    state.thumbImageTag = imageTags.Thumb;
                }
            }

            return state;
        };

        self.beginPlayerUpdates = function () {
            // Nothing to setup here
        };

        self.endPlayerUpdates = function () {
            // Nothing to setup here
        };

        self.onPlaybackStart = function (playerElement, item, mediaSource) {

            self.updateCanClientSeek(playerElement);

            ApiClient.reportPlaybackStart(Dashboard.getCurrentUserId(), item.Id, mediaSource.Id, true, item.MediaType);

            self.startProgressInterval(item.Id, mediaSource.Id);

            var state = self.getPlayerStateInternal(playerElement, item, mediaSource);

            $(self).trigger('playbackstart', [state]);
        };

        self.onVolumeChanged = function (playerElement) {

            self.saveVolume(playerElement.volume);

            var state = self.getPlayerStateInternal(playerElement, currentItem, currentMediaSource);

            $(self).trigger('volumechange', [state]);
        };

        self.onPlaybackStopped = function () {

            self.clearPauseStop();

            var playerElement = this;

            $(playerElement).off('ended.playbackstopped');

            var endTime = playerElement.currentTime;

            clearProgressInterval();

            var position = Math.floor(10000000 * endTime) + self.startTimeTicksOffset;

            var item = currentItem;
            var mediaSource = currentMediaSource;

            ApiClient.reportPlaybackStopped(Dashboard.getCurrentUserId(), item.Id, mediaSource.Id, position);

            if (item.MediaType == "Video") {
                ApiClient.stopActiveEncodings();
                if (self.isFullScreen()) {
                    self.exitFullScreen();
                }
                self.resetEnhancements();
            }

            var state = self.getPlayerStateInternal(playerElement, item, mediaSource);

            $(self).trigger('playbackstop', [state]);
        };

        self.onPlaystateChange = function (playerElement) {

            var state = self.getPlayerStateInternal(playerElement, currentItem, currentMediaSource);

            $(self).trigger('playstatechange', [state]);
        };

        $(window).on("beforeunload popstate", function () {

            // Try to report playback stopped before the browser closes
            if (currentItem && currentMediaElement && currentProgressInterval) {

                self.onPlaybackStopped.call(currentMediaElement);
            }
        });

        function replaceQueryString(url, param, value) {
            var re = new RegExp("([?|&])" + param + "=.*?(&|$)", "i");
            if (url.match(re))
                return url.replace(re, '$1' + param + "=" + value + '$2');
            else
                return url + '&' + param + "=" + value;
        }

        function sendProgressUpdate(itemId, mediaSourceId) {

            ApiClient.reportPlaybackProgress(Dashboard.getCurrentUserId(), itemId, mediaSourceId, self.getCurrentTicks(), currentMediaElement.paused, currentMediaElement.volume == 0);
        }

        function clearProgressInterval() {

            if (currentProgressInterval) {
                clearTimeout(currentProgressInterval);
                currentProgressInterval = null;
            }
        };

        function canPlayWebm() {

            return testableVideoElement.canPlayType('video/webm').replace(/no/, '');
        }

        function getAudioElement() {

            var elem = $('.mediaPlayerAudio');

            if (elem.length) {
                return elem;
            }

            var html = '';

            var requiresControls = $.browser.android || ($.browser.webkit && !$.browser.chrome);

            if (requiresControls) {
                html += '<div class="mediaPlayerAudioContainer"><div class="mediaPlayerAudioContainerInner">';;
            } else {
                html += '<div class="mediaPlayerAudioContainer" style="display:none;"><div class="mediaPlayerAudioContainerInner">';;
            }

            html += '<audio class="mediaPlayerAudio" preload="auto" controls>';
            html += '</audio></div></div>';

            $(document.body).append(html);

            return $('.mediaPlayerAudio');
        }

        var supportsAac = document.createElement('audio').canPlayType('audio/aac').replace(/no/, '');
        function playAudio(item, mediaSource, startPositionTicks) {

            startPositionTicks = startPositionTicks || 0;

            var baseParams = {
                audioChannels: 2,
                audioBitrate: 128000,
                StartTimeTicks: startPositionTicks,
                mediaSourceId: mediaSource.Id
            };

            var sourceContainer = (mediaSource.Container || '').toLowerCase();
            var isStatic = false;

            if (sourceContainer == 'mp3' ||
                (sourceContainer == 'aac' && supportsAac)) {

                for (var i = 0, length = mediaSource.MediaStreams.length; i < length; i++) {

                    var stream = mediaSource.MediaStreams[i];

                    if (stream.Type == "Audio") {

                        // Stream statically when possible
                        if (stream.BitRate <= 256000) {
                            isStatic = true;
                        }
                        break;
                    }
                }
            }

            var outputContainer = isStatic ? sourceContainer : 'mp3';
            var audioUrl = ApiClient.getUrl('Audio/' + item.Id + '/stream.' + outputContainer, $.extend({}, baseParams, {
                audioCodec: outputContainer
            }));

            if (isStatic) {
                var seekParam = startPositionTicks ? '#t=' + (startPositionTicks / 10000000) : '';
                audioUrl += "&static=true" + seekParam;
            }

            self.startTimeTicksOffset = isStatic ? 0 : startPositionTicks;

            var initialVolume = self.getSavedVolume();

            return getAudioElement().each(function () {

                this.src = audioUrl;
                this.volume = initialVolume;
                this.play();

            }).on("volumechange", function () {

                self.onVolumeChanged(this);

            }).one("playing", function () {

                $('.mediaPlayerAudioContainer').hide();

                self.onPlaybackStart(this, item, mediaSource);

            }).on("pause", function () {

                self.onPlaystateChange(this);

            }).on("playing", function () {

                self.onPlaystateChange(this);

            }).on("timeupdate", function () {

                self.setCurrentTime(self.getCurrentTicks(this));

            }).on("ended.playbackstopped", self.onPlaybackStopped).on('ended.playnext', self.playNextAfterEnded)[0];
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
        }

        var getItemFields = "MediaSources,Chapters";

        self.getCurrentTargetInfo = function () {
            return self.getTargets()[0];
        };
    }

    window.MediaPlayer = new mediaPlayer();

    window.MediaController.registerPlayer(window.MediaPlayer);
    window.MediaController.setActivePlayer(window.MediaPlayer);


})(document, setTimeout, clearTimeout, screen, localStorage, $, setInterval, window);