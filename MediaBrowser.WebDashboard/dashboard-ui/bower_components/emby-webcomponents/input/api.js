define(['connectionManager', 'playbackManager', 'events', 'inputManager', 'focusManager', 'appRouter'], function (connectionManager, playbackManager, events, inputManager, focusManager, appRouter) {
    'use strict';

    var serverNotifications = {};

    function notifyApp() {

        inputManager.notify();
    }

    function displayMessage(cmd) {

        var args = cmd.Arguments;

        if (args.TimeoutMs) {

            require(['toast'], function (toast) {
                toast({ title: args.Header, text: args.Text });
            });

        }
        else {
            require(['alert'], function (alert) {
                alert({ title: args.Header, text: args.Text });
            });
        }
    }

    function displayContent(cmd, apiClient) {

        if (!playbackManager.isPlayingLocally(['Video', 'Book', 'Game'])) {
            appRouter.showItem(cmd.Arguments.ItemId, apiClient.serverId());
        }
    }

    function playTrailers(apiClient, itemId) {

        apiClient.getItem(apiClient.getCurrentUserId(), itemId).then(function (item) {

            playbackManager.playTrailers(item);
        });
    }

    function processGeneralCommand(cmd, apiClient) {

        // Full list
        // https://github.com/MediaBrowser/MediaBrowser/blob/master/MediaBrowser.Model/Session/GeneralCommand.cs#L23
        //console.log('Received command: ' + cmd.Name);

        switch (cmd.Name) {

            case 'Select':
                inputManager.trigger('select');
                return;
            case 'Back':
                inputManager.trigger('back');
                return;
            case 'MoveUp':
                inputManager.trigger('up');
                return;
            case 'MoveDown':
                inputManager.trigger('down');
                return;
            case 'MoveLeft':
                inputManager.trigger('left');
                return;
            case 'MoveRight':
                inputManager.trigger('right');
                return;
            case 'PageUp':
                inputManager.trigger('pageup');
                return;
            case 'PageDown':
                inputManager.trigger('pagedown');
                return;
            case 'PlayTrailers':
                playTrailers(apiClient, cmd.Arguments.ItemId);
                break;
            case 'SetRepeatMode':
                playbackManager.setRepeatMode(cmd.Arguments.RepeatMode);
                break;
            case 'VolumeUp':
                inputManager.trigger('volumeup');
                return;
            case 'VolumeDown':
                inputManager.trigger('volumedown');
                return;
            case 'ChannelUp':
                inputManager.trigger('channelup');
                return;
            case 'ChannelDown':
                inputManager.trigger('channeldown');
                return;
            case 'Mute':
                inputManager.trigger('mute');
                return;
            case 'Unmute':
                inputManager.trigger('unmute');
                return;
            case 'ToggleMute':
                inputManager.trigger('togglemute');
                return;
            case 'SetVolume':
                notifyApp();
                playbackManager.setVolume(cmd.Arguments.Volume);
                break;
            case 'SetAudioStreamIndex':
                notifyApp();
                playbackManager.setAudioStreamIndex(parseInt(cmd.Arguments.Index));
                break;
            case 'SetSubtitleStreamIndex':
                notifyApp();
                playbackManager.setSubtitleStreamIndex(parseInt(cmd.Arguments.Index));
                break;
            case 'ToggleFullscreen':
                inputManager.trigger('togglefullscreen');
                return;
            case 'GoHome':
                inputManager.trigger('home');
                return;
            case 'GoToSettings':
                inputManager.trigger('settings');
                return;
            case 'DisplayContent':
                displayContent(cmd, apiClient);
                break;
            case 'GoToSearch':
                inputManager.trigger('search');
                return;
            case 'DisplayMessage':
                displayMessage(cmd);
                break;
            case 'ToggleOsd':
                // todo
                break;
            case 'ToggleContextMenu':
                // todo
                break;
            case 'TakeScreenShot':
                // todo
                break;
            case 'SendKey':
                // todo
                break;
            case 'SendString':
                // todo
                focusManager.sendText(cmd.Arguments.String);
                break;
            default:
                console.log('processGeneralCommand does not recognize: ' + cmd.Name);
                break;
        }

        notifyApp();
    }

    function onMessageReceived(e, msg) {

        var apiClient = this;

        if (msg.MessageType === "Play") {

            notifyApp();
            var serverId = apiClient.serverInfo().Id;

            if (msg.Data.PlayCommand === "PlayNext") {
                playbackManager.queueNext({ ids: msg.Data.ItemIds, serverId: serverId });
            }
            else if (msg.Data.PlayCommand === "PlayLast") {
                playbackManager.queue({ ids: msg.Data.ItemIds, serverId: serverId });
            }
            else {
                playbackManager.play({
                    ids: msg.Data.ItemIds,
                    startPositionTicks: msg.Data.StartPositionTicks,
                    mediaSourceId: msg.Data.MediaSourceId,
                    audioStreamIndex: msg.Data.AudioStreamIndex,
                    subtitleStreamIndex: msg.Data.SubtitleStreamIndex,
                    startIndex: msg.Data.StartIndex,
                    serverId: serverId
                });
            }

        }
        else if (msg.MessageType === "Playstate") {

            if (msg.Data.Command === 'Stop') {
                inputManager.trigger('stop');
            }
            else if (msg.Data.Command === 'Pause') {
                inputManager.trigger('pause');
            }
            else if (msg.Data.Command === 'Unpause') {
                inputManager.trigger('play');
            }
            else if (msg.Data.Command === 'PlayPause') {
                inputManager.trigger('playpause');
            }
            else if (msg.Data.Command === 'Seek') {
                playbackManager.seek(msg.Data.SeekPositionTicks);
            }
            else if (msg.Data.Command === 'NextTrack') {
                inputManager.trigger('next');
            }
            else if (msg.Data.Command === 'PreviousTrack') {
                inputManager.trigger('previous');
            } else {
                notifyApp();
            }
        }
        else if (msg.MessageType === "GeneralCommand") {
            var cmd = msg.Data;
            processGeneralCommand(cmd, apiClient);
        }
        else if (msg.MessageType === "UserDataChanged") {

            if (msg.Data.UserId === apiClient.getCurrentUserId()) {

                for (var i = 0, length = msg.Data.UserDataList.length; i < length; i++) {
                    events.trigger(serverNotifications, 'UserDataChanged', [apiClient, msg.Data.UserDataList[i]]);
                }
            }
        }
        else {

            events.trigger(serverNotifications, msg.MessageType, [apiClient, msg.Data]);
        }

    }

    function bindEvents(apiClient) {

        events.off(apiClient, "message", onMessageReceived);
        events.on(apiClient, "message", onMessageReceived);
    }

    connectionManager.getApiClients().forEach(bindEvents);

    events.on(connectionManager, 'apiclientcreated', function (e, newApiClient) {

        bindEvents(newApiClient);
    });

    return serverNotifications;
});