(function ($, window, document) {

    function loadForm(page, user) {

        $('#chkDisplayMissingEpisodes', page).checked(user.Configuration.DisplayMissingEpisodes || false).checkboxradio("refresh");
        $('#chkDisplayUnairedEpisodes', page).checked(user.Configuration.DisplayUnairedEpisodes || false).checkboxradio("refresh");

        $('#chkDisplayTrailersWithinMovieSuggestions', page).checked(user.Configuration.IncludeTrailersInSuggestions || false).checkboxradio("refresh");

        $('#chkGroupMoviesIntoCollections', page).checked(user.Configuration.GroupMoviesIntoBoxSets || false).checkboxradio("refresh");

        $('#selectThemeSong', page).val(appStorage.getItem('enableThemeSongs-' + user.Id) || '').selectmenu("refresh");
        $('#selectBackdrop', page).val(appStorage.getItem('enableBackdrops-' + user.Id) || '').selectmenu("refresh");

        $('#selectEnableItemPreviews', page).val(AppSettings.enableItemPreviews().toString().toLowerCase()).selectmenu("refresh");

        $('#chkEnableFullScreen', page).checked(AppSettings.enableFullScreen()).checkboxradio("refresh");

        $('#txtSyncPath', page).val(AppSettings.syncPath());

        Dashboard.hideLoadingMsg();
    }

    function saveUser(page, user) {

        user.Configuration.DisplayMissingEpisodes = $('#chkDisplayMissingEpisodes', page).checked();
        user.Configuration.DisplayUnairedEpisodes = $('#chkDisplayUnairedEpisodes', page).checked();
        user.Configuration.GroupMoviesIntoBoxSets = $('#chkGroupMoviesIntoCollections', page).checked();
        user.Configuration.IncludeTrailersInSuggestions = $('#chkDisplayTrailersWithinMovieSuggestions', page).checked();

        AppSettings.enableItemPreviews($('#selectEnableItemPreviews', page).val() == 'true');
        AppSettings.enableFullScreen($('#chkEnableFullScreen', page).checked());

        appStorage.setItem('enableThemeSongs-' + user.Id, $('#selectThemeSong', page).val());
        appStorage.setItem('enableBackdrops-' + user.Id, $('#selectBackdrop', page).val());

        AppSettings.syncPath($('#txtSyncPath', page).val());

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