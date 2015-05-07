(function ($, window, document) {

    function loadForm(page, userId, displayPreferences) {

        var externalPlayers = JSON.parse(store.getItem('externalplayers') || '[]');

        $('#selectMaxBitrate', page).val(AppSettings.maxStreamingBitrate()).selectmenu("refresh");
        $('#selectMaxChromecastBitrate', page).val(AppSettings.maxChromecastBitrate()).selectmenu("refresh");

        $('.chkExternalPlayer', page).each(function () {

            var chk = this;
            chk.checked = externalPlayers.filter(function (p) {

                return p.name == chk.getAttribute('data-name');

            }).length > 0;

        }).checkboxradio('refresh');

        $('#selectThemeSong', page).val(store.getItem('enableThemeSongs-' + userId) || '').selectmenu("refresh");
        $('#selectBackdrop', page).val(store.getItem('enableBackdrops-' + userId) || '').selectmenu("refresh");

        $('#selectHomeSection1', page).val(displayPreferences.CustomPrefs.home0 || '').selectmenu("refresh");
        $('#selectHomeSection2', page).val(displayPreferences.CustomPrefs.home1 || '').selectmenu("refresh");
        $('#selectHomeSection3', page).val(displayPreferences.CustomPrefs.home2 || '').selectmenu("refresh");
        $('#selectHomeSection4', page).val(displayPreferences.CustomPrefs.home3 || '').selectmenu("refresh");

        $('#chkEnableLibraryTileNames', page).checked(displayPreferences.CustomPrefs.enableLibraryTileNames != '0').checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    }

    function saveUser(page, userId, displayPreferences) {

        store.setItem('enableThemeSongs-' + userId, $('#selectThemeSong', page).val());
        store.setItem('enableBackdrops-' + userId, $('#selectBackdrop', page).val());

        displayPreferences.CustomPrefs.home0 = $('#selectHomeSection1', page).val();
        displayPreferences.CustomPrefs.home1 = $('#selectHomeSection2', page).val();
        displayPreferences.CustomPrefs.home2 = $('#selectHomeSection3', page).val();
        displayPreferences.CustomPrefs.home3 = $('#selectHomeSection4', page).val();
        displayPreferences.CustomPrefs.enableLibraryTileNames = $('#chkEnableLibraryTileNames', page).checked() ? '1' : '0';

        ApiClient.updateDisplayPreferences('home', displayPreferences, userId, 'webclient').done(function () {

            Dashboard.alert(Globalize.translate('SettingsSaved'));

        });
    }

    function onSubmit() {

        var page = $(this).parents('.page');

        Dashboard.showLoadingMsg();

        var externalPlayers = $('.chkExternalPlayer:checked', page).get().map(function (i) {

            return {
                name: i.getAttribute('data-name'),
                scheme: i.getAttribute('data-scheme')
            };

        });

        store.setItem('externalplayers', JSON.stringify(externalPlayers));

        AppSettings.maxStreamingBitrate($('#selectMaxBitrate', page).val());
        AppSettings.maxChromecastBitrate($('#selectMaxChromecastBitrate', page).val());

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

        $('.fldEnableBackdrops', page).show();
    });

    window.WebClientPreferencesPage = {
        onSubmit: onSubmit
    };

})(jQuery, window, document);

(function (window, store) {

    window.AppSettings = {

        maxStreamingBitrate: function (val) {

            if (val != null) {
                store.setItem('preferredVideoBitrate', val);
            }

            return parseInt(store.getItem('preferredVideoBitrate') || '') || 1500000;
        },
        maxChromecastBitrate: function (val) {

            if (val != null) {
                store.setItem('chromecastBitrate', val);
            }

            return parseInt(store.getItem('chromecastBitrate') || '') || 3000000;
        }

    };


})(window, window.store);