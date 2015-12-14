(function (document, setTimeout, clearTimeout, screen, setInterval, window) {

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

            var targets = [{
                name: Globalize.translate('MyDevice'),
                id: AppInfo.deviceId,
                playerName: self.name,
                playableMediaTypes: ['Audio', 'Video'],
                isLocalPlayer: true,
                supportedCommands: Dashboard.getSupportedRemoteCommands()
            }];

            return targets;
        };

        self.getVideoQualityOptions = function (videoWidth, videoHeight) {

            var bitrateSetting = AppSettings.maxStreamingBitrate();

            var maxAllowedWidth = videoWidth || 4096;
            var maxAllowedHeight = videoHeight || 2304;

            var options = [];

            // Some 1080- videos are reported as 1912?
            if (maxAllowedWidth >= 1900) {

                options.push({ name: '1080p - 40Mbps', maxHeight: 1080, bitrate: 40000000 });
                options.push({ name: '1080p - 35Mbps', maxHeight: 1080, bitrate: 35000000 });
                options.push({ name: '1080p - 30Mbps', maxHeight: 1080, bitrate: 30000000 });
                options.push({ name: '1080p - 25Mbps', maxHeight: 1080, bitrate: 25000000 });
                options.push({ name: '1080p - 20Mbps', maxHeight: 1080, bitrate: 20000000 });
                options.push({ name: '1080p - 15Mbps', maxHeight: 1080, bitrate: 15000000 });
                options.push({ name: '1080p - 10Mbps', maxHeight: 1080, bitrate: 10000001 });
                options.push({ name: '1080p - 8Mbps', maxHeight: 1080, bitrate: 8000001 });
                options.push({ name: '1080p - 6Mbps', maxHeight: 1080, bitrate: 6000001 });
                options.push({ name: '1080p - 5Mbps', maxHeight: 1080, bitrate: 5000001 });
                options.push({ name: '1080p - 4Mbps', maxHeight: 1080, bitrate: 4000002 });

            } else if (maxAllowedWidth >= 1260) {
                options.push({ name: '720p - 10Mbps', maxHeight: 720, bitrate: 10000000 });
                options.push({ name: '720p - 8Mbps', maxHeight: 720, bitrate: 8000000 });
                options.push({ name: '720p - 6Mbps', maxHeight: 720, bitrate: 6000000 });
                options.push({ name: '720p - 5Mbps', maxHeight: 720, bitrate: 5000000 });

            } else if (maxAllowedWidth >= 700) {
                options.push({ name: '480p - 4Mbps', maxHeight: 480, bitrate: 4000001 });
                options.push({ name: '480p - 3Mbps', maxHeight: 480, bitrate: 3000001 });
                options.push({ name: '480p - 2.5Mbps', maxHeight: 480, bitrate: 2500000 });
                options.push({ name: '480p - 2Mbps', maxHeight: 480, bitrate: 2000001 });
                options.push({ name: '480p - 1.5Mbps', maxHeight: 480, bitrate: 1500001 });
            }

            if (maxAllowedWidth >= 1260) {
                options.push({ name: '720p - 4Mbps', maxHeight: 720, bitrate: 4000000 });
                options.push({ name: '720p - 3Mbps', maxHeight: 720, bitrate: 3000000 });
                options.push({ name: '720p - 2Mbps', maxHeight: 720, bitrate: 2000000 });

                // The extra 1 is because they're keyed off the bitrate value
                options.push({ name: '720p - 1.5Mbps', maxHeight: 720, bitrate: 1500000 });
                options.push({ name: '720p - 1Mbps', maxHeight: 720, bitrate: 1000001 });
            }

            options.push({ name: '480p - 1.0Mbps', maxHeight: 480, bitrate: 1000000 });
            options.push({ name: '480p - 720kbps', maxHeight: 480, bitrate: 720000 });
            options.push({ name: '480p - 420kbps', maxHeight: 480, bitrate: 420000 });
            options.push({ name: '360p', maxHeight: 360, bitrate: 400000 });
            options.push({ name: '240p', maxHeight: 240, bitrate: 320000 });
            options.push({ name: '144p', maxHeight: 144, bitrate: 192000 });

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

        self.getDeviceProfile = function (maxHeight) {

            if (!maxHeight) {
                maxHeight = self.getVideoQualityOptions().filter(function (q) {
                    return q.selected;
                })[0].maxHeight;
            }

            var isVlc = AppInfo.isNativeApp && browserInfo.android;
            var bitrateSetting = AppSettings.maxStreamingBitrate();

            var supportedFormats = getSupportedFormats();

            var canPlayWebm = supportedFormats.indexOf('webm') != -1;
            var canPlayAc3 = supportedFormats.indexOf('ac3') != -1;
            var canPlayAac = supportedFormats.indexOf('aac') != -1;
            var canPlayMp3 = supportedFormats.indexOf('mp3') != -1;
            var canPlayMkv = supportedFormats.indexOf('mkv') != -1;

            var profile = {};

            profile.MaxStreamingBitrate = bitrateSetting;
            profile.MaxStaticBitrate = 8000000;
            profile.MusicStreamingTranscodingBitrate = Math.min(bitrateSetting, 192000);

            profile.DirectPlayProfiles = [];

            if (supportedFormats.indexOf('h264') != -1) {
                profile.DirectPlayProfiles.push({
                    Container: 'mp4,m4v',
                    Type: 'Video',
                    VideoCodec: 'h264',
                    AudioCodec: 'aac' + (canPlayMp3 ? ',mp3' : '') + (canPlayAc3 ? ',ac3' : '')
                });
            }

            if (canPlayMkv) {
                profile.DirectPlayProfiles.push({
                    Container: 'mkv,mov',
                    Type: 'Video',
                    VideoCodec: 'h264',
                    AudioCodec: 'aac' + (canPlayMp3 ? ',mp3' : '') + (canPlayAc3 ? ',ac3' : '')
                });
            }

            var directPlayVideoContainers = AppInfo.directPlayVideoContainers;

            if (directPlayVideoContainers && directPlayVideoContainers.length) {
                profile.DirectPlayProfiles.push({
                    Container: directPlayVideoContainers.join(','),
                    Type: 'Video'
                });
            }

            if (canPlayMp3) {
                profile.DirectPlayProfiles.push({
                    Container: 'mp3',
                    Type: 'Audio'
                });
            }

            if (canPlayAac) {
                profile.DirectPlayProfiles.push({
                    Container: 'aac',
                    Type: 'Audio'
                });
            }

            var directPlayAudioContainers = AppInfo.directPlayAudioContainers;

            if (directPlayAudioContainers && directPlayAudioContainers.length) {
                profile.DirectPlayProfiles.push({
                    Container: directPlayAudioContainers.join(','),
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

            // Can't use mkv on mobile because we have to use the native player controls and they won't be able to seek it
            if (canPlayMkv && !isVlc && !browserInfo.mobile) {
                profile.TranscodingProfiles.push({
                    Container: 'mkv',
                    Type: 'Video',
                    AudioCodec: 'aac' + (canPlayAc3 ? ',ac3' : ''),
                    VideoCodec: 'h264',
                    Context: 'Streaming'
                });
            }

            if (self.canPlayHls()) {
                profile.TranscodingProfiles.push({
                    Container: 'ts',
                    Type: 'Video',
                    AudioCodec: 'aac' + (canPlayAc3 ? ',ac3' : ''),
                    VideoCodec: 'h264',
                    Context: 'Streaming',
                    Protocol: 'hls'
                });

                if (canPlayAac && browserInfo.safari && !AppInfo.isNativeApp) {
                    profile.TranscodingProfiles.push({
                        Container: 'ts',
                        Type: 'Audio',
                        AudioCodec: 'aac',
                        Context: 'Streaming',
                        Protocol: 'hls'
                    });
                }
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

            profile.TranscodingProfiles.push({
                Container: 'mp4',
                Type: 'Video',
                AudioCodec: 'aac',
                VideoCodec: 'h264',
                Context: 'Static',
                Protocol: 'http'
            });

            if (canPlayAac && browserInfo.safari) {

                profile.TranscodingProfiles.push({
                    Container: 'aac',
                    Type: 'Audio',
                    AudioCodec: 'aac',
                    Context: 'Streaming',
                    Protocol: 'http'
                });

                profile.TranscodingProfiles.push({
                    Container: 'aac',
                    Type: 'Audio',
                    AudioCodec: 'aac',
                    Context: 'Static',
                    Protocol: 'http'
                });

            } else {
                profile.TranscodingProfiles.push({
                    Container: 'mp3',
                    Type: 'Audio',
                    AudioCodec: 'mp3',
                    Context: 'Streaming',
                    Protocol: 'http'
                });
                profile.TranscodingProfiles.push({
                    Container: 'mp3',
                    Type: 'Audio',
                    AudioCodec: 'mp3',
                    Context: 'Static',
                    Protocol: 'http'
                });
            }

            profile.ContainerProfiles = [];

            profile.CodecProfiles = [];
            profile.CodecProfiles.push({
                Type: 'Audio',
                Conditions: [{
                    Condition: 'LessThanEqual',
                    Property: 'AudioChannels',
                    Value: '2'
                }]
            });

            if (!isVlc) {
                profile.CodecProfiles.push({
                    Type: 'VideoAudio',
                    Codec: 'aac',
                    Container: 'mkv,mov',
                    Conditions: [
                        {
                            Condition: 'NotEquals',
                            Property: 'AudioProfile',
                            Value: 'HE-AAC'
                        }
                        // Disabling this is going to require us to learn why it was disabled in the first place
                        //,
                        //{
                        //    Condition: 'NotEquals',
                        //    Property: 'AudioProfile',
                        //    Value: 'LC'
                        //}
                    ]
                });
            }

            profile.CodecProfiles.push({
                Type: 'VideoAudio',
                Codec: 'aac',
                Conditions: [
                    {
                        Condition: 'LessThanEqual',
                        Property: 'AudioChannels',
                        Value: '6'
                    }
                ]
            });

            // These don't play very well
            if (isVlc) {
                profile.CodecProfiles.push({
                    Type: 'VideoAudio',
                    Codec: 'dca',
                    Conditions: [
                        {
                            Condition: 'LessThanEqual',
                            Property: 'AudioChannels',
                            Value: 6
                        }
                    ]
                });
            }

            if (isVlc) {
                profile.CodecProfiles.push({
                    Type: 'Video',
                    Codec: 'h264',
                    Conditions: [
                    {
                        Condition: 'EqualsAny',
                        Property: 'VideoProfile',
                        Value: 'high|main|baseline|constrained baseline'
                    },
                    {
                        Condition: 'LessThanEqual',
                        Property: 'VideoLevel',
                        Value: '41'
                    }]
                });
            } else {
                profile.CodecProfiles.push({
                    Type: 'Video',
                    Codec: 'h264',
                    Conditions: [
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
                        Property: 'Height',
                        Value: maxHeight
                    }]
                });
            }

            if (!isVlc) {
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
                        Property: 'Height',
                        Value: maxHeight
                    }]
                });
            }

            // Subtitle profiles
            // External vtt or burn in
            profile.SubtitleProfiles = [];
            if (self.supportsTextTracks()) {

                if (isVlc) {
                    profile.SubtitleProfiles.push({
                        Format: 'srt',
                        Method: 'External'
                    });
                    profile.SubtitleProfiles.push({
                        Format: 'srt',
                        Method: 'Embed'
                    });
                    profile.SubtitleProfiles.push({
                        Format: 'subrip',
                        Method: 'Embed'
                    });
                    profile.SubtitleProfiles.push({
                        Format: 'ass',
                        Method: 'Embed'
                    });
                    profile.SubtitleProfiles.push({
                        Format: 'ssa',
                        Method: 'Embed'
                    });
                    profile.SubtitleProfiles.push({
                        Format: 'pgs',
                        Method: 'Embed'
                    });
                    profile.SubtitleProfiles.push({
                        Format: 'pgssub',
                        Method: 'Embed'
                    });
                    profile.SubtitleProfiles.push({
                        Format: 'dvdsub',
                        Method: 'Embed'
                    });
                    profile.SubtitleProfiles.push({
                        Format: 'vtt',
                        Method: 'Embed'
                    });
                    profile.SubtitleProfiles.push({
                        Format: 'sub',
                        Method: 'Embed'
                    });
                    profile.SubtitleProfiles.push({
                        Format: 'idx',
                        Method: 'Embed'
                    });
                } else {
                    profile.SubtitleProfiles.push({
                        Format: 'vtt',
                        Method: 'External'
                    });
                }
            }

            profile.ResponseProfiles = [];

            profile.ResponseProfiles.push({
                Type: 'Video',
                Container: 'm4v',
                MimeType: 'video/mp4'
            });

            profile.ResponseProfiles.push({
                Type: 'Video',
                Container: 'mov',
                MimeType: 'video/webm'
            });

            return profile;
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
            var currentSrc = self.getCurrentSrc(mediaRenderer);

            if ((currentSrc || '').indexOf('.m3u8') != -1) {
                return true;
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
            return window.MediaSource != null;
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

            var deviceProfile = self.getDeviceProfile();

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
        };

        function changeStreamToUrl(mediaRenderer, playSessionId, streamInfo) {

            clearProgressInterval();

            Events.off(mediaRenderer, 'ended', self.onPlaybackStopped);
            Events.off(mediaRenderer, 'ended', self.playNextAfterEnded);

            $(mediaRenderer).one("play", function () {

                Events.on(this, 'ended', self.onPlaybackStopped);

                $(this).one('ended', self.playNextAfterEnded);

                self.startProgressInterval();
                sendProgressUpdate();

            });

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
                    isDefault: textStream.Index == mediaSource.DefaultSubtitleStreamIndex
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

            var timeText = Dashboard.getDisplayTime(ticks);
            var mediaRenderer = self.currentMediaRenderer;

            if (self.currentDurationTicks) {

                timeText += " / " + Dashboard.getDisplayTime(self.currentDurationTicks);

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
                currentTimeElement.html(timeText);
            }

            var state = self.getPlayerStateInternal(mediaRenderer, self.currentItem, self.currentMediaSource);

            Events.trigger(self, 'positionchange', [state]);
        };

        self.canQueueMediaType = function (mediaType) {

            return self.currentItem && self.currentItem.MediaType == mediaType;
        };

        function translateItemsForPlayback(items) {

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
                return new Promise(function (resolve, reject) {

                    promise.then(function (result) {

                        resolve(result.Items);
                    });
                });
            } else {

                return new Promise(function (resolve, reject) {

                    resolve(items);
                });
            }
        }

        self.play = function (options) {

            Dashboard.showLoadingMsg();

            Dashboard.getCurrentUser().then(function (user) {

                if (options.items) {

                    translateItemsForPlayback(options.items).then(function (items) {

                        self.playWithIntros(items, options, user);
                    });

                } else {

                    self.getItemsForPlayback({

                        Ids: options.ids.join(',')

                    }).then(function (result) {

                        translateItemsForPlayback(result.Items).then(function (items) {

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

            if (options.startPositionTicks || firstItem.MediaType !== 'Video' || !AppSettings.enableCinemaMode()) {

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

            var deferred = $.Deferred();

            var promises = versions.map(function (v) {
                return MediaController.supportsDirectPlay(v);
            });

            Promise.all(promises).then(function (responses) {

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

                deferred.resolveWith(null, [optimalVersion]);
            });

            return deferred.promise();
        }

        self.createStreamInfo = function (type, item, mediaSource, startPosition) {

            var deferred = $.Deferred();

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

                            if (mediaSource.RunTimeTicks) {
                                // Reports of stuttering with h264 stream copy in IE
                                mediaUrl += '&EnableAutoStreamCopy=false';
                            }

                            mediaUrl += seekParam;
                            contentType = 'application/x-mpegURL';
                        } else {

                            // Reports of stuttering with h264 stream copy in IE
                            if (mediaUrl.indexOf('.mkv') == -1) {
                                mediaUrl += '&EnableAutoStreamCopy=false';
                            }
                            startTimeTicksOffset = startPosition || 0;

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
                        Logger.log('LocalAssetManager.translateFilePath: path: ' + resultInfo.url + ' result: ' + path);
                        deferred.resolveWith(null, [resultInfo]);
                    });
                });

            }
            else {
                deferred.resolveWith(null, [resultInfo]);
            }

            return deferred.promise();
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

            var bitrateDetectionKey = ApiClient.serverAddress();

            if (item.MediaType == 'Video' && AppSettings.enableAutomaticBitrateDetection() && (new Date().getTime() - (self.lastBitrateDetections[bitrateDetectionKey] || 0)) > 300000) {

                Dashboard.showLoadingMsg();

                ApiClient.detectBitrate().then(function (bitrate) {
                    Logger.log('Max bitrate auto detected to ' + bitrate);
                    self.lastBitrateDetections[bitrateDetectionKey] = new Date().getTime();
                    AppSettings.maxStreamingBitrate(bitrate);

                    playOnDeviceProfileCreated(self.getDeviceProfile(), item, startPosition, callback);

                }, function () {

                    playOnDeviceProfileCreated(self.getDeviceProfile(), item, startPosition, callback);
                });

            } else {
                playOnDeviceProfileCreated(self.getDeviceProfile(), item, startPosition, callback);
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

        self.displayContent = function (options) {

            // Handle it the same as a remote control command
            Dashboard.onBrowseCommand(options);
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

                Logger.log('playing next track');

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

                Logger.log('MediaPlayer toggling mute');

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

                Logger.log('MediaPlayer setting volume to ' + val);
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

                mediaRenderer.stop();

                Events.off(mediaRenderer, 'ended', self.playNextAfterEnded);

                $(mediaRenderer).one("ended", function () {

                    $(this).off('.mediaplayerevent');

                    this.cleanup(destroyRenderer);

                    self.currentMediaRenderer = null;
                    self.currentItem = null;
                    self.currentMediaSource = null;
                    self.currentSubtitleStreamIndex = null;
                    self.streamInfo = {};

                });

                Events.trigger(mediaRenderer, "ended");

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

        self.isPlaying = function () {
            return self.playlist.length > 0;
        };

        self.getPlayerState = function () {

            var deferred = $.Deferred();

            var result = self.getPlayerStateInternal(self.currentMediaRenderer, self.currentItem, self.currentMediaSource);

            deferred.resolveWith(null, [result]);

            return deferred.promise();
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

                    state.PlayState.LiveStreamId = mediaSource.LiveStreamId;
                    state.PlayState.PlaySessionId = getParameterByName('PlaySessionId', currentSrc);
                }
            }

            if (mediaSource) {

                state.PlayState.MediaSourceId = mediaSource.Id;

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

            Logger.log('playback stopped');

            document.body.classList.remove('bodyWithPopupOpen');

            var mediaRenderer = this;

            Events.off(mediaRenderer, '.mediaplayerevent');

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

            var state = self.getPlayerStateInternal(mediaRenderer, self.currentItem, self.currentMediaSource);

            Events.trigger(self, 'playstatechange', [state]);
        };

        window.addEventListener("beforeunload", function () {

            // Try to report playback stopped before the browser closes
            if (self.currentItem && self.currentMediaRenderer && currentProgressInterval) {

                self.onPlaybackStopped.call(self.currentMediaRenderer);
            }
        });

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

            info = $.extend(info, state.PlayState);
            console.log('repeat mode ' + info.RepeatMode);
            ApiClient.reportPlaybackProgress(info);
        }

        function clearProgressInterval() {

            if (currentProgressInterval) {
                clearTimeout(currentProgressInterval);
                currentProgressInterval = null;
            }
        }

        function canPlayH264() {

            var userAgent = navigator.userAgent.toLowerCase();
            if (userAgent.indexOf('firefox') != -1) {
                if (userAgent.indexOf('windows') != -1) {
                    return true;
                }
                return false;
            }

            return true;
        }

        var supportedFormats;
        function getSupportedFormats() {

            if (supportedFormats) {
                return supportedFormats;
            }

            var list = [];
            var elem = document.createElement('video');

            if (elem.canPlayType('video/webm').replace(/no/, '')) {
                list.push('webm');
            }
            if (elem.canPlayType('audio/mp4; codecs="ac-3"').replace(/no/, '')) {
                list.push('ac3');
            }

            var canPlayH264 = true;
            var userAgent = navigator.userAgent.toLowerCase();
            if (userAgent.indexOf('firefox') != -1 && userAgent.indexOf('windows') == -1) {
                canPlayH264 = false;
            }

            if (canPlayH264) {
                list.push('h264');
            }

            if (document.createElement('audio').canPlayType('audio/aac').replace(/no/, '')) {
                list.push('aac');
            }
            if (document.createElement('audio').canPlayType('audio/mp3').replace(/no/, '')) {
                list.push('mp3');
            }

            if (browserInfo.chrome) {
                list.push('mkv');
            }

            supportedFormats = list;
            return list;
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

                Events.on(mediaRenderer, "volumechange.mediaplayerevent", function () {

                    Logger.log('audio element event: volumechange');

                    self.onVolumeChanged(this);

                });

                $(mediaRenderer).one("playing.mediaplayerevent", function () {

                    Logger.log('audio element event: playing');

                    // For some reason this is firing at the start, so don't bind until playback has begun
                    Events.on(this, 'ended', self.onPlaybackStopped);

                    $(this).one('ended', self.playNextAfterEnded);

                    self.onPlaybackStart(this, item, mediaSource);

                }).on("pause.mediaplayerevent", function () {

                    Logger.log('audio element event: pause');

                    self.onPlaystateChange(this);

                    // In the event timeupdate isn't firing, at least we can update when this happens
                    self.setCurrentTime(self.getCurrentTicks());

                }).on("playing.mediaplayerevent", function () {

                    Logger.log('audio element event: playing');

                    self.onPlaystateChange(this);

                    // In the event timeupdate isn't firing, at least we can update when this happens
                    self.setCurrentTime(self.getCurrentTicks());

                }).on("timeupdate.mediaplayerevent", onTimeUpdate);

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

        var getItemFields = "MediaSources,Chapters";

        self.tryPair = function (target) {

            var deferred = $.Deferred();
            deferred.resolve();
            return deferred.promise();
        };
    }

    window.MediaPlayer = new mediaPlayer();

    window.MediaController.registerPlayer(window.MediaPlayer);
    window.MediaController.setActivePlayer(window.MediaPlayer, window.MediaPlayer.getTargets()[0]);


})(document, setTimeout, clearTimeout, screen, setInterval, window);