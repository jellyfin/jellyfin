(function () {

    var syncPromise;
    var lastStart = 0;

    window.LocalSync = {

        isSupported: function () {
            return AppInfo.isNativeApp;
        },

        startSync: function () {

            if (!syncPromise) {
                require(['multiserversync'], function () {

                    lastStart = new Date().getTime();
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

    var syncInterval = 1800000;

    function restartInterval() {
        if (LocalSync.isSupported) {
            setInterval(function () {

                LocalSync.startSync();

            }, syncInterval);

            if (lastStart > 0 && (now - lastStart) >= syncInterval) {
                LocalSync.startSync();
            }
        }
        //LocalSync.startSync();
    }

    Dashboard.ready(restartInterval);
    document.addEventListener("resume", restartInterval, false);
})();