define(['appSettings', 'apphost', 'emby-checkbox', 'emby-select', 'emby-input'], function (appSettings, appHost) {

    function loadForm(page, user) {

        page.querySelector('#txtSyncPath').value = appSettings.syncPath() || '';
        page.querySelector('#chkWifi').checked = appSettings.syncOnlyOnWifi();
        page.querySelector('.selectAudioBitrate').value = appSettings.maxStaticMusicBitrate() || '';
    }

    function saveUser(page) {

        var syncPath = page.querySelector('#txtSyncPath').value;

        appSettings.syncPath(syncPath);
        appSettings.syncOnlyOnWifi(page.querySelector('#chkWifi').checked);
        appSettings.maxStaticMusicBitrate(page.querySelector('.selectAudioBitrate').value || null);
    }

    return function (view, params) {

        view.querySelector('form').addEventListener('submit', function (e) {

            saveUser(view);

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

        view.addEventListener('viewbeforehide', function () {

            saveUser(this);
        });
    };

});