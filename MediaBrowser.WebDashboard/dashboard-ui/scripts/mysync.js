define(['loading', 'localsync'], function (loading) {

    function refreshSyncStatus(page) {

        if (LocalSync.isSupported()) {

            var status = LocalSync.getSyncStatus();

            page.querySelector('.labelSyncStatus').innerHTML = Globalize.translate('LabelLocalSyncStatusValue', status);
            if (status == 'Active') {
                loading.show();
            } else {
                loading.hide();
            }

            if (status == "Active") {
                page.querySelector('.btnSyncNow').classList.add('hide');
            }
            else {
                page.querySelector('.btnSyncNow').classList.remove('hide');
            }

        }
    }

    function syncNow(page) {

        LocalSync.sync();
        require(['toast'], function (toast) {
            toast(Globalize.translate('MessageSyncStarted'));
        });
        refreshSyncStatus(page);
    }

    return function (view, params) {

        var interval;

        view.querySelector('.btnSyncNow').addEventListener('click', function () {
            syncNow(view);
        });

        if (LocalSync.isSupported()) {

            view.querySelector('.localSyncStatus').classList.remove('hide');

        } else {
            view.querySelector('.localSyncStatus').classList.add('hide');
        }

        view.addEventListener('viewbeforeshow', function () {
            var page = this;

            refreshSyncStatus(page);

            interval = setInterval(function () {
                refreshSyncStatus(page);
            }, 5000);
        });

        view.addEventListener('viewbeforehide', function () {
            var page = this;

            loading.hide();

            if (interval) {
                clearInterval(interval);
                interval = null;
            }
        });
    };
});