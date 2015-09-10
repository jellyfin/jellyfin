(function () {

    var syncPromise;

    window.LocalSync = {

        isSupported: function () {
            return AppInfo.isNativeApp;
        },

        startSync: function () {

            if (!syncPromise) {
                require(['multiserversync'], function () {

                    syncPromise = new MediaBrowser.MultiServerSync(ConnectionManager).sync().done(function () {

                        syncPromise = null;

                    }).fail(function () {

                        syncPromise = null;
                    });
                });
            }
        },

        getSyncStatus: function () {

            if (syncPromise != null) {
                return 'Syncing';
            }
            return 'Idle';
        }
    };

    Dashboard.ready(function () {
        if (LocalSync.isSupported) {
            setInterval(function () {

                LocalSync.startSync();

            }, 3600000);
        }
    });

})();