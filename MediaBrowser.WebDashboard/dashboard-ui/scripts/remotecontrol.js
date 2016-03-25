define([], function () {

    function sendPlayCommand(options, playType) {

        var sessionId = MediaController.getPlayerInfo().id;

        var ids = options.ids || options.items.map(function (i) {
            return i.Id;
        });

        var remoteOptions = {
            ItemIds: ids.join(','),

            PlayCommand: playType
        };

        if (options.startPositionTicks) {
            remoteOptions.startPositionTicks = options.startPositionTicks;
        }

        ApiClient.sendPlayCommand(sessionId, remoteOptions);
    }

    function sendPlayStateCommand(command, options) {

        var sessionId = MediaController.getPlayerInfo().id;

        ApiClient.sendPlayStateCommand(sessionId, command, options);
    }

    function remoteControlPlayer() {

        var self = this;

        self.name = 'Remote Control';

        function sendCommandByName(name, options) {

            var command = {
                Name: name
            };

            if (options) {
                command.Arguments = options;
            }

            self.sendCommand(command);
        }

        self.sendCommand = function (command) {

            var sessionId = MediaController.getPlayerInfo().id;

            ApiClient.sendCommand(sessionId, command);
        };

        self.play = function (options) {

            sendPlayCommand(options, 'PlayNow');
        };

        self.shuffle = function (id) {

            sendPlayCommand({ ids: [id] }, 'PlayShuffle');
        };

        self.instantMix = function (id) {

            sendPlayCommand({ ids: [id] }, 'PlayInstantMix');
        };

        self.queue = function (options) {

            sendPlayCommand(options, 'PlayNext');
        };

        self.queueNext = function (options) {

            sendPlayCommand(options, 'PlayLast');
        };

        self.canQueueMediaType = function (mediaType) {

            return mediaType == 'Audio' || mediaType == 'Video';
        };

        self.stop = function () {
            sendPlayStateCommand('stop');
        };

        self.nextTrack = function () {
            sendPlayStateCommand('nextTrack');
        };

        self.previousTrack = function () {
            sendPlayStateCommand('previousTrack');
        };

        self.seek = function (positionTicks) {
            sendPlayStateCommand('seek',
                {
                    SeekPositionTicks: positionTicks
                });
        };

        self.pause = function () {
            sendPlayStateCommand('Pause');
        };

        self.unpause = function () {
            sendPlayStateCommand('Unpause');
        };

        self.mute = function () {
            sendCommandByName('Mute');
        };

        self.unMute = function () {
            sendCommandByName('Unmute');
        };

        self.toggleMute = function () {
            sendCommandByName('ToggleMute');
        };

        self.setVolume = function (vol) {
            sendCommandByName('SetVolume', {
                Volume: vol
            });
        };

        self.volumeUp = function () {
            sendCommandByName('VolumeUp');
        };

        self.volumeDown = function () {
            sendCommandByName('VolumeDown');
        };

        self.toggleFullscreen = function () {
            sendCommandByName('ToggleFullscreen');
        };

        self.setAudioStreamIndex = function (index) {
            sendCommandByName('SetAudioStreamIndex', {
                Index: index
            });
        };

        self.setSubtitleStreamIndex = function (index) {
            sendCommandByName('SetSubtitleStreamIndex', {
                Index: index
            });
        };

        self.setRepeatMode = function (mode) {

            sendCommandByName('SetRepeatMode', {
                RepeatMode: mode
            });
        };

        self.displayContent = function (options) {

            sendCommandByName('DisplayContent', options);
        };

        self.getPlayerState = function () {

            return new Promise(function (resolve, reject) {

                var apiClient = window.ApiClient;

                if (apiClient) {
                    apiClient.getSessions().then(function (sessions) {

                        var currentTargetId = MediaController.getPlayerInfo().id;

                        // Update existing data
                        //updateSessionInfo(popup, msg.Data);
                        var session = sessions.filter(function (s) {
                            return s.Id == currentTargetId;
                        })[0];

                        if (session) {
                            session = getPlayerState(session);
                        }

                        resolve(session);
                    });
                } else {
                    resolve({});
                }
            });
        };

        var pollInterval;

        function onPollIntervalFired() {

            if (!ApiClient.isWebSocketOpen()) {
                var apiClient = window.ApiClient;

                if (apiClient) {
                    apiClient.getSessions().then(processUpdatedSessions);
                }
            }
        }

        self.subscribeToPlayerUpdates = function () {

            self.isUpdating = true;

            if (ApiClient.isWebSocketOpen()) {

                ApiClient.sendWebSocketMessage("SessionsStart", "100,800");
            }
            if (pollInterval) {
                clearInterval(pollInterval);
                pollInterval = null;
            }
            pollInterval = setInterval(onPollIntervalFired, 5000);
        };

        function unsubscribeFromPlayerUpdates() {

            self.isUpdating = true;

            if (ApiClient.isWebSocketOpen()) {

                ApiClient.sendWebSocketMessage("SessionsStop");
            }
            if (pollInterval) {
                clearInterval(pollInterval);
                pollInterval = null;
            }
        }

        var playerListenerCount = 0;
        self.beginPlayerUpdates = function () {

            if (playerListenerCount <= 0) {

                playerListenerCount = 0;

                self.subscribeToPlayerUpdates();
            }

            playerListenerCount++;
        };

        self.endPlayerUpdates = function () {

            playerListenerCount--;

            if (playerListenerCount <= 0) {

                unsubscribeFromPlayerUpdates();
                playerListenerCount = 0;
            }
        };

        self.getTargets = function () {

            return new Promise(function (resolve, reject) {

                var sessionQuery = {
                    ControllableByUserId: Dashboard.getCurrentUserId()
                };

                var apiClient = window.ApiClient;

                if (apiClient) {
                    apiClient.getSessions(sessionQuery).then(function (sessions) {

                        var targets = sessions.filter(function (s) {

                            return s.DeviceId != apiClient.deviceId();

                        }).map(function (s) {
                            return {
                                name: s.DeviceName,
                                deviceName: s.DeviceName,
                                id: s.Id,
                                playerName: self.name,
                                appName: s.Client,
                                playableMediaTypes: s.PlayableMediaTypes,
                                isLocalPlayer: false,
                                supportedCommands: s.SupportedCommands
                            };
                        });

                        resolve(targets);

                    }, function () {

                        reject();
                    });

                } else {
                    resolve([]);
                }
            });
        };

        self.tryPair = function(target) {

            return new Promise(function (resolve, reject) {

                resolve();
            });
        };
    }

    var player = new remoteControlPlayer();

    MediaController.registerPlayer(player);

    function getPlayerState(session) {

        return session;
    }

    function firePlaybackEvent(name, session) {

        Events.trigger(player, name, [getPlayerState(session)]);
    }

    function onWebSocketConnectionChange() {

        // Reconnect
        if (player.isUpdating) {
            player.subscribeToPlayerUpdates();
        }
    }

    function processUpdatedSessions(sessions) {
        
        var currentTargetId = MediaController.getPlayerInfo().id;

        // Update existing data
        //updateSessionInfo(popup, msg.Data);
        var session = sessions.filter(function (s) {
            return s.Id == currentTargetId;
        })[0];

        if (session) {
            firePlaybackEvent('playstatechange', session);
        }
    }

    function onWebSocketMessageReceived(e, msg) {

        var apiClient = this;

        if (msg.MessageType === "Sessions") {

            processUpdatedSessions(msg.Data);
        }
        else if (msg.MessageType === "SessionEnded") {

            console.log("Server reports another session ended");

            if (MediaController.getPlayerInfo().id == msg.Data.Id) {
                MediaController.setDefaultPlayerActive();
            }
        }
        else if (msg.MessageType === "PlaybackStart") {

            if (msg.Data.DeviceId != apiClient.deviceId()) {
                if (MediaController.getPlayerInfo().id == msg.Data.Id) {
                    firePlaybackEvent('playbackstart', msg.Data);
                }
            }
        }
        else if (msg.MessageType === "PlaybackStopped") {

            if (msg.Data.DeviceId != apiClient.deviceId()) {
                if (MediaController.getPlayerInfo().id == msg.Data.Id) {
                    firePlaybackEvent('playbackstop', msg.Data);
                }
            }
        }
    }

    function initializeApiClient(apiClient) {
        Events.on(apiClient, "websocketmessage", onWebSocketMessageReceived);
        Events.on(apiClient, "websocketopen", onWebSocketConnectionChange);
    }

    if (window.ApiClient) {
        initializeApiClient(window.ApiClient);
    }

    Events.on(ConnectionManager, 'apiclientcreated', function (e, apiClient) {
        initializeApiClient(apiClient);
    });

});