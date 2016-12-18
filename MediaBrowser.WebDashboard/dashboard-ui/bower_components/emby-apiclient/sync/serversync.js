define([], function () {
    'use strict';

    return function (connectionManager) {

        var self = this;

        self.sync = function (server, options) {

            if (!server.AccessToken && !server.ExchangeToken) {

                console.log('Skipping sync to server ' + server.Id + ' because there is no saved authentication information.');
                return Promise.resolve();
            }

            var connectionOptions = {
                updateDateLastAccessed: false,
                enableWebSocket: false,
                reportCapabilities: false,
                enableAutomaticBitrateDetection: false
            };

            return connectionManager.connectToServer(server, connectionOptions).then(function (result) {

                if (result.State === MediaBrowser.ConnectionState.SignedIn) {
                    return performSync(server, options);
                } else {
                    console.log('Unable to connect to server id: ' + server.Id);
                    return Promise.reject();
                }

            }, function (err) {

                console.log('Unable to connect to server id: ' + server.Id);
                throw err;
            });
        };

        function performSync(server, options) {

            console.log("ServerSync.performSync to server: " + server.Id);

            options = options || {};

            var uploadPhotos = options.uploadPhotos !== false;

            if (options.cameraUploadServers && options.cameraUploadServers.indexOf(server.Id) === -1) {
                uploadPhotos = false;
            }

            var pr = syncOfflineUsers(server, options);

            return pr.then(function () {

                if (uploadPhotos) {
                    return uploadContent(server, options);
                }

                return Promise.resolve();

            }).then(function () {

                return syncMedia(server, options);
            });
        }


        function syncOfflineUsers(server, options) {

            if (options.syncOfflineUsers === false) {
                return Promise.resolve();
            }

            return new Promise(function (resolve, reject) {

                require(['offlineusersync'], function (OfflineUserSync) {

                    var apiClient = connectionManager.getApiClient(server.Id);

                    new OfflineUserSync().sync(apiClient, server).then(resolve, reject);
                });
            });
        }

        function uploadContent(server, options) {

            return new Promise(function (resolve, reject) {

                require(['contentuploader'], function (contentuploader) {

                    uploader = new ContentUploader(connectionManager);
                    uploader.uploadImages(server).then(resolve, reject);
                });
            });
        }

        function syncMedia(server, options) {

            return new Promise(function (resolve, reject) {

                require(['mediasync'], function (MediaSync) {

                    var apiClient = connectionManager.getApiClient(server.Id);

                    new MediaSync().sync(apiClient, server, options).then(resolve, reject);
                });
            });
        }
    };
});