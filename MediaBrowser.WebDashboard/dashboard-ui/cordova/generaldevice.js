(function () {

    function connectSDKPlayer() {

        var self = this;

        var PlayerName = "ConnectSDK";
        var currentDevice;
        var currentDeviceId;
        var currentMediaControl;

        // MediaController needs this
        self.name = PlayerName;

        self.getItemsForPlayback = function (query) {

            var userId = Dashboard.getCurrentUserId();

            query.Limit = query.Limit || 100;
            query.ExcludeLocationTypes = "Virtual";

            return ApiClient.getItems(userId, query);
        };

        var castPlayer = {};

        $(castPlayer).on("playbackstart", function (e, data) {

            Logger.log('cc: playbackstart');

            var state = self.getPlayerStateInternal(data);
            $(self).trigger("playbackstart", [state]);
        });

        $(castPlayer).on("playbackstop", function (e, data) {

            Logger.log('cc: playbackstop');
            var state = self.getPlayerStateInternal(data);

            $(self).trigger("playbackstop", [state]);

            // Reset this so the next query doesn't make it appear like content is playing.
            self.lastPlayerData = {};
        });

        $(castPlayer).on("playbackprogress", function (e, data) {

            Logger.log('cc: positionchange');
            var state = self.getPlayerStateInternal(data);

            $(self).trigger("positionchange", [state]);
        });

        self.play = function (options) {

            Dashboard.getCurrentUser().then(function (user) {

                if (options.items) {

                    self.playWithCommand(options, 'PlayNow');

                } else {

                    self.getItemsForPlayback({

                        Ids: options.ids.join(',')

                    }).then(function (result) {

                        options.items = result.Items;
                        self.playWithCommand(options, 'PlayNow');

                    });
                }

            });

        };

        self.playWithCommand = function (options, command) {

            if (!options.items) {
                ApiClient.getItem(Dashboard.getCurrentUserId(), options.ids[0]).then(function (item) {

                    options.items = [item];
                    self.playWithCommand(options, command);
                });

                return;
            }

            playItemInternal(items[0], null, serverAddress);
        };

        function validatePlaybackInfoResult(result) {

            if (result.ErrorCode) {

                MediaController.showPlaybackInfoErrorMessage(result.ErrorCode);
                return false;
            }

            return true;
        }

        function getOptimalMediaSource(mediaType, versions) {

            var optimalVersion = versions.filter(function (v) {

                v.enableDirectPlay = MediaController.supportsDirectPlay(v);

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

        function playItemInternal(item, startPosition) {

            if (item == null) {
                throw new Error("item cannot be null");
            }

            if (item.MediaType !== 'Audio' && item.MediaType !== 'Video') {
                throw new Error("Unrecognized media type");
            }

            if (item.IsPlaceHolder) {
                MediaController.showPlaybackInfoErrorMessage('PlaceHolder');
                return;
            }

            var deviceProfile = self.getDeviceProfile();

            if (item.MediaType === "Video") {

                Dashboard.showModalLoadingMsg();
            }

            MediaController.getPlaybackInfo(item.Id, deviceProfile, startPosition).done(function (playbackInfoResult) {

                if (validatePlaybackInfoResult(playbackInfoResult)) {

                    var mediaSource = getOptimalMediaSource(item.MediaType, playbackInfoResult.MediaSources);

                    if (mediaSource) {

                        if (mediaSource.RequiresOpening) {

                            getLiveStream(item.Id, playbackInfoResult.PlaySessionId, deviceProfile, startPosition, mediaSource, null, null).done(function (openLiveStreamResult) {

                                openLiveStreamResult.MediaSource.enableDirectPlay = supportsDirectPlay(openLiveStreamResult.MediaSource);

                                playInternalPostMediaSourceSelection(item, openLiveStreamResult.MediaSource, startPosition, callback);
                            });

                        } else {
                            playInternalPostMediaSourceSelection(item, mediaSource, startPosition, callback);
                        }
                    } else {
                        Dashboard.hideModalLoadingMsg();
                        MediaController.showPlaybackInfoErrorMessage('NoCompatibleStream');
                    }
                }

            });
        }

        function playInternalPostMediaSourceSelection(item, mediaSource, startPosition, deferred) {

            Dashboard.hideModalLoadingMsg();

            var streamInfo = MediaPlayer.createStreamInfo('Video', item, mediaSource, startPosition);

            currentDevice.getMediaPlayer().playMedia(
                        streamInfo.url,
                        streamInfo.MimeType,
                        {
                            title: item.Name,
                            description: item.Overview || '',
                            shouldLoop: false
                        }
                    ).success(function (launchSession, mediaControl) {

                        Logger.log("Video launch successful");
                        currentMediaControl = mediaControl && mediaControl.acquire();

                    }).error(function (err) {

                        Logger.log("error: " + err.message);
                    });

            deferred.resolveWith(null, [streamInfo]);
        }

        self.unpause = function () {
            if (currentMediaControl) {
                currentMediaControl.pause();
            }
        };

        self.pause = function () {
            if (currentMediaControl) {
                currentMediaControl.pause();
            }
        };

        self.shuffle = function (id) {

            var userId = Dashboard.getCurrentUserId();

            ApiClient.getItem(userId, id).then(function (item) {

                self.playWithCommand({

                    items: [item]

                }, 'Shuffle');

            });

        };

        self.instantMix = function (id) {

            var userId = Dashboard.getCurrentUserId();

            ApiClient.getItem(userId, id).then(function (item) {

                self.playWithCommand({

                    items: [item]

                }, 'InstantMix');

            });

        };

        self.canQueueMediaType = function (mediaType) {
            return mediaType == "Audio";
        };

        self.queue = function (options) {
            self.playWithCommnd(options, 'PlayLast');
        };

        self.queueNext = function (options) {
            self.playWithCommand(options, 'PlayNext');
        };

        self.stop = function () {
            if (currentMediaControl) {
                currentMediaControl.stop();
            }
        };

        self.displayContent = function (options) {

            // TODO
        };

        self.mute = function () {
            if (currentDevice) {
                currentDevice.getVolumeControl().setMute(true);
            }
        };

        self.unMute = function () {
            self.setVolume(getCurrentVolume() + 2);
        };

        self.toggleMute = function () {

            var state = self.lastPlayerData || {};
            state = state.PlayState || {};

            if (state.IsMuted) {
                self.unMute();
            } else {
                self.mute();
            }
        };

        self.getDeviceProfile = function () {

            var qualityOption = self.getVideoQualityOptions().filter(function (q) {
                return q.selected;
            })[0];

            var bitrateSetting = AppSettings.maxStreamingBitrate();

            var profile = {};

            profile.MaxStreamingBitrate = bitrateSetting;
            profile.MaxStaticBitrate = 40000000;
            profile.MusicStreamingTranscodingBitrate = Math.min(bitrateSetting, 192000);

            profile.DirectPlayProfiles = [];
            profile.DirectPlayProfiles.push({
                Container: 'mp4,m4v',
                Type: 'Video',
                VideoCodec: 'h264',
                AudioCodec: 'aac,mp3,ac3'
            });
            profile.DirectPlayProfiles.push({
                Container: 'mov',
                Type: 'Video'
            });

            profile.DirectPlayProfiles.push({
                Container: 'mp3',
                Type: 'Audio'
            });

            profile.DirectPlayProfiles.push({
                Container: 'aac',
                Type: 'Audio'
            });

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

            profile.TranscodingProfiles.push({
                Container: 'mp4',
                Type: 'Video',
                AudioCodec: 'aac',
                VideoCodec: 'h264',
                Context: 'Streaming',
                Protocol: 'http'
            });

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

            profile.CodecProfiles.push({
                Type: 'VideoAudio',
                Codec: 'aac,mp3',
                Conditions: [
                    {
                        Condition: 'LessThanEqual',
                        Property: 'AudioChannels',
                        Value: '6'
                    }
                ]
            });

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
                    Property: 'Width',
                    Value: qualityOption.maxWidth
                }]
            });

            // Subtitle profiles
            profile.SubtitleProfiles = [];
            profile.ResponseProfiles = [];

            profile.ResponseProfiles.push({
                Type: 'Video',
                Container: 'm4v',
                MimeType: 'video/mp4'
            });

            //profile.ResponseProfiles.push({
            //    Type: 'Video',
            //    Container: 'mkv',
            //    MimeType: 'video/webm'
            //});

            return profile;
        };

        function getBaseTargetInfo() {
            var target = {};

            target.playableMediaTypes = ["Audio", "Video"];
            target.isLocalPlayer = false;
            target.supportedCommands = [
                "VolumeUp",
                "VolumeDown",
                "Mute",
                "Unmute",
                "ToggleMute",
                "SetVolume"
            ];

            return target;
        }

        function convertDeviceToTarget(device) {

            var target = getBaseTargetInfo();

            target.appName = target.name = target.deviceName = device.getFriendlyName();
            target.playerName = PlayerName;
            target.id = device.getId();

            return target;
        }

        function isValid(device) {

            var validTokens = ['AirPlay', 'Airplay', 'airplay'];

            return validTokens.filter(function (t) {

                return device.hasService(t);

            }).length > 0;
        }

        self.getTargets = function () {

            return ConnectSDKHelper.getDeviceList().filter(function (d) {

                return isValid(d);

            }).map(convertDeviceToTarget);
        };

        self.seek = function (position) {

            position = parseInt(position);
            position = position / 10000000;

            // TODO
        };

        self.setAudioStreamIndex = function (index) {
            // TODO
        };

        self.setSubtitleStreamIndex = function (index) {
            // TODO
        };

        self.nextTrack = function () {
            // TODO
        };

        self.previousTrack = function () {
            // TODO
        };

        self.beginPlayerUpdates = function () {
            // Setup polling here
        };

        self.endPlayerUpdates = function () {
            // Stop polling here
        };

        function getCurrentVolume() {
            var state = self.lastPlayerData || {};
            state = state.PlayState || {};

            return state.VolumeLevel == null ? 100 : state.VolumeLevel;
        }

        self.volumeDown = function () {

            if (currentDevice) {
                currentDevice.getVolumeControl().volumeDown();
            }
        };

        self.volumeUp = function () {

            if (currentDevice) {
                currentDevice.getVolumeControl().volumeUp();
            }
        };

        self.setVolume = function (vol) {

            vol = Math.min(vol, 100);
            vol = Math.max(vol, 0);

            if (currentDevice) {
                currentDevice.getVolumeControl().setVolume(vol / 100);
            }
        };

        self.getPlayerState = function () {

            var deferred = $.Deferred();

            var result = self.getPlayerStateInternal();

            deferred.resolveWith(null, [result]);

            return deferred.promise();
        };

        self.lastPlayerData = {};

        self.getPlayerStateInternal = function (data) {

            data = data || self.lastPlayerData;
            self.lastPlayerData = data;

            Logger.log(JSON.stringify(data));
            return data;
        };

        function handleSessionDisconnect() {
            Logger.log("session disconnected");

            cleanupSession();
            MediaController.removeActivePlayer(PlayerName);
        }

        function cleanupSession() {

        }

        function launchWebApp(device) {

            if (currentDevice) {
                cleanupSession();
            }

            Logger.log('session.connect succeeded');

            MediaController.setActivePlayer(PlayerName, convertDeviceToTarget(device));
            currentDevice = device;
            currentDeviceId = device.getId();
        }

        function onDeviceReady(device) {

            device.off("ready");

            Logger.log('creating webAppSession');

            launchWebApp(device);
        }

        self.tryPair = function (target) {

            var deferred = $.Deferred();

            var device = ConnectSDKHelper.getDeviceList().filter(function (d) {

                return d.getId() == target.id;
            })[0];

            if (device) {

                self.tryPairWithDevice(device, deferred);

            } else {
                deferred.reject();
            }

            return deferred.promise();
        };

        self.tryPairWithDevice = function (device, deferred) {

            Logger.log('Will attempt to connect to Connect Device');

            device.on("disconnect", function () {
                device.off("ready");
                device.off("disconnect");
            });

            if (device.isReady()) {
                Logger.log('Device is already ready, calling onDeviceReady');
                onDeviceReady(device);
            } else {

                Logger.log('Binding device ready handler');

                device.on("ready", function () {
                    Logger.log('device.ready fired');
                    onDeviceReady(device);
                });

                Logger.log('Calling device.connect');
                device.connect();
            }
        };

        $(MediaController).on('playerchange', function (e, newPlayer, newTarget) {

            if (currentDevice) {
                if (newTarget.id != currentDeviceId) {
                    if (currentDevice) {
                        Logger.log('Disconnecting from connect device');
                        //currentDevice.disconnect();
                        cleanupSession();
                        currentDevice = null;
                        currentDeviceId = null;
                        currentMediaControl = null;
                    }
                }
            }
        });

        function onResume() {

            var deviceId = currentDeviceId;

            if (deviceId) {
                self.tryPair({
                    id: deviceId
                });
            }
        }

        document.addEventListener("resume", onResume, false);
    }

    MediaController.registerPlayer(new connectSDKPlayer());

})();