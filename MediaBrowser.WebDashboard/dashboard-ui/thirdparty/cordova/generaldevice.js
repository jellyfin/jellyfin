(function () {

    var currentPairingDeviceId;
    var currentPairedDeviceId;
    var currentDevice;

    var PlayerName = "ConnectSDK";

    function connectPlayer() {

        var self = this;

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

            console.log('cc: playbackstart');

            var state = self.getPlayerStateInternal(data);
            $(self).trigger("playbackstart", [state]);
        });

        $(castPlayer).on("playbackstop", function (e, data) {

            console.log('cc: playbackstop');
            var state = self.getPlayerStateInternal(data);

            $(self).trigger("playbackstop", [state]);

            // Reset this so the next query doesn't make it appear like content is playing.
            self.lastPlayerData = {};
        });

        $(castPlayer).on("playbackprogress", function (e, data) {

            console.log('cc: positionchange');
            var state = self.getPlayerStateInternal(data);

            $(self).trigger("positionchange", [state]);
        });

        self.play = function (options) {

            Dashboard.getCurrentUser().done(function (user) {

                if (options.items) {

                    self.playWithCommand(options, 'PlayNow');

                } else {

                    self.getItemsForPlayback({

                        Ids: options.ids.join(',')

                    }).done(function (result) {

                        options.items = result.Items;
                        self.playWithCommand(options, 'PlayNow');

                    });
                }

            });

        };

        self.playWithCommand = function (options, command) {

            if (!options.items) {
                ApiClient.getItem(Dashboard.getCurrentUserId(), options.ids[0]).done(function (item) {

                    options.items = [item];
                    self.playWithCommand(options, command);
                });

                return;
            }

            playInternal(options.items);
        };

        function playInternal(items, serverAddress) {

            playItemInternal(items[0], null, serverAddress);

        }

        function playItemInternal(items, startPosition) {

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

            //getPlaybackInfo(item.Id, deviceProfile, startPosition).done(function (playbackInfoResult) {

            //    if (validatePlaybackInfoResult(playbackInfoResult)) {

            //        var mediaSource = getOptimalMediaSource(item.MediaType, playbackInfoResult.MediaSources);

            //        if (mediaSource) {

            //            if (mediaSource.RequiresOpening) {

            //                getLiveStream(item.Id, playbackInfoResult.PlaySessionId, deviceProfile, startPosition, mediaSource, null, null).done(function (openLiveStreamResult) {

            //                    openLiveStreamResult.MediaSource.enableDirectPlay = supportsDirectPlay(openLiveStreamResult.MediaSource);

            //                    playInternalPostMediaSourceSelection(item, openLiveStreamResult.MediaSource, startPosition, callback);
            //                });

            //            } else {
            //                playInternalPostMediaSourceSelection(item, mediaSource, startPosition, callback);
            //            }
            //        } else {
            //            Dashboard.hideModalLoadingMsg();
            //            MediaController.showPlaybackInfoErrorMessage('NoCompatibleStream');
            //        }
            //    }

            //});
        }

        self.unpause = function () {
            currentDevice.getMediaControl().play();
        };

        self.pause = function () {
            currentDevice.getMediaControl().pause();
        };

        self.shuffle = function (id) {

            var userId = Dashboard.getCurrentUserId();

            ApiClient.getItem(userId, id).done(function (item) {

                self.playWithCommand({

                    items: [item]

                }, 'Shuffle');

            });

        };

        self.instantMix = function (id) {

            var userId = Dashboard.getCurrentUserId();

            ApiClient.getItem(userId, id).done(function (item) {

                self.playWithCommand({

                    items: [item]

                }, 'InstantMix');

            });

        };

        self.canQueueMediaType = function (mediaType) {
            return false;
        };

        self.queue = function (options) {
        };

        self.queueNext = function (options) {
        };

        self.stop = function () {
            currentDevice.getMediaControl().stop();
        };

        self.displayContent = function (options) {

        };

        self.mute = function () {
            currentDevice.getVolumeControl().setMute(true);
        };

        self.unMute = function () {
            currentDevice.getVolumeControl().setMute(false);
        };

        self.toggleMute = function () {

            var volumeControl = currentDevice.getVolumeControl();

            volumeControl.setMute(!volumeControl.getMute());
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

            return ConnectHelper.getDeviceList().filter(function (d) {

                return isValid(d);

            }).map(convertDeviceToTarget);
        };

        self.seek = function (position) {
        };

        self.setAudioStreamIndex = function (index) {
        };

        self.setSubtitleStreamIndex = function (index) {
        };

        self.nextTrack = function () {
        };

        self.previousTrack = function () {
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

            currentDevice.getVolumeControl().volumeDown();
        };

        self.volumeUp = function () {

            currentDevice.getVolumeControl().volumeUp();
        };

        self.setVolume = function (vol) {

            vol = Math.min(vol, 100);
            vol = Math.max(vol, 0);

            currentDevice.getVolumeControl().setVolume(vol / 100);
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

            console.log(JSON.stringify(data));
            return data;
        };

        function cleanupSession() {

            if (currentDevice != null) {
                currentDevice.off("ready");
                currentDevice.off("disconnect");

                currentDevice.disconnect();
            }

            currentPairedDeviceId = null;
            currentDevice = null;
        }

        function onDeviceReady(device, deferred) {

            if (currentPairingDeviceId != device.getId()) {
                console.log('device ready fired for a different device. ignoring.');
                return;
            }

            deferred.resolve();
        }

        self.tryPair = function (target) {

            var deferred = $.Deferred();

            var device = ConnectHelper.getDeviceList().filter(function (d) {

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

            var deviceId = device.getId();
            currentPairingDeviceId = deviceId;

            console.log('Will attempt to connect to Connect device');

            Dashboard.showModalLoadingMsg();
            setTimeout(Dashboard.hideModalLoadingMsg, 3000);

            if (device.isReady()) {
                console.log('Device is already ready, calling onDeviceReady');
                onDeviceReady(device, deferred);
            } else {

                console.log('Binding device ready handler');

                device.on("ready", function () {
                    console.log('device.ready fired');
                    onDeviceReady(device, deferred);
                });

                device.on("disconnect", function () {
                    device.off("ready");
                    device.off("disconnect");
                });

                console.log('Calling device.connect');
                device.connect();
            }
        };

        $(MediaController).on('playerchange', function (e, newPlayer, newTarget) {

            if (currentPairedDeviceId) {
                if (newTarget.id != currentPairedDeviceId) {
                    if (currentDevice) {
                        console.log('Disconnecting from connect device');
                        cleanupSession();
                    }
                }
            }
        });
    }

    function initSdk() {

        MediaController.registerPlayer(new connectPlayer());
    }

    initSdk();

})();