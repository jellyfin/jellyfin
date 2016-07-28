define(['appSettings', 'userSettings', 'appStorage', 'datetime'], function (appSettings, userSettings, appStorage, datetime) {

    function mediaPlayer() {

        var self = this;

        var currentProgressInterval;
        var currentPlaylistIndex = -1;

        self.currentMediaRenderer = null;
        self.currentItem = null;
        self.currentMediaSource = null;

        self.currentDurationTicks = null;
        self.startTimeTicksOffset = null;

        self.playlist = [];

        self.isLocalPlayer = true;
        self.isDefaultPlayer = true;
        self.streamInfo = {};

        self.name = 'Html5 Player';

        self.getTargets = function () {

            return new Promise(function (resolve, reject) {

                resolve(self.getTargetsInternal());
            });
        };

        self.getTargetsInternal = function () {

            var targets = [{
                name: Globalize.translate('MyDevice'),
                id: ConnectionManager.deviceId(),
                playerName: self.name,
                playableMediaTypes: ['Audio', 'Video'],
                isLocalPlayer: true,
                supportedCommands: Dashboard.getSupportedRemoteCommands()
            }];

            return targets;
        };

        var supportsTextTracks;
        self.supportsTextTracks = function () {

            if (supportsTextTracks == null) {
                supportsTextTracks = document.createElement('video').textTracks != null;
            }

            // For now, until ready
            return supportsTextTracks;
        };

        // Returns true if the player can seek using native client-side seeking functions
        function canPlayerSeek() {

            var mediaRenderer = self.currentMediaRenderer;
            var currentSrc = (self.getCurrentSrc(mediaRenderer) || '').toLowerCase();

            if (currentSrc.indexOf('.m3u8') != -1) {
                if (currentSrc.indexOf('forcelivestream=true') == -1) {
                    return true;
                }
            } else {
                var duration = mediaRenderer.duration();
                return duration && !isNaN(duration) && duration != Number.POSITIVE_INFINITY && duration != Number.NEGATIVE_INFINITY;
            }
        }

        self.getCurrentSrc = function (mediaRenderer) {
            return mediaRenderer.currentSrc();
        };

        self.getCurrentTicks = function (mediaRenderer) {

            var playerTime = Math.floor(10000 * (mediaRenderer || self.currentMediaRenderer).currentTime());

            playerTime += self.startTimeTicksOffset;

            return playerTime;
        };

        self.playNextAfterEnded = function () {

            console.log('playNextAfterEnded');

            Events.off(this, 'ended', self.playNextAfterEnded);

            self.nextTrack();
        };

        self.startProgressInterval = function () {

            clearProgressInterval();

            var intervalTime = ApiClient.isWebSocketOpen() ? 1200 : 5000;
            // Ease up with safari because it doesn't perform as well
            if (browserInfo.safari) {
                intervalTime = Math.max(intervalTime, 5000);
            }
            self.lastProgressReport = 0;

            currentProgressInterval = setInterval(function () {

                if (self.currentMediaRenderer) {

                    if ((new Date().getTime() - self.lastProgressReport) > intervalTime) {

                        self.lastProgressReport = new Date().getTime();
                        sendProgressUpdate();
                    }
                }

            }, 250);
        };

        self.getCurrentMediaExtension = function (currentSrc) {
            currentSrc = currentSrc.split('?')[0];

            return currentSrc.substring(currentSrc.lastIndexOf('.'));
        };

        self.canPlayNativeHls = function () {

            if (AppInfo.isNativeApp) {
                return true;
            }

            var media = document.createElement('video');

            if (media.canPlayType('application/x-mpegURL').replace(/no/, '') ||
                media.canPlayType('application/vnd.apple.mpegURL').replace(/no/, '')) {
                return true;
            }

            return false;
        };

        self.canPlayHls = function () {

            if (self.canPlayNativeHls()) {
                return true;
            }

            // viblast can help us here
            //return true;
            return window.MediaSource && !browserInfo.firefox;
        };

        self.changeStream = function (ticks, params) {

            var mediaRenderer = self.currentMediaRenderer;

            if (canPlayerSeek() && params == null) {

                mediaRenderer.currentTime(ticks / 10000);
                return;
            }

            params = params || {};

            var currentSrc = mediaRenderer.currentSrc();

            var playSessionId = getParameterByName('PlaySessionId', currentSrc);
            var liveStreamId = getParameterByName('LiveStreamId', currentSrc);

            Dashboard.getDeviceProfile().then(function (deviceProfile) {

                var audioStreamIndex = params.AudioStreamIndex == null ? (getParameterByName('AudioStreamIndex', currentSrc) || null) : params.AudioStreamIndex;
                if (typeof (audioStreamIndex) == 'string') {
                    audioStreamIndex = parseInt(audioStreamIndex);
                }

                var subtitleStreamIndex = params.SubtitleStreamIndex == null ? (getParameterByName('SubtitleStreamIndex', currentSrc) || null) : params.SubtitleStreamIndex;
                if (typeof (subtitleStreamIndex) == 'string') {
                    subtitleStreamIndex = parseInt(subtitleStreamIndex);
                }

                MediaController.getPlaybackInfo(self.currentItem.Id, deviceProfile, ticks, self.currentMediaSource, audioStreamIndex, subtitleStreamIndex, liveStreamId).then(function (result) {

                    if (validatePlaybackInfoResult(result)) {

                        self.currentMediaSource = result.MediaSources[0];
                        self.createStreamInfo(self.currentItem.MediaType, self.currentItem, self.currentMediaSource, ticks).then(function (streamInfo) {

                            if (!streamInfo.url) {
                                MediaController.showPlaybackInfoErrorMessage('NoCompatibleStream');
                                self.stop();
                                return;
                            }

                            self.currentSubtitleStreamIndex = subtitleStreamIndex;

                            changeStreamToUrl(mediaRenderer, playSessionId, streamInfo);
                        });
                    }
                });
            });
        };

        function changeStreamToUrl(mediaRenderer, playSessionId, streamInfo) {

            clearProgressInterval();

            Events.off(mediaRenderer, 'ended', self.onPlaybackStopped);
            Events.off(mediaRenderer, 'ended', self.playNextAfterEnded);

            function onPlayingOnce() {

                Events.off(mediaRenderer, "play", onPlayingOnce);
                Events.on(mediaRenderer, 'ended', self.onPlaybackStopped);

                Events.on(mediaRenderer, 'ended', self.playNextAfterEnded);

                self.startProgressInterval();
                sendProgressUpdate();
            }

            Events.on(mediaRenderer, "play", onPlayingOnce);

            if (self.currentItem.MediaType == "Video") {
                ApiClient.stopActiveEncodings(playSessionId).then(function () {

                    self.setSrcIntoRenderer(mediaRenderer, streamInfo, self.currentItem, self.currentMediaSource);
                });

            } else {
                self.setSrcIntoRenderer(mediaRenderer, streamInfo, self.currentItem, self.currentMediaSource);
            }
        }

        self.setSrcIntoRenderer = function (mediaRenderer, streamInfo, item, mediaSource) {

            var subtitleStreams = mediaSource.MediaStreams.filter(function (s) {
                return s.Type == 'Subtitle';
            });

            var textStreams = subtitleStreams.filter(function (s) {
                return s.DeliveryMethod == 'External';
            });

            var tracks = [];

            for (var i = 0, length = textStreams.length; i < length; i++) {

                var textStream = textStreams[i];
                var textStreamUrl = !textStream.IsExternalUrl ? ApiClient.getUrl(textStream.DeliveryUrl) : textStream.DeliveryUrl;

                tracks.push({
                    url: textStreamUrl,
                    language: (textStream.Language || 'und'),
                    isDefault: textStream.Index == mediaSource.DefaultSubtitleStreamIndex,
                    index: textStream.Index,
                    format: textStream.Codec
                });
            }

            self.startTimeTicksOffset = streamInfo.startTimeTicksOffset || 0;

            mediaRenderer.setCurrentSrc(streamInfo, item, mediaSource, tracks);
            self.streamInfo = streamInfo;
            //self.updateTextStreamUrls(streamInfo.startTimeTicksOffset || 0);
        };

        self.setCurrentTime = function (ticks, positionSlider, currentTimeElement) {

            // Convert to ticks
            ticks = Math.floor(ticks);

            var timeText = datetime.getDisplayRunningTime(ticks);
            var mediaRenderer = self.currentMediaRenderer;

            if (self.currentDurationTicks) {

                timeText += " / " + datetime.getDisplayRunningTime(self.currentDurationTicks);

                if (positionSlider) {

                    var percent = ticks / self.currentDurationTicks;
                    percent *= 100;

                    positionSlider.value = percent;
                }
            }

            if (positionSlider) {

                positionSlider.disabled = !((self.currentDurationTicks || 0) > 0 || canPlayerSeek());
            }

            if (currentTimeElement) {
                currentTimeElement.innerHTML = timeText;
            }

            var state = self.getPlayerStateInternal(mediaRenderer, self.currentItem, self.currentMediaSource);

            Events.trigger(self, 'positionchange', [state]);
        };

        self.canQueueMediaType = function (mediaType) {

            return self.currentItem && self.currentItem.MediaType == mediaType;
        };

        function translateItemsForPlayback(items, smart) {

            var firstItem = items[0];
            var promise;

            if (firstItem.Type == "Playlist") {

                promise = self.getItemsForPlayback({
                    ParentId: firstItem.Id,
                });
            }
            else if (firstItem.Type == "MusicArtist") {

                promise = self.getItemsForPlayback({
                    ArtistIds: firstItem.Id,
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
            else if (firstItem.IsFolder) {

                promise = self.getItemsForPlayback({
                    ParentId: firstItem.Id,
                    Filters: "IsNotFolder",
                    Recursive: true,
                    SortBy: "SortName",
                    MediaTypes: "Audio,Video"
                });
            }
            else if (smart && firstItem.Type == "Episode" && items.length == 1) {

                promise = ApiClient.getCurrentUser().then(function (user) {

                    if (!user.Configuration.EnableNextEpisodeAutoPlay || !firstItem.SeriesId) {
                        return null;
                    }

                    return ApiClient.getEpisodes(firstItem.SeriesId, {
                        IsVirtualUnaired: false,
                        IsMissing: false,
                        UserId: ApiClient.getCurrentUserId(),
                        Fields: getItemFields

                    }).then(function (episodesResult) {

                        var foundItem = false;
                        episodesResult.Items = episodesResult.Items.filter(function (e) {

                            if (foundItem) {
                                return true;
                            }
                            if (e.Id == firstItem.Id) {
                                foundItem = true;
                                return true;
                            }

                            return false;
                        });
                        episodesResult.TotalRecordCount = episodesResult.Items.length;
                        return episodesResult;
                    });
                });
            }

            if (promise) {
                return promise.then(function (result) {

                    return result ? result.Items : items;
                });
            } else {

                return Promise.resolve(items);
            }
        }

        self.play = function (options) {

            Dashboard.showLoadingMsg();

            Dashboard.getCurrentUser().then(function (user) {

                if (options.items) {

                    translateItemsForPlayback(options.items, true).then(function (items) {

                        self.playWithIntros(items, options, user);
                    });

                } else {

                    self.getItemsForPlayback({

                        Ids: options.ids.join(',')

                    }).then(function (result) {

                        translateItemsForPlayback(result.Items, true).then(function (items) {

                            self.playWithIntros(items, options, user);
                        });

                    });
                }

            });

        };

        self.playWithIntros = function (items, options, user) {

            var firstItem = items[0];

            if (firstItem.MediaType === "Video") {

                Dashboard.showLoadingMsg();
            }

            if (options.startPositionTicks || firstItem.MediaType !== 'Video' || !userSettings.enableCinemaMode()) {

                self.playInternal(firstItem, options.startPositionTicks, function () {
                    self.setPlaylistState(0, items);
                });

                return;
            }

            ApiClient.getJSON(ApiClient.getUrl('Users/' + user.Id + '/Items/' + firstItem.Id + '/Intros')).then(function (intros) {

                items = intros.Items.concat(items);
                self.playInternal(items[0], options.startPositionTicks, function () {
                    self.setPlaylistState(0, items);
                });

            });
        };

        function getOptimalMediaSource(mediaType, versions) {

            var promises = versions.map(function (v) {
                return MediaController.supportsDirectPlay(v);
            });

            return Promise.all(promises).then(function (responses) {

                for (var i = 0, length = versions.length; i < length; i++) {
                    versions[i].enableDirectPlay = responses[i] || false;
                }
                var optimalVersion = versions.filter(function (v) {

                    return v.enableDirectPlay;

                })[0];

                if (!optimalVersion) {
                    optimalVersion = versions.filter(function (v) {

                        return v.SupportsDirectStream;

                    })[0];
                }

                optimalVersion = optimalVersion || versions.filter(function (s) {
                    return s.SupportsTranscoding;
                })[0];

                return optimalVersion;
            });
        }

        self.createStreamInfo = function (type, item, mediaSource, startPosition) {

            return new Promise(function (resolve, reject) {

                var mediaUrl;
                var contentType;
                var startTimeTicksOffset = 0;

                var startPositionInSeekParam = startPosition ? (startPosition / 10000000) : 0;
                var seekParam = startPositionInSeekParam ? '#t=' + startPositionInSeekParam : '';
                var playMethod = 'Transcode';

                if (type == 'Video') {

                    contentType = 'video/' + mediaSource.Container;

                    if (mediaSource.enableDirectPlay) {
                        mediaUrl = mediaSource.Path;

                        playMethod = 'DirectPlay';

                    } else {

                        if (mediaSource.SupportsDirectStream) {

                            var directOptions = {
                                Static: true,
                                mediaSourceId: mediaSource.Id,
                                deviceId: ApiClient.deviceId(),
                                api_key: ApiClient.accessToken()
                            };

                            if (mediaSource.LiveStreamId) {
                                directOptions.LiveStreamId = mediaSource.LiveStreamId;
                            }

                            mediaUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.' + mediaSource.Container, directOptions);
                            mediaUrl += seekParam;

                            playMethod = 'DirectStream';
                        } else if (mediaSource.SupportsTranscoding) {

                            mediaUrl = ApiClient.getUrl(mediaSource.TranscodingUrl);

                            if (mediaSource.TranscodingSubProtocol == 'hls') {

                                if (mediaUrl.toLowerCase().indexOf('forcelivestream=true') != -1) {
                                    startPositionInSeekParam = 0;
                                    startTimeTicksOffset = startPosition || 0;
                                }

                                contentType = 'application/x-mpegURL';

                            } else {

                                if (mediaUrl.toLowerCase().indexOf('copytimestamps=true') == -1) {
                                    startPositionInSeekParam = 0;
                                    startTimeTicksOffset = startPosition || 0;
                                }

                                contentType = 'video/' + mediaSource.TranscodingContainer;
                            }
                        }
                    }

                } else {

                    contentType = 'audio/' + mediaSource.Container;

                    if (mediaSource.enableDirectPlay) {

                        mediaUrl = mediaSource.Path;

                        playMethod = 'DirectPlay';

                    } else {

                        var isDirectStream = mediaSource.SupportsDirectStream;

                        if (isDirectStream) {

                            var outputContainer = (mediaSource.Container || '').toLowerCase();

                            var directOptions = {
                                Static: true,
                                mediaSourceId: mediaSource.Id,
                                deviceId: ApiClient.deviceId(),
                                api_key: ApiClient.accessToken()
                            };

                            if (mediaSource.LiveStreamId) {
                                directOptions.LiveStreamId = mediaSource.LiveStreamId;
                            }

                            mediaUrl = ApiClient.getUrl('Audio/' + item.Id + '/stream.' + outputContainer, directOptions);
                            mediaUrl += seekParam;

                            playMethod = 'DirectStream';

                        } else if (mediaSource.SupportsTranscoding) {

                            mediaUrl = ApiClient.getUrl(mediaSource.TranscodingUrl);

                            if (mediaSource.TranscodingSubProtocol == 'hls') {

                                mediaUrl += seekParam;
                                contentType = 'application/x-mpegURL';
                            } else {

                                startTimeTicksOffset = startPosition || 0;
                                contentType = 'audio/' + mediaSource.TranscodingContainer;
                            }
                        }
                    }
                }

                var resultInfo = {
                    url: mediaUrl,
                    mimeType: contentType,
                    startTimeTicksOffset: startTimeTicksOffset,
                    startPositionInSeekParam: startPositionInSeekParam,
                    playMethod: playMethod
                };

                if (playMethod == 'DirectPlay' && mediaSource.Protocol == 'File') {

                    require(['localassetmanager'], function () {

                        LocalAssetManager.translateFilePath(resultInfo.url).then(function (path) {

                            resultInfo.url = path;
                            console.log('LocalAssetManager.translateFilePath: path: ' + resultInfo.url + ' result: ' + path);
                            resolve(resultInfo);
                        });
                    });

                }
                else {
                    resolve(resultInfo);
                }
            });
        };

        self.lastBitrateDetections = {};

        self.playInternal = function (item, startPosition, callback) {

            if (item == null) {
                throw new Error("item cannot be null");
            }

            if (self.isPlaying()) {
                self.stop(false);
            }

            if (item.MediaType !== 'Audio' && item.MediaType !== 'Video') {
                throw new Error("Unrecognized media type");
            }

            if (item.IsPlaceHolder) {
                Dashboard.hideLoadingMsg();
                MediaController.showPlaybackInfoErrorMessage('PlaceHolder');
                return;
            }

            var onBitrateDetected = function () {
                Dashboard.getDeviceProfile().then(function (deviceProfile) {
                    playOnDeviceProfileCreated(deviceProfile, item, startPosition, callback);
                });
            };

            var bitrateDetectionKey = ApiClient.serverAddress();

            if (item.MediaType == 'Video' && appSettings.enableAutomaticBitrateDetection() && (new Date().getTime() - (self.lastBitrateDetections[bitrateDetectionKey] || 0)) > 300000) {

                Dashboard.showLoadingMsg();

                ApiClient.detectBitrate().then(function (bitrate) {
                    console.log('Max bitrate auto detected to ' + bitrate);
                    self.lastBitrateDetections[bitrateDetectionKey] = new Date().getTime();
                    appSettings.maxStreamingBitrate(bitrate);

                    onBitrateDetected();

                }, onBitrateDetected);

            } else {
                onBitrateDetected();
            }
        };

        self.tryStartPlayback = function (deviceProfile, item, startPosition, callback) {

            if (item.MediaType === "Video") {

                Dashboard.showLoadingMsg();
            }

            MediaController.getPlaybackInfo(item.Id, deviceProfile, startPosition).then(function (playbackInfoResult) {

                if (validatePlaybackInfoResult(playbackInfoResult)) {

                    getOptimalMediaSource(item.MediaType, playbackInfoResult.MediaSources).then(function (mediaSource) {
                        if (mediaSource) {

                            if (mediaSource.RequiresOpening) {

                                MediaController.getLiveStream(item.Id, playbackInfoResult.PlaySessionId, deviceProfile, startPosition, mediaSource, null, null).then(function (openLiveStreamResult) {

                                    MediaController.supportsDirectPlay(openLiveStreamResult.MediaSource).then(function (result) {

                                        openLiveStreamResult.MediaSource.enableDirectPlay = result;
                                        callback(openLiveStreamResult.MediaSource);
                                    });

                                });

                            } else {
                                callback(mediaSource);
                            }
                        } else {
                            Dashboard.hideLoadingMsg();
                            MediaController.showPlaybackInfoErrorMessage('NoCompatibleStream');
                        }
                    });
                }
            });
        };

        function playOnDeviceProfileCreated(deviceProfile, item, startPosition, callback) {

            self.tryStartPlayback(deviceProfile, item, startPosition, function (mediaSource) {

                playInternalPostMediaSourceSelection(item, mediaSource, startPosition, callback);
            });
        }

        function playInternalPostMediaSourceSelection(item, mediaSource, startPosition, callback) {

            Dashboard.hideLoadingMsg();

            self.currentMediaSource = mediaSource;
            self.currentItem = item;

            if (item.MediaType === "Video") {

                requirejs(['videorenderer', 'scripts/mediaplayer-video'], function () {
                    self.playVideo(item, self.currentMediaSource, startPosition, callback);
                });

            } else if (item.MediaType === "Audio") {

                playAudio(item, self.currentMediaSource, startPosition, callback);
            }
        }

        function validatePlaybackInfoResult(result) {

            if (result.ErrorCode) {

                MediaController.showPlaybackInfoErrorMessage(result.ErrorCode);
                return false;
            }

            return true;
        }

        self.getPosterUrl = function (item) {

            var screenWidth = Math.max(screen.height, screen.width);

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

        self.displayContent = function (cmd) {

            var apiClient = ApiClient;
            apiClient.getItem(apiClient.getCurrentUserId(), cmd.ItemId).then(function (item) {
                require(['embyRouter'], function (embyRouter) {
                    embyRouter.showItem(item);
                });
            });
        };

        self.getItemsForPlayback = function (query) {

            var userId = Dashboard.getCurrentUserId();

            if (query.Ids && query.Ids.split(',').length == 1) {

                return new Promise(function (resolve, reject) {

                    ApiClient.getItem(userId, query.Ids.split(',')).then(function (item) {
                        resolve({
                            Items: [item],
                            TotalRecordCount: 1
                        });
                    });
                });
            }
            else {

                query.Limit = query.Limit || 100;
                query.Fields = getItemFields;
                query.ExcludeLocationTypes = "Virtual";

                return ApiClient.getItems(userId, query);
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

            self.playInternal(newItem, 0, function () {
                self.setPlaylistState(i);
            });
        };

        // Set currentPlaylistIndex and playlist. Using a method allows for overloading in derived player implementations
        self.setPlaylistState = function (i, items) {
            if (!isNaN(i)) {
                currentPlaylistIndex = i;
            }
            if (items) {
                self.playlist = items;
            }
            if (self.updatePlaylistUi) {
                self.updatePlaylistUi();
            }
        };

        self.nextTrack = function () {

            var newIndex;

            switch (self.getRepeatMode()) {

                case 'RepeatOne':
                    newIndex = currentPlaylistIndex;
                    break;
                case 'RepeatAll':
                    newIndex = currentPlaylistIndex + 1;
                    if (newIndex >= self.playlist.length) {
                        newIndex = 0;
                    }
                    break;
                default:
                    newIndex = currentPlaylistIndex + 1;
                    break;
            }

            var newItem = self.playlist[newIndex];

            if (newItem) {

                console.log('playing next track');

                self.playInternal(newItem, 0, function () {
                    self.setPlaylistState(newIndex);
                });
            }
        };

        self.previousTrack = function () {
            var newIndex = currentPlaylistIndex - 1;
            if (newIndex >= 0) {
                var newItem = self.playlist[newIndex];

                if (newItem) {
                    self.playInternal(newItem, 0, function () {
                        self.setPlaylistState(newIndex);
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

            if (!self.playlist.length) {
                self.play(options);
                return;
            }

            Dashboard.getCurrentUser().then(function (user) {

                if (options.items) {

                    translateItemsForPlayback(options.items).then(function (items) {

                        self.queueItems(items);
                    });

                } else {

                    self.getItemsForPlayback({

                        Ids: options.ids.join(',')

                    }).then(function (result) {

                        translateItemsForPlayback(result.Items).then(function (items) {

                            self.queueItems(items);
                        });

                    });
                }
            });
        };

        self.queueNext = function (options) {

            if (!self.playlist.length) {
                self.play(options);
                return;
            }

            Dashboard.getCurrentUser().then(function (user) {

                if (options.items) {

                    self.queueItemsNext(options.items);

                } else {

                    self.getItemsForPlayback({

                        Ids: options.ids.join(',')

                    }).then(function (result) {

                        options.items = result.Items;

                        self.queueItemsNext(options.items);

                    });
                }

            });
        };

        self.pause = function () {

            self.currentMediaRenderer.pause();
        };

        self.unpause = function () {
            self.currentMediaRenderer.unpause();
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

        self.volume = function () {
            return self.currentMediaRenderer.volume() * 100;
        };

        self.toggleMute = function () {

            if (self.currentMediaRenderer) {

                console.log('MediaPlayer toggling mute');

                if (self.volume()) {
                    self.mute();
                } else {
                    self.unMute();
                }
            }
        };

        self.volumeDown = function () {

            if (self.currentMediaRenderer) {
                self.setVolume(Math.max(self.volume() - 2, 0));
            }
        };

        self.volumeUp = function () {

            if (self.currentMediaRenderer) {
                self.setVolume(Math.min(self.volume() + 2, 100));
            }
        };

        // Sets volume using a 0-100 scale
        self.setVolume = function (val) {

            if (self.currentMediaRenderer) {

                console.log('MediaPlayer setting volume to ' + val);
                self.currentMediaRenderer.volume(val / 100);

                self.onVolumeChanged(self.currentMediaRenderer);

                //self.saveVolume();
            }
        };

        self.saveVolume = function (val) {

            if (val) {
                appStorage.setItem("volume", val);
            }

        };

        self.getSavedVolume = function () {
            return appStorage.getItem("volume") || 0.5;
        };

        self.shuffle = function (id) {

            var userId = Dashboard.getCurrentUserId();

            ApiClient.getItem(userId, id).then(function (item) {

                var query = {
                    UserId: userId,
                    Fields: getItemFields,
                    Limit: 100,
                    Filters: "IsNotFolder",
                    Recursive: true,
                    SortBy: "Random"
                };

                if (item.Type == "MusicArtist") {

                    query.MediaTypes = "Audio";
                    query.ArtistIds = item.Id;

                }
                else if (item.Type == "MusicGenre") {

                    query.MediaTypes = "Audio";
                    query.Genres = item.Name;

                }
                else if (item.IsFolder) {
                    query.ParentId = id;

                }
                else {
                    return;
                }

                self.getItemsForPlayback(query).then(function (result) {

                    self.play({ items: result.Items });

                });

            });

        };

        self.instantMix = function (id) {

            var itemLimit = 100;

            ApiClient.getInstantMixFromItem(id, {
                UserId: Dashboard.getCurrentUserId(),
                Fields: getItemFields,
                Limit: itemLimit

            }).then(function (result) {

                self.play({ items: result.Items });

            });
        };

        self.stop = function (destroyRenderer) {

            var mediaRenderer = self.currentMediaRenderer;

            if (mediaRenderer) {

                Events.off(mediaRenderer, 'ended', self.playNextAfterEnded);

                var stopTranscoding = false;
                if (!currentProgressInterval) {
                    stopTranscoding = true;
                }

                mediaRenderer.stop();

                Events.trigger(mediaRenderer, "ended");
                //self.onPlaybackStopped.call(mediaRenderer);

                // TODO: Unbind video events
                unBindAudioEvents(mediaRenderer);

                mediaRenderer.cleanup(destroyRenderer);

                self.currentMediaRenderer = null;
                self.currentItem = null;

                self.currentSubtitleStreamIndex = null;
                self.streamInfo = {};

                self.currentMediaSource = null;

                if (stopTranscoding) {
                    ApiClient.stopActiveEncodings();
                }


            } else {
                self.currentMediaRenderer = null;
                self.currentItem = null;
                self.currentMediaSource = null;
                self.currentSubtitleStreamIndex = null;
                self.streamInfo = {};
            }

            if (self.resetEnhancements) {
                self.resetEnhancements();
            }
        };

        function unBindAudioEvents(mediaRenderer) {

            Events.off(mediaRenderer, "volumechange", onVolumeChange);
            Events.off(mediaRenderer, "pause", onPause);
            Events.off(mediaRenderer, "playing", onPlaying);
            Events.off(mediaRenderer, "timeupdate", onTimeUpdate);
        }

        self.isPlaying = function () {
            return self.playlist.length > 0;
        };

        self.getPlayerState = function () {

            return new Promise(function (resolve, reject) {

                var result = self.getPlayerStateInternal(self.currentMediaRenderer, self.currentItem, self.currentMediaSource);
                resolve(result);
            });
        };

        self.getPlayerStateInternal = function (mediaRenderer, item, mediaSource) {

            var state = {
                PlayState: {}
            };

            if (mediaRenderer) {

                state.PlayState.VolumeLevel = mediaRenderer.volume() * 100;
                state.PlayState.IsMuted = mediaRenderer.volume() == 0;
                state.PlayState.IsPaused = mediaRenderer.paused();
                state.PlayState.PositionTicks = self.getCurrentTicks(mediaRenderer);
                state.PlayState.RepeatMode = self.getRepeatMode();

                var currentSrc = mediaRenderer.currentSrc();

                if (currentSrc) {

                    var audioStreamIndex = getParameterByName('AudioStreamIndex', currentSrc);

                    if (audioStreamIndex) {
                        state.PlayState.AudioStreamIndex = parseInt(audioStreamIndex);
                    }
                    state.PlayState.SubtitleStreamIndex = self.currentSubtitleStreamIndex;

                    state.PlayState.PlayMethod = self.streamInfo.playMethod;

                    state.PlayState.PlaySessionId = getParameterByName('PlaySessionId', currentSrc);
                }
            }

            if (mediaSource) {

                state.PlayState.MediaSourceId = mediaSource.Id;
                state.PlayState.LiveStreamId = mediaSource.LiveStreamId;

                state.NowPlayingItem = {
                    RunTimeTicks: mediaSource.RunTimeTicks
                };

                state.PlayState.CanSeek = (mediaSource.RunTimeTicks || 0) > 0 || canPlayerSeek();
            }

            if (item) {

                state.NowPlayingItem = self.getNowPlayingItemForReporting(item, mediaSource);
            }

            return state;
        };

        self.getNowPlayingItemForReporting = function (item, mediaSource) {

            var nowPlayingItem = {};

            nowPlayingItem.RunTimeTicks = mediaSource.RunTimeTicks;

            nowPlayingItem.Id = item.Id;
            nowPlayingItem.MediaType = item.MediaType;
            nowPlayingItem.Type = item.Type;
            nowPlayingItem.Name = item.Name;

            nowPlayingItem.IndexNumber = item.IndexNumber;
            nowPlayingItem.IndexNumberEnd = item.IndexNumberEnd;
            nowPlayingItem.ParentIndexNumber = item.ParentIndexNumber;
            nowPlayingItem.ProductionYear = item.ProductionYear;
            nowPlayingItem.PremiereDate = item.PremiereDate;
            nowPlayingItem.SeriesName = item.SeriesName;
            nowPlayingItem.Album = item.Album;
            nowPlayingItem.AlbumId = item.AlbumId;
            nowPlayingItem.Artists = item.Artists;
            nowPlayingItem.ArtistItems = item.ArtistItems;

            var imageTags = item.ImageTags || {};

            if (item.SeriesPrimaryImageTag) {

                nowPlayingItem.PrimaryImageItemId = item.SeriesId;
                nowPlayingItem.PrimaryImageTag = item.SeriesPrimaryImageTag;
            }
            else if (imageTags.Primary) {

                nowPlayingItem.PrimaryImageItemId = item.Id;
                nowPlayingItem.PrimaryImageTag = imageTags.Primary;
            }
            else if (item.AlbumPrimaryImageTag) {

                nowPlayingItem.PrimaryImageItemId = item.AlbumId;
                nowPlayingItem.PrimaryImageTag = item.AlbumPrimaryImageTag;
            }
            else if (item.SeriesPrimaryImageTag) {

                nowPlayingItem.PrimaryImageItemId = item.SeriesId;
                nowPlayingItem.PrimaryImageTag = item.SeriesPrimaryImageTag;
            }

            if (item.BackdropImageTags && item.BackdropImageTags.length) {

                nowPlayingItem.BackdropItemId = item.Id;
                nowPlayingItem.BackdropImageTag = item.BackdropImageTags[0];
            }
            else if (item.ParentBackdropImageTags && item.ParentBackdropImageTags.length) {
                nowPlayingItem.BackdropItemId = item.ParentBackdropItemId;
                nowPlayingItem.BackdropImageTag = item.ParentBackdropImageTags[0];
            }

            if (imageTags.Thumb) {

                nowPlayingItem.ThumbItemId = item.Id;
                nowPlayingItem.ThumbImageTag = imageTags.Thumb;
            }

            if (imageTags.Logo) {

                nowPlayingItem.LogoItemId = item.Id;
                nowPlayingItem.LogoImageTag = imageTags.Logo;
            }
            else if (item.ParentLogoImageTag) {

                nowPlayingItem.LogoItemId = item.ParentLogoItemId;
                nowPlayingItem.LogoImageTag = item.ParentLogoImageTag;
            }

            return nowPlayingItem;
        };

        self.beginPlayerUpdates = function () {
            // Nothing to setup here
        };

        self.endPlayerUpdates = function () {
            // Nothing to setup here
        };

        self.onBeforePlaybackStart = function (mediaRenderer, item, mediaSource) {

            var state = self.getPlayerStateInternal(mediaRenderer, item, mediaSource);

            Events.trigger(self, 'beforeplaybackstart', [state]);
        };

        self.onPlaybackStart = function (mediaRenderer, item, mediaSource) {

            var state = self.getPlayerStateInternal(mediaRenderer, item, mediaSource);

            Events.trigger(self, 'playbackstart', [state]);

            self.startProgressInterval();
        };

        self.onVolumeChanged = function (mediaRenderer) {

            self.saveVolume(mediaRenderer.volume());

            var state = self.getPlayerStateInternal(mediaRenderer, self.currentItem, self.currentMediaSource);

            Events.trigger(self, 'volumechange', [state]);
        };

        self.cleanup = function () {

        };

        self.onPlaybackStopped = function () {

            console.log('playback stopped');

            document.body.classList.remove('bodyWithPopupOpen');

            var mediaRenderer = this;

            // TODO: Unbind other events
            unBindAudioEvents(mediaRenderer);
            Events.off(mediaRenderer, 'ended', self.onPlaybackStopped);

            var item = self.currentItem;
            var mediaSource = self.currentMediaSource;

            var state = self.getPlayerStateInternal(mediaRenderer, item, mediaSource);

            self.cleanup(mediaRenderer);

            clearProgressInterval();

            if (item.MediaType == "Video") {

                self.resetEnhancements();
            }

            Events.trigger(self, 'playbackstop', [state]);
        };

        self.onPlaystateChange = function (mediaRenderer) {

            console.log('mediaplayer onPlaystateChange');

            var state = self.getPlayerStateInternal(mediaRenderer, self.currentItem, self.currentMediaSource);

            Events.trigger(self, 'playstatechange', [state]);
        };

        function onAppClose() {
            // Try to report playback stopped before the browser closes
            if (self.currentItem && self.currentMediaRenderer) {

                if (currentProgressInterval) {

                    self.onPlaybackStopped.call(self.currentMediaRenderer);
                } else {
                    ApiClient.stopActiveEncodings();
                }
            }
        }

        window.addEventListener("beforeunload", onAppClose);

        //if (browserInfo.safari) {
        //    document.addEventListener("pause", onAppClose);
        //}

        function sendProgressUpdate() {

            var mediaRenderer = self.currentMediaRenderer;

            if (mediaRenderer.enableProgressReporting === false) {
                return;
            }

            var state = self.getPlayerStateInternal(mediaRenderer, self.currentItem, self.currentMediaSource);

            var info = {
                QueueableMediaTypes: state.NowPlayingItem.MediaType,
                ItemId: state.NowPlayingItem.Id,
                NowPlayingItem: state.NowPlayingItem
            };

            info = Object.assign(info, state.PlayState);
            //console.log(JSON.stringify(info));
            ApiClient.reportPlaybackProgress(info);
        }

        function clearProgressInterval() {

            if (currentProgressInterval) {
                clearTimeout(currentProgressInterval);
                currentProgressInterval = null;
            }
        }

        self.canAutoPlayAudio = function () {

            if (AppInfo.isNativeApp) {
                return true;
            }

            if (browserInfo.mobile) {
                return false;
            }

            return true;
        };

        var repeatMode = 'RepeatNone';
        self.getRepeatMode = function () {
            return repeatMode;
        };

        self.setRepeatMode = function (mode) {
            repeatMode = mode;
        };

        function onTimeUpdate() {

            var currentTicks = self.getCurrentTicks(this);
            self.setCurrentTime(currentTicks);
        }

        function playAudio(item, mediaSource, startPositionTicks, callback) {

            requirejs(['audiorenderer'], function () {
                playAudioInternal(item, mediaSource, startPositionTicks);

                if (callback) {
                    callback();
                }
            });
        }

        function playAudioInternal(item, mediaSource, startPositionTicks) {

            self.createStreamInfo('Audio', item, mediaSource, startPositionTicks).then(function (streamInfo) {

                self.startTimeTicksOffset = streamInfo.startTimeTicksOffset;

                var initialVolume = self.getSavedVolume();

                var mediaRenderer = new AudioRenderer({
                    poster: self.getPosterUrl(item)
                });

                function onPlayingOnce() {

                    Events.off(mediaRenderer, "playing", onPlayingOnce);

                    console.log('audio element event: playing');

                    // For some reason this is firing at the start, so don't bind until playback has begun
                    Events.on(mediaRenderer, 'ended', self.onPlaybackStopped);
                    Events.on(mediaRenderer, 'ended', self.playNextAfterEnded);

                    self.onPlaybackStart(mediaRenderer, item, mediaSource);
                }

                Events.on(mediaRenderer, "volumechange", onVolumeChange);
                Events.on(mediaRenderer, "playing", onPlayingOnce);
                Events.on(mediaRenderer, "pause", onPause);
                Events.on(mediaRenderer, "playing", onPlaying);
                Events.on(mediaRenderer, "timeupdate", onTimeUpdate);

                self.currentMediaRenderer = mediaRenderer;
                self.currentDurationTicks = self.currentMediaSource.RunTimeTicks;

                mediaRenderer.init().then(function () {

                    // Set volume first to avoid an audible change
                    mediaRenderer.volume(initialVolume);

                    self.onBeforePlaybackStart(mediaRenderer, item, mediaSource);

                    mediaRenderer.setCurrentSrc(streamInfo, item, mediaSource);
                    self.streamInfo = streamInfo;
                });
            });
        }

        function onVolumeChange() {
            console.log('audio element event: pause');

            self.onPlaystateChange(this);

            // In the event timeupdate isn't firing, at least we can update when this happens
            self.setCurrentTime(self.getCurrentTicks());
        }

        function onPause() {

            console.log('audio element event: pause');

            self.onPlaystateChange(this);

            // In the event timeupdate isn't firing, at least we can update when this happens
            self.setCurrentTime(self.getCurrentTicks());
        }

        function onPlaying() {
            console.log('audio element event: playing');

            self.onPlaystateChange(this);

            // In the event timeupdate isn't firing, at least we can update when this happens
            self.setCurrentTime(self.getCurrentTicks());
        }

        var getItemFields = "MediaSources,Chapters";

        self.tryPair = function (target) {

            return new Promise(function (resolve, reject) {

                resolve();
            });
        };
    }

    window.MediaPlayer = new mediaPlayer();

    window.MediaPlayer.init = function () {
        window.MediaController.registerPlayer(window.MediaPlayer);
        window.MediaController.setActivePlayer(window.MediaPlayer, window.MediaPlayer.getTargetsInternal()[0]);
    };

});