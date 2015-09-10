(function (globalScope) {

    function mediaSync() {

        var self = this;

        self.sync = function (apiClient) {

            var deferred = DeferredBuilder.Deferred();

            deferred.resolve();

            return deferred.promise();
        };
    }

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.MediaSync = mediaSync;

})(this);