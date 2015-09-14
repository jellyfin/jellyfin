(function (globalScope) {

    function mediaSync() {

        var self = this;

        self.sync = function (apiClient) {

            var deferred = DeferredBuilder.Deferred();

            reportOfflineActions(apiClient).done(function () {

                // Do the first data sync
                syncData(apiClient, false).done(function () {

                    // Download new content
                    getNewMedia(apiClient).done(function () {

                        // Do the second data sync
                        syncData(apiClient, false).done(function () {

                            deferred.resolve();

                        }).fail(getOnFail(deferred));

                    }).fail(getOnFail(deferred));

                }).fail(getOnFail(deferred));

            }).fail(getOnFail(deferred));

            return deferred.promise();
        };

        function reportOfflineActions(apiClient) {

            var deferred = DeferredBuilder.Deferred();

            deferred.resolve();

            return deferred.promise();
        }

        function syncData(apiClient, syncUserItemAccess) {

            var deferred = DeferredBuilder.Deferred();

            deferred.resolve();

            return deferred.promise();
        }

        function getNewMedia(apiClient) {

            var deferred = DeferredBuilder.Deferred();

            deferred.resolve();

            return deferred.promise();
        }

        function getOnFail(deferred) {
            return function () {

                deferred.reject();
            };
        }
    }

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.MediaSync = mediaSync;

})(this);