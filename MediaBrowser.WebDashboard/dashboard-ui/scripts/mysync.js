define(['jQuery'], function ($) {

    function refreshSyncStatus(page) {

        require(['localsync'], function () {

            if (LocalSync.isSupported()) {

                var status = LocalSync.getSyncStatus();

                page.querySelector('.labelSyncStatus').innerHTML = Globalize.translate('LabelLocalSyncStatusValue', status);
                page.querySelector('.syncSpinner').active = status == "Active";

                if (status == "Active") {
                    page.querySelector('.btnSyncNow').classList.add('hide');
                }
                else {
                    page.querySelector('.btnSyncNow').classList.remove('hide');
                }

            }
        });
    }

    function syncNow(page) {

        require(['localsync'], function () {

            LocalSync.sync();
            require(['toast'], function (toast) {
                toast(Globalize.translate('MessageSyncStarted'));
            });
            refreshSyncStatus(page);
        });
    }

    var interval;

    $(document).on('pageinit', "#mySyncActivityPage", function () {

        var page = this;

        $('.btnSyncNow', page).on('click', function () {
            syncNow(page);
        });

        require(['localsync'], function () {

            if (LocalSync.isSupported()) {

                page.querySelector('.localSyncStatus').classList.remove('hide');

            } else {
                page.querySelector('.localSyncStatus').classList.add('hide');
                page.querySelector('.syncSpinner').active = false;
            }
        });

    }).on('pagebeforeshow', "#mySyncActivityPage", function () {

        var page = this;

        refreshSyncStatus(page);

        interval = setInterval(function () {
            refreshSyncStatus(page);
        }, 5000);

    }).on('pagebeforehide', "#mySyncActivityPage", function () {

        var page = this;

        page.querySelector('.syncSpinner').active = false;

        if (interval) {
            clearInterval(interval);
            interval = null;
        }
    });

});