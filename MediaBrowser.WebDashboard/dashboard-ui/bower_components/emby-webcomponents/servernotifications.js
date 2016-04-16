define(['connectionManager', 'events'], function (connectionManager, events) {

    var serverNotifications = {};

    function onWebSocketMessageReceived(e, msg) {

        var apiClient = this;

        if (msg.MessageType === "LibraryChanged") {
        }
        else if (msg.MessageType === "ServerShuttingDown") {
            events.trigger(serverNotifications, 'ServerShuttingDown', [apiClient]);
        }
        else if (msg.MessageType === "ServerRestarting") {
            events.trigger(serverNotifications, 'ServerRestarting', [apiClient]);
        }
        else if (msg.MessageType === "RestartRequired") {
            events.trigger(serverNotifications, 'RestartRequired', [apiClient]);
        }
        else if (msg.MessageType === "UserDataChanged") {

            if (msg.Data.UserId == apiClient.getCurrentUserId()) {

                for (var i = 0, length = msg.Data.UserDataList.length; i < length; i++) {
                    events.trigger(serverNotifications, 'UserDataChanged', [apiClient, msg.Data.UserDataList[i]]);
                }
            }
        }
    }

    function bindEvents(apiClient) {

        events.off(apiClient, "websocketmessage", onWebSocketMessageReceived);
        events.on(apiClient, "websocketmessage", onWebSocketMessageReceived);
    }

    //var current = connectionManager.currentApiClient();
    //if (current) {
    //    bindEvents(current);
    //}

    events.on(connectionManager, 'apiclientcreated', function (e, newApiClient) {

        bindEvents(newApiClient);
    });

    return serverNotifications;
});