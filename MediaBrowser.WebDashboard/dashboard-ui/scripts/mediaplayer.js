(function (document, setTimeout, clearTimeout, screen, store, $, setInterval, window) {

    function mediaPlayer() {

        var self = this;

        var testableVideoElement = document.createElement('video');
        var currentProgressInterval;
        var canClientSeek;
        var currentPlaylistIndex = 0;

        self.currentMediaElement = null;
        self.currentItem = null;
        self.currentMediaSource = null;

        self.currentDurationTicks = null;
        self.startTimeTicksOffset = null;

        self.playlist = [];

        self.isLocalPlayer = true;
        self.isDefaultPlayer = true;

        self.name = 'Html5 Player';

        self.getTargets = function () {

            var targets = [{
                name: 'My Browser',
                id: ConnectionManager.deviceId(),
                playerName: self.name,
                playableMediaTypes: ['Audio', 'Video'],
                isLocalPlayer: true,
                supportedCommands: Dashboard.getSupportedRemoteCommands()
            }];

            return targets;
        };

        var supportsAac = document.createElement('audio').canPlayType('audio/aac').replace(/no/, '');

        self.getVideoQualityOptions = function (videoWidth) {

            var bitrateSetting = AppSettings.maxStreamingBitrate();

            var maxAllowedWidth = videoWidth || 4096;

            var options = [];

            // Some 1080- videos are reported as 1912?
            if (maxAllowedWidth >= 1900) {
                options.push({ name: '1080p - 30Mbps', maxWidth: 1920, bitrate: 30000000 });
                options.push({ name: '1080p - 25Mbps', maxWidth: 1920, bitrate: 25000000 });
                options.push({ name: '1080p - 20Mbps', maxWidth: 1920, bitrate: 20000000 });
                options.push({ name: '1080p - 15Mbps', maxWidth: 1920, bitrate: 15000000 });
                options.push({ name: '1080p - 10Mbps', maxWidth: 1920, bitrate: 10000000 });
                options.push({ name: '1080p - 8Mbps', maxWidth: 1920, bitrate: 8000000 });
                options.push({ name: '1080p - 6Mbps', maxWidth: 1920, bitrate: 6000000 });
                options.push({ name: '1080p - 5Mbps', maxWidth: 1920, bitrate: 5000000 });
            } else if (maxAllowedWidth >= 1260) {
                options.push({ name: '720p - 10Mbps', maxWidth: 1280, bitrate: 10000000 });
                options.push({ name: '720p - 8Mbps', maxWidth: 1280, bitrate: 8000000 });
                options.push({ name: '720p - 6Mbps', maxWidth: 1280, bitrate: 6000000 });
                options.push({ name: '720p - 5Mbps', maxWidth: 1280, bitrate: 5000000 });
            } else if (maxAllowedWidth >= 460) {
                options.push({ name: '480p - 4Mbps', maxWidth: 720, bitrate: 4000000 });
                options.push({ name: '480p - 3Mbps', maxWidth: 720, bitrate: 3000000 });
                options.push({ name: '480p - 2.5Mbps', maxWidth: 720, bitrate: 2500000 });
                options.push({ name: '480p - 2Mbps', maxWidth: 720, bitrate: 2000000 });
                options.push({ name: '480p - 1.5Mbps', maxWidth: 720, bitrate: 1500000 });
            }

            if (maxAllowedWidth >= 1260) {
                options.push({ name: '720p - 4Mbps', maxWidth: 1280, bitrate: 4000000 });
                options.push({ name: '720p - 3Mbps', maxWidth: 1280, bitrate: 3000000 });
                options.push({ name: '720p - 2Mbps', maxWidth: 1280, bitrate: 2000000 });

                // The extra 1 is because they're keyed off the bitrate value
                options.push({ name: '720p - 1Mbps', maxWidth: 1280, bitrate: 1000001 });
            }

            options.push({ name: '480p - 1.0Mbps', maxWidth: 720, bitrate: 1000000 });
            options.push({ name: '480p - 720kbps', maxWidth: 720, bitrate: 720000 });
            options.push({ name: '480p - 420kbps', maxWidth: 720, bitrate: 420000 });
            options.push({ name: '360p', maxWidth: 640, bitrate: 400000 });
            options.push({ name: '240p', maxWidth: 426, bitrate: 320000 });
            options.push({ name: '144p', maxWidth: 256, bitrate: 192000 });

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

        self.getDeviceProfile = function () {

            var qualityOption = self.getVideoQualityOptions().filter(function (q) {
                return q.selected;
            })[0];

            var bitrateSetting = AppSettings.maxStreamingBitrate();

            var canPlayWebm = self.canPlayWebm();

            var profile = {};

            profile.MaxStreamingBitrate = bitrateSetting;
            profile.MaxStaticBitrate = 40000000;
            profile.MusicStreamingTranscodingBitrate = Math.min(bitrateSetting, 192000);

            profile.DirectPlayProfiles = [];
            profile.DirectPlayProfiles.push({
                Container: 'mp4',
                Type: 'Video',
                VideoCodec: 'h264',
                AudioCodec: 'aac,mp3'
            });

            if ($.browser.chrome) {
                profile.DirectPlayProfiles.push({
                    Container: 'mkv,m4v',
                    Type: 'Video',
                    VideoCodec: 'h264',
                    AudioCodec: 'aac,mp3'
                });
            }

            profile.DirectPlayProfiles.push({
                Container: 'mp3',
                Type: 'Audio'
            });

            if (supportsAac) {
                profile.DirectPlayProfiles.push({
                    Container: 'aac',
                    Type: 'Audio'
                });
            }

            if (canPlayWebm) {
                profile.DirectPlayProfiles.push({
                    Container: 'webm',
                    Type: 'Video'
                });
                profile.DirectPlayProfiles.push({
                    Container: 'webm,webma',
                    Type: 'Audio'
                });
            }

            profile.TranscodingProfiles = [];
            profile.TranscodingProfiles.push({
                Container: 'mp3',
                Type: 'Audio',
                AudioCodec: 'mp3',
                Context: 'Streaming',
                Protocol: 'http'
            });

            if (self.canPlayHls()) {
                profile.TranscodingProfiles.push({
                    Container: 'ts',
                    Type: 'Video',
                    AudioCodec: 'aac',
                    VideoCodec: 'h264',
                    Context: 'Streaming',
                    Protocol: 'hls'
                });
            }

            if (canPlayWebm) {

                profile.TranscodingProfiles.push({
                    Container: 'webm',
                    Type: 'Video',
                    AudioCodec: 'vorbis',
                    VideoCodec: 'vpx',
                    Context: 'Streaming',
                    Protocol: 'http'
                });
            }

            profile.TranscodingProfiles.push({
                Container: 'mp4',
                Type: 'Video',
                AudioCodec: 'aac',
                VideoCodec: 'h264',
                Context: 'Streaming',
                Protocol: 'http'
            });

            profile.ContainerProfiles = [];

            var audioConditions = [];
            if ($.browser.msie) {
                audioConditions.push({
                    Condition: 'LessThanEqual',
                    Property: 'AudioChannels',
                    Value: '2'
                });
            }

            profile.CodecProfiles = [];
            profile.CodecProfiles.push({
                Type: 'Audio',
                Conditions: audioConditions
            });

            profile.CodecProfiles.push({
                Type: 'VideoAudio',
                Conditions: audioConditions
            });

            profile.CodecProfiles.push({
                Type: 'Video',
                Codec: 'h264',
                Conditions: [
                {
                    Condition: 'Equals',
                    Property: 'IsCabac',
                    Value: 'true',
                    IsRequired: false
                },
                {
                    Condition: 'NotEquals',
                    Property: 'IsAnamorphic',
                    Value: 'true',
                    IsRequired: false
                },
                {
                    Condition: 'EqualsAny',
                    Property: 'VideoProfile',
                    Value: 'high|main|baseline|constrained baseline'
                },
                {
                    Condition: 'LessThanEqual',
                    Property: 'VideoLevel',
                    Value: '41'
                },
                {
                    Condition: 'LessThanEqual',
                    Property: 'Width',
                    Value: qualityOption.maxWidth
                }]
            });

            profile.CodecProfiles.push({
                Type: 'Video',
                Codec: 'vpx',
                Conditions: [
                {
                    Condition: 'NotEquals',
                    Property: 'IsAnamorphic',
                    Value: 'true',
                    IsRequired: false
                },
                {
                    Condition: 'LessThanEqual',
                    Property: 'Width',
                    Value: qualityOption.maxWidth
                }]
            });

            // Subtitle profiles
            // External vtt or burn in
            profile.SubtitleProfiles = [];
            if (self.supportsTextTracks()) {
                profile.SubtitleProfiles.push({
                    Format: 'vtt',
                    Method: 'External'
                });
            }

            return profile;
        };

        self.updateCanClientSeek = function (elem) {

            var duration = elem.duration;

            canClientSeek = duration && !isNaN(duration) && duration != Number.POSITIVE_INFINITY && duration != Number.NEGATIVE_INFINITY;
        };

        self.getCurrentSrc = function (mediaElement) {
            return mediaElement.currentSrc;
        };

        self.getCurrentTicks = function (mediaElement) {

            var playerTime = Math.floor(10000000 * (mediaElement || self.currentMediaElement).currentTime);

            playerTime += self.startTimeTicksOffset;

            return playerTime;
        };

        self.playNextAfterEnded = function () {

            self.nextTrack();
        };

        self.startProgressInterval = function () {

            clearProgressInterval();

            var intervalTime = ApiClient.isWebSocketOpen() ? 1200 : 5000;
            self.lastProgressReport = 0;

            currentProgressInterval = setInterval(function () {

                if (self.currentMediaElement) {

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

        self.canPlayHls = function () {

            var media = testableVideoElement;

            // safari
            if (media.canPlayType('application/x-mpegURL').replace(/no/, '') ||
                media.canPlayType('application/vnd.apple.mpegURL').replace(/no/, '')) {
                return true;
            }

            return false;
        };

        self.getVideoTranscodingExtension = function (currentSrc) {

            if (currentSrc) {
                return self.getCurrentMediaExtension(currentSrc);
            }

            // safari
            if (self.canPlayHls()) {
                return '.m3u8';
            }

            // Chrome, Firefox or IE with plugin installed
            // For some reason in chrome pausing mp4 is causing the video to fail. 
            // So for now it will have to prioritize webm
            if (self.canPlayWebm()) {

                if ($.browser.msie) {
                    return '.webm';
                }
                if ($.browser.chrome) {
                    return '.webm';
                }

                // Firefox suddenly having trouble with our webm
                return '.webm';
            }

            return '.mp4';
        };

        self.changeStream = function (ticks, params) {

            var element = self.currentMediaElement;

            if (canClientSeek && params == null) {

                element.currentTime = ticks / (1000 * 10000);
                return;
            }

            params = params || {};

            var currentSrc = element.currentSrc;

            var playSessionId = getParameterByName('PlaySessionId', currentSrc);
            var liveStreamId = getParameterByName('LiveStreamId', currentSrc);

            if (params.AudioStreamIndex == null && params.SubtitleStreamIndex == null && params.Bitrate == null) {

                currentSrc = replaceQueryString(currentSrc, 'starttimeticks', ticks || 0);
                changeStreamToUrl(element, playSessionId, currentSrc, ticks);
                return;
            }

            var deviceProfile = self.getDeviceProfile();

            var audioStreamIndex = params.AudioStreamIndex == null ? (getParameterByName('AudioStreamIndex', currentSrc) || null) : params.AudioStreamIndex;
            if (typeof (audioStreamIndex) == 'string') {
                audioStreamIndex = parseInt(audioStreamIndex);
            }

            var subtitleStreamIndex = params.SubtitleStreamIndex == null ? (getParameterByName('SubtitleStreamIndex', currentSrc) || null) : params.SubtitleStreamIndex;
            if (typeof (subtitleStreamIndex) == 'string') {
                subtitleStreamIndex = parseInt(subtitleStreamIndex);
            }

            getPlaybackInfo(self.currentItem.Id, deviceProfile, ticks, self.currentMediaSource, audioStreamIndex, subtitleStreamIndex, liveStreamId).done(function (result) {

                if (validatePlaybackInfoResult(result)) {

                    self.currentMediaSource = result.MediaSources[0];
                    self.currentSubtitleStreamIndex = subtitleStreamIndex;

                    currentSrc = self.currentMediaSource.TranscodingUrl;
                    changeStreamToUrl(element, playSessionId, currentSrc, ticks);
                }
            });
        };

        function changeStreamToUrl(element, playSessionId, url, newPositionTicks) {

            clearProgressInterval();

            $(element).off('ended.playbackstopped').off('ended.playnext').one("play", function () {

                self.updateCanClientSeek(this);

                $(this).on('ended.playbackstopped', self.onPlaybackStopped).one('ended.playnext', self.playNextAfterEnded);

                self.startProgressInterval();
                sendProgressUpdate();

            });

            if (self.currentItem.MediaType == "Video") {
                ApiClient.stopActiveEncodings(playSessionId).done(function () {

                    self.startTimeTicksOffset = newPositionTicks;
                    element.src = url;

                });

                self.updateTextStreamUrls(newPositionTicks || 0);
            } else {
                self.startTimeTicksOffset = newPositionTicks;
                element.src = url;
                element.play();
            }
        }

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

            var state = self.getPlayerStateInternal(self.currentMediaElement, self.currentItem, self.currentMediaSource);

            $(self).trigger('positionchange', [state]);
        };

        var supportsTextTracks;

        self.supportsTextTracks = function () {

            if (supportsTextTracks == null) {
                supportsTextTracks = document.createElement('video').textTracks != null;
            }

            // For now, until ready
            return supportsTextTracks;
        };

        self.canQueueMediaType = function (mediaType) {

            return self.currentItem && self.currentItem.MediaType == mediaType;
        };

        function translateItemsForPlayback(items) {

            var deferred = $.Deferred();

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

                        self.playWithIntros(items, options, user);
                    });

                } else {

                    self.getItemsForPlayback({

                        Ids: options.ids.join(',')

                    }).done(function (result) {

                        translateItemsForPlayback(result.Items).done(function (items) {

                            self.playWithIntros(items, options, user);
                        });

                    });
                }

            });

        };

        self.playWithIntros = function (items, options, user) {

            var firstItem = items[0];

            if (options.startPositionTicks || firstItem.MediaType !== 'Video' || !self.canAutoPlayVideo()) {

                self.playInternal(firstItem, options.startPositionTicks, function () {
                    self.setPlaylistState(0, items);
                });

                return;
            }

            ApiClient.getJSON(ApiClient.getUrl('Users/' + user.Id + '/Items/' + firstItem.Id + '/Intros')).done(function (intros) {

                items = intros.Items.concat(items);
                self.playInternal(items[0], options.startPositionTicks, function () {
                    self.setPlaylistState(0, items);
                });

            });
        };

        function supportsDirectPlay(mediaSource) {

            if (mediaSource.SupportsDirectPlay && mediaSource.Protocol == 'Http' && !mediaSource.RequiredHttpHeaders.length) {

                // TODO: Need to verify the host is going to be reachable
                return true;
            }

            return false;
        }

        function getOptimalMediaSource(mediaType, versions) {

            var optimalVersion = versions.filter(function (v) {

                v.enableDirectPlay = supportsDirectPlay(v);

                return v.enableDirectPlay;

            })[0];

            if (!optimalVersion) {
                optimalVersion = versions.filter(function (v) {

                    return v.SupportsDirectStream;

                })[0];
            }

            return optimalVersion || versions.filter(function (s) {
                return s.SupportsTranscoding;
            })[0];
        }

        function getPlaybackInfo(itemId, deviceProfile, startPosition, mediaSource, audioStreamIndex, subtitleStreamIndex, liveStreamId) {

            var postData = {
                DeviceProfile: deviceProfile
            };

            var query = {
                UserId: Dashboard.getCurrentUserId(),
                StartTimeTicks: startPosition || 0
            };

            if (audioStreamIndex != null) {
                query.AudioStreamIndex = audioStreamIndex;
            }
            if (subtitleStreamIndex != null) {
                query.SubtitleStreamIndex = subtitleStreamIndex;
            }
            if (mediaSource) {
                query.MediaSourceId = mediaSource.Id;
            }
            if (liveStreamId) {
                query.LiveStreamId = liveStreamId;
            }

            return ApiClient.ajax({
                url: ApiClient.getUrl('Items/' + itemId + '/PlaybackInfo', query),
                type: 'POST',
                data: JSON.stringify(postData),
                contentType: "application/json",
                dataType: "json"

            });
        }

        function getLiveStream(itemId, deviceProfile, startPosition, mediaSource, audioStreamIndex, subtitleStreamIndex) {

            var postData = {
                DeviceProfile: deviceProfile,
                OpenToken: mediaSource.OpenToken
            };

            var query = {
                UserId: Dashboard.getCurrentUserId(),
                StartTimeTicks: startPosition || 0,
                ItemId: itemId
            };

            if (audioStreamIndex != null) {
                query.AudioStreamIndex = audioStreamIndex;
            }
            if (subtitleStreamIndex != null) {
                query.SubtitleStreamIndex = subtitleStreamIndex;
            }

            return ApiClient.ajax({
                url: ApiClient.getUrl('LiveStreams/Open', query),
                type: 'POST',
                data: JSON.stringify(postData),
                contentType: "application/json",
                dataType: "json"

            });
        }

        self.createStreamInfo = function (type, item, mediaSource, startPosition) {

            var mediaUrl;
            var contentType;
            var startTimeTicksOffset = 0;

            var startPositionInSeekParam = startPosition ? (startPosition / 10000000) : 0;
            var seekParam = startPositionInSeekParam ? '#t=' + startPositionInSeekParam : '';

            if (type == 'video') {

                contentType = 'video/' + mediaSource.Container;

                if (mediaSource.enableDirectPlay) {
                    mediaUrl = mediaSource.Path;
                } else {

                    if (mediaSource.SupportsDirectStream) {

                        mediaUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.' + mediaSource.Container, {
                            Static: true,
                            mediaSourceId: mediaSource.Id,
                            api_key: ApiClient.accessToken()
                        });
                        mediaUrl += seekParam;

                    } else {

                        startTimeTicksOffset = startPosition || 0;
                        mediaUrl = mediaSource.TranscodingUrl;

                        if (mediaSource.TranscodingSubProtocol == 'hls') {

                            mediaUrl += seekParam;
                            contentType = 'application/x-mpegURL';
                        } else {

                            contentType = 'video/' + mediaSource.TranscodingContainer;
                        }
                    }
                }

            } else {

                contentType = 'audio/' + mediaSource.Container;

                if (mediaSource.enableDirectPlay) {

                    mediaUrl = mediaSource.Path;

                } else {

                    var isDirectStream = mediaSource.SupportsDirectStream;

                    if (isDirectStream) {

                        var outputContainer = (mediaSource.Container || '').toLowerCase();

                        mediaUrl = ApiClient.getUrl('Audio/' + item.Id + '/stream.' + outputContainer, {
                            mediaSourceId: mediaSource.Id,
                            deviceId: ApiClient.deviceId(),
                            api_key: ApiClient.accessToken()
                        });
                        mediaUrl += "&static=true" + seekParam;
                    } else {

                        contentType = 'audio/' + mediaSource.TranscodingContainer;

                        mediaUrl = mediaSource.TranscodingUrl;
                    }

                    startTimeTicksOffset = startPosition || 0;
                }
            }

            return {
                url: mediaUrl,
                contentType: contentType,
                startTimeTicksOffset: startTimeTicksOffset,
                startPositionInSeekParam: startPositionInSeekParam
            };
        };

        self.playInternal = function (item, startPosition, callback) {

            if (item == null) {
                throw new Error("item cannot be null");
            }

            if (self.isPlaying()) {
                self.stop();
            }

            if (item.MediaType !== 'Audio' && item.MediaType !== 'Video') {
                throw new Error("Unrecognized media type");
            }

            if (item.IsPlaceHolder) {
                showPlaybackInfoErrorMessage('PlaceHolder');
                return;
            }

            var mediaSource;
            var deviceProfile = self.getDeviceProfile();

            getPlaybackInfo(item.Id, deviceProfile, startPosition).done(function (result) {

                if (validatePlaybackInfoResult(result)) {

                    mediaSource = getOptimalMediaSource(item.MediaType, result.MediaSources);

                    if (mediaSource) {

                        if (mediaSource.RequiresOpening) {

                            getLiveStream(item.Id, deviceProfile, startPosition, mediaSource, null, null).done(function (openLiveStreamResult) {

                                openLiveStreamResult.MediaSource.enableDirectPlay = supportsDirectPlay(openLiveStreamResult.MediaSource);
                                playInternalPostMediaSourceSelection(item, openLiveStreamResult.MediaSource, startPosition, callback);
                            });

                        } else {
                            playInternalPostMediaSourceSelection(item, mediaSource, startPosition, callback);
                        }
                    } else {
                        showPlaybackInfoErrorMessage('NoCompatibleStream');
                    }
                }

            });
        };

        function playInternalPostMediaSourceSelection(item, mediaSource, startPosition, callback) {

            self.currentMediaSource = mediaSource;
            self.currentItem = item;

            if (item.MediaType === "Video") {

                self.currentMediaElement = self.playVideo(item, self.currentMediaSource, startPosition);
                self.currentDurationTicks = self.currentMediaSource.RunTimeTicks;

                self.updateNowPlayingInfo(item);

            } else if (item.MediaType === "Audio") {

                self.currentMediaElement = playAudio(item, self.currentMediaSource, startPosition);
                self.currentDurationTicks = self.currentMediaSource.RunTimeTicks;
            }

            if (callback) {
                callback();
            }
        }

        function validatePlaybackInfoResult(result) {

            if (result.ErrorCode) {

                showPlaybackInfoErrorMessage(result.ErrorCode);
                return false;
            }

            return true;
        }

        function showPlaybackInfoErrorMessage(errorCode) {

            // This timeout is messy, but if jqm is in the act of hiding a popup, it will not show a new one
            // If we're coming from the popup play menu, this will be a problem

            setTimeout(function () {
                Dashboard.alert({
                    message: Globalize.translate('MessagePlaybackError' + errorCode),
                    title: Globalize.translate('HeaderPlaybackError')
                });
            }, 300);

        }

        self.getNowPlayingNameHtml = function (playerState) {

            var nowPlayingItem = playerState.NowPlayingItem;
            var topText = nowPlayingItem.Name;

            if (nowPlayingItem.MediaType == 'Video') {
                if (nowPlayingItem.IndexNumber != null) {
                    topText = nowPlayingItem.IndexNumber + " - " + topText;
                }
                if (nowPlayingItem.ParentIndexNumber != null) {
                    topText = nowPlayingItem.ParentIndexNumber + "." + topText;
                }
            }

            var bottomText = '';

            if (nowPlayingItem.Artists && nowPlayingItem.Artists.length) {
                bottomText = topText;
                topText = nowPlayingItem.Artists[0];
            }
            else if (nowPlayingItem.SeriesName || nowPlayingItem.Album) {
                bottomText = topText;
                topText = nowPlayingItem.SeriesName || nowPlayingItem.Album;
            }
            else if (nowPlayingItem.ProductionYear) {
                bottomText = nowPlayingItem.ProductionYear;
            }

            return bottomText ? topText + '<br/>' + bottomText : topText;
        };

        self.displayContent = function (options) {

            // Handle it the same as a remote control command
            Dashboard.onBrowseCommand(options);
        };

        self.getItemsForPlayback = function (query) {

            var userId = Dashboard.getCurrentUserId();

            if (query.Ids && query.Ids.split(',').length == 1) {
                var deferred = DeferredBuilder.Deferred();

                ApiClient.getItem(userId, query.Ids.split(',')).done(function (item) {
                    deferred.resolveWith(null, [
                    {
                        Items: [item],
                        TotalRecordCount: 1
                    }]);
                });

                return deferred.promise();
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

            var newIndex = currentPlaylistIndex + 1;
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

            if (!self.currentMediaElement) {
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

            if (!self.currentMediaElement) {
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

            self.currentMediaElement.pause();
        };

        self.unpause = function () {
            self.currentMediaElement.play();
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
            return self.currentMediaElement.volume * 100;
        };

        self.toggleMute = function () {

            if (self.currentMediaElement) {

                console.log('MediaPlayer toggling mute');

                if (self.volume()) {
                    self.mute();
                } else {
                    self.unMute();
                }
            }
        };

        self.volumeDown = function () {

            if (self.currentMediaElement) {
                self.setVolume(Math.max(self.volume() - 2, 0));
            }
        };

        self.volumeUp = function () {

            if (self.currentMediaElement) {
                self.setVolume(Math.min(self.volume() + 2, 100));
            }
        };

        // Sets volume using a 0-100 scale
        self.setVolume = function (val) {

            if (self.currentMediaElement) {

                console.log('MediaPlayer setting volume to ' + val);
                self.currentMediaElement.volume = val / 100;

                self.onVolumeChanged(self.currentMediaElement);

                self.saveVolume();
            }
        };

        self.saveVolume = function (val) {

            if (val) {
                store.setItem("volume", val);
            }

        };

        self.getSavedVolume = function () {
            return store.getItem("volume") || 0.5;
        };

        self.shuffle = function (id) {

            var userId = Dashboard.getCurrentUserId();

            ApiClient.getItem(userId, id).done(function (item) {

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

                self.getItemsForPlayback(query).done(function (result) {

                    self.play({ items: result.Items });

                });

            });

        };

        self.instantMix = function (id) {

            var userId = Dashboard.getCurrentUserId();

            ApiClient.getItem(userId, id).done(function (item) {

                var promise;
                var itemLimit = 100;

                if (item.Type == "MusicArtist") {

                    promise = ApiClient.getInstantMixFromArtist({
                        UserId: Dashboard.getCurrentUserId(),
                        Fields: getItemFields,
                        Limit: itemLimit,
                        Id: id
                    });

                }
                else if (item.Type == "MusicGenre") {

                    promise = ApiClient.getInstantMixFromMusicGenre({
                        UserId: Dashboard.getCurrentUserId(),
                        Fields: getItemFields,
                        Limit: itemLimit,
                        Id: id
                    });

                }
                else if (item.Type == "MusicAlbum") {

                    promise = ApiClient.getInstantMixFromAlbum(id, {
                        UserId: Dashboard.getCurrentUserId(),
                        Fields: getItemFields,
                        Limit: itemLimit
                    });

                }
                else if (item.Type == "Playlist") {

                    promise = ApiClient.getInstantMixFromPlaylist(id, {
                        UserId: Dashboard.getCurrentUserId(),
                        Fields: getItemFields,
                        Limit: itemLimit
                    });

                }
                else if (item.Type == "Audio") {

                    promise = ApiClient.getInstantMixFromSong(id, {
                        UserId: Dashboard.getCurrentUserId(),
                        Fields: getItemFields,
                        Limit: itemLimit
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

            var elem = self.currentMediaElement;

            if (elem) {

                elem.pause();

                $(elem).off("ended.playnext").one("ended", function () {

                    $(this).off();

                    if (this.tagName.toLowerCase() != 'audio') {
                        $(this).remove();
                    }

                    elem.src = null;
                    elem.src = "";
                    self.currentMediaElement = null;
                    self.currentItem = null;
                    self.currentMediaSource = null;

                    // When the browser regains focus it may start auto-playing the last video
                    if ($.browser.safari) {
                        elem.src = 'files/dummy.mp4';
                        elem.play();
                    }

                }).trigger("ended");

            } else {
                self.currentMediaElement = null;
                self.currentItem = null;
                self.currentMediaSource = null;
            }

            if (self.isFullScreen()) {
                self.exitFullScreen();
            }
            self.resetEnhancements();
        };

        self.isPlaying = function () {
            return self.currentMediaElement != null;
        };

        self.getPlayerState = function () {

            var deferred = $.Deferred();

            var result = self.getPlayerStateInternal(self.currentMediaElement, self.currentItem, self.currentMediaSource);

            deferred.resolveWith(null, [result]);

            return deferred.promise();
        };

        self.getPlayerStateInternal = function (playerElement, item, mediaSource) {

            var state = {
                PlayState: {}
            };

            if (playerElement) {

                state.PlayState.VolumeLevel = playerElement.volume * 100;
                state.PlayState.IsMuted = playerElement.volume == 0;
                state.PlayState.IsPaused = playerElement.paused;
                state.PlayState.PositionTicks = self.getCurrentTicks(playerElement);

                var currentSrc = playerElement.currentSrc;

                if (currentSrc) {

                    var audioStreamIndex = getParameterByName('AudioStreamIndex', currentSrc);

                    if (audioStreamIndex) {
                        state.PlayState.AudioStreamIndex = parseInt(audioStreamIndex);
                    }
                    state.PlayState.SubtitleStreamIndex = self.currentSubtitleStreamIndex;

                    state.PlayState.PlayMethod = getParameterByName('static', currentSrc) == 'true' ?
                        'DirectStream' :
                        'Transcode';

                    state.PlayState.LiveStreamId = getParameterByName('LiveStreamId', currentSrc);
                    state.PlayState.PlaySessionId = getParameterByName('PlaySessionId', currentSrc);
                }
            }

            if (mediaSource) {

                state.PlayState.MediaSourceId = mediaSource.Id;

                state.NowPlayingItem = {
                    RunTimeTicks: mediaSource.RunTimeTicks
                };

                state.PlayState.CanSeek = mediaSource.RunTimeTicks && mediaSource.RunTimeTicks > 0;
            }

            if (item) {

                state.NowPlayingItem = state.NowPlayingItem || {};
                var nowPlayingItem = state.NowPlayingItem;

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
                nowPlayingItem.Artists = item.Artists;

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

            var state = self.getPlayerStateInternal(playerElement, item, mediaSource);

            $(self).trigger('playbackstart', [state]);

            self.startProgressInterval();
        };

        self.onVolumeChanged = function (playerElement) {

            self.saveVolume(playerElement.volume);

            var state = self.getPlayerStateInternal(playerElement, self.currentItem, self.currentMediaSource);

            $(self).trigger('volumechange', [state]);
        };

        self.cleanup = function () {

        };

        self.onPlaybackStopped = function () {

            console.log('playback stopped');

            $('body').removeClass('bodyWithPopupOpen');

            var playerElement = this;

            var playSessionId = getParameterByName('PlaySessionId', playerElement.currentSrc);

            $(playerElement).off('.mediaplayerevent').off('ended.playbackstopped');

            self.cleanup(playerElement);

            clearProgressInterval();

            var item = self.currentItem;
            var mediaSource = self.currentMediaSource;

            if (item.MediaType == "Video") {

                ApiClient.stopActiveEncodings(playSessionId);
                if (self.isFullScreen()) {
                    self.exitFullScreen();
                }
                self.resetEnhancements();
            }

            var state = self.getPlayerStateInternal(playerElement, item, mediaSource);

            $(self).trigger('playbackstop', [state]);
        };

        self.onPlaystateChange = function (playerElement) {

            var state = self.getPlayerStateInternal(playerElement, self.currentItem, self.currentMediaSource);

            $(self).trigger('playstatechange', [state]);
        };

        $(window).on("beforeunload", function () {

            // Try to report playback stopped before the browser closes
            if (self.currentItem && self.currentMediaElement && currentProgressInterval) {

                self.onPlaybackStopped.call(self.currentMediaElement);
            }
        });

        function sendProgressUpdate() {

            var element = self.currentMediaElement;
            var state = self.getPlayerStateInternal(element, self.currentItem, self.currentMediaSource);

            var info = {
                QueueableMediaTypes: state.NowPlayingItem.MediaType,
                ItemId: state.NowPlayingItem.Id,
                NowPlayingItem: state.NowPlayingItem
            };

            info = $.extend(info, state.PlayState);

            ApiClient.reportPlaybackProgress(info);
        }

        function clearProgressInterval() {

            if (currentProgressInterval) {
                clearTimeout(currentProgressInterval);
                currentProgressInterval = null;
            }
        }

        self.canPlayWebm = function () {

            return testableVideoElement.canPlayType('video/webm').replace(/no/, '');
        };

        self.canAutoPlayAudio = function () {

            if ($.browser.android || ($.browser.webkit && !$.browser.chrome)) {
                return false;
            }

            return true;
        };

        function getAudioElement() {

            var elem = $('.mediaPlayerAudio');

            if (elem.length) {
                return elem;
            }

            var html = '';

            var requiresControls = !self.canAutoPlayAudio();

            if (requiresControls) {
                html += '<div class="mediaPlayerAudioContainer"><div class="mediaPlayerAudioContainerInner">';;
            } else {
                html += '<div class="mediaPlayerAudioContainer" style="display:none;"><div class="mediaPlayerAudioContainerInner">';;
            }

            html += '<audio class="mediaPlayerAudio" controls>';
            html += '</audio></div></div>';

            $(document.body).append(html);

            return $('.mediaPlayerAudio');
        }

        function playAudio(item, mediaSource, startPositionTicks) {

            var streamInfo = self.createStreamInfo('audio', item, mediaSource, startPositionTicks);
            var audioUrl = streamInfo.url;
            self.startTimeTicksOffset = streamInfo.startTimeTicksOffset;

            var initialVolume = self.getSavedVolume();

            return getAudioElement().each(function () {

                this.src = audioUrl;
                this.volume = initialVolume;
                this.play();

            }).on("volumechange.mediaplayerevent", function () {

                console.log('audio element event: volumechange');

                self.onVolumeChanged(this);

            }).one("playing.mediaplayerevent", function () {

                console.log('audio element event: playing');

                $('.mediaPlayerAudioContainer').hide();

                // For some reason this is firing at the start, so don't bind until playback has begun
                $(this).on("ended.playbackstopped", self.onPlaybackStopped).one('ended.playnext', self.playNextAfterEnded);

                self.onPlaybackStart(this, item, mediaSource);

            }).on("pause.mediaplayerevent", function () {

                console.log('audio element event: pause');

                self.onPlaystateChange(this);

                // In the event timeupdate isn't firing, at least we can update when this happens
                self.setCurrentTime(self.getCurrentTicks());

            }).on("playing.mediaplayerevent", function () {

                console.log('audio element event: playing');

                self.onPlaystateChange(this);

                // In the event timeupdate isn't firing, at least we can update when this happens
                self.setCurrentTime(self.getCurrentTicks());

            }).on("timeupdate.mediaplayerevent", function () {

                self.setCurrentTime(self.getCurrentTicks(this));

            })[0];
        };

        var getItemFields = "MediaSources,Chapters";

        self.getCurrentTargetInfo = function () {
            return self.getTargets()[0];
        };
    }

    window.MediaPlayer = new mediaPlayer();

    window.MediaController.registerPlayer(window.MediaPlayer);
    window.MediaController.setActivePlayer(window.MediaPlayer);


})(document, setTimeout, clearTimeout, screen, window.store, $, setInterval, window);