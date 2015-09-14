(function (globalScope) {

    function serverSync(connectionManager) {

        var self = this;

        self.sync = function (server) {

            var deferred = DeferredBuilder.Deferred();

            if (!server.AccessToken && !server.ExchangeToken) {

                Logger.log('Skipping sync to server ' + server.Id + ' because there is no saved authentication information.');
                deferred.resolve();
                return deferred.promise();
            }

            var connectionOptions = {
                updateDateLastAccessed: false,
                enableWebSocket: false,
                reportCapabilities: false
            };

            connectionManager.connectToServer(server, connectionOptions).done(function (result) {

                if (result.State == MediaBrowser.ConnectionState.SignedIn) {
                    performSync(server, deferred);
                } else {
                    Logger.log('Unable to connect to server id: ' + server.Id);
                    deferred.reject();
                }

            }).fail(function () {

                Logger.log('Unable to connect to server id: ' + server.Id);
                deferred.reject();
            });

            return deferred.promise();
        };

        function performSync(server, deferred) {

            Logger.log("Creating ContentUploader to server: " + server.Id);

            require(['contentuploader'], function () {

                new MediaBrowser.ContentUploader(connectionManager).uploadImages(server).done(function () {

                    Logger.log("ContentUploaded succeeded to server: " + server.Id);

                    syncOfflineUsers(server, deferred);

                }).fail(function () {

                    Logger.log("ContentUploaded failed to server: " + server.Id);

                    syncOfflineUsers(server, deferred);
                });
            });
        }

        function syncOfflineUsers(server, deferred) {

            require(['offlineusersync'], function () {

                var apiClient = connectionManager.getApiClient(server.Id);

                new MediaBrowser.OfflineUserSync().sync(apiClient, server).done(function () {

                    Logger.log("OfflineUserSync succeeded to server: " + server.Id);

                    syncMedia(server, deferred);

                }).fail(function () {

                    Logger.log("OfflineUserSync failed to server: " + server.Id);

                    deferred.reject();
                });
            });
        }

        function syncMedia(server, deferred) {

            require(['mediasync'], function () {

                var apiClient = connectionManager.getApiClient(server.Id);

                new MediaBrowser.MediaSync().sync(apiClient, server).done(function () {

                    Logger.log("MediaSync succeeded to server: " + server.Id);

                    deferred.resolve();

                }).fail(function () {

                    Logger.log("MediaSync failed to server: " + server.Id);

                    deferred.reject();
                });
            });
        }
    }

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.ServerSync = serverSync;

})(this);