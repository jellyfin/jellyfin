(function (globalScope) {

    function fileUpload() {

        var self = this;

        self.upload = function (path, name, url) {

            var deferred = DeferredBuilder.Deferred();

            resolveLocalFileSystemURL(path, function (fileEntry) {

                fileEntry.file(function (file) {

                    var mimeType = (file.type || '');

                    if (mimeType.indexOf('image/') != 0) {
                        Logger.log('Skipping upload because file is not an image. path: ' + path + ' mimeType: ' + mimeType);
                        deferred.reject();
                        return;
                    }

                    Logger.log('mimeType for file ' + path + ' is ' + file);

                    var onSuccess = function (r) {
                        console.log("Code = " + r.responseCode);
                        console.log("Response = " + r.response);
                        console.log("Sent = " + r.bytesSent);
                        deferred.resolve();
                    }

                    var onFail = function (error) {
                        console.log("upload error source " + error.source);
                        console.log("upload error target " + error.target);
                        deferred.reject();
                    }

                    var options = new FileUploadOptions();
                    options.fileKey = "file";
                    options.fileName = name;
                    options.mimeType = mimeType;

                    var params = {};
                    options.params = params;

                    new FileTransfer().upload(path, url, onSuccess, onFail, options);

                }, function () {
                    Logger.log('File upload failed. fileEntry.file returned an error');
                    deferred.reject();
                });

            }, function () {

                Logger.log('File upload failed. resolveLocalFileSystemURL returned an error');
                deferred.reject();
            });

            return deferred.promise();
        };
    }

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.FileUpload = fileUpload;

})(this);