(function () {

    var lastStart = 0;

    function onDeviceReady() {

        //var fetcher = window.BackgroundFetch;

        //fetcher.configure(onBackgroundFetch, onBackgroundFetchFailed, {
        //    stopOnTerminate: false  // <-- false is default
        //});
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
            enableNewDownloads: true
        });
    }

    function onBackgroundFetchFailed() {
        Logger.log('- BackgroundFetch failed');
    }

    var syncInterval = 900000;
    var photoUploadInterval = 21600000;
    var offlineUserSyncInterval = 43200000;
    function startIntervalSync() {

        startSync(false, {
            uploadPhotos: true,
            enableNewDownloads: true
        });
    }

    function normalizeSyncOptions(options) {

        options.enableBackgroundTransfer = true;

        options.uploadPhotos = (new Date().getTime() - lastStart) >= photoUploadInterval;
        options.syncOfflineUsers = (new Date().getTime() - lastStart) >= offlineUserSyncInterval;
    }

    Dashboard.ready(function () {

        require(['localsync'], function () {

            LocalSync.normalizeSyncOptions = normalizeSyncOptions;
        });
    });

    pageClassOn('pageshow', "libraryPage", function () {

        if (!Dashboard.getCurrentUserId()) {
            return;
        }

        if ((new Date().getTime() - lastStart) >= syncInterval) {

            setTimeout(function () {
                startIntervalSync();

            }, 10000);
        }

    });

    onDeviceReady();
})();