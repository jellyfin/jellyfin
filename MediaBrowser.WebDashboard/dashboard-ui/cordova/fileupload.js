(function (globalScope) {

    function fileUpload() {

        var self = this;

        self.upload = function (path, name, url) {

            return new Promise(function (resolve, reject) {

                resolveLocalFileSystemURL(path, function (fileEntry) {

                    fileEntry.file(function (file) {

                        var mimeType = (file.type || '');

                        if (mimeType.indexOf('image/') != 0) {
                            Logger.log('Skipping upload because file is not an image. path: ' + path + ' mimeType: ' + mimeType);
                            reject();
                            return;
                        }

                        Logger.log('mimeType for file ' + path + ' is ' + file);

                        var onSuccess = function (r) {
                            console.log("Code = " + r.responseCode);
                            console.log("Response = " + r.response);
                            console.log("Sent = " + r.bytesSent);
                            resolve();
                        }

                        var onFail = function (error) {
                            console.log("upload error source " + error.source);
                            console.log("upload error target " + error.target);
                            reject();
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
                        reject();
                    });

                }, function () {

                    Logger.log('File upload failed. resolveLocalFileSystemURL returned an error');
                    reject();
                });
            });
        };
    }

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.FileUpload = fileUpload;

})(this);