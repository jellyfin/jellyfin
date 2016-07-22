define(['connectionManager', 'playbackManager', 'events', 'inputManager', 'focusManager', 'embyRouter'], function (connectionManager, playbackManager, events, inputManager, focusManager, embyRouter) {

    function onOneDocumentClick() {

        document.removeEventListener('click', onOneDocumentClick);
        document.removeEventListener('keydown', onOneDocumentClick);

        if (window.Notification) {
            Notification.requestPermission();
        }
    }
    document.addEventListener('click', onOneDocumentClick);
    document.addEventListener('keydown', onOneDocumentClick);

    function onLibraryChanged(data, apiClient) {

        var newItems = data.ItemsAdded;

        if (!newItems.length || /*AppInfo.isNativeApp ||*/ !window.Notification || Notification.permission !== "granted") {
            return;
        }

        if (playbackManager.isPlayingVideo()) {
            return;
        }

        apiClient.getItems(apiClient.getCurrentUserId(), {

            Recursive: true,
            Limit: 3,
            IsFolder: false,
            SortBy: "DateCreated",
            SortOrder: "Descending",
            ImageTypes: "Primary",
            Ids: newItems.join(',')

        }).then(function (result) {

            var items = result.Items;

            for (var i = 0, length = items.length ; i < length; i++) {

                var item = items[i];

                var notification = {
                    title: "New " + item.Type,
                    body: item.Name,
                    timeout: 15000,
                    vibrate: true,

                    data: {
                        //options: {
                        //    url: LibraryBrowser.getHref(item)
                        //}
                    }
                };

                var imageTags = item.ImageTags || {};

                if (imageTags.Primary) {

                    notification.icon = apiClient.getScaledImageUrl(item.Id, {
                        width: 80,
                        tag: imageTags.Primary,
                        type: "Primary"
                    });
                }

                var notif = new Notification(notification.title, notification);

                if (notif.show) {
                    notif.show();
                }

                if (notification.timeout) {
                    setTimeout(function () {

                        if (notif.close) {
                            notif.close();
                        }
                        else if (notif.cancel) {
                            notif.cancel();
                        }
                    }, notification.timeout);
                }
            }
        });
    }

    function onWebSocketMessageReceived(e, msg) {

        var apiClient = this;

        if (msg.MessageType === "LibraryChanged") {
            var cmd = msg.Data;
            onLibraryChanged(cmd, apiClient);
        }
    }

    function bindEvents(apiClient) {

        if (!apiClient) {
            return;
        }

        events.off(apiClient, "websocketmessage", onWebSocketMessageReceived);
        events.on(apiClient, "websocketmessage", onWebSocketMessageReceived);
    }

    bindEvents(connectionManager.currentApiClient());

    events.on(connectionManager, 'apiclientcreated', function (e, newApiClient) {

        bindEvents(newApiClient);
    });

});