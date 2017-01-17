define(['playbackManager', 'events', 'serverNotifications', 'connectionManager'], function (playbackManager, events, serverNotifications, connectionManager) {
    'use strict';

    function getActivePlayerId() {
        var info = playbackManager.getPlayerInfo();
        return info ? info.id : null;
    }

    function sendPlayCommand(apiClient, options, playType) {

        var sessionId = getActivePlayerId();

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

        return apiClient.sendPlayCommand(sessionId, remoteOptions);
    }

    function sendPlayStateCommand(apiClient, command, options) {

        var sessionId = getActivePlayerId();

        apiClient.sendPlayStateCommand(sessionId, command, options);
    }

    return function () {

        var self = this;

        self.name = 'Remote Control';
        self.type = 'mediaplayer';
        self.isLocalPlayer = false;
        self.id = 'remoteplayer';

        var currentServerId;

        function getCurrentApiClient() {

            if (currentServerId) {
                return connectionManager.getApiClient(currentServerId);
            }

            return connectionManager.currentApiClient();
        }

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

            var sessionId = getActivePlayerId();

            var apiClient = getCurrentApiClient();
            apiClient.sendCommand(sessionId, command);
        };

        self.play = function (options) {

            var playOptions = {};
            playOptions.ids = options.ids || options.items.map(function (i) {
                return i.Id;
            });

            if (options.startPositionTicks) {
                playOptions.startPositionTicks = options.startPositionTicks;
            }

            return sendPlayCommand(getCurrentApiClient(), playOptions, 'PlayNow');
        };

        self.shuffle = function (item) {

            sendPlayCommand(getCurrentApiClient(), { ids: [item.Id] }, 'PlayShuffle');
        };

        self.instantMix = function (item) {

            sendPlayCommand(getCurrentApiClient(), { ids: [item.Id] }, 'PlayInstantMix');
        };

        self.queue = function (options) {

            sendPlayCommand(getCurrentApiClient(), options, 'PlayNext');
        };

        self.queueNext = function (options) {

            sendPlayCommand(getCurrentApiClient(), options, 'PlayLast');
        };

        self.canPlayMediaType = function (mediaType) {

            mediaType = (mediaType || '').toLowerCase();
            return mediaType === 'audio' || mediaType === 'video';
        };

        self.canQueueMediaType = function (mediaType) {
            return self.canPlayMediaType(mediaType);
        };

        self.stop = function () {
            sendPlayStateCommand(getCurrentApiClient(), 'stop');
        };

        self.nextTrack = function () {
            sendPlayStateCommand(getCurrentApiClient(), 'nextTrack');
        };

        self.previousTrack = function () {
            sendPlayStateCommand(getCurrentApiClient(), 'previousTrack');
        };

        self.seek = function (positionTicks) {
            sendPlayStateCommand(getCurrentApiClient(), 'seek',
            {
                SeekPositionTicks: positionTicks
            });
        };

        self.currentTime = function (val) {

            if (val != null) {
                return self.seek(val);
            }

            var state = self.lastPlayerData || {};
            state = state.PlayState || {};
            return state.PositionTicks;
        };

        self.duration = function () {
            var state = self.lastPlayerData || {};
            state = state.NowPlayingItem || {};
            return state.RunTimeTicks;
        };

        self.paused = function () {
            var state = self.lastPlayerData || {};
            state = state.PlayState || {};
            return state.IsPaused;
        };

        self.getVolume = function () {
            var state = self.lastPlayerData || {};
            state = state.PlayState || {};
            return state.VolumeLevel;
        };

        self.pause = function () {
            sendPlayStateCommand(getCurrentApiClient(), 'Pause');
        };

        self.unpause = function () {
            sendPlayStateCommand(getCurrentApiClient(), 'Unpause');
        };

        self.setMute = function (isMuted) {

            if (isMuted) {
                sendCommandByName('Mute');
            } else {
                sendCommandByName('Unmute');
            }
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

        self.audioTracks = function () {
            return [];
        };

        self.getAudioStreamIndex = function () {

        };

        self.setAudioStreamIndex = function (index) {
            sendCommandByName('SetAudioStreamIndex', {
                Index: index
            });
        };

        self.subtitleTracks = function () {
            return [];
        };

        self.getSubtitleStreamIndex = function () {

        };

        self.setSubtitleStreamIndex = function (index) {
            sendCommandByName('SetSubtitleStreamIndex', {
                Index: index
            });
        };

        self.getMaxStreamingBitrate = function () {

        };

        self.setMaxStreamingBitrate = function (bitrate) {

        };

        self.isFullscreen = function () {

        };

        self.toggleFullscreen = function () {

        };

        self.getRepeatMode = function () {

        };

        self.setRepeatMode = function (mode) {

            sendCommandByName('SetRepeatMode', {
                RepeatMode: mode
            });
        };

        self.displayContent = function (options) {

            sendCommandByName('DisplayContent', options);
        };

        self.isPlaying = function () {
            var state = self.lastPlayerData || {};
            return state.NowPlayingItem != null;
        };

        self.isPlayingVideo = function () {
            var state = self.lastPlayerData || {};
            state = state.NowPlayingItem || {};
            return state.MediaType === 'Video';
        };

        self.isPlayingAudio = function () {
            var state = self.lastPlayerData || {};
            state = state.NowPlayingItem || {};
            return state.MediaType === 'Audio';
        };

        self.getPlayerState = function () {

            var apiClient = getCurrentApiClient();

            if (apiClient) {
                return apiClient.getSessions().then(function (sessions) {

                    var currentTargetId = getActivePlayerId();

                    // Update existing data
                    //updateSessionInfo(popup, msg.Data);
                    var session = sessions.filter(function (s) {
                        return s.Id === currentTargetId;
                    })[0];

                    if (session) {
                        session = getPlayerState(session);
                    }

                    return session;
                });
            } else {
                return Promise.resolve({});
            }
        };

        var pollInterval;

        function onPollIntervalFired() {

            var apiClient = getCurrentApiClient();
            if (!apiClient.isWebSocketOpen()) {

                if (apiClient) {
                    apiClient.getSessions().then(function (sessions) {
                        processUpdatedSessions(sessions, apiClient);
                    });
                }
            }
        }

        self.subscribeToPlayerUpdates = function () {

            self.isUpdating = true;

            var apiClient = getCurrentApiClient();
            if (apiClient.isWebSocketOpen()) {

                apiClient.sendWebSocketMessage("SessionsStart", "100,800");
            }
            if (pollInterval) {
                clearInterval(pollInterval);
                pollInterval = null;
            }
            pollInterval = setInterval(onPollIntervalFired, 5000);
        };

        function unsubscribeFromPlayerUpdates() {

            self.isUpdating = true;

            var apiClient = getCurrentApiClient();
            if (apiClient.isWebSocketOpen()) {

                apiClient.sendWebSocketMessage("SessionsStop");
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

            var apiClient = getCurrentApiClient();

            var sessionQuery = {
                ControllableByUserId: apiClient.getCurrentUserId()
            };

            if (apiClient) {
                return apiClient.getSessions(sessionQuery).then(function (sessions) {

                    return sessions.filter(function (s) {
                        return s.DeviceId !== apiClient.deviceId();

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

                });

            } else {
                return Promise.resolve([]);
            }
        };

        self.tryPair = function (target) {

            return Promise.resolve();
        };

        function getPlayerState(session) {

            return session;
        }

        function normalizeImages(state) {

            if (state && state.NowPlayingItem) {

                var item = state.NowPlayingItem;

                if (!item.ImageTags || !item.ImageTags.Primary) {
                    if (item.PrimaryImageTag) {
                        item.ImageTags = item.ImageTags || {};
                        item.ImageTags.Primary = item.PrimaryImageTag;
                    }
                }
                if (item.BackdropImageTag && item.BackdropItemId === item.Id) {
                    item.BackdropImageTags = [item.BackdropImageTag];
                }
                if (item.BackdropImageTag && item.BackdropItemId !== item.Id) {
                    item.ParentBackdropImageTags = [item.BackdropImageTag];
                    item.ParentBackdropItemId = item.BackdropItemId;
                }
            }
        }

        function firePlaybackEvent(name, session) {

            var state = getPlayerState(session);

            normalizeImages(state);

            self.lastPlayerData = state;

            events.trigger(self, name, [state]);
        }

        function onWebSocketConnectionChange() {

            // Reconnect
            if (self.isUpdating) {
                self.subscribeToPlayerUpdates();
            }
        }

        function processUpdatedSessions(sessions, apiClient) {

            var serverId = apiClient.serverId();

            sessions.map(function (s) {
                if (s.NowPlayingItem) {
                    s.NowPlayingItem.ServerId = serverId;
                }
            });

            var currentTargetId = getActivePlayerId();
            // Update existing data
            //updateSessionInfo(popup, msg.Data);
            var session = sessions.filter(function (s) {
                return s.Id === currentTargetId;
            })[0];

            if (session) {
                firePlaybackEvent('statechange', session);
                firePlaybackEvent('timeupdate', session);
                firePlaybackEvent('pause', session);
            }
        }

        events.on(serverNotifications, 'Sessions', function (e, apiClient, data) {
            processUpdatedSessions(data, apiClient);
        });

        events.on(serverNotifications, 'SessionEnded', function (e, apiClient, data) {
            console.log("Server reports another session ended");

            if (getActivePlayerId() === data.Id) {
                playbackManager.setDefaultPlayerActive();
            }
        });

        events.on(serverNotifications, 'PlaybackStart', function (e, apiClient, data) {
            if (data.DeviceId !== apiClient.deviceId()) {
                if (getActivePlayerId() === data.Id) {
                    firePlaybackEvent('playbackstart', data);
                }
            }
        });

        events.on(serverNotifications, 'PlaybackStopped', function (e, apiClient, data) {
            if (data.DeviceId !== apiClient.deviceId()) {
                if (getActivePlayerId() === data.Id) {
                    firePlaybackEvent('playbackstop', data);
                }
            }
        });
    };
});