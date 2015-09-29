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

    function startSync(reportToFetcher, syncOptions) {
        lastStart = new Date().getTime();

        require(['localsync'], function () {

            if (LocalSync.getSyncStatus() == 'Syncing') {
                onSyncFinish();
                return;
            }

            var promise = LocalSync.sync(syncOptions);

            if (reportToFetcher) {
                promise.done(onSyncFinish).fail(onSyncFail);
            }
        });
    }

    function onBackgroundFetch() {

        Logger.log('BackgroundFetch initiated');

        startSync(true, {
            uploadPhotos: false,
            enableBackgroundTransfer: true,
            enableNewDownloads: true
        });
    }

    function onBackgroundFetchFailed() {
        Logger.log('- BackgroundFetch failed');
    }

    var syncInterval = 1800000;

    function restartInterval() {

        setInterval(function () {

            startIntervalSync();

        }, syncInterval);

        if (lastStart > 0 && (new Date().getTime() - lastStart) >= syncInterval) {

            setTimeout(function () {
                startIntervalSync();

            }, 5000);
        }
    }

    function startIntervalSync() {

        startSync(false, {
            uploadPhotos: true,
            enableNewDownloads: false,
            enableBackgroundTransfer: true
        });
    }

    function normalizeSyncOptions(options) {

        options.enableBackgroundTransfer = true;

        if (options.enableNewDownloads == null) {
            options.enableNewDownloads = false;
        }
    }

    Dashboard.ready(function () {

        require(['localsync'], function () {

            LocalSync.normalizeSyncOptions = normalizeSyncOptions;
        });

        restartInterval();
    });
    document.addEventListener("resume", restartInterval, false);

    onDeviceReady();
})();