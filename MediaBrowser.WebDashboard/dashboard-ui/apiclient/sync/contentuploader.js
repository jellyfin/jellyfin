(function (globalScope) {

    function contentUploader(connectionManager) {

        var self = this;

        self.uploadImages = function (server) {

            var deferred = DeferredBuilder.Deferred();

            var apiClient = connectionManager.getApiClient(server.Id);

            apiClient.getDevicesOptions().done(function (devicesOptions) {

                if (!devicesOptions.EnabledCameraUploadDevices || devicesOptions.EnabledCameraUploadDevices.indexOf(apiClient.deviceId()) == -1) {
                    Logger.log("Camera upload is not enabled for this device.");
                    deferred.reject();
                }
                else {
                    uploadImagesInternal(server, apiClient, deferred);
                }

            }).fail(function () {
                deferred.reject();
            });

            return deferred.promise();
        };

        function uploadImagesInternal(server, apiClient, deferred) {

            apiClient.getContentUploadHistory().done(function (result) {

                uploadImagesWithHistory(server, result, apiClient, deferred);

            }).fail(function () {
                deferred.reject();
            });
        }

        function uploadImagesWithHistory(server, uploadHistory, apiClient, deferred) {

            require(['localassetmanager'], function () {

                // TODO: Mimic java version of ContentUploader.UploadImagesInternal
                deferred.resolve();
            });
        }
    }

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.ContentUploader = contentUploader;

})(this);