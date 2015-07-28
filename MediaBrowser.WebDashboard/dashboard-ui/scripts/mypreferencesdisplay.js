(function ($, window, document) {

    function loadForm(page, user) {

        page.querySelector('.chkDisplayMissingEpisodes').checked = user.Configuration.DisplayMissingEpisodes || false;
        page.querySelector('.chkDisplayUnairedEpisodes').checked = user.Configuration.DisplayUnairedEpisodes || false;
        page.querySelector('.chkDisplayTrailersWithinMovieSuggestions').checked = user.Configuration.IncludeTrailersInSuggestions || false;
        page.querySelector('.chkGroupMoviesIntoCollections').checked = user.Configuration.GroupMoviesIntoBoxSets || false;

        $('#selectThemeSong', page).val(appStorage.getItem('enableThemeSongs-' + user.Id) || '').selectmenu("refresh");
        $('#selectBackdrop', page).val(appStorage.getItem('enableBackdrops-' + user.Id) || '').selectmenu("refresh");

        page.querySelector('.chkEnableFullScreen').checked = AppSettings.enableFullScreen();
        page.querySelector('.chkSyncToExternalCard').checked = AppSettings.enableSyncToExternalStorage();

        Dashboard.hideLoadingMsg();
    }

    function saveUser(page, user) {

        user.Configuration.DisplayMissingEpisodes = page.querySelector('.chkDisplayMissingEpisodes').checked;
        user.Configuration.DisplayUnairedEpisodes = page.querySelector('.chkDisplayUnairedEpisodes').checked;
        user.Configuration.IncludeTrailersInSuggestions = page.querySelector('.chkDisplayTrailersWithinMovieSuggestions').checked;
        user.Configuration.GroupMoviesIntoBoxSets = page.querySelector('.chkGroupMoviesIntoCollections').checked;

        AppSettings.enableFullScreen(page.querySelector('.chkEnableFullScreen').checked);

        appStorage.setItem('enableThemeSongs-' + user.Id, $('#selectThemeSong', page).val());
        appStorage.setItem('enableBackdrops-' + user.Id, $('#selectBackdrop', page).val());

        AppSettings.enableSyncToExternalStorage(page.querySelector('.chkSyncToExternalCard').checked);

        ApiClient.updateUserConfiguration(user.Id, user.Configuration).done(function () {
            Dashboard.alert(Globalize.translate('SettingsSaved'));

            loadForm(page, user);
        });
    }

    function onSubmit() {

        var page = $(this).parents('.page');

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getUser(userId).done(function (user) {

            saveUser(page, user);

        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinitdepends', "#displayPreferencesPage", function () {

        var page = this;

        $('.displayPreferencesForm').off('submit', onSubmit).on('submit', onSubmit);

        $('.btnSelectSyncPath', page).on('click', function () {

            require(['nativedirectorychooser'], function () {
                NativeDirectoryChooser.chooseDirectory().done(function (path) {
                    $('#txtSyncPath', page).val(path);
                });
            });
        });

    }).on('pageshowready', "#displayPreferencesPage", function () {

        var page = this;

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getUser(userId).done(function (user) {

            loadForm(page, user);

            if (user.Policy.EnableUserPreferenceAccess) {
                $('.requiresUserPreferences', page).show();
            } else {
                $('.requiresUserPreferences', page).hide();
            }
        });

        $('.fldEnableBackdrops', page).show();

        if (AppInfo.supportsFullScreen) {
            $('.fldFullscreen', page).show();
        } else {
            $('.fldFullscreen', page).hide();
        }

        if (AppInfo.supportsSyncPathSetting) {
            $('.syncSettingsSection', page).show();
        } else {
            $('.syncSettingsSection', page).hide();
        }
    });

})(jQuery, window, document);