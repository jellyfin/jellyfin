define(['apphost', 'globalize', 'syncJobList', 'events', 'localsync', 'emby-button', 'paper-icon-button-light'], function (appHost, globalize, syncJobList, events, localSync) {
    'use strict';

    return function (view, params) {

        var interval;

        function isLocalSyncManagement() {
            return appHost.supports('sync') && params.mode == 'offline';
        }

        function refreshSyncStatus(page) {

            if (isLocalSyncManagement()) {

                var status = localSync.getSyncStatus();

                if (status == "Active") {
                    page.querySelector('.btnSyncNow').classList.add('hide');
                }
                else {
                    page.querySelector('.btnSyncNow').classList.remove('hide');
                }
            }
        }

        function syncNow(page) {

            localSync.sync();
            require(['toast'], function (toast) {
                toast(Globalize.translate('MessageSyncStarted'));
            });
            refreshSyncStatus(page);
        }

        view.querySelector('.btnSyncNow').addEventListener('click', function () {
            syncNow(view);
        });

        if (isLocalSyncManagement()) {

            view.querySelector('.localSyncStatus').classList.remove('hide');

        } else {
            view.querySelector('.localSyncStatus').classList.add('hide');
        }

        var mySyncJobList = new syncJobList({
            isLocalSync: params.mode === 'offline',
            serverId: ApiClient.serverId(),
            userId: params.mode === 'offline' ? null : ApiClient.getCurrentUserId(),
            element: view.querySelector('.syncActivity'),
            mode: params.mode
        });

        view.addEventListener('viewbeforeshow', function () {

            refreshSyncStatus(view);

            if (appHost.supports('sync')) {
                interval = setInterval(function () {
                    refreshSyncStatus(view);
                }, 5000);
            }
        });

        view.addEventListener('viewbeforehide', function () {

            if (interval) {
                clearInterval(interval);
                interval = null;
            }
        });

        view.addEventListener('viewdestroy', function () {

            mySyncJobList.destroy();
        });
    };
});