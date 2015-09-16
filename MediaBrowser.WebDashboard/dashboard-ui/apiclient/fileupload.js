(function (globalScope) {

    function fileUpload() {

        var self = this;

        self.upload = function (file, name, url) {

            var deferred = DeferredBuilder.Deferred();

            deferred.reject();

            return deferred.promise();
        };
    }

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.FileUpload = fileUpload;

})(this);