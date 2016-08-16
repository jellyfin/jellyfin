define(['serverNotifications', 'events', 'loading', 'connectionManager'], function (serverNotifications, events, loading, connectionManager) {

    function onSyncJobsUpdated(e, apiClient, data) {

    }

    function renderList(listInstance, items) {

    }

    function fetchData(listInstance) {

        listInstance.lastDataLoad = 0;
        loading.show();

        var options = {};
        var apiClient = connectionManager.getApiClient(listInstance.options.serverId);

        if (listInstance.options.userId) {
            options.UserId = listInstance.options.userId;
        }

        if (listInstance.options.isLocalSync) {
            options.TargetId = apiClient.deviceId();
        }

        return apiClient.getJSON(ApiClient.getUrl('Sync/Jobs', options)).then(function (response) {

            renderList(listInstance, response.Items);
            loading.hide();
        });
    }

    function syncJobList(options) {
        this.options = options;

        var onSyncJobsUpdatedHandler = onSyncJobsUpdated.bind(this);
        this.onSyncJobsUpdatedHandler = null;
        events.on(serverNotifications, 'SyncJobs', onSyncJobsUpdatedHandler);

        fetchData(this);
    }

    syncJobList.prototype.destroy = function () {
        this.options = null;

        var onSyncJobsUpdatedHandler = this.onSyncJobsUpdatedHandler;
        this.onSyncJobsUpdatedHandler = null;
        events.off(serverNotifications, 'SyncJobs', onSyncJobsUpdatedHandler);
    };

    return syncJobList;
});