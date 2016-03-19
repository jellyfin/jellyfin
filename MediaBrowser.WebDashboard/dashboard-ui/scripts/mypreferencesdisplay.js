define(['userSettings', 'appStorage', 'jQuery'], function (userSettings, appStorage, $) {

    function loadForm(page, user) {

        page.querySelector('.chkDisplayMissingEpisodes').checked = user.Configuration.DisplayMissingEpisodes || false;
        page.querySelector('.chkDisplayUnairedEpisodes').checked = user.Configuration.DisplayUnairedEpisodes || false;
        page.querySelector('.chkGroupMoviesIntoCollections').checked = user.Configuration.GroupMoviesIntoBoxSets || false;

        $('#selectThemeSong', page).val(appStorage.getItem('enableThemeSongs-' + user.Id) || '');
        $('#selectBackdrop', page).val(appStorage.getItem('enableBackdrops-' + user.Id) || '');

        $('#selectLanguage', page).val(userSettings.language() || '');

        Dashboard.hideLoadingMsg();
    }

    function saveUser(page, user) {

        user.Configuration.DisplayMissingEpisodes = page.querySelector('.chkDisplayMissingEpisodes').checked;
        user.Configuration.DisplayUnairedEpisodes = page.querySelector('.chkDisplayUnairedEpisodes').checked;
        user.Configuration.GroupMoviesIntoBoxSets = page.querySelector('.chkGroupMoviesIntoCollections').checked;

        userSettings.language(page.querySelector('#selectLanguage').value);

        appStorage.setItem('enableThemeSongs-' + user.Id, $('#selectThemeSong', page).val());
        appStorage.setItem('enableBackdrops-' + user.Id, $('#selectBackdrop', page).val());

        return ApiClient.updateUserConfiguration(user.Id, user.Configuration);
    }

    function save(page) {

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        if (!AppInfo.enableAutoSave) {
            Dashboard.showLoadingMsg();
        }

        ApiClient.getUser(userId).then(function (user) {

            saveUser(page, user).then(function () {

                Dashboard.hideLoadingMsg();
                if (!AppInfo.enableAutoSave) {
                    require(['toast'], function (toast) {
                        toast(Globalize.translate('SettingsSaved'));
                    });
                }

            }, function () {
                Dashboard.hideLoadingMsg();
            });

        });
    }

    function onSubmit() {

        var page = $(this).parents('.page')[0];

        save(page);

        // Disable default form submission
        return false;
    }

    pageIdOn('pageinit', "displayPreferencesPage", function () {

        var page = this;

        $('.displayPreferencesForm').off('submit', onSubmit).on('submit', onSubmit);

        if (AppInfo.enableAutoSave) {
            page.querySelector('.btnSave').classList.add('hide');
        } else {
            page.querySelector('.btnSave').classList.remove('hide');
        }

    });
    pageIdOn('pageshow', "displayPreferencesPage", function () {

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
    pageIdOn('pagebeforehide', "displayPreferencesPage", function () {

        var page = this;

        if (AppInfo.enableAutoSave) {
            save(page);
        }

    });

});