(function (globalScope) {

    function fileUpload() {

        var self = this;

        self.upload = function (file, name, url) {

            var deferred = DeferredBuilder.Deferred();

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
            options.mimeType = 'image/jpg';

            var params = {};
            options.params = params;

            new FileTransfer().upload(file, url, onSuccess, onFail, options);

            return deferred.promise();
        };
    }

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.FileUpload = fileUpload;

})(this);