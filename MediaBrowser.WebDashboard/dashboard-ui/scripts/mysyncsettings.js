(function ($, window, document) {

    function loadForm(page, user) {

        page.querySelector('#txtSyncPath').value = AppSettings.syncPath();
        page.querySelector('#chkWifi').checked = AppSettings.syncOnlyOnWifi();
        page.querySelector('#chkSyncLosslessAudio').checked = AppSettings.syncLosslessAudio();

        Dashboard.hideLoadingMsg();
    }

    function saveUser(page, user) {

        AppSettings.syncPath(page.querySelector('#txtSyncPath').value);
        AppSettings.syncOnlyOnWifi(page.querySelector('#chkWifi').checked);
        AppSettings.syncLosslessAudio(page.querySelector('#chkSyncLosslessAudio').checked);

        Dashboard.hideLoadingMsg();
        Dashboard.alert(Globalize.translate('SettingsSaved'));
    }

    function onSubmit() {

        var page = $(this).parents('.page')[0];

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getUser(userId).done(function (user) {

            saveUser(page, user);

        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinitdepends', "#syncPreferencesPage", function () {

        var page = this;

        $('form', page).off('submit', onSubmit).on('submit', onSubmit);

        $('.btnSelectSyncPath', page).on('click', function () {

            require(['nativedirectorychooser'], function () {
                NativeDirectoryChooser.chooseDirectory().done(function (path) {
                    $('#txtSyncPath', page).val(path);
                });
            });
        });

    }).on('pageshowready', "#syncPreferencesPage", function () {

        var page = this;

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getUser(userId).done(function (user) {

            loadForm(page, user);
        });
    });

})(jQuery, window, document);