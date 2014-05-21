(function ($, window, document) {

    function loadForm(page, user) {

        $('#chkDisplayMissingEpisodes', page).checked(user.Configuration.DisplayMissingEpisodes || false).checkboxradio("refresh");
        $('#chkDisplayUnairedEpisodes', page).checked(user.Configuration.DisplayUnairedEpisodes || false).checkboxradio("refresh");

        $('#chkGroupMoviesIntoCollections', page).checked(user.Configuration.GroupMoviesIntoBoxSets || false).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    }

    function saveUser(page, user) {

        user.Configuration.DisplayMissingEpisodes = $('#chkDisplayMissingEpisodes', page).checked();
        user.Configuration.DisplayUnairedEpisodes = $('#chkDisplayUnairedEpisodes', page).checked();
        user.Configuration.GroupMoviesIntoBoxSets = $('#chkGroupMoviesIntoCollections', page).checked();

        ApiClient.updateUser(user).done(function () {
            Dashboard.alert(Globalize.translate("SettingsSaved"));
        });
    }

    function onSubmit() {

        var page = $(this).parents('.page');

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getUser(userId).done(function (result) {
            
            saveUser(page, result);
            
        });

        // Disable default form submission
        return false;

    }

    $(document).on('pageinit', "#displayPreferencesPage", function () {

        var page = this;

    }).on('pageshow', "#displayPreferencesPage", function () {

        var page = this;

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getUser(userId).done(function (user) {

            loadForm(page, user);

        });
        
    }).on('pageshow', ".userPreferencesPage", function () {

        var page = this;

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        $('.lnkDisplayPreferences', page).attr('href', 'mypreferencesdisplay.html?userId=' + userId);
        $('.lnkLanguagePreferences', page).attr('href', 'mypreferenceslanguages.html?userId=' + userId);
        $('.lnkWebClientPreferences', page).attr('href', 'mypreferenceswebclient.html?userId=' + userId);
    });

    window.DisplayPreferencesPage = {
        onSubmit: onSubmit
    };

})(jQuery, window, document);