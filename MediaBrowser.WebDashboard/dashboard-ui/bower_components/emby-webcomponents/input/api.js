define(['connectionManager', 'playbackManager', 'events', 'inputManager', 'focusManager', 'embyRouter'], function (connectionManager, playbackManager, events, inputManager, focusManager, embyRouter) {

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

        apiClient.getItem(apiClient.getCurrentUserId(), cmd.ItemId).then(function (item) {
            embyRouter.showItem(item);
        });
    }

    function processGeneralCommand(cmd, apiClient) {

        // Full list
        // https://github.com/MediaBrowser/MediaBrowser/blob/master/MediaBrowser.Model/Session/GeneralCommand.cs#L23
        //console.log('Received command: ' + cmd.Name);

        switch (cmd.Name) {

            case 'Select':
                inputManager.trigger('select');
                break;
            case 'Back':
                inputManager.trigger('back');
                break;
            case 'MoveUp':
                inputManager.trigger('up');
                break;
            case 'MoveDown':
                inputManager.trigger('down');
                break;
            case 'MoveLeft':
                inputManager.trigger('left');
                break;
            case 'MoveRight':
                inputManager.trigger('right');
                break;
            case 'PageUp':
                inputManager.trigger('pageup');
                break;
            case 'PageDown':
                inputManager.trigger('pagedown');
                break;
            case 'SetRepeatMode':
                playbackManager.setRepeatMode(cmd.Arguments.RepeatMode);
                break;
            case 'VolumeUp':
                inputManager.trigger('volumeup');
                break;
            case 'VolumeDown':
                inputManager.trigger('volumedown');
                break;
            case 'ChannelUp':
                inputManager.trigger('channelup');
                break;
            case 'ChannelDown':
                inputManager.trigger('channeldown');
                break;
            case 'Mute':
                inputManager.trigger('mute');
                break;
            case 'Unmute':
                inputManager.trigger('unmute');
                break;
            case 'ToggleMute':
                inputManager.trigger('togglemute');
                break;
            case 'SetVolume':
                playbackManager.volume(cmd.Arguments.Volume);
                break;
            case 'SetAudioStreamIndex':
                playbackManager.setAudioStreamIndex(parseInt(cmd.Arguments.Index));
                break;
            case 'SetSubtitleStreamIndex':
                playbackManager.setSubtitleStreamIndex(parseInt(cmd.Arguments.Index));
                break;
            case 'ToggleFullscreen':
                inputManager.trigger('togglefullscreen');
                break;
            case 'GoHome':
                inputManager.trigger('home');
                break;
            case 'GoToSettings':
                inputManager.trigger('settings');
                break;
            case 'DisplayContent':
                displayContent(cmd, apiClient);
                break;
            case 'GoToSearch':
                inputManager.trigger('search');
                break;
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
    }

    function onWebSocketMessageReceived(e, msg) {

        var apiClient = this;

        if (msg.MessageType === "Play") {

            var serverId = apiClient.serverInfo().Id;

            if (msg.Data.PlayCommand == "PlayNext") {
                playbackManager.queueNext({ ids: msg.Data.ItemIds, serverId: serverId });
            }
            else if (msg.Data.PlayCommand == "PlayLast") {
                playbackManager.queue({ ids: msg.Data.ItemIds, serverId: serverId });
            }
            else {
                playbackManager.play({ ids: msg.Data.ItemIds, startPositionTicks: msg.Data.StartPositionTicks, serverId: serverId });
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
            else if (msg.Data.Command === 'Seek') {
                playbackManager.seek(msg.Data.SeekPositionTicks);
            }
            else if (msg.Data.Command === 'NextTrack') {
                inputManager.trigger('next');
            }
            else if (msg.Data.Command === 'PreviousTrack') {
                inputManager.trigger('previous');
            }
        }
        else if (msg.MessageType === "GeneralCommand") {
            var cmd = msg.Data;
            processGeneralCommand(cmd, apiClient);
        }
    }

    function bindEvents(apiClient) {

        events.off(apiClient, "websocketmessage", onWebSocketMessageReceived);
        events.on(apiClient, "websocketmessage", onWebSocketMessageReceived);
    }

    var current = connectionManager.currentApiClient();
    if (current) {
        bindEvents(current);
    }

    events.on(connectionManager, 'apiclientcreated', function (e, newApiClient) {

        bindEvents(newApiClient);
    });

});