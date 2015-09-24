(function () {

    var lastStart = 0;

    function onDeviceReady() {

        var fetcher = window.BackgroundFetch;

        fetcher.configure(onBackgroundFetch, onBackgroundFetchFailed, {
            stopOnTerminate: false  // <-- false is default
        });
    }

    function onSyncFinish() {

        Logger.log('BackgroundFetch completed');

        var fetcher = window.BackgroundFetch;
        fetcher.finish();   // <-- N.B. You MUST called #finish so that native-side can signal completion of the background-thread to the os.
    }

    function onSyncFail() {

        Logger.log('BackgroundFetch completed - sync failed');

        var fetcher = window.BackgroundFetch;
        fetcher.finish();   // <-- N.B. You MUST called #finish so that native-side can signal completion of the background-thread to the os.
    }

    function startSync(uploadPhotos) {
        lastStart = new Date().getTime();

        require(['localsync'], function () {

            if (LocalSync.getSyncStatus() == 'Syncing') {
                onSyncFinish();
                return;
            }

            var syncOptions = {
                uploadPhotos: uploadPhotos
            };

            LocalSync.sync(syncOptions).done(onSyncFinish).fail(onSyncFail);
        });
    }

    function onBackgroundFetch() {

        Logger.log('BackgroundFetch initiated');
        startSync(false);
    }

    function onBackgroundFetchFailed() {
        Logger.log('- BackgroundFetch failed');
    }

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

    onDeviceReady();
})();