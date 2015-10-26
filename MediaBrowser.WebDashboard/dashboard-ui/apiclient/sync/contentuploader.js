(function (globalScope) {

    function contentUploader(connectionManager) {

        var self = this;

        self.uploadImages = function (server) {

            var deferred = DeferredBuilder.Deferred();

            LocalAssetManager.getCameraPhotos().done(function (photos) {

                if (!photos.length) {
                    deferred.resolve();
                    return;
                }

                var apiClient = connectionManager.getApiClient(server.Id);

                apiClient.getContentUploadHistory().done(function (uploadHistory) {

                    photos = getFilesToUpload(photos, uploadHistory);

                    Logger.log('Found ' + photos.length + ' files to upload');

                    uploadNext(photos, 0, server, apiClient, deferred);

                }).fail(function () {
                    deferred.reject();
                });

            }).fail(function () {
                deferred.reject();
            });

            return deferred.promise();
        };

        function getFilesToUpload(files, uploadHistory) {

            return files.filter(function (file) {

                // Seeing some null entries for some reason
                if (!file) {
                    return false;
                }

                return uploadHistory.FilesUploaded.filter(function (u) {

                    return getUploadId(file) == u.Id;

                }).length == 0;
            });
        }

        function getUploadId(file) {
            return CryptoJS.SHA1(file + "1").toString();
        }

        function uploadNext(files, index, server, apiClient, deferred) {

            var length = files.length;

            if (index >= length) {

                deferred.resolve();
                return;
            }

            uploadFile(files[index], apiClient).done(function () {

                uploadNext(files, index + 1, server, apiClient, deferred);
            }).fail(function () {
                uploadNext(files, index + 1, server, apiClient, deferred);
            });
        }

        function uploadFile(file, apiClient) {

            var deferred = DeferredBuilder.Deferred();

            require(['fileupload', "cryptojs-sha1"], function () {

                var name = 'camera image ' + new Date().getTime();

                var url = apiClient.getUrl('Devices/CameraUploads', {
                    DeviceId: apiClient.deviceId(),
                    Name: name,
                    Album: 'Camera Roll',
                    Id: getUploadId(file),
                    api_key: apiClient.accessToken()
                });

                Logger.log('Uploading file to ' + url);

                new MediaBrowser.FileUpload().upload(file, name, url).done(function () {

                    Logger.log('File upload succeeded');
                    deferred.resolve();

                }).fail(function () {

                    Logger.log('File upload failed');
                    deferred.reject();
                });
            });

            return deferred.promise();
        }
    }

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.ContentUploader = contentUploader;

})(this);