(function (globalScope) {

    function serverSync(connectionManager) {

        var self = this;

        self.sync = function (server, options) {

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

            connectionManager.connectToServer(server, connectionOptions).then(function (result) {

                if (result.State == MediaBrowser.ConnectionState.SignedIn) {
                    performSync(server, options, deferred);
                } else {
                    Logger.log('Unable to connect to server id: ' + server.Id);
                    deferred.reject();
                }

            }, function () {

                Logger.log('Unable to connect to server id: ' + server.Id);
                deferred.reject();
            });

            return deferred.promise();
        };

        function performSync(server, options, deferred) {

            Logger.log("Creating ContentUploader to server: " + server.Id);

            var nextAction = function () {
                syncOfflineUsers(server, options, deferred);
            };

            options = options || {};

            var uploadPhotos = options.uploadPhotos !== false;

            if (options.cameraUploadServers && options.cameraUploadServers.indexOf(server.Id) == -1) {
                uploadPhotos = false;
            }

            if (!uploadPhotos) {
                nextAction();
                return;
            }

            require(['contentuploader'], function () {

                new MediaBrowser.ContentUploader(connectionManager).uploadImages(server).then(function () {

                    Logger.log("ContentUploaded succeeded to server: " + server.Id);

                    nextAction();

                }, function () {

                    Logger.log("ContentUploaded failed to server: " + server.Id);

                    nextAction();
                });
            });
        }

        function syncOfflineUsers(server, options, deferred) {

            if (options.syncOfflineUsers === false) {
                syncMedia(server, options, deferred);
                return;
            }

            require(['offlineusersync'], function () {

                var apiClient = connectionManager.getApiClient(server.Id);

                new MediaBrowser.OfflineUserSync().sync(apiClient, server).then(function () {

                    Logger.log("OfflineUserSync succeeded to server: " + server.Id);

                    syncMedia(server, options, deferred);

                }, function () {

                    Logger.log("OfflineUserSync failed to server: " + server.Id);

                    deferred.reject();
                });
            });
        }

        function syncMedia(server, options, deferred) {

            require(['mediasync'], function () {

                var apiClient = connectionManager.getApiClient(server.Id);

                new MediaBrowser.MediaSync().sync(apiClient, server, options).then(function () {

                    Logger.log("MediaSync succeeded to server: " + server.Id);

                    deferred.resolve();

                }, function () {

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