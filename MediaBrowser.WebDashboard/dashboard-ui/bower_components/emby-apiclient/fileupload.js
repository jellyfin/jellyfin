(function (globalScope) {

    function fileUpload() {

        var self = this;

        self.upload = function (file, name, url) {

            return new Promise(function (resolve, reject) {

                reject();
            });
        };
    }

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.FileUpload = fileUpload;

})(this);