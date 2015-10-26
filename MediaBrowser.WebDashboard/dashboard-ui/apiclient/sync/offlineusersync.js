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

            syncUser(users[index], apiClient).done(function () {

                syncNext(users, index + 1, deferred, apiClient, server);
            }).fail(function () {
                syncNext(users, index + 1, deferred, apiClient, server);
            });
        }

        function syncUser(user, apiClient) {

            var deferred = DeferredBuilder.Deferred();

            apiClient.getOfflineUser(user.Id).done(function (result) {

                require(['localassetmanager'], function () {

                    LocalAssetManager.saveOfflineUser(result).done(function () {
                        deferred.resolve();
                    }).fail(function () {
                        deferred.resolve();
                    });
                });

            }).fail(function () {

                // TODO: We should only delete if there's a 401 response

                require(['localassetmanager'], function () {

                    LocalAssetManager.deleteOfflineUser(user.Id).done(function () {
                        deferred.resolve();
                    }).fail(function () {
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