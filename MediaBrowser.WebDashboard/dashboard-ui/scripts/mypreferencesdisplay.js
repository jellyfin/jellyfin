(function ($, window, document) {

    function loadForm(page, user) {

        page.querySelector('.chkDisplayMissingEpisodes').checked = user.Configuration.DisplayMissingEpisodes || false;
        page.querySelector('.chkDisplayUnairedEpisodes').checked = user.Configuration.DisplayUnairedEpisodes || false;
        page.querySelector('.chkGroupMoviesIntoCollections').checked = user.Configuration.GroupMoviesIntoBoxSets || false;

        $('#selectThemeSong', page).val(appStorage.getItem('enableThemeSongs-' + user.Id) || '');
        $('#selectBackdrop', page).val(appStorage.getItem('enableBackdrops-' + user.Id) || '');

        $('#selectLanguage', page).val(AppSettings.displayLanguage());

        Dashboard.hideLoadingMsg();
    }

    function saveUser(page, user) {

        user.Configuration.DisplayMissingEpisodes = page.querySelector('.chkDisplayMissingEpisodes').checked;
        user.Configuration.DisplayUnairedEpisodes = page.querySelector('.chkDisplayUnairedEpisodes').checked;
        user.Configuration.GroupMoviesIntoBoxSets = page.querySelector('.chkGroupMoviesIntoCollections').checked;

        AppSettings.displayLanguage(page.querySelector('#selectLanguage').value);

        appStorage.setItem('enableThemeSongs-' + user.Id, $('#selectThemeSong', page).val());
        appStorage.setItem('enableBackdrops-' + user.Id, $('#selectBackdrop', page).val());

        ApiClient.updateUserConfiguration(user.Id, user.Configuration).then(function () {
            Dashboard.alert(Globalize.translate('SettingsSaved'));

            loadForm(page, user);
        });
    }

    function onSubmit() {

        var page = $(this).parents('.page')[0];

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getUser(userId).then(function (user) {

            saveUser(page, user);

        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinit', "#displayPreferencesPage", function () {

        var page = this;

        $('.displayPreferencesForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#displayPreferencesPage", function () {

        var page = this;

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getUser(userId).then(function (user) {

            loadForm(page, user);

            if (user.Policy.EnableUserPreferenceAccess) {
                $('.requiresUserPreferences', page).show();
            } else {
                $('.requiresUserPreferences', page).hide();
            }
        });

        $('.fldEnableBackdrops', page).show();

        if (AppInfo.supportsUserDisplayLanguageSetting) {
            $('.languageSection', page).show();
        } else {
            $('.languageSection', page).hide();
        }
    });

})(jQuery, window, document);