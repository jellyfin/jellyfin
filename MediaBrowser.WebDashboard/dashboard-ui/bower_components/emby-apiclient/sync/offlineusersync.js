define(['localassetmanager'], function (localAssetManager) {
    'use strict';

    function syncNext(users, index, resolve, reject, apiClient, server) {

        var length = users.length;

        if (index >= length) {

            resolve();
            return;
        }

        var onFinish = function () {
            syncNext(users, index + 1, resolve, reject, apiClient, server);
        };

        syncUser(users[index], apiClient).then(onFinish, onFinish);
    }

    function syncUser(user, apiClient) {

        return apiClient.getOfflineUser(user.Id).then(function (result) {

            return localAssetManager.saveOfflineUser(result);

        }, function () {

            // TODO: We should only delete if there's a 401 response
            return localAssetManager.deleteOfflineUser(user.Id).catch(function () {
                return Promise.resolve();
            });
        });
    }

    return function offlineUserSync() {

        var self = this;

        self.sync = function (apiClient, server) {

            return new Promise(function (resolve, reject) {

                var users = server.Users || [];
                syncNext(users, 0, resolve, reject, apiClient, server);
            });
        };
    };
});