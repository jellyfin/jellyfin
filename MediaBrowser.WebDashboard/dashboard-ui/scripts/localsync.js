define(['appSettings', 'connectionManager'], function (appSettings, connectionManager) {

    var syncPromise;

    return {

        sync: function (options) {

            if (syncPromise) {
                return syncPromise.promise();
            }

            return new Promise(function (resolve, reject) {

                require(['multiserversync'], function (MultiServerSync) {

                    options = options || {};

                    options.cameraUploadServers = appSettings.cameraUploadServers();

                    syncPromise = new MultiServerSync(connectionManager).sync(options).then(function () {

                        syncPromise = null;
                        resolve();

                    }, function () {

                        syncPromise = null;
                    });
                });

            });
        },

        getSyncStatus: function () {

            if (syncPromise != null) {
                return 'Syncing';
            }
            return 'Idle';
        }
    };

});