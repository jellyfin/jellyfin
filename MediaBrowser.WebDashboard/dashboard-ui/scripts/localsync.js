(function () {

    var syncPromise;
    var lastStart = 0;

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

                lastStart = new Date().getTime();

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

    var syncInterval = 1800000;

    function restartInterval() {
        if (LocalSync.isSupported) {
            setInterval(function () {

                //LocalSync.startSync();

            }, syncInterval);

            if (lastStart > 0 && (now - lastStart) >= syncInterval) {
                //LocalSync.startSync();
            }
        }
        //LocalSync.startSync();
    }

    Dashboard.ready(restartInterval);
    document.addEventListener("resume", restartInterval, false);
})();