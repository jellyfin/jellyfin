(function () {

    var PlayerName = "Chromecast";
    var ApplicationID = "F4EB2E8E";
    var currentDevice;

    function chromecastPlayer() {

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

        $(castPlayer).on("connect", function (e) {

            console.log('cc: connect');
            // Reset this so the next query doesn't make it appear like content is playing.
            self.lastPlayerData = {};
        });

        $(castPlayer).on("playbackstart", function (e, data) {

            console.log('cc: playbackstart');

            castPlayer.initializeCastPlayer();

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

            castPlayer.loadMedia(options, command);
        };

        self.unpause = function () {
            castPlayer.playMedia();
        };

        self.pause = function () {
            castPlayer.pauseMedia();
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
            return mediaType == "Audio";
        };

        self.queue = function (options) {
            self.playWithCommnd(options, 'PlayLast');
        };

        self.queueNext = function (options) {
            self.playWithCommand(options, 'PlayNext');
        };

        self.stop = function () {
            castPlayer.stopMedia();
        };

        self.displayContent = function (options) {

            castPlayer.sendMessage({
                options: options,
                command: 'DisplayContent'
            });
        };

        self.mute = function () {
            castPlayer.mute();
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

        function getBaseTargetInfo() {
            var target = {};

            target.playerName = PlayerName;
            target.playableMediaTypes = ["Audio", "Video"];
            target.isLocalPlayer = false;
            target.appName = PlayerName;
            target.supportedCommands = [
                "VolumeUp",
                "VolumeDown",
                "Mute",
                "Unmute",
                "ToggleMute",
                "SetVolume",
                "SetAudioStreamIndex",
                "SetSubtitleStreamIndex",
                "DisplayContent"
            ];

            return target;
        }

        function convertDeviceToTarget(device) {

            var target = getBaseTargetInfo();

            target.name = target.deviceName = device.getFriendlyName();
            target.id = device.getId();

            return target;
        }

        self.getTargets = function () {

            var manager = ConnectSDK.discoveryManager;

            return manager.getDeviceList().map(convertDeviceToTarget);
        };

        self.seek = function (position) {
            castPlayer.seekMedia(position);
        };

        self.setAudioStreamIndex = function (index) {
            castPlayer.sendMessage({
                options: {
                    index: index
                },
                command: 'SetAudioStreamIndex'
            });
        };

        self.setSubtitleStreamIndex = function (index) {
            castPlayer.sendMessage({
                options: {
                    index: index
                },
                command: 'SetSubtitleStreamIndex'
            });
        };

        self.nextTrack = function () {
            castPlayer.sendMessage({
                options: {},
                command: 'NextTrack'
            });
        };

        self.previousTrack = function () {
            castPlayer.sendMessage({
                options: {},
                command: 'PreviousTrack'
            });
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

            self.setVolume(getCurrentVolume() - 2);
        };

        self.volumeUp = function () {

            self.setVolume(getCurrentVolume() + 2);
        };

        self.setVolume = function (vol) {

            vol = Math.min(vol, 100);
            vol = Math.max(vol, 0);

            castPlayer.setReceiverVolume(false, (vol / 100));
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

        self.tryPair = function (target) {

            var deferred = $.Deferred();
            deferred.resolve();
            return deferred.promise();
        };
    }

    function onDeviceLost() {

    }

    function initSdk() {

        var manager = ConnectSDK.discoveryManager;

        manager.addListener('devicelost', onDeviceLost);

        MediaController.registerPlayer(new chromecastPlayer());

        $(MediaController).on('playerchange', function () {

            if (MediaController.getPlayerInfo().name == PlayerName) {

                // launch app if needed
            }
        });
    }

    initSdk();

})();