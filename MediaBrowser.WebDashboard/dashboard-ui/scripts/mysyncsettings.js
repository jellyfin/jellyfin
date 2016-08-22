define(['appSettings', 'apphost'], function (appSettings, appHost) {

    function loadForm(page, user) {

        page.querySelector('#txtSyncPath').value = appSettings.syncPath() || '';
        page.querySelector('#chkWifi').checked = appSettings.syncOnlyOnWifi();

        Dashboard.hideLoadingMsg();
    }

    function saveUser(page, user) {

        var syncPath = page.querySelector('#txtSyncPath').value;

        appSettings.syncPath(syncPath);
        appSettings.syncOnlyOnWifi(page.querySelector('#chkWifi').checked);

        Dashboard.hideLoadingMsg();
        require(['toast'], function (toast) {
            toast(Globalize.translate('SettingsSaved'));
        });

        if (syncPath) {
            if (window.MainActivity) {
                MainActivity.authorizeStorage();
            }
        }
    }

    return function (view, params) {

        view.querySelector('form').addEventListener('submit', function (e) {

            Dashboard.showLoadingMsg();

            var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

            ApiClient.getUser(userId).then(function (user) {

                saveUser(view, user);

            });

            // Disable default form submission
            e.preventDefault();
            return false;
        });

        view.querySelector('#btnSelectSyncPath').addEventListener('click', function () {

            require(['nativedirectorychooser'], function () {
                NativeDirectoryChooser.chooseDirectory().then(function (path) {

                    if (path) {
                        view.querySelector('#txtSyncPath').value = path;
                    }
                });
            });
        });

        view.addEventListener('viewshow', function () {
            var page = this;

            Dashboard.showLoadingMsg();

            var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

            ApiClient.getUser(userId).then(function (user) {

                loadForm(page, user);
            });

            if (appHost.supports('customsyncpath')) {
                page.querySelector('.fldSyncPath').classList.remove('hide');
            } else {
                page.querySelector('.fldSyncPath').classList.add('hide');
            }
        });
    };

});