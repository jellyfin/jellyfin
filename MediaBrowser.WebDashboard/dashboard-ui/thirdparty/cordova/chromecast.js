(function () {

    var PlayerName = "Chromecast";
    var ApplicationID = "2D4B1DA3";
    var currentPairingDeviceId;
    var currentPairedDeviceId;
    var currentDeviceFriendlyName;
    var currentWebAppSession;
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
                    ApiClient.getPublicSystemInfo().done(function (info) {

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

            position = parseInt(position);
            position = position / 10000000;

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

        function handleMessage(message) {
            // message could be either a string or an object
            if (typeof message === 'string') {
                onMessage(JSON.parse(message));
            } else {
                onMessage(message);
            }
        }

        function handleSessionDisconnect() {
            console.log("session disconnected");

            cleanupSession();
            MediaController.removeActivePlayer(PlayerName);
        }

        function setupWebAppSession(device, session) {

            // hold on to a reference
            currentWebAppSession = session.acquire();

            currentWebAppSession.on('message', handleMessage);

            currentWebAppSession.connect().success(function () {

                console.log('session.connect succeeded');
                currentWebAppSession.on('disconnect', handleSessionDisconnect);

                MediaController.setActivePlayer(PlayerName, convertDeviceToTarget(device));
                currentDeviceFriendlyName = device.getFriendlyName();
                currentPairedDeviceId = device.getId();
                currentDevice = device;

                $(castPlayer).trigger('connect');

                sendMessageToDevice({
                    options: {},
                    command: 'Identify'
                });

            }).error(handleSessionError);
        }

        function handleSessionError() {
            cleanupSession();
        }

        function cleanupSession() {

            var session = currentWebAppSession;

            if (session) {
                // Clean up listeners
                session.off("message");
                session.off("disconnect");

                // Release session to free up memory
                session.disconnect();
                session.release();
            }

            if (currentDevice != null) {
                currentDevice.off("ready");
                currentDevice.off("disconnect");

                currentDevice.disconnect();
            }

            currentWebAppSession = null;
            currentPairedDeviceId = null;
            currentDeviceFriendlyName = null;
            currentDevice = null;
        }

        function tryLaunchWebSession(device) {

            console.log('calling launchWebApp');
            device.getWebAppLauncher().launchWebApp(ApplicationID).success(function (session) {

                console.log('launchWebApp success. calling onSessionConnected');
                setupWebAppSession(device, session);

            }).error(function (err1) {

                console.log('launchWebApp error:' + JSON.stringify(err1));

            });
        }

        function tryJoinWebSession(device, enableRetry) {

            // First try to join existing session. If it fails, launch a new one

            console.log('calling joinWebApp');
            device.getWebAppLauncher().joinWebApp(ApplicationID).success(function (session) {

                console.log('joinWebApp success. calling onSessionConnected');
                setupWebAppSession(device, session);

            }).error(function (err) {

                console.log('joinWebApp error: ' + JSON.stringify(err));

                if (enableRetry) {
                    tryJoinWebSession(device, false);
                    return;
                }

                console.log('calling launchWebApp');
                tryLaunchWebSession(device);

            });
        }

        function launchWebApp(device) {

            tryJoinWebSession(device, true);
        }

        function onDeviceReady(device) {

            if (currentPairingDeviceId != device.getId()) {
                console.log('device ready fired for a different device. ignoring.');
                return;
            }

            console.log('creating webAppSession');

            launchWebApp(device);
        }

        self.tryPair = function (target) {

            var deferred = $.Deferred();

            var device = ConnectSDK.discoveryManager.getDeviceList().filter(function (d) {

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

            console.log('Will attempt to connect to Chromecast');

            if (device.isReady()) {
                console.log('Device is already ready, calling onDeviceReady');
                onDeviceReady(device);
            } else {

                console.log('Binding device ready handler');

                device.on("ready", function () {
                    console.log('device.ready fired');
                    onDeviceReady(device);
                });

                device.on("disconnect", function () {
                    device.off("ready");
                });

                console.log('Calling device.connect');
                device.connect();
            }
        };

        $(MediaController).on('playerchange', function (e, newPlayer, newTarget) {

            if (currentPairedDeviceId) {
                if (newTarget.id != currentPairedDeviceId) {
                    if (currentWebAppSession) {
                        console.log('Disconnecting from chromecast');
                        cleanupSession();
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