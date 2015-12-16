(function (globalScope) {

    function offlineUserSync() {

        var self = this;

        self.sync = function (apiClient, server) {

            var deferred = DeferredBuilder.Deferred();

            var users = server.Users || [];
            syncNext(users, 0, deferred, apiClient, server);

            return deferred.promise();
        };

        function syncNext(users, index, deferred, apiClient, server) {

            var length = users.length;

            if (index >= length) {

                deferred.resolve();
                return;
            }

            syncUser(users[index], apiClient).then(function () {

                syncNext(users, index + 1, deferred, apiClient, server);
            }, function () {
                syncNext(users, index + 1, deferred, apiClient, server);
            });
        }

        function syncUser(user, apiClient) {

            var deferred = DeferredBuilder.Deferred();

            apiClient.getOfflineUser(user.Id).then(function (result) {

                require(['localassetmanager'], function () {

                    LocalAssetManager.saveOfflineUser(result).then(function () {
                        deferred.resolve();
                    }, function () {
                        deferred.resolve();
                    });
                });

            }, function () {

                // TODO: We should only delete if there's a 401 response

                require(['localassetmanager'], function () {

                    LocalAssetManager.deleteOfflineUser(user.Id).then(function () {
                        deferred.resolve();
                    }, function () {
                        deferred.resolve();
                    });
                });
            });

            return deferred.promise();
        }

    }

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.OfflineUserSync = offlineUserSync;

})(this);