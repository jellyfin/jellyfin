(function (globalScope) {

    function serverSync(connectionManager) {

        self.sync = function (server) {

            var deferred = DeferredBuilder.Deferred();

            if (!server.AccessToken && !server.ExchangeToken) {

                Logger.log('Skipping sync to server ' + server.Id + ' because there is no saved authentication information.');
                deferred.resolve();
                return;
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

                    deferred.resolve();

                }).fail(function () {

                    deferred.resolve();
                });
            });
        }
    }

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.ServerSync = serverSync;

})(this);