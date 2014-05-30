(function ($, window, document) {

    function loadForm(page, userId, displayPreferences) {

        $('#selectThemeSong', page).val(LocalSettings.val('enableThemeSongs', userId) || '').selectmenu("refresh");
        $('#selectBackdrop', page).val(LocalSettings.val('enableBackdrops', userId) || '').selectmenu("refresh");

        $('#selectHomeSection1', page).val(displayPreferences.CustomPrefs.home0 || '').selectmenu("refresh");
        $('#selectHomeSection2', page).val(displayPreferences.CustomPrefs.home1 || '').selectmenu("refresh");
        $('#selectHomeSection3', page).val(displayPreferences.CustomPrefs.home2 || '').selectmenu("refresh");

        Dashboard.hideLoadingMsg();
    }

    function saveUser(page, userId, displayPreferences) {

        LocalSettings.val('enableThemeSongs', userId, $('#selectThemeSong', page).val());
        LocalSettings.val('enableBackdrops', userId, $('#selectBackdrop', page).val());

        displayPreferences.CustomPrefs.home0 = $('#selectHomeSection1', page).val();
        displayPreferences.CustomPrefs.home1 = $('#selectHomeSection2', page).val();
        displayPreferences.CustomPrefs.home2 = $('#selectHomeSection3', page).val();

        ApiClient.updateDisplayPreferences('home', displayPreferences, userId, 'webclient').done(function () {

            Dashboard.alert(Globalize.translate('SettingsSaved'));
            
        });
    }

    function onSubmit() {

        var page = $(this).parents('.page');

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getDisplayPreferences('home', userId, 'webclient').done(function (result) {

            saveUser(page, userId, result);

        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinit', "#webClientPreferencesPage", function () {

        var page = this;

    }).on('pagebeforeshow', "#webClientPreferencesPage", function () {

        var page = this;

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getDisplayPreferences('home', userId, 'webclient').done(function (result) {

            loadForm(page, userId, result);

        });
    });

    window.WebClientPreferencesPage = {
        onSubmit: onSubmit
    };

})(jQuery, window, document);