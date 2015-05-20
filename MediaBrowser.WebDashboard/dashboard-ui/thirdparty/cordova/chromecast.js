(function () {

    var PlayerName = "Chromecast";
    var ApplicationID = "F4EB2E8E";
    var currentPairingDeviceId;
    var currentPairedDeviceId;
    var currentDeviceFriendlyName;
    var currentWebAppSession;

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

        var endpointInfo;
        function getEndpointInfo() {

            if (endpointInfo) {

                var deferred = $.Deferred();
                deferred.resolveWith(null, [endpointInfo]);
                return deferred.promise();
            }

            return ApiClient.getJSON(ApiClient.getUrl('System/Endpoint')).done(function (info) {

                endpointInfo = info;
            });
        }

        function sendMessageToDevice(message) {

            var bitrateSetting = AppSettings.maxChromecastBitrate();

            message = $.extend(message, {
                userId: Dashboard.getCurrentUserId(),
                deviceId: ApiClient.deviceId(),
                accessToken: ApiClient.accessToken(),
                serverAddress: ApiClient.serverAddress(),
                maxBitrate: bitrateSetting,
                receiverName: currentDeviceFriendlyName
            });

            getEndpointInfo().done(function (endpoint) {

                if (endpoint.IsLocal || endpoint.IsInNetwork) {
                    ApiClient.getSystemInfo().done(function (info) {

                        message.serverAddress = info.LocalAddress;
                        sendMessageInternal(message);
                    });
                } else {
                    sendMessageInternal(message);
                }
            });

        }

        function sendMessageInternal(message) {
            currentWebAppSession.sendText(JSON.stringify(message));
        }

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

            // Convert the items to smaller stubs to send the minimal amount of information
            options.items = options.items.map(function (i) {

                return {
                    Id: i.Id,
                    Name: i.Name,
                    Type: i.Type,
                    MediaType: i.MediaType,
                    IsFolder: i.IsFolder
                };
            });

            sendMessageToDevice({
                options: options,
                command: command
            });
        };

        self.unpause = function () {
            sendMessageToDevice({
                command: 'Unpause'
            });
        };

        self.pause = function () {
            sendMessageToDevice({
                command: 'Pause'
            });
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
            sendMessageToDevice({
                command: 'Stop'
            });
        };

        self.displayContent = function (options) {

            sendMessageToDevice({
                options: options,
                command: 'DisplayContent'
            });
        };

        self.mute = function () {
            sendMessageToDevice({
                command: 'Mute'
            });
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

        function isChromecast(name) {

            name = (name || '').toLowerCase();
            var validTokens = ['nexusplayer', 'chromecast', 'eurekadongle'];

            return validTokens.filter(function (t) {

                return name.replace(' ', '').indexOf(t) != -1;

            }).length > 0;
        }

        self.getTargets = function () {

            var manager = ConnectSDK.discoveryManager;

            return manager.getDeviceList().filter(function (d) {

                return isChromecast(d.getModelName()) || isChromecast(d.getFriendlyName());

            }).map(convertDeviceToTarget);
        };

        self.seek = function (position) {
            sendMessageToDevice({
                options: {
                    position: position
                },
                command: 'Seek'
            });
        };

        self.setAudioStreamIndex = function (index) {
            sendMessageToDevice({
                options: {
                    index: index
                },
                command: 'SetAudioStreamIndex'
            });
        };

        self.setSubtitleStreamIndex = function (index) {
            sendMessageToDevice({
                options: {
                    index: index
                },
                command: 'SetSubtitleStreamIndex'
            });
        };

        self.nextTrack = function () {
            sendMessageToDevice({
                options: {},
                command: 'NextTrack'
            });
        };

        self.previousTrack = function () {
            sendMessageToDevice({
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

            sendMessageToDevice({
                options: {
                    volume: vol
                },
                command: 'SetVolume'
            });
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

        function onMessage(message) {

            if (message.type == 'playbackerror') {

                var errorCode = message.data;

                setTimeout(function () {
                    Dashboard.alert({
                        message: Globalize.translate('MessagePlaybackError' + errorCode),
                        title: Globalize.translate('HeaderPlaybackError')
                    });
                }, 300);

            }
            else if (message.type == 'connectionerror') {

                setTimeout(function () {
                    Dashboard.alert({
                        message: Globalize.translate('MessageChromecastConnectionError'),
                        title: Globalize.translate('HeaderError')
                    });
                }, 300);

            }
            else if (message.type && message.type.indexOf('playback') == 0) {
                $(castPlayer).trigger(message.type, [message.data]);
            }
        }

        function onSessionConnected(device, session) {

            // hold on to a reference
            currentWebAppSession = session.acquire();

            session.connect().success(function () {

                console.log('session.connect succeeded');

                MediaController.setActivePlayer(PlayerName, convertDeviceToTarget(device));
                currentDeviceFriendlyName = device.getFriendlyName();
                currentPairedDeviceId = device.getId();

                $(castPlayer).trigger('connect');

                sendMessageToDevice({
                    options: {},
                    command: 'Identify'
                });
            });

            session.on('message', function (message) {
                // message could be either a string or an object
                if (typeof message === 'string') {
                    onMessage(JSON.parse(message));
                } else {
                    onMessage(message);
                }
            });

            session.on('disconnect', function () {

                console.log("session disconnected");

                if (currentPairedDeviceId == device.getId()) {
                    onDisconnected();
                    MediaController.removeActivePlayer(PlayerName);
                }

            });

        }
        
        function onDisconnected() {
            currentWebAppSession = null;
            currentPairedDeviceId = null;
            currentDeviceFriendlyName = null;
        }

        function launchWebApp(device) {
            device.getWebAppLauncher().launchWebApp(ApplicationID).success(function (session) {

                console.log('launchWebApp success. calling onSessionConnected');
                onSessionConnected(device, session);

            }).error(function (err) {

                console.log('launchWebApp error: ' + JSON.stringify(err) + '. calling joinWebApp');

                device.getWebAppLauncher().joinWebApp(ApplicationID).success(function (session) {

                    console.log('joinWebApp success. calling onSessionConnected');
                    onSessionConnected(device, session);

                }).error(function (err1) {

                    console.log('joinWebApp error:' + JSON.stringify(err1));

                });

            });
        }

        function onDeviceReady(device) {

            if (currentPairingDeviceId != device.getId()) {
                console.log('device ready fired for a different device. ignoring.');
                return;
            }

            console.log('calling launchWebApp');

            setTimeout(function () {

                launchWebApp(device);

            }, 0);
        }

        var boundHandlers = [];

        self.tryPair = function (target) {

            var deferred = $.Deferred();

            var manager = ConnectSDK.discoveryManager;

            var device = manager.getDeviceList().filter(function (d) {

                return d.getId() == target.id;
            })[0];

            if (device) {

                var deviceId = device.getId();
                currentPairingDeviceId = deviceId;

                console.log('Will attempt to connect to Chromecast');

                if (device.isReady()) {
                    console.log('Device is already ready, calling onDeviceReady');
                    onDeviceReady(device);
                } else {

                    console.log('Binding device ready handler');

                    if (boundHandlers.indexOf(deviceId) == -1) {

                        boundHandlers.push(deviceId);
                        device.on("ready", function () {
                            console.log('device.ready fired');
                            onDeviceReady(device);
                        });
                    }

                    console.log('Calling device.connect');
                    device.connect();
                }
                //deferred.resolve();

            } else {
                deferred.reject();
            }

            return deferred.promise();
        };

        $(MediaController).on('playerchange', function (e, newPlayer, newTarget) {

            if (currentPairedDeviceId) {
                if (newTarget.id != currentPairedDeviceId) {
                    if (currentWebAppSession) {
                        console.log('Disconnecting from chromecast');
                        currentWebAppSession.disconnect();
                        onDisconnected();
                    }
                }
            }
        });
    }

    function initSdk() {

        MediaController.registerPlayer(new chromecastPlayer());
    }

    initSdk();

})();