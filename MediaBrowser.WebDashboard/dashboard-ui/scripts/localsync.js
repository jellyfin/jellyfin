(function () {

    var syncPromise;

    window.LocalSync = {

        isSupported: function () {
            return AppInfo.isNativeApp;
        },

        sync: function (options) {

            if (syncPromise) {
                return syncPromise.promise();
            }

            var deferred = DeferredBuilder.Deferred();

            require(['multiserversync'], function () {

                options = options || {};

                if ($.browser.safari) {
                    options.enableBackgroundTransfer = true;
                }

                syncPromise = new MediaBrowser.MultiServerSync(ConnectionManager).sync(options).done(function () {

                    syncPromise = null;
                    deferred.resolve();

                }).fail(function () {

                    syncPromise = null;
                });
            });

            return deferred.promise();
        },

        getSyncStatus: function () {

            if (syncPromise != null) {
                return 'Syncing';
            }
            return 'Idle';
        }
    };
})();