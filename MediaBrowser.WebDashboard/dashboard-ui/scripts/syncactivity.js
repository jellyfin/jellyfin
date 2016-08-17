define(['loading', 'apphost', 'globalize', 'syncJobList', 'events', 'scripts/taskbutton', 'localsync', 'emby-button', 'paper-icon-button-light'], function (loading, appHost, globalize, syncJobList, events, taskButton) {

    function getTabs() {
        return [
        {
            href: 'syncactivity.html',
            name: Globalize.translate('TabSyncJobs')
        },
         {
             href: 'devicesupload.html',
             name: Globalize.translate('TabCameraUpload')
         },
        {
            href: 'appservices.html?context=sync',
            name: Globalize.translate('TabServices')
        },
         {
             href: 'syncsettings.html',
             name: Globalize.translate('TabSettings')
         }];
    }

    function initSupporterInfo(view, params) {

        view.querySelector('.supporterPromotion .mainText').innerHTML = globalize.translate('HeaderSyncRequiresSupporterMembership');

        var apiClient = ApiClient;
        apiClient.getPluginSecurityInfo().then(function (regInfo) {

            if (regInfo.IsMBSupporter) {
                view.querySelector('.supporterPromotionContainer').classList.add('hide');
            } else {
                view.querySelector('.supporterPromotionContainer').classList.remove('hide');
            }

        }, function () {

            view.querySelector('.supporterPromotionContainer').classList.remove('hide');
        });
    }

    return function (view, params) {

        initSupporterInfo(view, params);
        var mySyncJobList = new syncJobList({
            isLocalSync: params.mode === 'offline',
            serverId: ApiClient.serverId(),
            userId: params.mode === 'offline' ? null : ApiClient.getCurrentUserId(),
            element: view.querySelector('.syncActivity')
        });

        events.on(mySyncJobList, 'jobedit', function (e, jobId, serverId) {

            Dashboard.navigate('syncjob.html?id=' + jobId);
        });

        view.addEventListener('viewshow', function () {

            LibraryMenu.setTabs('syncadmin', 0, getTabs);

            taskButton({
                mode: 'on',
                progressElem: view.querySelector('.syncProgress'),
                taskKey: 'SyncPrepare',
                button: view.querySelector('.btnSync')
            });
        });

        view.addEventListener('viewbeforehide', function () {

            taskButton({
                mode: 'off',
                taskKey: 'SyncPrepare',
                button: view.querySelector('.btnSync')
            });
        });

        view.addEventListener('viewdestroy', function () {

            mySyncJobList.destroy();
        });
    };

});