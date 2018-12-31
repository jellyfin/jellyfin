define(['globalize', 'loading', 'connectionManager', 'registrationServices'], function (globalize, loading, connectionManager, registrationServices) {
    'use strict';

    function changeRecordingToSeries(apiClient, timerId, programId, confirmTimerCancellation) {

        loading.show();

        return apiClient.getItem(apiClient.getCurrentUserId(), programId).then(function (item) {

            if (item.IsSeries) {
                // create series
                return apiClient.getNewLiveTvTimerDefaults({ programId: programId }).then(function (timerDefaults) {

                    return apiClient.createLiveTvSeriesTimer(timerDefaults).then(function () {

                        loading.hide();
                        sendToast(globalize.translate('sharedcomponents#SeriesRecordingScheduled'));
                    });
                });
            } else {
                // cancel 
                if (confirmTimerCancellation) {
                    return cancelTimerWithConfirmation(timerId, apiClient.serverId());
                }

                return cancelTimer(apiClient.serverId(), timerId, true);
            }
        });
    }

    function cancelTimerWithConfirmation(timerId, serverId) {

        return new Promise(function (resolve, reject) {

            require(['confirm'], function (confirm) {

                confirm({

                    text: globalize.translate('sharedcomponents#MessageConfirmRecordingCancellation'),
                    primary: 'cancel',
                    confirmText: globalize.translate('sharedcomponents#HeaderCancelRecording'),
                    cancelText: globalize.translate('sharedcomponents#HeaderKeepRecording')

                }).then(function () {

                    loading.show();

                    var apiClient = connectionManager.getApiClient(serverId);
                    cancelTimer(apiClient, timerId, true).then(resolve, reject);

                }, reject);
            });
        });
    }

    function cancelSeriesTimerWithConfirmation(timerId, serverId) {

        return new Promise(function (resolve, reject) {

            require(['confirm'], function (confirm) {

                confirm({

                    text: globalize.translate('sharedcomponents#MessageConfirmRecordingCancellation'),
                    primary: 'cancel',
                    confirmText: globalize.translate('sharedcomponents#HeaderCancelSeries'),
                    cancelText: globalize.translate('sharedcomponents#HeaderKeepSeries')

                }).then(function () {

                    loading.show();

                    var apiClient = connectionManager.getApiClient(serverId);
                    apiClient.cancelLiveTvSeriesTimer(timerId).then(function () {

                        require(['toast'], function (toast) {
                            toast(globalize.translate('sharedcomponents#SeriesCancelled'));
                        });

                        loading.hide();
                        resolve();
                    }, reject);

                }, reject);
            });
        });
    }

    function cancelTimer(apiClient, timerId, hideLoading) {
        loading.show();
        return apiClient.cancelLiveTvTimer(timerId).then(function () {

            if (hideLoading !== false) {
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

    function showMultiCancellationPrompt(serverId, programId, timerId, timerStatus, seriesTimerId) {
        return new Promise(function (resolve, reject) {

            require(['dialog'], function (dialog) {

                var items = [];

                items.push({
                    name: globalize.translate('sharedcomponents#HeaderKeepRecording'),
                    id: 'cancel',
                    type: 'submit'
                });

                if (timerStatus === 'InProgress') {
                    items.push({
                        name: globalize.translate('sharedcomponents#HeaderStopRecording'),
                        id: 'canceltimer',
                        type: 'cancel'
                    });
                } else {
                    items.push({
                        name: globalize.translate('sharedcomponents#HeaderCancelRecording'),
                        id: 'canceltimer',
                        type: 'cancel'
                    });
                }

                items.push({
                    name: globalize.translate('sharedcomponents#HeaderCancelSeries'),
                    id: 'cancelseriestimer',
                    type: 'cancel'
                });

                dialog({

                    text: globalize.translate('sharedcomponents#MessageConfirmRecordingCancellation'),
                    buttons: items

                }).then(function (result) {

                    var apiClient = connectionManager.getApiClient(serverId);

                    if (result === 'canceltimer') {
                        loading.show();

                        cancelTimer(apiClient, timerId, true).then(resolve, reject);
                    }
                    else if (result === 'cancelseriestimer') {

                        loading.show();

                        apiClient.cancelLiveTvSeriesTimer(seriesTimerId).then(function () {

                            require(['toast'], function (toast) {
                                toast(globalize.translate('sharedcomponents#SeriesCancelled'));
                            });

                            loading.hide();
                            resolve();
                        }, reject);
                    } else {
                        resolve();
                    }

                }, reject);
            });
        });
    }

    function toggleRecording(serverId, programId, timerId, timerStatus, seriesTimerId) {

        return registrationServices.validateFeature('dvr').then(function () {
            var apiClient = connectionManager.getApiClient(serverId);

            var hasTimer = timerId && timerStatus !== 'Cancelled';

            if (seriesTimerId && hasTimer) {

                // cancel 
                return showMultiCancellationPrompt(serverId, programId, timerId, timerStatus, seriesTimerId);

            } else if (hasTimer && programId) {

                // change to series recording, if possible
                // otherwise cancel individual recording
                return changeRecordingToSeries(apiClient, timerId, programId, true);

            } else if (programId) {
                // schedule recording
                return createRecording(apiClient, programId);
            } else {
                return Promise.reject();
            }
        });
    }

    return {
        cancelTimer: cancelTimer,
        createRecording: createRecording,
        changeRecordingToSeries: changeRecordingToSeries,
        toggleRecording: toggleRecording,
        cancelTimerWithConfirmation: cancelTimerWithConfirmation,
        cancelSeriesTimerWithConfirmation: cancelSeriesTimerWithConfirmation
    };
});