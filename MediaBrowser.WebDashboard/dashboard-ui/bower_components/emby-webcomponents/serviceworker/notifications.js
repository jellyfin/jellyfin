(function () {
    'use strict';

    var connectionManager;

    function getApiClient(serverId) {

        if (connectionManager) {
            return Promise.resolve(connectionManager.getApiClient(serverId));
        }

        //importScripts('serviceworker-cache-polyfill.js');

        return Promise.reject();
    }

    function executeAction(action, data, serverId) {

        return getApiClient(serverId).then(function (apiClient) {

            switch (action) {
                case 'cancel-install':
                    var id = data.id;
                    return apiClient.cancelPackageInstallation(id);
                case 'restart':
                    return apiClient.restartServer();
                default:
                    clients.openWindow("/");
                    return Promise.resolve();
            }
        });
    }

    self.addEventListener('notificationclick', function (event) {

        var notification = event.notification;
        notification.close();

        var data = notification.data;
        var serverId = data.serverId;
        var action = event.action;

        if (!action) {
            clients.openWindow("/");
            event.waitUntil(Promise.resolve());
            return;
        }

        event.waitUntil(executeAction(action, data, serverId));

    }, false);
})();