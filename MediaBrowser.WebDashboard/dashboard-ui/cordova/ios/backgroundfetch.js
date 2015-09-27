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

    function startSync(reportToFetcher) {
        lastStart = new Date().getTime();

        require(['localsync'], function () {

            if (LocalSync.getSyncStatus() == 'Syncing') {
                onSyncFinish();
                return;
            }

            var promise = LocalSync.sync();

            if (reportToFetcher) {
                promise.done(onSyncFinish).fail(onSyncFail);
            }
        });
    }

    function onBackgroundFetch() {

        Logger.log('BackgroundFetch initiated');
        startSync(true);
    }

    function onBackgroundFetchFailed() {
        Logger.log('- BackgroundFetch failed');
    }

    var syncInterval = 1800000;

    function restartInterval() {

        setInterval(function () {

            //startSync();

        }, syncInterval);

        if (lastStart > 0 && (new Date().getTime() - lastStart) >= syncInterval) {

            setTimeout(function () {
                //startSync();

            }, 5000);
        }
    }

    Dashboard.ready(restartInterval);
    document.addEventListener("resume", restartInterval, false);

    onDeviceReady();
})();