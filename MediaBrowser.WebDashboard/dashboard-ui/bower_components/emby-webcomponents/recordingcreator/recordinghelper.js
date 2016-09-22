define(['globalize', 'loading'], function (globalize, loading) {

    function changeRecordingToSeries(apiClient, timerId, programId) {

        loading.show();

        apiClient.getItem(apiClient.getCurrentUserId(), programId).then(function (item) {

            if (item.IsSeries) {
                // cancel, then create series
                cancelTimer(apiClient, timerId, false).then(function () {
                    apiClient.getNewLiveTvTimerDefaults({ programId: programId }).then(function (timerDefaults) {

                        apiClient.createLiveTvSeriesTimer(timerDefaults).then(function () {

                            loading.hide();
                            sendToast(globalize.translate('sharedcomponents#SeriesRecordingScheduled'));
                        });
                    });
                });
            } else {
                // cancel 
                cancelTimer(apiClient, timerId, true);
            }
        });
    }

    function cancelTimer(apiClient, timerId, hideLoading) {
        loading.show();
        return apiClient.cancelLiveTvTimer(timerId).then(function () {

            if (hideLoading) {
                loading.hide();
                sendToast(globalize.translate('sharedcomponents#RecordingCancelled'));
            }
        });
    }

    function createRecording(apiClient, programId, isSeries) {

        loading.show();
        return apiClient.getNewLiveTvTimerDefaults({ programId: programId }).then(function (item) {

            var promise = isSeries ?
                apiClient.createLiveTvSeriesTimer(item) :
                apiClient.createLiveTvTimer(item);

            return promise.then(function () {

                loading.hide();
                sendToast(globalize.translate('sharedcomponents#RecordingScheduled'));
            });
        });
    }

    function sendToast(msg) {
        require(['toast'], function (toast) {
            toast(msg);
        });
    }

    function toggleRecording(serverId, programId, timerId, seriesTimerId) {

        var apiClient = connectionManager.getApiClient(serverId);

        if (seriesTimerId && timerId) {

            // cancel 
            return cancelTimer(apiClient, timerId, true);

        } else if (timerId && programId) {

            // change to series recording, if possible
            // otherwise cancel individual recording
            return changeRecordingToSeries(apiClient, timerId, programId);

        } else if (programId) {
            // schedule recording
            return createRecording(apiClient, programId);
        } else {
            return Promise.reject();
        }
    }

    return {
        cancelTimer: cancelTimer,
        createRecording: createRecording,
        changeRecordingToSeries: changeRecordingToSeries,
        toggleRecording: toggleRecording
    };
});