(function (globalScope) {

    function offlineUserSync() {

        var self = this;

        self.sync = function (apiClient, server) {

            return new Promise(function (resolve, reject) {

                var users = server.Users || [];
                syncNext(users, 0, resolve, reject, apiClient, server);
            });
        };

        function syncNext(users, index, resolve, reject, apiClient, server) {

            var length = users.length;

            if (index >= length) {

                resolve();
                return;
            }

            var onFinish = function() {
                syncNext(users, index + 1, resolve, reject, apiClient, server);
            };

            syncUser(users[index], apiClient).then(onFinish, onFinish);
        }

        function syncUser(user, apiClient) {

            return new Promise(function (resolve, reject) {

                apiClient.getOfflineUser(user.Id).then(function (result) {

                    require(['localassetmanager'], function () {

                        LocalAssetManager.saveOfflineUser(result).then(resolve, resolve);
                    });

                }, function () {

                    // TODO: We should only delete if there's a 401 response

                    require(['localassetmanager'], function () {

                        LocalAssetManager.deleteOfflineUser(user.Id).then(resolve, resolve);
                    });
                });
            });
        }

    }

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.OfflineUserSync = offlineUserSync;

})(this);