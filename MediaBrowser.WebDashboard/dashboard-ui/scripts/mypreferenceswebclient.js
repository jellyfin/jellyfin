(function ($, window, document) {

    function loadForm(page, userId, displayPreferences) {

        $('#selectThemeSong', page).val(store.getItem('enableThemeSongs-' + userId) || '').selectmenu("refresh");
        $('#selectBackdrop', page).val(store.getItem('enableBackdrops-' + userId) || '').selectmenu("refresh");

        $('#selectHomeSection1', page).val(displayPreferences.CustomPrefs.home0 || '').selectmenu("refresh");
        $('#selectHomeSection2', page).val(displayPreferences.CustomPrefs.home1 || '').selectmenu("refresh");
        $('#selectHomeSection3', page).val(displayPreferences.CustomPrefs.home2 || '').selectmenu("refresh");
        $('#selectHomeSection4', page).val(displayPreferences.CustomPrefs.home3 || '').selectmenu("refresh");

        Dashboard.hideLoadingMsg();
    }

    function saveUser(page, userId, displayPreferences) {

        store.setItem('enableThemeSongs-' + userId, $('#selectThemeSong', page).val());
        store.setItem('enableBackdrops-' + userId, $('#selectBackdrop', page).val());

        displayPreferences.CustomPrefs.home0 = $('#selectHomeSection1', page).val();
        displayPreferences.CustomPrefs.home1 = $('#selectHomeSection2', page).val();
        displayPreferences.CustomPrefs.home2 = $('#selectHomeSection3', page).val();
        displayPreferences.CustomPrefs.home3 = $('#selectHomeSection4', page).val();

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

        // See backrops.js for comments on this
        if ($.browser.msie) {
            $('.fldEnableBackdrops', page).hide();
        } else {
            $('.fldEnableBackdrops', page).show();
        }
    });

    window.WebClientPreferencesPage = {
        onSubmit: onSubmit
    };

})(jQuery, window, document);