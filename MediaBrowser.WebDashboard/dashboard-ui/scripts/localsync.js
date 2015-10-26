(function () {

    var syncPromise;

    window.LocalSync = {

        isSupported: function () {
            return AppInfo.isNativeApp && Dashboard.capabilities().SupportsSync;
        },

        sync: function (options) {

            if (syncPromise) {
                return syncPromise.promise();
            }

            var deferred = DeferredBuilder.Deferred();

            require(['multiserversync'], function () {

                options = options || {};

                LocalSync.normalizeSyncOptions(options);

                options.cameraUploadServers = AppSettings.cameraUploadServers();

                syncPromise = new MediaBrowser.MultiServerSync(ConnectionManager).sync(options).done(function () {

                    syncPromise = null;
                    deferred.resolve();

                }).fail(function () {

                    syncPromise = null;
                });
            });

            return deferred.promise();
        },

        normalizeSyncOptions: function (options) {
            
        },

        getSyncStatus: function () {

            if (syncPromise != null) {
                return 'Syncing';
            }
            return 'Idle';
        }
    };
})();