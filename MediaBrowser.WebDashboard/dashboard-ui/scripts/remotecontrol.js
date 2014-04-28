(function (window, document, $) {

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

        self.displayContent = function (options) {

            sendCommandByName('DisplayContent', {

                ItemName: options.itemName,
                ItemType: options.itemType,
                ItemId: options.itemId,
                Context: options.context

            });
        };

        self.getPlayerState = function () {

            var deferred = $.Deferred();

            ApiClient.getSessions().done(function (sessions) {

                var currentTargetId = MediaController.getPlayerInfo().id;

                // Update existing data
                //updateSessionInfo(popup, msg.Data);
                var session = sessions.filter(function (s) {
                    return s.Id == currentTargetId;
                })[0];

                if (session) {
                    session = getPlayerState(session);
                }

                deferred.resolveWith(null, [session]);
            });

            return deferred.promise();
        };

        function subscribeToPlayerUpdates() {

            if (ApiClient.isWebSocketOpen()) {

                ApiClient.sendWebSocketMessage("SessionsStart", "100,700");
            }
        }

        function unsubscribeFromPlayerUpdates() {

            if (ApiClient.isWebSocketOpen()) {

                ApiClient.sendWebSocketMessage("SessionsStop");
            }
        }

        var playerListenerCount = 0;
        self.beginPlayerUpdates = function () {

            if (playerListenerCount <= 0) {

                playerListenerCount = 0;

                subscribeToPlayerUpdates();
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

            var deferred = $.Deferred();

            var sessionQuery = {
                SupportsRemoteControl: true,
                ControllableByUserId: Dashboard.getCurrentUserId()
            };

            ApiClient.getSessions(sessionQuery).done(function (sessions) {

                var targets = sessions.filter(function (s) {

                    return s.DeviceId != ApiClient.deviceId();

                }).map(function (s) {
                    return {
                        name: s.DeviceName,
                        id: s.Id,
                        playerName: self.name,
                        appName: s.Client,
                        playableMediaTypes: s.PlayableMediaTypes,
                        isLocalPlayer: false,
                        supportedCommands: s.SupportedCommands
                    };
                });

                deferred.resolveWith(null, [targets]);

            }).fail(function () {

                deferred.reject();
            });

            return deferred.promise();
        };
    }

    var player = new remoteControlPlayer();

    MediaController.registerPlayer(player);

    function getPlayerState(session) {

        return session;
    }

    function firePlaybackEvent(name, session) {

        $(player).trigger(name, [getPlayerState(session)]);
    }

    function onWebSocketMessageReceived(e, msg) {

        if (msg.MessageType === "Sessions") {

            var currentTargetId = MediaController.getPlayerInfo().id;

            // Update existing data
            //updateSessionInfo(popup, msg.Data);
            var session = msg.Data.filter(function (s) {
                return s.Id == currentTargetId;
            })[0];

            if (session) {
                firePlaybackEvent('playstatechange', session);
            }
        }
        else if (msg.MessageType === "SessionEnded") {

            console.log("Server reports another session ended");

            if (MediaController.getPlayerInfo().id == msg.Data.Id) {
                MediaController.setDefaultPlayerActive();
            }
        }
        else if (msg.MessageType === "PlaybackStart") {
            firePlaybackEvent('playbackstart', msg.Data);
        }
        else if (msg.MessageType === "PlaybackStopped") {
            firePlaybackEvent('playbackstop', msg.Data);
        }
    }

    $(ApiClient).on("websocketmessage", onWebSocketMessageReceived);

})(window, document, jQuery);