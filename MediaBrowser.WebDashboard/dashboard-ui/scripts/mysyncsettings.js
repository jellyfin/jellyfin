define(['appSettings'], function (appSettings) {

    function loadForm(page, user) {

        page.querySelector('#txtSyncPath').value = appSettings.syncPath() || '';
        page.querySelector('#chkWifi').checked = appSettings.syncOnlyOnWifi();

        var uploadServers = appSettings.cameraUploadServers();

        page.querySelector('.uploadServerList').innerHTML = ConnectionManager.getSavedServers().map(function (s) {

            var checkedHtml = uploadServers.indexOf(s.Id) == -1 ? '' : ' checked';
            var html = '<label><input type="checkbox" is="emby-checkbox"' + checkedHtml + ' class="chkUploadServer" data-id="' + s.Id + '"/><span>' + s.Name + '</span></label>';

            return html;

        }).join('');

        Dashboard.hideLoadingMsg();
    }

    function saveUser(page, user) {

        var syncPath = page.querySelector('#txtSyncPath').value;

        appSettings.syncPath(syncPath);
        appSettings.syncOnlyOnWifi(page.querySelector('#chkWifi').checked);

        var chkUploadServer = page.querySelectorAll('.chkUploadServer');
        var cameraUploadServers = [];

        for (var i = 0, length = chkUploadServer.length; i < length; i++) {
            if (chkUploadServer[i].checked) {
                cameraUploadServers.push(chkUploadServer[i].getAttribute('data-id'));
            }
        }

        appSettings.cameraUploadServers(cameraUploadServers);

        Dashboard.hideLoadingMsg();
        require(['toast'], function (toast) {
            toast(Globalize.translate('SettingsSaved'));
        });

        if (cameraUploadServers.length || syncPath) {
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

            if (AppInfo.supportsSyncPathSetting) {
                page.querySelector('.fldSyncPath').classList.remove('hide');
            } else {
                page.querySelector('.fldSyncPath').classList.add('hide');
            }
        });
    };

});