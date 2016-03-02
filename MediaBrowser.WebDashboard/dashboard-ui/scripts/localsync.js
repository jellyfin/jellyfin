define(['appSettings'], function (appSettings) {

    var syncPromise;

    window.LocalSync = {

        isSupported: function () {
            return AppInfo.isNativeApp && Dashboard.capabilities().SupportsSync;
        },

        sync: function (options) {

            if (syncPromise) {
                return syncPromise.promise();
            }

            return new Promise(function (resolve, reject) {

                require(['multiserversync'], function () {

                    options = options || {};

                    LocalSync.normalizeSyncOptions(options);

                    options.cameraUploadServers = appSettings.cameraUploadServers();

                    syncPromise = new MediaBrowser.MultiServerSync(ConnectionManager).sync(options).then(function () {

                        syncPromise = null;
                        resolve();

                    }, function () {

                        syncPromise = null;
                    });
                });

            });
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

});